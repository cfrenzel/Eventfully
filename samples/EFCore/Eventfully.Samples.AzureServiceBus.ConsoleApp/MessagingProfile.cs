using Eventfully.Transports.AzureServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eventfully.Samples.ConsoleApp
{
    public class MessagingProfile : Profile
    {

        public MessagingProfile(Microsoft.Extensions.Configuration.IConfiguration config)
        {
            ConfigureEndpoint("Events")
                .AsOutbound()
                .AsEventDefault()
                .BindEvent<PaymentMethodCreated>()
                    //see AuzureKeyVaultSample for a better way
                    .UseAesEncryption(config.GetSection("SampleAESKey").Value)
               .UseAzureServiceBusTransport()
                ;
            ConfigureEndpoint("Events-Sub")
              .AsInbound()
              .BindEvent<PaymentMethodCreated>()
                 //see AuzureKeyVaultSample for a better way
                .UseAesEncryption(config.GetSection("SampleAESKey").Value)
              .UseAzureServiceBusTransport()
              ;

            ConfigureEndpoint("Commands")
                .AsInboundOutbound()
                .UseAzureServiceBusTransport()
                ;

            ConfigureEndpoint("Replies")
                .AsInboundOutbound()
                .AsReplyDefault()
                .UseAzureServiceBusTransport()
                ;
        }
    }
}
