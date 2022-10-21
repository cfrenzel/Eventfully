/*
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Eventfully.Transports
{
    public interface ITransport
    {
        bool SupportsDelayedDispatch { get; }

        Task StartAsync(IEndpoint endpoint, Handler handler, CancellationToken cancellationToken);

        Task StopAsync();

        Task Dispatch(string messageTypeIdenfifier, byte[] message, IEndpoint endpoint, MessageMetaData metaData = null);

        void SetReplyToForCommand(IEndpoint endpoint, ICommand command, MessageMetaData meta);

        IEndpoint FindEndpointForReply(MessageContext commandContext);

    }
    
    public interface ITransportFactory
    {
        ITransport Create(TransportSettings settings);
    }
    
    public interface ITransportSettings
    {
        ITransportFactory Factory { get; }
     
        ITransport Create();
    }

    public abstract class TransportSettings : ITransportSettings
    {
        public List<EndpointSettings> EndpointSettings { get; private set; } = new List<EndpointSettings>();

        public abstract ITransportFactory Factory { get; }

        
        public virtual EndpointSettings ConfigureEndpoint(string name)
        {
            var endpointSettings = new EndpointSettings(name);
            EndpointSettings.Add(endpointSettings);
            return endpointSettings;
        }
        
        public ITransport Create()
        {
            return Factory.Create(this);
        }

       
    }
}
*/
