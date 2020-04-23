using System;
using System.Collections.Generic;
using System.Composition;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ZXing;

namespace NosAyudamos
{
    class QRCode : IQRCode
    {
        readonly HttpClient httpClient;
        readonly Lazy<BarcodeReader> reader;

        public QRCode(HttpClient httpClient)
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
                        PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
                    },
                });
        }

        public async Task<string?> ReadAsync(Uri imageUri)
        {
            var bytes = await httpClient.GetByteArrayAsync(imageUri);

            using var mem = new MemoryStream(bytes);
            using var image = (Bitmap)Image.FromStream(mem);

            var result = reader.Value.Decode(image);

            return result?.Text;
        }
    }

    interface IQRCode
    {
        Task<string?> ReadAsync(Uri imageUri);
    }
}
