using System;

namespace Contracts.Events
{
    /*******************/
    /***** ORDERING ****/
    public class OrderCreated
    {
        public Guid OrderId { get; set; }
        public string Sku { get; set; }
        public DateTime OrderedAt { get; set; }
        public string DeliveryMethod { get; set; }
        public string PaymentStyle { get; set; }

        private OrderCreated(){}

        public OrderCreated(Guid orderId, string sku, DateTime orderedAt, string deliveryMethod, string paymentStyle)
        {
            OrderId = orderId;
            Sku = sku;
            OrderedAt = orderedAt;
            DeliveryMethod = deliveryMethod;
            PaymentStyle = paymentStyle;
        }
    }

    
    /*******************/
    /***** KITCHEN ****/
    
    public class OrderPrepStarted
    {
        public Guid OrderId { get; set; }
    }
    
    public class OrderReady
    {
        public Guid OrderId { get; set; }
    }

    /*******************/
    /***** PAYMENT ****/
    public class ProcessPaymentCommand
    {
        public Guid OrderId { get; set; }
    }
    
    public class PaymentProcessed
    {
        public Guid OrderId { get; set; }
        public DateTime PaidAt { get; set; }
    }
    
    
    /*******************/
    /***** DELIVERY ****/
    public class OrderClaimed
    {
        public Guid OrderId { get; set; }
    }
    
    public class InTransit
    {
        public Guid OrderId { get; set; }
        public DateTime PaidAt { get; set; }
    }
    
    public class Fulfilled
    {
        public Guid OrderId { get; set; }
        public DateTime DeliveredAt { get; set; }
    }
    
}