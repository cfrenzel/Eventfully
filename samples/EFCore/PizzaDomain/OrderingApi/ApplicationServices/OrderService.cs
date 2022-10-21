using Contracts.Commands;
using Contracts.Events;
using MassTransit;
using OrderingApi.Entities;
using Sample.Api.Processes;

namespace OrderingApi.ApplicationServices;

public class OrderService
{
    private readonly ApplicationDbContext _db;
    private readonly IPublishEndpoint _publisher;
    private readonly ISendEndpointProvider _sender;

 

    public OrderService(ApplicationDbContext db, IPublishEndpoint publisher, ISendEndpointProvider sender)
    {
        _db = db;
        _publisher = publisher;
        _sender = sender;
    }
   
    public async Task<Order> SubmitOrder(SubmitOrderCommand o)
    {
        var order = new Order()
        {
            Sku = o.Sku,
            OrderedAt = DateTime.UtcNow,
            Quantity = o.Quantity,
            DeliveryMethod = Enumeration<DeliveryMethod, string>.FromValue(o.DeliveryMethodType),
            PaymentStyle = Enumeration<PaymentStyle, string>.FromValue(o.PaymentStyleType),
            Amount = o.Amount,
        };

        //Save the order and publish the message to the outbox within a db transaction
        await _db.Orders.AddAsync(order);
        OrderSubmissionProcess process = new OrderSubmissionProcess(null);
        
        
        await _publisher.Publish(new OrderCreated(order.Id, order.Sku, order.OrderedAt, order.DeliveryMethod.Value, order.PaymentStyle.Value));
        if (order.PaymentStyle == PaymentStyle.PrePay)
        {
            var paymentEndpoint = await _sender.GetSendEndpoint(new Uri("queue:process-payment"));
            await paymentEndpoint.Send(new ProcessPayment()
            {
                OrderId = order.Id,
                Amount = order.Amount,
                Currency = "usd",
                IdempotencyKey = NewId.NextSequentialGuid().ToString(),
            });
            var timeoutEndpoint = await _sender.GetSendEndpoint(new Uri("queue:payment-request-timeout"));
            await timeoutEndpoint.Send(new PaymentRequestTimeout(){
                 OrderId = order.Id,
            }, context => context.SetScheduledEnqueueTime(TimeSpan.FromSeconds(60)));
        }
        
        await _db.SaveChangesAsync();
        return order;
    }
    
    public class SubmitOrderCommand
    {
        public string Sku { get; set; }
        public int Quantity { get; set; }
        public string DeliveryMethodType { get; set; }          
        public string PaymentStyleType { get; set; }

        public Decimal Amount { get; set; }
    }
}