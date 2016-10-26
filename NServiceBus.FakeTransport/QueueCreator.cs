using System.Threading.Tasks;
using NServiceBus.Transport;

namespace NServiceBus.FakeTransport
{
    class QueueCreator : ICreateQueues
    {
        public Task CreateQueueIfNecessary(QueueBindings queueBindings, string identity)
        {
            return Task.CompletedTask;
        }
    }
}