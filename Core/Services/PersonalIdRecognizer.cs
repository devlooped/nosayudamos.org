using System.Collections.Generic;
using System.Threading.Tasks;
using ZXing;
using System.IO;
using System;
using System.Drawing;
using System.Globalization;
using System.Net.Http;

namespace NosAyudamos
{
    class PersonalId
    {
        public PersonalId(
            string firstName,
            string lastName,
            string nationalId,
            DateTime dateOfBirth,
            Sex sex)
            => (FirstName, LastName, NationalId, DateOfBirth, Sex)
            = (firstName, lastName, nationalId, dateOfBirth, sex);

        public string FirstName { get; }
        public string LastName { get; }
        public string NationalId { get; }
        public DateTime DateOfBirth { get; }
        public Sex Sex { get; }
    }

    class PersonalIdRecognizer : IPersonalIdRecognizer
    {
        readonly Lazy<BarcodeReader> reader;
        readonly HttpClient http;

        public PersonalIdRecognizer(HttpClient http)
        {
            this.http = http;

            reader = new Lazy<BarcodeReader>(
                () => new BarcodeReader()
                {
                    AutoRotate = true,
                    TryInverted = true,
                    Options = new ZXing.Common.DecodingOptions()
                    {
                        TryHarder = true,
                        PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.PDF_417 }
                    },
                });
        }

        public async Task<PersonalId?> RecognizeAsync(Uri imageUri)
        {
            var image = imageUri.Scheme == "file" ?
                await File.ReadAllBytesAsync(imageUri.AbsolutePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)) :
                await http.GetByteArrayAsync(imageUri);

            return await RecognizeAsync(image);
        }

        public Task<PersonalId?> RecognizeAsync(byte[] image)
        {
            using var mem = new MemoryStream(image);
            using var bitmap = (Bitmap)Image.FromStream(mem);

            var result = reader.Value.Decode(bitmap);
            if (result != null)
            {
                //00501862505@ANDERSON@JAMIE FALKLAND@M@19055847@A@13/10/1974@03/07/2017
                var elements = result.Text.Split("@");
                if (elements.Length > 0)
                {
                    return Task.FromResult<PersonalId?>(new PersonalId(
                        CultureInfo.CurrentCulture.TextInfo.ToTitleCase(elements[2].ToLower(CultureInfo.CurrentCulture)),
                        CultureInfo.CurrentCulture.TextInfo.ToTitleCase(elements[1].ToLower(CultureInfo.CurrentCulture)),
                        elements[4],
                        DateTime.ParseExact(elements[6], "dd/MM/yyyy", CultureInfo.CurrentCulture),
                        elements[3] == "M" ? Sex.Male : Sex.Female));
                }
            }
            // TODO: add fallback via Computer Vision API + TaxIdRecognizer lookup to increase confidence and recognition results?

            return Task.FromResult<PersonalId?>(default);
        }
    }

    interface IPersonalIdRecognizer
    {
        Task<PersonalId?> RecognizeAsync(byte[] image);
        Task<PersonalId?> RecognizeAsync(Uri imageUri);
    }
}
