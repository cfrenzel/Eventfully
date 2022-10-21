using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SimulationWorker
{
    
    /// <summary>
    /// Periodically Order Pizzas to simulate online orders
    /// </summary>
    public class SimulationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _provider;
        private readonly IHttpClientFactory _clientFactory;
        private readonly Random _random = new Random();

        private readonly ConcurrentDictionary<string, OrderInfo>
            _orders = new ConcurrentDictionary<string, OrderInfo>();
            
        public SimulationBackgroundService(IServiceProvider provider, IHttpClientFactory clientFactory)
        {
            _provider = provider;
            _clientFactory = clientFactory;
        }

        private async Task<OrderInfo> PlaceOrder(CancellationToken stoppingToken)
        {
            var deliveryMethodType = _random.Choose(new string[] { "Pickup", "Delivery" });
            var paymentStyleType = deliveryMethodType.Equals("Delivery")
                ? "PrePay" : _random.Choose(new string[] { "PrePay", "UponReceipt" });

            HttpClient client = _clientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:7001");
            client.DefaultRequestHeaders.Accept.Add( new MediaTypeWithQualityHeaderValue("application/json"));
        
            
            var orderInfo = new OrderInfo
            {
                ///TODO: add some kind of reference if we don't get back a valid response we can still check if it went through
                Sku = _random.String(12),
                Quantity = _random.Next(1, 5),
                Amount = _random.Money(),
                DeliveryMethodType = deliveryMethodType,
                PaymentStyleType = paymentStyleType,
            };
            
            HttpResponseMessage response = await client.PostAsJsonAsync("Orders", orderInfo);
            if (!response.IsSuccessStatusCode)
                return null; 
            
            var responseContent = await response.Content.ReadAsStringAsync();
            orderInfo.OrderId = responseContent;
            if(this._orders.TryAdd(responseContent, orderInfo))
               return orderInfo;
            return null;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _provider.GetRequiredService<IServiceScopeFactory>().CreateScope())
                {
                    var orderInfo = await this.PlaceOrder(stoppingToken);
                    if(orderInfo != null)
                        Console.WriteLine($"OrderId: {orderInfo.OrderId} Submitted");
                    await Task.Delay(30000, stoppingToken);
                }
            }
        }
        
    }
    

    public class OrderInfo
    {
        public string OrderId { get; set; }
        public string Sku { get; set; }
        public int Quantity { get; set; }
        public string DeliveryMethodType { get; set; }          
        public string PaymentStyleType { get; set; }
        public Decimal Amount { get; set; }      

    }
}