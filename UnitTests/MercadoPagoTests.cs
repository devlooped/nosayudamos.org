using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using ZXing;

namespace NosAyudamos
{
    public class MercadoPagoTests
    {
        readonly ITestOutputHelper output;

        public MercadoPagoTests(ITestOutputHelper output) => this.output = output;

        //[Fact]
        public async Task Pay()
        {
            var reader = new BarcodeReader
            {
                AutoRotate = true,
                TryInverted = true,
                Options = new ZXing.Common.DecodingOptions()
                {
                    TryHarder = true,
                    PossibleFormats = new List<BarcodeFormat>
                    {
                        BarcodeFormat.QR_CODE
                    }
                },
            };

            var result = reader.Decode((Bitmap)Image.FromFile("Content\\KzuQR.png"));
            var env = new Environment();
            var headers = env.GetVariable("MercadoPagoHeaders");

            // Static QR flow

            var sessionId = Guid.NewGuid().ToString();

            using var qrReq = new HttpRequestMessage(HttpMethod.Get, env.GetVariable("MercadoPagoUrlQR") + result.Text).AddHeaders(headers, sessionId);

            using var http = new HttpClient();
            using var qrResp = await http.SendAsync(qrReq);

            qrResp.EnsureSuccessStatusCode();

            var json = await qrResp.Content.ReadAsStringAsync();
            dynamic data = JObject.Parse(json);

            output.WriteLine(qrReq.RequestUri.ToString());
            output.WriteLine(data.ToString());

            Assert.True((object)data.checkout_data != null, "QR has not been set up for a specific payment yet.");

            var orderId = (string)data.checkout_data.merchant_order.id;
            var items = ((JToken)data.checkout_data.checkout_preference.items).ToString();
            // Use first item's title as the overall transaction title
            var title = (string)data.checkout_data.checkout_preference.items[0].title;
            var amount = (double)data.checkout_data.checkout_preference.amount;
            var productId = (string)data.checkout_data.product_id;
            var posId = (string)data.checkout_data.internal_metadata.pos_id;
            var collectorId = (string)data.checkout_data.checkout_preference.collector_id;

            // This is not really necessary, it's for the case of a QR where you enter a manual amount.
            // TODO: see if we encounter this scenario at all, but I doubt it.

            //var checkout = env.GetVariable("MercadoPagoBodyCheckoutData");
            //checkout = checkout
            //    .Replace("$collector_id$", collectorId)
            //    .Replace("$pos_id$", posId)
            //    .Replace("$product_id$", productId)
            //    .Replace("$items$", items);

            //using var checkoutDataReq = new HttpRequestMessage(HttpMethod.Post, env.GetVariable("MercadoPagoUrlCheckoutData")).AddHeaders(headers, sessionId);
            //checkoutDataReq.Content = new StringContent(checkout, Encoding.UTF8, "application/json");
            //using var checkoutDataResp = await http.SendAsync(checkoutDataReq);

            //json = await checkoutDataResp.Content.ReadAsStringAsync();

            //checkoutDataResp.EnsureSuccessStatusCode();

            //data = JObject.Parse(json);

            //output.WriteLine(checkoutDataReq.RequestUri.ToString());
            //output.WriteLine(data.ToString());

            var checkout = env.GetVariable("MercadoPagoBodyCheckout");
            checkout = checkout
                .Replace("$collector_id$", collectorId)
                .Replace("$pos_id$", posId)
                .Replace("$product_id$", productId)
                .Replace("$items$", items);

            using var checkoutReq = new HttpRequestMessage(HttpMethod.Post, env.GetVariable("MercadoPagoUrlCheckout")).AddHeaders(headers, sessionId);
            checkoutReq.Content = new StringContent(checkout, Encoding.UTF8, "application/json");

            checkoutReq.Headers.Remove("x-platform");
            checkoutReq.Headers.TryAddWithoutValidation("x-platform", "MP");
            checkoutReq.Headers.TryAddWithoutValidation("x-flow-id", "/instore");
            checkoutReq.Headers.TryAddWithoutValidation("x-product-id", productId);

            using var checkoutResp = await http.SendAsync(checkoutReq);

            json = await checkoutResp.Content.ReadAsStringAsync();

            checkoutResp.EnsureSuccessStatusCode();

            data = JObject.Parse(json);

            output.WriteLine(checkoutReq.RequestUri.ToString());
            output.WriteLine(data.ToString());

            var balance = (double)data.one_tap[0].account_money.available_balance;
            if (amount > balance)
            {
                // Insufficient funds, should never happen, the person is at the store!!!
            }

            var payment = env.GetVariable("MercadoPagoBodyPayment");
            payment = payment
                .Replace("$collector_id$", collectorId)
                .Replace("$pos_id$", posId)
                .Replace("$order_id$", orderId)
                .Replace("$title$", title)
                .Replace("$amount$", amount.ToString(CultureInfo.CurrentCulture));

            using var paymentReq = new HttpRequestMessage(HttpMethod.Post, env.GetVariable("MercadoPagoUrlPayment")).AddHeaders(headers, sessionId);
            paymentReq.Content = new StringContent(payment, Encoding.UTF8, "application/json");

            paymentReq.Headers.TryAddWithoutValidation("x-product-id", productId);
            paymentReq.Headers.TryAddWithoutValidation("x-tracking-id", "scan_qr");

            var idempotency = $"-{DateTime.Now.Ticks}--159125{new Random().Next(2500000, 9999999)}";
            paymentReq.Headers.TryAddWithoutValidation("x-idempotency-key", idempotency);

            using var paymentResp = await http.SendAsync(paymentReq);

            json = await paymentResp.Content.ReadAsStringAsync();

            paymentResp.EnsureSuccessStatusCode();

            data = JObject.Parse(json);

            output.WriteLine(paymentReq.RequestUri.ToString());
            output.WriteLine(data.ToString());

        }
    }

    public static class MercadoPago
    {
        public static HttpRequestMessage AddHeaders(this HttpRequestMessage request, string headers, string sessionId)
        {
            var values = headers
                .Replace("$session-id$", sessionId)
                .Replace("$request-id$", Guid.NewGuid().ToString());

            foreach (var header in values.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = header.Split('|', StringSplitOptions.RemoveEmptyEntries);
                if (kv.Length == 2)
                    request.Headers.TryAddWithoutValidation(kv[0], kv[1]);
            }

            return request;
        }
    }
}
