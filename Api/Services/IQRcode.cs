using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using ZXing;

namespace NosAyudamos
{
    interface IQRCode
    {
        Task<string?> ReadAsync(Uri imageUri);
    }

    class QRCode : IQRCode
    {
        private readonly Lazy<BarcodeReader> reader;

        public QRCode()
        {
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
            var bytes = await Utility.DownloadBlobAsync(imageUri);

            using var mem = new MemoryStream(bytes);
            using var image = (Bitmap)Image.FromStream(mem);

            var result = reader.Value.Decode(image);

            return result?.Text;
        }
    }
}
