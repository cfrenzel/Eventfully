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
                .AsInboundOutbound()
                .BindEvent<PaymentMethodCreated>()
                    //see AuzureKeyVaultSample for a better way
                    .UseAesEncryption(config.GetSection("SampleAESKey").Value)
                    .UseLocalTransport()
                ;

            ConfigureEndpoint("Commands")
                .AsInboundOutbound()
                .UseLocalTransport()
                ;

            ConfigureEndpoint("Replies")
                .AsInboundOutbound()
                .AsReplyDefault()
                .UseLocalTransport()
                ;
        }
    }
}
