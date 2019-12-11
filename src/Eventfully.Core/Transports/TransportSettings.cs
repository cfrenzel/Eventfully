using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully.Transports
{
    public interface ITransportSettings
    {
        ITransportFactory Factory { get; }
        //List<string> MessageTypeIdentifiers { get; }
        //List<Type> MessageTypes { get; }

        //TransportSettings AddCommand(string messageTypeIdentifier);
        //TransportSettings BindCommand<T>() where T : IIntegrationCommand;
        //TransportSettings BindEvent<T>() where T : IIntegrationEvent;
        Transport Create();
    }


    public abstract class TransportSettings : ITransportSettings
    {
        public abstract ITransportFactory Factory { get; }

        //public List<Type> MessageTypes { get; protected set; } = new List<Type>();
        //public List<string> MessageTypeIdentifiers { get; protected set; } = new List<string>();

        //public virtual TransportSettings BindEvent<T>() where T : IIntegrationEvent
        //{
        //    this.MessageTypes.Add(typeof(T));
        //    return this;
        //}

        //public virtual TransportSettings BindCommand<T>() where T : IIntegrationCommand
        //{
        //    this.MessageTypes.Add(typeof(T));
        //    return this;
        //}

        //public virtual TransportSettings AddCommand(string messageTypeIdentifier)
        //{
        //    this.MessageTypeIdentifiers.Add(messageTypeIdentifier);
        //    return this;
        //}

        public Transport Create()
        {
            return Factory.Create(this);
        }
    }


  
}
