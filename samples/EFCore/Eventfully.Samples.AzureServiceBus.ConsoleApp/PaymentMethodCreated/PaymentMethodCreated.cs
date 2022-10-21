using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully.Samples.ConsoleApp
{

    public class PaymentMethodCreated : Event
    {
        public override string MessageType => "Sales.PaymentMethodCreated";

        public Guid PaymentMethodId { get; private set; }

        public CardInfo MethodInfo { get; private set; }
        
        private PaymentMethodCreated(){}

        public PaymentMethodCreated(Guid methodId, CardInfo methodInfo)
        {
            this.PaymentMethodId = methodId;
            this.MethodInfo = methodInfo;
        }

        public class CardInfo
        {
            public string Number { get; private set; }
            public string NameOnCard { get; set; }
            public string ExpirationYear { get; private set; }
            public string ExpirationMonth { get; private set; }
      
            private CardInfo(){}
            public CardInfo(string number, string nameOn, string expirationYear, string expirationMonth)
            {
                this.Number = number;
                this.NameOnCard = nameOn;
                this.ExpirationYear = expirationYear;
                this.ExpirationMonth = expirationMonth;
            }

        }
    }
}
