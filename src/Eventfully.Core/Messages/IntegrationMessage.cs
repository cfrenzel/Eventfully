﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Eventfully
{
    public abstract class IntegrationMessage : IIntegrationMessage
    {
        public abstract string MessageType { get; }
        
        public DateTime? CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public IntegrationMessage()
        {
        }


    }

}
