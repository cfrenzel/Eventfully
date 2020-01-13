using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Eventfully.Transports.Testing
{

    public class TestEndpoint : Endpoint
    {
        public TestEndpoint(string name, List<Type> boundedMessageTypes,  List<string> boundedMessageTypeIds)//, Func<string, byte[], IEndpoint, MessageMetaData, Task> dispatchCallback)
        :base (
             new TestEndpointSettings(name, boundedMessageTypes, boundedMessageTypeIds),
             new TestTransport()
        )
        {}
    }

    public class TestOutboundEndpoint : Endpoint
    {
        public TestOutboundEndpoint(string name, List<Type> boundedMessageTypes, List<string> boundedMessageTypeIds)//, Func<string, byte[], IEndpoint, MessageMetaData, Task> dispatchCallback)
        : base(
             new TestOutboundEndpointSettings(name, boundedMessageTypes, boundedMessageTypeIds),
             new TestTransport()
        )
        { }
    }


    public class TestFailingOutboundEndpoint : TestOutboundEndpoint
    {
        public TestFailingOutboundEndpoint(string name, List<Type> boundedMessageTypes, List<string> boundedMessageTypeIds)//, Func<string, byte[], IEndpoint, MessageMetaData, Task> dispatchCallback)
       :base(name, boundedMessageTypes, boundedMessageTypeIds)
        {}
       
        public override Task Dispatch(string messageTypeIdenfifier, byte[] message, MessageMetaData metaData = null) 
        {
            throw new ApplicationException("Test Failure");
        }
    }

    public class TestEndpointSettings : EndpointSettings
    {
        public TestEndpointSettings(string name, List<Type> boundedMessageTypes = null, List<string> boundMessageTypeIds = null) : base(name)
        {
            this.IsReader = true;
            this.IsWriter = true;
            this.MessageTypeIdentifiers = boundMessageTypeIds;
            this.MessageTypes = boundedMessageTypes;
        }
    }
    public class TestOutboundEndpointSettings : EndpointSettings
    {
        public TestOutboundEndpointSettings(string name, List<Type> boundedMessageTypes = null, List<string> boundMessageTypeIds = null) 
            : base(name)
        {
            this.IsReader = false;
            this.IsWriter = true;
            this.MessageTypeIdentifiers = boundMessageTypeIds;
            this.MessageTypes = boundedMessageTypes;
        }
    }

    public class TestTransportFactory : ITransportFactory
    {
        public TestTransportFactory()//TestEndpoint endpoint, Func<string, byte[], IEndpoint, MessageMetaData, Task> dispatchCallback)
        {
            //Endpoint = endpoint;
            //_dispatchCallback = dispatchCallback;
        }
        public Transport Create(TransportSettings settings) => new TestTransport();// Endpoint, _dispatchCallback);
        
    }

    public class TestNullTransportFactory : ITransportFactory
    {
        public TestNullTransportFactory()//TestEndpoint endpoint, Func<string, byte[], IEndpoint, MessageMetaData, Task> dispatchCallback)
        {
        }
        public Transport Create(TransportSettings settings) => null;// Endpoint, _dispatchCallback);

    }

    public class TestNullTransportEndpoint : IEndpoint
    {
       
        public List<string> BoundMessageIdentifiers => null;
        public List<Type> BoundMessageTypes => null;
        public bool IsReader => false;

        public bool IsWriter => true;

        public string Name => "TestNullTransportEndpoint";

        public EndpointSettings Settings => null;

        public bool SupportsDelayedDispatch => false;

        public Transport Transport => null;

        public Task Dispatch(string messageTypeIdenfifier, byte[] message, MessageMetaData metaData = null)
        {
            throw new NotImplementedException();
        }

        public void SetReplyToForCommand(IIntegrationCommand command, MessageMetaData meta)
        {
            throw new NotImplementedException();
        }

        public Task Start(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    public class TestTransportSettings : TransportSettings
    {
        private readonly ITransportFactory _factory;
        public override ITransportFactory Factory => _factory;
        public TestTransportSettings() {
            _factory = new TestTransportFactory();
        }//TestEndpoint endpoint, Func<string, byte[], IEndpoint, MessageMetaData, Task> dispatchCallback) => _factory = new TestOutboundTransportFactory(endpoint, dispatchCallback);

    }
    public class TestNullTransportSettings : TransportSettings
    {
        private readonly ITransportFactory _factory;
        public override ITransportFactory Factory => _factory;
        public TestNullTransportSettings()
        {
            _factory = new TestNullTransportFactory();
        }
    }
    public class TestTransport : Transport
    {
        //public TestEndpoint Endpoint;
        //private Func<string, byte[], IEndpoint, MessageMetaData, Task> _dispatchCallBack;
    

        public override bool SupportsDelayedDispatch => false;
        public TestTransport() { }// TestEndpoint endpoint, Func<string, byte[], IEndpoint, MessageMetaData, Task> dispatchCallback)
        public override Task Dispatch(string messageTypeIdentifier, byte[] message, IEndpoint endpoint, MessageMetaData metaData = null) => Task.CompletedTask;// => _dispatchCallBack(messageTypeIdentifier, message, endpoint, metaData);
        public override IEndpoint FindEndpointForReply(MessageContext commandContext) => null;// Endpoint;
        public override void SetReplyToForCommand(IEndpoint endpoint, IIntegrationCommand command, MessageMetaData meta) => meta.ReplyTo = null;//this.Endpoint.Name;
        public override Task Start(IEndpoint endpoint, CancellationToken cancellationToken) => Task.CompletedTask;
     
    }

}
