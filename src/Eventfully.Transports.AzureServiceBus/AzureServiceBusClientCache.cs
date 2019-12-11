using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Eventfully.Transports.AzureServiceBus
{
    public static class AzureServiceBusClientCache
    {

        internal static ConcurrentDictionary<string, IClientEntity> Clients = new ConcurrentDictionary<string, IClientEntity>();

        public static IMessageSender GetSender(string connectionString)
        {
            var builder = new ServiceBusConnectionStringBuilder(connectionString);
            var entityInfo = _getEntityNameAndUniqueKey(builder, isSender: true);
            if (!Clients.ContainsKey(entityInfo.Item2))
            {
                var sender = new MessageSender(builder);
                Clients.TryAdd(entityInfo.Item2, sender);
                return sender;
            }
            else
                return (IMessageSender)Clients[entityInfo.Item2];
        }

        public static bool ContainsSender(string connectionString)
        {
            var builder = new ServiceBusConnectionStringBuilder(connectionString);
            var entityInfo = _getEntityNameAndUniqueKey(builder, isSender: true);
            return Clients.ContainsKey(entityInfo.Item2);
        }

        public static IMessageReceiver GetReceiver(string connectionString)
        {

            var builder = new ServiceBusConnectionStringBuilder(connectionString);
            var entityInfo = _getEntityNameAndUniqueKey(builder);
            if (!Clients.ContainsKey(entityInfo.Item2))
            {
                var receiver = new MessageReceiver(builder);
                Clients.TryAdd(entityInfo.Item2, receiver);
                return receiver;
            }
            else
                return (IMessageReceiver)Clients[entityInfo.Item2];
        }

        public static bool ContainsReceiver(string connectionString)
        {
            var builder = new ServiceBusConnectionStringBuilder(connectionString);
            var entityInfo = _getEntityNameAndUniqueKey(builder);
            return Clients.ContainsKey(entityInfo.Item2);
        }




        public static ITopicClient GetTopicClient(string connectionString, string topicName = null)
        {
            var builder = new ServiceBusConnectionStringBuilder(connectionString, topicName, null);
            var entityInfo = _getEntityNameAndUniqueKey(builder);
            if (!Clients.ContainsKey(entityInfo.Item2))
            {
                var topicClient = new TopicClient(builder);
                Clients.TryAdd(entityInfo.Item2, topicClient);
                return topicClient;
            }
            else
                return (ITopicClient)Clients[entityInfo.Item2];
        }

        public static bool ContainsTopic(string connectionString, string topicName = null)
        {
            var builder = new ServiceBusConnectionStringBuilder(connectionString, topicName, null);
            var entityInfo = _getEntityNameAndUniqueKey(builder);
            return Clients.ContainsKey(entityInfo.Item2);
        }

     
        public static IQueueClient GetQueueClient(string connectionString, string queueName)
        {
            var builder = new ServiceBusConnectionStringBuilder(connectionString, queueName, null);
            var entityInfo = _getEntityNameAndUniqueKey(builder);
            if (!Clients.ContainsKey(entityInfo.Item2))
            {
                var queueClient = new QueueClient(builder);
                Clients.TryAdd(entityInfo.Item2, queueClient);
                return queueClient;
            }
            else
                return (IQueueClient)Clients[entityInfo.Item2];
        }
      
        public static bool ContainsQueue(string connectionString, string queueName = null)
        {
            var builder = new ServiceBusConnectionStringBuilder(connectionString, queueName, null);
            var entityInfo = _getEntityNameAndUniqueKey(builder);
            return Clients.ContainsKey(entityInfo.Item2);
        }

        public static ISubscriptionClient GetSubscriptionClient(string connectionString, string subscriptionName, string topicName = null)
        {
            
            var builder = new ServiceBusConnectionStringBuilder(connectionString, topicName, null);
            var entityInfo = _getEntityNameAndUniqueKey(builder, subscriptionName: subscriptionName);
            if (!Clients.ContainsKey(entityInfo.Item2))
            {
                var subscription = new SubscriptionClient(builder, subscriptionName);
                Clients.TryAdd(entityInfo.Item2, subscription);
                return subscription;
            }
            else
                return (ISubscriptionClient)Clients[entityInfo.Item2];
        }
        public static bool ContainsSubscription(string subscriptionName)
        {
            return Clients.ContainsKey(subscriptionName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="IsSender">Since Sender and Receiver aren't interchangable even when the the underlying client is (queue),
        /// wee need to cache them seperately (the underlying framework will cache them anyhow; so as long as they aren't closed/disposed</param>
        /// <param name="subscriptionName"></param>
        /// <returns></returns>
        private static Tuple<string, string> _getEntityNameAndUniqueKey(ServiceBusConnectionStringBuilder builder, bool isSender = false, string subscriptionName = null)
        {
            if(!String.IsNullOrEmpty(subscriptionName))
               builder.EntityPath = EntityNameHelper.FormatSubscriptionPath(builder.EntityPath, subscriptionName);
            return new Tuple<string, string>(builder.EntityPath, $"{builder.Endpoint}/{builder.EntityPath}{isSender}".ToLowerInvariant());
        }


       
    }
}
