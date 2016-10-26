using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Transport;

namespace NServiceBus.FakeTransport
{
    public class SubscriptionManager : IManageSubscriptions
    {
        public List<Type> subscriptions = new List<Type>();

        public Task Subscribe(Type eventType, ContextBag context)
        {
            subscriptions.Add(eventType);
            return Task.CompletedTask;
        }

        public Task Unsubscribe(Type eventType, ContextBag context)
        {
            subscriptions.Remove(eventType);
            return Task.CompletedTask;
        }
    }
}