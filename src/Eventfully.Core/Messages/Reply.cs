using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{

    public interface IReply : IMessage
    {
    }

    public abstract class Reply : Message, IReply
    {
        public Reply() {}

    }

}
