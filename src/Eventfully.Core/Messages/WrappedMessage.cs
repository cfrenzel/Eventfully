using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{

    public interface IsSerialized
    {
        byte[] Message { get; set; }
    }

    /// <summary>
    /// A pre-serialized message whose MessageTypeIdentifier is specified explicitly
    /// This is useful for sending pre-serialized messages through the Outbound message
    /// pipeline
    /// </summary>
    public class WrappedMessage : IIntegrationMessage, IsSerialized
    {
        public string MessageType { get; set; }
        public byte[] Message { get; set; }

        public WrappedMessage() { }
        public WrappedMessage(string messageTypeIdenfifier, byte[] message)
        {
            this.MessageType = messageTypeIdenfifier;
            this.Message = message;
        }
    }

    public class WrappedCommand : WrappedMessage, IIntegrationCommand {
        public WrappedCommand(string messageTypeIdenfifier, byte[] message) : base(messageTypeIdenfifier, message) { }
    }
    public class WrappedEvent : WrappedMessage, IIntegrationEvent {
        public WrappedEvent(string messageTypeIdenfifier, byte[] message) : base(messageTypeIdenfifier, message) { }
    }
    public class WrappedReply : WrappedMessage, IIntegrationReply {
        public WrappedReply(string messageTypeIdenfifier, byte[] message) : base(messageTypeIdenfifier, message) { }
    }
}
