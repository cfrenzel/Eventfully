using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OrderingApi.Entities
{
    public class Order
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Key.NewId();

        [MaxLength(50)]
        public string Sku { get; set; }

        public int Quantity { get; set; }
        [Precision(11,2)]
        public Decimal Amount { get; set; }
        public DeliveryMethod DeliveryMethod { get; set; }
        public PaymentStyle PaymentStyle { get; set; }
        public DateTime OrderedAt { get; set; }          

        
        [Timestamp, ConcurrencyCheck] 
        public byte[] ConcurrencyStamp { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }
    
    public class DeliveryMethod : Enumeration<DeliveryMethod, string>
    {
        public static readonly DeliveryMethod Pickup = new DeliveryMethod("Pickup", "Pickup");
        public static readonly DeliveryMethod Delivery = new DeliveryMethod("Delivery", "Delivery");
        
        private DeliveryMethod() : base(Pickup.Value, Pickup.DisplayName) { }
        private DeliveryMethod(string value, string displayName) : base(value, displayName) { }
    }
    
    public class PaymentStyle : Enumeration<PaymentStyle, string>
    {
        public static readonly PaymentStyle PrePay = new PaymentStyle("PrePay", "Pre Pay");
        public static readonly PaymentStyle OnDelivery = new PaymentStyle("UponReceipt", "Upon Receipt");
        
        private PaymentStyle() : base(PrePay.Value, PrePay.DisplayName) { }
        private PaymentStyle(string value, string displayName) : base(value, displayName) { }
    }
    
   
}