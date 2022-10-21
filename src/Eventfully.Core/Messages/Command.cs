using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{

    public interface ICommand : IMessage
    {
    }

    public abstract class Command : Message, ICommand
    {
        public Command()
        {
        } 
    }

}
