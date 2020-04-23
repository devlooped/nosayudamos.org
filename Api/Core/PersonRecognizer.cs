using System.Collections.Generic;
using System.Threading.Tasks;
using ZXing;
using System.IO;
using System;
using System.Drawing;
using System.Globalization;
using System.Composition;
using System.Net.Http;

namespace NosAyudamos
{
    static class PersonRecognizerExtensions
    {
        public static Task<Person?> RecognizeAsync(this IPersonRecognizer recognizer, string? imageUrl)
        {
            if (imageUrl == null ||
                !Uri.TryCreate(imageUrl, UriKind.Absolute, out var imageUri) ||
                imageUri == null)
            {
                return Task.FromResult<Person?>(default);
            }

            return (recognizer ?? throw new ArgumentNullException(nameof(recognizer))).RecognizeAsync(imageUri);
        }
    }

    class PersonRecognizer : IPersonRecognizer
    {
        readonly Lazy<BarcodeReader> reader;
        readonly HttpClient httpClient;

        public PersonRecognizer(HttpClient httpClient)
        {
            this.httpClient = httpClient;

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

        public async Task<Person?> RecognizeAsync(Uri imageUri)
        {
            var bytes = await httpClient.GetByteArrayAsync(imageUri);

            using var mem = new MemoryStream(bytes);
            using var image = (Bitmap)Image.FromStream(mem);

            var result = reader.Value.Decode(image);
            if (result != null)
            {
                //00501862505@ANDERSON@JAMIE FALKLAND@M@19055847@A@13/10/1974@03/07/2017
                var elements = result.Text.Split("@");

                if (elements.Length > 0)
                {
                    return new Person(
                        CultureInfo.CurrentCulture.TextInfo.ToTitleCase(elements[2].ToLower(CultureInfo.CurrentCulture)),
                        CultureInfo.CurrentCulture.TextInfo.ToTitleCase(elements[1].ToLower(CultureInfo.CurrentCulture)),
                        elements[4],
                        elements[6],
                        elements[3]);
                }
            }

            return null;
        }
    }

    interface IPersonRecognizer
    {
        Task<Person?> RecognizeAsync(Uri imageUri);
    }
}
