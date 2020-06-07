using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ZXing;

namespace NosAyudamos
{
    class QRCode : IQRCode
    {
        readonly HttpClient http;
        readonly Lazy<BarcodeReader> reader;

        public QRCode(HttpClient http)
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
                        PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
                    },
                });
        }

        public async Task<string?> ReadAsync(Uri imageUri)
        {
            var bytes = imageUri.Scheme == "file" ?
                await File.ReadAllBytesAsync(imageUri.AbsolutePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)) :
                await http.GetByteArrayAsync(imageUri);

            return Decode(bytes);
        }

        internal string? Decode(byte[] bytes)
        {
            using var mem = new MemoryStream(bytes);
            using var image = (Bitmap)Image.FromStream(mem);

            var result = reader.Value.Decode(image);
            var text = result?.Text;

            return string.IsNullOrEmpty(text) ? null : text;
        }
    }

    interface IQRCode
    {
        Task<string?> ReadAsync(Uri imageUri);
    }
}
