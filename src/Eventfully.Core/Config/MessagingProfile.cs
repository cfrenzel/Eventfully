using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Eventfully
{
    public class Profile
    {
        public List<EndpointSettings> EndpointSettings { get; private set; } = new List<EndpointSettings>();

        public EndpointSettings ConfigureEndpoint(string name) => ConfigureEndpoint(name, null);

        public EndpointSettings ConfigureEndpoint(string name, string connectionString)
        {
            var endpointSettings = new EndpointSettings(name, connectionString);
            EndpointSettings.Add(endpointSettings);
            return endpointSettings;
        }

        public Profile AddBindings(EndpointBindings bindings)
        {
            if (bindings == null || EndpointSettings.Count < 1)
                return this;

            foreach(var binding in bindings.Endpoints)
            {
                var endpoint = EndpointSettings.SingleOrDefault(x => x.Name.Equals(binding.Name, StringComparison.OrdinalIgnoreCase));
                if (endpoint != null)
                {
                    if (String.IsNullOrEmpty(endpoint.ConnectionString))
                        endpoint.ConnectionString = binding.ConnectionString;
                }
            }
            return this;
        }


    }
}
