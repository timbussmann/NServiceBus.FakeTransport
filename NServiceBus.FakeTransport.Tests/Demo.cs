using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus.Serializers.XML;
using NUnit.Framework;

namespace NServiceBus.FakeTransport.Tests
{
    [TestFixture]
    class Demo
    {
        [Test]
        public async Task ShouldStartAndStop()
        {
            var endpointConfig = new EndpointConfiguration("test");
            endpointConfig.UseTransport<FakeTransport>();

            endpointConfig.UsePersistence<InMemoryPersistence>();
            endpointConfig.SendFailedMessagesTo("error");

            var endpoint = await Endpoint.Start(endpointConfig);
            await endpoint.Stop();
        }

        [Test]
        public async Task ShouldReceiveMessage()
        {
            var endpointConfig = new EndpointConfiguration("test");

            // must-have config:
            var routing = endpointConfig.UseTransport<FakeTransport>().Routing();
            routing.RouteToEndpoint(typeof(OutgoingMessage), "demoDestination");
            //endpointConfig.UseSerialization<FakeSerializerDefinition>();

            endpointConfig.UsePersistence<InMemoryPersistence>();
            endpointConfig.SendFailedMessagesTo("error");

            var endpoint = await Endpoint.Start(endpointConfig);

            await endpoint.SendLocal(new DemoMessage());

            await endpoint.Stop();

            Assert.IsTrue(DemoMessageHandler1.ReceivedMessage);
        }

        [Test]
        public async Task ShouldCaptureOutgoingMessages()
        {
            var endpointConfig = new EndpointConfiguration("test");

            // must-have config:
            var routing = endpointConfig.UseTransport<FakeTransport>().Routing();
            routing.RouteToEndpoint(typeof(OutgoingMessage), "demoDestination");
            //endpointConfig.UseSerialization<FakeSerializerDefinition>();

            endpointConfig.UsePersistence<InMemoryPersistence>();
            endpointConfig.SendFailedMessagesTo("error");


            var endpoint = await Endpoint.Start(endpointConfig);

            await endpoint.SendLocal(new DemoMessage());
            //await endpoint.Receive(new DemoMessage());

            await endpoint.Stop();

//            var command = FakeTransportInfrastructure.Dispatcher.SentMessages.Single();
//            Assert.That(command, Is.TypeOf<OutgoingMessage>());
//
//            var @event = FakeTransportInfrastructure.Dispatcher.PublishedEvents.Single();
//            Assert.That(@event, Is.TypeOf<OutgoingEvent>());
        }

        class DemoMessage : ICommand
        {
        }

        class OutgoingMessage : ICommand
        {
        }

        class OutgoingEvent : IEvent
        {
        }

        class DemoMessageHandler1 : IHandleMessages<DemoMessage>
        {
            public static bool ReceivedMessage;
            public Task Handle(DemoMessage message, IMessageHandlerContext context)
            {
                ReceivedMessage = true;
                return Task.CompletedTask;
            }
        }

        class DemoMessageHandler2 : IHandleMessages<DemoMessage>
        {
            public static bool ReceivedMessage;
            public async Task Handle(DemoMessage message, IMessageHandlerContext context)
            {
                await context.Send(new OutgoingMessage());
                await context.Publish<OutgoingEvent>();
            }
        }
    }
}
