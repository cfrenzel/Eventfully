using System.Threading.Tasks;
using Eventfully.Outboxing;

namespace Eventfully
{
    public interface IMessagingClient
    {

        /// <summary>
        /// Send command using the configured outbox
        /// </summary>
        /// <param name="command"></param>
        /// <param name="metaData"></param>
        /// <returns></returns>
        Task Send(ICommand command, MessageMetaData metaData = null);


        /// <summary>
        /// Send a pre-serialized Command using the configured outbox
        /// </summary>
        /// <param name="command"></param>
        /// <param name="metaData"></param>
        /// <returns></returns>
        Task Send(string messageTypeIdenfier, byte[] command, MessageMetaData metaData = null);

        /// <summary>
        /// Send Command skipping the outbox, the outbound message filter pipeline will still be honored
        /// </summary>
        /// <param name="command"></param>
        /// <param name="metaData"></param>
        /// <returns></returns>
        Task SendSynchronously(ICommand command, MessageMetaData metaData = null);

        /// <summary>
        /// Send a pre-serialized Command skipping the outbox, the outbound message filter pipeline will still be honored
        /// </summary>
        /// <param name="messageTypeIdenfier"></param>
        /// <param name="command"></param>
        /// <param name="metaData"></param>
        /// <returns></returns>
        Task SendSynchronously(string messageTypeIdenfier, byte[] command, MessageMetaData metaData = null);

        /// <summary>
        /// Publish an event using the configured outbox
        /// </summary>
        /// <param name="event"></param>
        /// <param name="metaData"></param>
        /// <returns></returns>
        Task Publish(IEvent @event, MessageMetaData metaData = null);


        /// <summary>
        /// Publish a pre-serialized event using the configured outbox
        /// </summary>
        /// <param name="event"></param>
        /// <param name="metaData"></param>
        /// <returns></returns>
        Task Publish(string messageTypeIdenfier, byte[] @event, MessageMetaData metaData = null);


        /// <summary>
        /// Publish Event skipping the outbox, the outbound message filter pipeline will still be honored
        /// </summary>
        /// <param name="event"></param>
        /// <param name="metaData"></param>
        /// <returns></returns>
        Task PublishSynchronously(IEvent @event, MessageMetaData metaData = null);
    }


    public class MessagingClient : IMessagingClient
    {
        private readonly MessagingService _messagingService;
        private readonly IOutboxSession _outbox;
        public MessagingClient(MessagingService messagingService, IOutboxSession outbox)
        {
            _messagingService = messagingService;
            _outbox = outbox;
        }

        /// <summary>
        /// Send command using the configured outbox
        /// </summary>
        /// <param name="command"></param>
        /// <param name="metaData"></param>
        /// <returns></returns>
        public Task Send(ICommand command, MessageMetaData metaData = null)
        {
            return _messagingService.Send(command, _outbox, metaData);
        }

        /// <summary>
        /// Send command using a preserialized form of the command using the configured outbox
        /// the outbound message filter pipeline will still be used
        /// </summary>
        /// <param name="messageTypeIdenfier"></param>
        /// <param name="command"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public Task Send(string messageTypeIdenfier, byte[] command, MessageMetaData options = null)
        {
            return _messagingService.Send(messageTypeIdenfier, command, _outbox, options);
        }

        /// <summary>
        /// Send Command skipping the outbox, the outbound message filter pipeline will still be honored
        /// </summary>
        /// <param name="command"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public Task SendSynchronously(ICommand command, MessageMetaData options = null)
        {
            return _messagingService.SendSynchronously(command, options);
        }

        /// <summary>
        /// Send a preserialized Command skipping the outbox, the outbound message filter pipeline will still be honored
        /// </summary>
        /// <param name="messageTypeIdenfier"></param>
        /// <param name="command"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public Task SendSynchronously(string messageTypeIdenfier, byte[] command, MessageMetaData options = null)
        {
            return _messagingService.SendSynchronously(messageTypeIdenfier, command, options);
        }

        /// <summary>
        /// Publish an event using the configured outbox
        /// </summary>
        /// <param name="event"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public Task Publish(IEvent @event, MessageMetaData options = null)
        {
            return _messagingService.Publish(@event, _outbox, options);
        }

        /// <summary>
        /// Publish event using a preserialized form of the event using the configured outbox
        /// the outbound message filter pipeline will still be used
        /// </summary>
        /// <param name="messageTypeIdenfier"></param>
        /// <param name="event"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public Task Publish(string messageTypeIdenfier, byte[] @event, MessageMetaData options = null)
        {
            return _messagingService.Publish(messageTypeIdenfier, @event, _outbox, options);
        }

        /// <summary>
        /// Publish Event skipping the outbox, the outbound message filter pipeline will still be honored
        /// </summary>
        /// <param name="event"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public Task PublishSynchronously(IEvent @event, MessageMetaData options = null)
        {
            return _messagingService.PublishSynchronously(@event, options);
        }
    }
}
