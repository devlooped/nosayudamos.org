using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NosAyudamos
{
    class PaymentEventsHandler :
        IEventHandler<PaymentCodeReceived>,
        IEventHandler<PaymentRequested>,
        IEventHandler<PaymentApproved>
    {
        readonly IEnvironment env;
        readonly IQRCode qr;
        readonly IEventStreamAsync events;
        readonly IPersonRepository peopleRepo;
        readonly HttpClient http;

        public PaymentEventsHandler(IEnvironment env, IQRCode qr, IEventStreamAsync events, IPersonRepository peopleRepo, HttpClient http)
            => (this.env, this.qr, this.events, this.peopleRepo, this.http)
            = (env, qr, events, peopleRepo, http);

        public async Task HandleAsync(PaymentCodeReceived e)
        {
            var decoded = await qr.ReadAsync(e.ImageUri);
            // NOTE: this shouldn't happen
            if (decoded == null)
                return;

            var headers = env.GetVariable("MercadoPagoHeaders");
            var sessionId = Guid.NewGuid().ToString();

            dynamic data = await DecodeDataAsync(decoded, headers, sessionId);

            // If the QR isn't a payment one, the show hasn't still set it up 
            // for a specific payment.
            if (data.checkout_data == null)
                throw new HttpStatusException(HttpStatusCode.PaymentRequired);

            // Use first item's title as the overall transaction title
            var merchant =
                (string?)data.checkout_data?.merchant_order?.collector?.company?.soft_descriptor ??
                (string)data.checkout_data.merchant_order.collector.id;

            var amount = (double)data.checkout_data.checkout_preference.amount;
            // Use first item's title as the overall transaction title
            var description = (string)data.checkout_data.checkout_preference.items[0].title;

            // Re-queue as an event so we can properly track and retry validation of the amount sent.
            await events.PushAsync(new PaymentRequested(amount, description, merchant, e.PersonId, decoded));
        }

        // TODO: validate amount submitted WRT to the original amount requested, 
        // with the current account balance and so on.
        //await events.PushAsync(new PaymentApproved(e.Amount, e.Description, e.Merchant, e.PersonId, e.QRData));
        public Task HandleAsync(PaymentRequested e) => Task.CompletedTask;

        public async Task HandleAsync(PaymentApproved e)
        {
            var headers = env.GetVariable("MercadoPagoHeaders");
            var sessionId = Guid.NewGuid().ToString();

            dynamic qrdata = await DecodeDataAsync(e.QRData, headers, sessionId);

            var orderId = (string)qrdata.checkout_data.merchant_order.id;
            var productId = (string)qrdata.checkout_data.product_id;
            var items = ((JToken)qrdata.checkout_data.checkout_preference.items).ToString();
            var posId = (string)qrdata.checkout_data.internal_metadata.pos_id;
            var collectorId = (string)qrdata.checkout_data.checkout_preference.collector_id;

            var checkout = env.GetVariable("MercadoPagoBodyCheckout");
            checkout = checkout
                .Replace("$collector_id$", collectorId, StringComparison.Ordinal)
                .Replace("$pos_id$", posId, StringComparison.Ordinal)
                .Replace("$product_id$", productId, StringComparison.Ordinal)
                .Replace("$items$", items, StringComparison.Ordinal);

            using var checkoutReq = AddHeaders(new HttpRequestMessage(HttpMethod.Post, env.GetVariable("MercadoPagoUrlCheckout")), headers, sessionId);
            checkoutReq.Content = new StringContent(checkout, Encoding.UTF8, "application/json");

            checkoutReq.Headers.Remove("x-platform");
            checkoutReq.Headers.TryAddWithoutValidation("x-platform", "MP");
            checkoutReq.Headers.TryAddWithoutValidation("x-flow-id", "/instore");
            checkoutReq.Headers.TryAddWithoutValidation("x-product-id", productId);

            using var checkoutResp = await http.SendAsync(checkoutReq);
            checkoutResp.EnsureSuccessStatusCode();

            dynamic checkoutData = JObject.Parse(await checkoutResp.Content.ReadAsStringAsync());

            var balance = (double)checkoutData.one_tap[0].account_money.available_balance;
            if (e.Amount > balance)
            {
                // TODO: insufficient funds, should never happen?
            }

            var payment = env.GetVariable("MercadoPagoBodyPayment");
            payment = payment
                .Replace("$collector_id$", collectorId, StringComparison.Ordinal)
                .Replace("$pos_id$", posId, StringComparison.Ordinal)
                .Replace("$order_id$", orderId, StringComparison.Ordinal)
                .Replace("$title$", e.Description, StringComparison.Ordinal)
                .Replace("$amount$", e.Amount.ToString(CultureInfo.CurrentCulture), StringComparison.Ordinal);

            using var paymentReq = AddHeaders(new HttpRequestMessage(HttpMethod.Post, env.GetVariable("MercadoPagoUrlPayment")), headers, sessionId);
            paymentReq.Content = new StringContent(payment, Encoding.UTF8, "application/json");

            paymentReq.Headers.TryAddWithoutValidation("x-product-id", productId);
            paymentReq.Headers.TryAddWithoutValidation("x-tracking-id", "scan_qr");

            var idempotency = $"-{DateTime.Now.Ticks}--159125{new Random().Next(2500000, 9999999)}";
            paymentReq.Headers.TryAddWithoutValidation("x-idempotency-key", idempotency);

            using var paymentResp = await http.SendAsync(paymentReq);
            paymentResp.EnsureSuccessStatusCode();

            await events.PushAsync(new PaymentCompleted(e.Amount, e.Description, e.Merchant, e.PersonId));

            //json = await paymentResp.Content.ReadAsStringAsync();
            //data = JObject.Parse(json);
        }

        async Task<JObject> DecodeDataAsync(string data, string headers, string sessionId)
        {
            using var qrReq = AddHeaders(new HttpRequestMessage(HttpMethod.Get, env.GetVariable("MercadoPagoUrlQR") + data), headers, sessionId);
            using var qrResp = await http.SendAsync(qrReq);

            qrResp.EnsureSuccessStatusCode();

            var json = await qrResp.Content.ReadAsStringAsync();
            return JObject.Parse(json);
        }

        static HttpRequestMessage AddHeaders(HttpRequestMessage request, string headers, string sessionId)
        {
            var values = headers
                .Replace("$session-id$", sessionId, StringComparison.Ordinal)
                .Replace("$request-id$", Guid.NewGuid().ToString(), StringComparison.Ordinal);

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
