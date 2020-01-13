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


- There are 3 types of messages supported
  - Events - communicate information about a state changes that have occured.  Events are defined by their publishers.  Events don't have a specific receiver - anyone can listen and consume events 
  - Commands - communicate an action for the receiver to perform.  Commands are defined by the receiver and are sent from a sender to a known receiver.  Commands require a reply.  
  - Replies - Replies are sent from the receiver of a command as a response to that command

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


- Eventfully plugs into your DI framework

- Eventfully hooks into your DbContext/UnitOfWork to allow sending messages within a transaction.  If your transaction fails the message doesn't 
