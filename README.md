# Eventfully .NET
Lightweight Reliable Messaging Framework using Outbox Pattern / EFCore / AzureServiceBus 

[![NuGet version (Eventfully.Core)](https://img.shields.io/nuget/v/Eventfully.Core.svg?style=flat-square)](https://www.nuget.org/packages/Eventfully.Core/)
[![Build status](https://ci.appveyor.com/api/projects/status/38p46q88w79akoe7?svg=true)](https://ci.appveyor.com/project/cfrenzel/eventfully)

**Why**
- Dispatch Messages within a Transaction/UnitOfWork using an Outbox within your database
- Simple Configuration
- Advanced Retry Logic for all your eventual consistency needs
- No requirement for shared message classes between apps/services
- EFCore support
- Azure Service Bus support
- Dependency Injection support 
- Easy to customize message deserialization
- Encryption support (AES)
  - Azure Key Vault support out of the box
- Configurable Message Processing Pipeline
- Pluggable Transports, Outboxes, MessageHandling, Encryption, Dependency Injection
- Supports Events, Command/Reply


**Events**

- Events implement <code>IIntegrationEvent</code>.  A base class <code>IntegrationEvent</code> is provided.  Overriding MessageType provides a unique identifier for our new Event type.   
```csharp
 public class OrderCreated : IntegrationEvent
 {
     public override string MessageType => "Sales.OrderCreated";
     public Guid OrderId { get; private set; }
     public Decimal TotalDue { get; private set; }
     public string CurrencyCode { get; private set; }
 }
```
**Event Handlers**

- Event handlers implement <code>IMessageHandler&lt;Event&gt;</code>.
  
  ```csharp
  public class OrderCreatedHandler : IMessageHandler<OrderCreated>
  {
        public Task Handle(OrderCreated ev, MessageContext context)
        {
            Console.WriteLine($"Received OrderCreated Event");
            Console.WriteLine($"\tOrderId: {ev.OrderId}");
            Console.WriteLine($"\tTotal Due: {ev.TotalDue} {ev.CurrencyCode}");
            return Task.CompletedTask;
        }
  }
  ```

**Publishing Events**
- To Publish an <code>OrderCreated</code> event only if saving the <code>Order</code> succeeds - inject an <code>IMessagingClient</code> into your constructor.  Use the <code>IMessagingClient</code> to publish the event before calling <code>DbContext.SaveChanges</code>.  This will save the event to the <code>Outbox</code> within the same transaction as the <code>Order</code>.  The framework will try (and retry) to publish the event to the configured Transport in the background.

```csharp
  public class OrderCreator
  {
            private readonly ApplicationDbContext _db;
            private readonly ILogger<Handler> _log;
            private readonly IMessagingClient _messagingClient;

            public OrderCreator(ApplicationDbContext db, ILogger<Handler> log, IMessagingClient messagingClient)
            {
                _db = db;
                _log = log;
                _messagingClient = messagingClient;
            }

            public async Task<Guid?> CreateOrder(CreateOrderCommand command, CancellationToken cancellationToken)
            {
                try
                {
                    Order newOrder = new Order(command.Amount, command.OrderedAtUtc);
                    _db.Add(newOrder);
                    _messagingClient.Publish(
                        new OrderCreated.Event(newOrder.Id, newOrder.Amount, "USD", null)
                     );
                     await _db.SaveChangesAsync();
                     return r.Id;
                }
                catch (Exception exc)
                {
                    _log.LogError(exc, "Error creating rate", null);
                    return null;
                }
            }
```

**Configure Messaging**

-The simplest way to configure Transports (think AzureServiceBus) and Endpoints (think a specific queue or topic) is to implement a <code>Profile</code>.
  - Configure an Endpoint by providing a name: "Events"
  - Specify whether the endpoint is 
    - <code>Inbound</code> - we want to receive and handle messages from it
    - <code>Outbound</code> - we want to write messages to it
    - <code>InboundOutbound</code> - we want to do both.  Useful for apps that asynchronously talk to themselves
  - For <code>Outbound</code> endpoints we can bind specific Message Types to the endpoint.  This allows us to publish Events without specifying an Endpoint
    - <code>.BindEvent<OrderCreated>()</code>
  - <code>.AsEventDefault()</code> to make the endpoint the default for all Events
```csharp
public class MessagingProfile : Profile
    {
        public MessagingProfile(Microsoft.Extensions.Configuration.IConfiguration config)
        {
            ConfigureEndpoint("Events")
                .AsInboundOutbound() //for our example will be reading and writing to this endpoint
                .BindEvent<PaymentMethodCreated>()
                    //see AuzureKeyVaultSample for a better way
                    .UseAesEncryption(config.GetSection("SampleAESKey").Value)
                    .UseLocalTransport()
                ;
        }
    }
```

**Configuring Transient Dispatch for EFCore and SqlServer**

```csharp
public class ApplicationDbContext : DbContext, ISupportTransientDispatch
    {

        private readonly IDomainEventDispatcher _dispatcher;
        public event EventHandler ChangesPersisted;
```



- Eventfully plugs into your DI framework

- Eventfully hooks into your DbContext/UnitOfWork to allow sending messages within a transaction.  If your transaction fails the message doesn't 



**Setup Encryption for an Event Type **
```csharp
public class MessagingProfile : Profile
    {
        public MessagingProfile(Microsoft.Extensions.Configuration.IConfiguration config)
        {
            ConfigureEndpoint("Events")
                .AsInboundOutbound()
                .BindEvent<PaymentMethodCreated>()
                    //see AuzureKeyVaultSample for a better way
                    .UseAesEncryption(config.GetSection("SampleAESKey").Value)
                    .UseLocalTransport()
                ;

            ConfigureEndpoint("Commands")
                .AsInboundOutbound()
                .UseLocalTransport()
                ;

            ConfigureEndpoint("Replies")
                .AsInboundOutbound()
                .AsReplyDefault()
                .UseLocalTransport()
                ;
        }
    }
```
