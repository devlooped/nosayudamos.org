using System;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ZXing;
using ZXing.PDF417;

namespace NosAyudamos.Slack
{
    class RegisterDoneeProcessor : ISlackPayloadProcessor
    {
        readonly IEnvironment env;
        readonly IEventStreamAsync events;
        readonly IBlobStorage storage;
        readonly IEntityRepository<PhoneSystem> phoneDir;

        public RegisterDoneeProcessor(
            IEnvironment env, IEventStreamAsync events,
            IBlobStorage storage, IEntityRepository<PhoneSystem> phoneDir)
            => (this.env, this.events, this.storage, this.phoneDir)
            = (env, events, storage, phoneDir);

        public bool AppliesTo(JObject payload) =>
            (string?)payload["type"] == "view_submission";

        public async Task ProcessAsync(JObject payload)
        {
            var sender = payload.GetSender();
            if (sender == null)
                return;

            var map = await phoneDir.GetAsync(sender);
            if (map == null)
                return;

            var lastName = payload.SelectString("$.view.state.values.lastName.lastName.value")!;
            var firstName = payload.SelectString("$.view.state.values.firstName.firstName.value")!;
            var nationalId = payload.SelectString("$.view.state.values.nationalId.nationalId.value")!;
            var sex = payload.SelectString("$.view.state.values.sex.sex.selected_option.value")!;
            var bdate = DateTime.Parse(
                payload.SelectString("$.view.state.values.dateOfBirth.dateOfBirth.selected_date")!,
                CultureInfo.CurrentCulture);

            //00000000000@LASTNAME@FIRSTNAME@SEX@ID@A@DATEOFBIRTH@DATEOFISSUE
            var data = $"00000000000@{lastName.ToUpperInvariant()}@{firstName.ToUpperInvariant()}@{sex}@{nationalId}@A@{bdate:dd/MM/yyyy}@{DateTime.Now:dd/MM/yyyy}";
            var writer = new BarcodeWriterGeneric
            {
                Format = BarcodeFormat.PDF_417,
                Options = new PDF417EncodingOptions
                {
                    Height = 60,
                    Width = 240,
                    Margin = 10
                }
            };

            using var bitmap = writer.WriteAsBitmap(data);
            using var mem = new MemoryStream();
            bitmap.Save(mem, ImageFormat.Png);
            mem.Position = 0;

            var uri = await storage.UploadAsync(mem.ToArray(),
                env.GetVariable("AttachmentsContainerName"), $"cel_{sender}.png")
                .ConfigureAwait(false);

            // Before pushing the message, remove the pause on the user, so regular processing can happen 
            // as if the image came from them.
            if (map.AutomationPaused == true)
            {
                map.AutomationPaused = false;
                await phoneDir.PutAsync(map);
            }

            await events.PushAsync(new MessageReceived(sender, map.SystemNumber, uri.OriginalString));
        }
    }
}
