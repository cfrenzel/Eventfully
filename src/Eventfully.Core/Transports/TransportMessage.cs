using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{

    /// <summary>
    /// The Message meta data and message content byte[] 
    /// standardized to send/receive from a transport
    /// </summary>
    public class TransportMessage
    {
        public string MessageTypeIdentifier { get; set; }
        public byte[] Data { get; set; }
        public MessageMetaData MetaData { get; set; }

        public TransportMessage(string messageTypeIdentifier, byte[] data, MessageMetaData meta)
        {
            this.MessageTypeIdentifier = messageTypeIdentifier;
            this.Data = data;
            this.MetaData = meta;
        }
    }
}
