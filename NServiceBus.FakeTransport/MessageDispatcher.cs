using System;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Transport;

namespace NServiceBus.FakeTransport
{
    public class MessageDispatcher : IDispatchMessages
    {
        public delegate void SendEventHandler(UnicastTransportOperation operation, string destination);
        public delegate void PublishEventHandler(MulticastTransportOperation operation);

        public event SendEventHandler MessageSent;
        public event PublishEventHandler MessagePublished;

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, ContextBag context)
        {
            InMemoryTransaction inMemoryTransaction = null;
            if (transaction.TryGet(out inMemoryTransaction))
            {
            }

            foreach (var unicastTransportOperation in outgoingMessages.UnicastTransportOperations)
            {
                if (inMemoryTransaction != null && unicastTransportOperation.RequiredDispatchConsistency != DispatchConsistency.Isolated)
                {
                    var x = unicastTransportOperation;
                    inMemoryTransaction.Enlist(() => DispatchUnicastMessage(x));
                }
                else
                {
                    DispatchUnicastMessage(unicastTransportOperation);
                }
            }

            foreach (var multicastTransportOperation in outgoingMessages.MulticastTransportOperations)
            {
                if (inMemoryTransaction != null && multicastTransportOperation.RequiredDispatchConsistency != DispatchConsistency.Isolated)
                {
                    inMemoryTransaction.Enlist(() => DispatchMulticastMessage(multicastTransportOperation));
                }
                else
                {
                    DispatchMulticastMessage(multicastTransportOperation);
                }
            }

            return Task.CompletedTask;
        }

        private void DispatchMulticastMessage(MulticastTransportOperation multicastTransportOperation)
        {
            try
            {
                MessagePublished?.Invoke(multicastTransportOperation);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void DispatchUnicastMessage(UnicastTransportOperation unicastTransportOperation)
        {
            try
            {
                MessageSent?.Invoke(
                    unicastTransportOperation,
                    unicastTransportOperation.Destination);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}