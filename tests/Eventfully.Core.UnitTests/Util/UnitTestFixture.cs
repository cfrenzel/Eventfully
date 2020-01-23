using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FakeItEasy;
using System.Reflection;
using Eventfully.Handlers;
using Newtonsoft.Json;
using System.Text;
using Eventfully.Outboxing;

namespace Eventfully.Core.UnitTests
{

    public class UnitTestFixture 
    {
        
        protected static IConfigurationRoot _config;
        protected static IServiceProvider _serviceProvider;
        //public static MessagingService MessagingService;
        public static IOutbox Outbox { get; set; }

        static UnitTestFixture()
        {
            //Outbox = A.Fake<IOutbox>();
            //var handlerFactory = A.Fake<IMessageHandlerFactory>();
            //MessagingService = new MessagingService(Outbox, handlerFactory);
        }

        //public static IServiceScope NewScope() => _serviceProvider.CreateScope();


    }
}
