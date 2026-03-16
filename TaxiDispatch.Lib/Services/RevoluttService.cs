#nullable disable
using TaxiDispatch.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;

namespace TaxiDispatch.Services
{
    public class RevoluttService : BaseService<RevoluttService>
    {
        // test enviroment
        //private string token = "sk_b9uhuz9MO0h2Qpm_bp3JvKAx1LdHnqdwM3R7bGT-h4vSQCtKZyuCEXPui-Ask1Q7";
        //private string url = "https://sandbox-merchant.revolut.com";

        private string token = "sk_eI_3SwYzzpq_1mF7z16eXQDz46WaOgJSN1QFHgKbyWS5mU4U1F_M3y-23_mQilqb";
        private string url = "https://merchant.revolut.com";

        public RevoluttService(IDbContextFactory<TaxiDispatchContext> factory, ILogger<RevoluttService> logger) : base(factory,logger)
        {

        }

        public async Task<bool> IsChargeVatOnCard()
        {
            var value = await _dB.CompanyConfig.Select(o=>o.AddVatOnCardPayments).FirstOrDefaultAsync();
            return value;
        }


        public async Task<CreateOrderResponse> CreateOrder(double value, string description)
        {
            var client = new RestClient(url);
            var request = new RestRequest("api/orders", Method.Post);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddHeader("Revolut-Api-Version", "2024-09-01");
            var obj = new { amount = value, currency = "GBP", description = "" };
            var body = JsonConvert.SerializeObject(obj);

            request.AddStringBody(body, DataFormat.Json);
            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessStatusCode)
            { 
                var obj1 = JsonConvert.DeserializeObject<CreateOrderResponse>(response.Content);
                return obj1;
            }

            throw new Exception("Check the request, invalid result.");
        }

        public async Task CancelOrder(string orderid)
        {
            var client = new RestClient(url);
            var request = new RestRequest($"api/orders/{orderid}/cancel", Method.Post);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddHeader("Revolut-Api-Version", "2024-09-01");

            RestResponse response = await client.ExecuteAsync(request);
            Console.WriteLine(response.Content);
        }

        public async Task RefundOrder(string orderid, double value)
        {
            var client = new RestClient(url);
            var request = new RestRequest($"api/1.0/orders/{orderid}/refund", Method.Post);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddHeader("Revolut-Api-Version", "2024-09-01");

            var obj = new { amount = value, description = "Ace Taxis - Refund"};

            var body = JsonConvert.SerializeObject(obj);

            request.AddStringBody(body, DataFormat.Json);
            RestResponse response = await client.ExecuteAsync(request);
            Console.WriteLine(response.Content);
        }

        public async Task<RetrieveOrderResponse> GetOrderStatus(string orderid)
        {
            var client = new RestClient(url);
            var request = new RestRequest($"api/orders/{orderid}", Method.Get);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddHeader("Revolut-Api-Version", "2024-09-01");

            RestResponse response = await client.ExecuteAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var obj1 = JsonConvert.DeserializeObject<RetrieveOrderResponse>(response.Content);
                return obj1;
            }

            throw new Exception("Check the request, invalid result.");
        }

        public async Task<CreateWebhookResponse> CreateWebHook()
        {
            var client = new RestClient(url);
            var request = new RestRequest("api/1.0/webhooks", Method.Post);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddHeader("Revolut-Api-Version", "2024-09-01");

            var body = @"{" + "\n" +
            @"  ""url"": ""https://api.acetaxisdorset.co.uk/api/bookings/revpaymentupdate""," + "\n" +
            //@"  ""url"": ""https://dev.ace-api.1soft.co.uk/api/bookings/revpaymentupdate""," + "\n" +
            //@"  ""url"": ""https://e685-2a02-c7e-5e1d-6d00-df5-e2d3-bfa3-f491.ngrok-free.app/api/bookings/revpaymentupdate""," + "\n" +
            @"  ""events"": [" + "\n" +
            @"    ""ORDER_COMPLETED""," + "\n" +
            @"    ""ORDER_AUTHORISED""" + "\n" +
            @"  ]" + "\n" +
            @"}";

            request.AddStringBody(body, DataFormat.Json);
            RestResponse response = await client.ExecuteAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var obj1 = JsonConvert.DeserializeObject<CreateWebhookResponse>(response.Content);
                return obj1;
            }

            throw new Exception("Check the request, invalid result.");
        }

        public async Task<List<WebHookListItem>> GetWebHookList()
        {
            var client = new RestClient(url);
            var request = new RestRequest("api/1.0/webhooks", Method.Get);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddHeader("Revolut-Api-Version", "2024-09-01");

            RestResponse response = await client.ExecuteAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var obj1 = JsonConvert.DeserializeObject<List<WebHookListItem>>(response.Content);
                return obj1;
            }

            throw new Exception("Check the request, invalid result.");
        }

        public async Task DeleteWebHook(string webhookId)
        {
            var client = new RestClient(url);
            var request = new RestRequest($"api/1.0/webhooks/{webhookId}", Method.Delete);
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddHeader("Revolut-Api-Version", "2024-09-01");

            RestResponse response = await client.ExecuteAsync(request);
            Console.WriteLine(response.Content);
        }

        public class WebHookListItem
        {
            public string id { get; set; }
            public string url { get; set; }
            public List<string> events { get; set; }
        }

        public class WebHookCallback
        {
            public string @event { get; set; }
            public string order_id { get; set; }
            public string? merchant_order_ext_ref { get; set; }
        }

        public class CreateWebhookResponse
        {
            public string id { get; set; }
            public string url { get; set; }
            public List<string> events { get; set; }
            public string signing_secret { get; set; }
        }

        public class CreateOrderResponse
        {
            public string id { get; set; }
            public string token { get; set; }
            public string type { get; set; }
            public string state { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
            public int amount { get; set; }
            public string currency { get; set; }
            public int outstanding_amount { get; set; }
            public string capture_mode { get; set; }
            public string checkout_url { get; set; }
            public string enforce_challenge { get; set; }
        }

        // retreieve order

        public class Checks
        {
            public ThreeDs three_ds { get; set; }
            public string cvv_verification { get; set; }
        }

        public class Customer
        {
            public string id { get; set; }
            public string email { get; set; }
        }

        public class Fee
        {
            public string type { get; set; }
            public int amount { get; set; }
            public string currency { get; set; }
        }

        public class Payment
        {
            public string id { get; set; }
            public string state { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
            public int amount { get; set; }
            public string currency { get; set; }
            public int settled_amount { get; set; }
            public string settled_currency { get; set; }
            public string risk_level { get; set; }
            public List<Fee> fees { get; set; }
            public PaymentMethod payment_method { get; set; }
        }

        public class PaymentMethod
        {
            public string type { get; set; }
            public string card_brand { get; set; }
            public string funding { get; set; }
            public string card_country_code { get; set; }
            public string card_bin { get; set; }
            public string card_last_four { get; set; }
            public string card_expiry { get; set; }
            public string cardholder_name { get; set; }
            public Checks checks { get; set; }
        }

        public class RetrieveOrderResponse
        {
            public string id { get; set; }
            public string token { get; set; }
            public string type { get; set; }
            public string state { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
            public int amount { get; set; }
            public string currency { get; set; }
            public int refunded_amount { get; set; }
            public int outstanding_amount { get; set; }
            public string capture_mode { get; set; }
            public List<Payment> payments { get; set; }
            public string enforce_challenge { get; set; }
            public Customer customer { get; set; }
        }

        public class ThreeDs
        {
            public string eci { get; set; }
            public string state { get; set; }
            public int version { get; set; }
        }

    }
}



