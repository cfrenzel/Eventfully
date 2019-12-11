using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{
    public class EndpointBindings
    {

        public List<EndpointBinding> Endpoints { get; set; }
        public class EndpointBinding
        {
            public string Name { get; set; }
            public string ConnectionString { get; set; }

        }

    }
}
