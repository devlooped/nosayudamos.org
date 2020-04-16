using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using ZXing;
using System.IO;
using System;
using System.Drawing;

namespace NosAyudamos
{
    public interface IPersonRecognizer
    {
        Task<Person?> Recognize(string imageUrl);
    }

    public class PersonRecognizer : IPersonRecognizer
    {
        private readonly Lazy<BarcodeReader> reader;

        public PersonRecognizer()
        {
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

        public async Task<Person?> Recognize(string imageUrl)
        {
            using var httpClient = new HttpClient();

            var bytes = await httpClient.GetByteArrayAsync(imageUrl);

            using var image = (System.Drawing.Bitmap)Bitmap.FromStream(
                new MemoryStream(bytes));

            var result = await Task.Run(() => reader.Value.Decode(image));

            if (result != null)
            {
                //00501862505@ANDERSON@JAMIE FALKLAND@M@19055847@A@13/10/1974@03/07/2017
                var elements = result.Text.Split("@");

                if (elements.Length > 0)
                {
                    return new Person { LastName = elements[1], FirstName = elements[2], NationalId = elements[4] };
                }
            }

            return null;
        }

        private async static Task<byte[]> DownloadImage(string imageUrl)
        {
            using var client = new HttpClient();
            return await client.GetByteArrayAsync(imageUrl);
        }
    }
}