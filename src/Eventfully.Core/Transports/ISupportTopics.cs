using System;

namespace Eventfully.Transports
{
    public interface ISupportTopics<T>
    {
        T ConfigureTopic(string name, string subscriptionName = null);
        
        /// <summary>
        /// Convenience to configure a topic that publishes a single event type
        /// </summary>
        /// <param name="name"></param>
        /// <typeparam name="T">The event type for the topic</typeparam>
        /// <returns></returns>
        T ConfigureTopic<M>(string name, string subscriptionName = null, Action<MessageSettings> configBuilder = null);
    }
}