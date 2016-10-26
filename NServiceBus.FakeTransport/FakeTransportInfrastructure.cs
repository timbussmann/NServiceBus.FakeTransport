using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Routing;
using NServiceBus.Transport;

namespace NServiceBus.FakeTransport
{
    public class FakeTransportInfrastructure : TransportInfrastructure
    {
        public readonly MessageDispatcher Dispatcher = new MessageDispatcher();
        public readonly SubscriptionManager SubscriptionManager = new SubscriptionManager();
        public MessagePump MainPump;

        public List<MessagePump> messagePumps { get; set; } = new List<MessagePump>();

        public FakeTransportInfrastructure()
        {
        }

        private MessagePump CreateReceiver()
        {
            var messagePump = new MessagePump();
            messagePumps.Add(messagePump);

            if (messagePumps.Count == 1)
            {
                // the first queue is the main input pump
                MainPump = messagePump;
            }

            return messagePump;
        }


        public override TransportReceiveInfrastructure ConfigureReceiveInfrastructure()
        {
            return new TransportReceiveInfrastructure(CreateReceiver, () => new QueueCreator(), () => Task.FromResult(StartupCheckResult.Success));
        }

        public override TransportSendInfrastructure ConfigureSendInfrastructure()
        {
            return new TransportSendInfrastructure(() => Dispatcher, () => Task.FromResult(StartupCheckResult.Success));
        }

        public override TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure()
        {
            return new TransportSubscriptionInfrastructure(() => SubscriptionManager);
        }

        public override EndpointInstance BindToLocalEndpoint(EndpointInstance instance)
        {
            return instance;
        }

        public override string ToTransportAddress(LogicalAddress logicalAddress)
        {
            var transportAddress = $"{logicalAddress.EndpointInstance.Endpoint}/{logicalAddress.Qualifier}/{logicalAddress.EndpointInstance.Discriminator}".TrimEnd('/');
            return transportAddress;
        }

        public override TransportTransactionMode TransactionMode { get; } = TransportTransactionMode.SendsAtomicWithReceive;
        public override OutboundRoutingPolicy OutboundRoutingPolicy { get; } = new OutboundRoutingPolicy(OutboundRoutingType.Unicast, OutboundRoutingType.Multicast, OutboundRoutingType.Unicast);

        public override IEnumerable<Type> DeliveryConstraints { get; } = Enumerable.Empty<Type>();
    }
}