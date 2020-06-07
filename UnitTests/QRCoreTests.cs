using System;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using Xunit;
using ZXing;

namespace NosAyudamos
{
    public class QRCoreTests
    {
        [Fact]
        public void CanReadImageWithQR()
        {
            var writer = new BarcodeWriterGeneric
            {
                Format = BarcodeFormat.QR_CODE,
            };

            var data = Guid.NewGuid().ToString();

            using var bitmap = writer.WriteAsBitmap(data);
            using var mem = new MemoryStream();
            bitmap.Save(mem, ImageFormat.Png);

            using var http = new HttpClient();
            var qr = new QRCode(http);

            var result = qr.Decode(mem.ToArray());

            Assert.NotNull(result);
            Assert.Equal(data, result);
        }

        [Fact]
        public void NonQRImageReturnsNullString()
        {
            using var http = new HttpClient();
            var qr = new QRCode(http);

            var result = qr.Decode(File.ReadAllBytes("Content\\logo.png"));

            Assert.Null(result);
        }
    }
}
