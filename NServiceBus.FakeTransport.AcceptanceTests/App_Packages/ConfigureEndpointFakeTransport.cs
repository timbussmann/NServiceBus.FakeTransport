using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.AcceptanceTests.ScenarioDescriptors;
using NServiceBus.FakeTransport;
using NServiceBus.Transport;

public class ConfigureScenariosForFakeTransport : IConfigureSupportedScenariosForTestExecution
{
    public IEnumerable<Type> UnsupportedScenarioDescriptorTypes { get; } = new Type[]
    {
        typeof(AllDtcTransports),
        typeof(AllTransportsWithMessageDrivenPubSub)
    };
}

public class ConfigureEndpointFakeTransport : IConfigureEndpointTestExecution
{
    private static FakeTransportRouter router;

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings)
    {
        if (router == null)
        {
            router = new FakeTransportRouter();
        }

        var infrastructure = new FakeTransportInfrastructure();
        router.Add(infrastructure);
        configuration.UseTransport<FakeTransport>().Use(infrastructure);

        return Task.CompletedTask;
    }



    public Task Cleanup()
    {
        router = null;
        return Task.CompletedTask;
    }

    class FakeTransportRouter
    {
        readonly IList<FakeTransportInfrastructure> infrastructures = new List<FakeTransportInfrastructure>();

        public void Add(FakeTransportInfrastructure infrastructure)
        {
            infrastructures.Add(infrastructure);

            infrastructure.Dispatcher.MessageSent += DispatcherOnMessageSent;
            infrastructure.Dispatcher.MessagePublished += DispatcherOnMessagePublished;
        }

        private void DispatcherOnMessagePublished(MulticastTransportOperation operation)
        {
            Dictionary<string, MessagePump> subscribers = new Dictionary<string, MessagePump>();
            foreach (var infrastructure in infrastructures)
            {
                if (infrastructure.SubscriptionManager.subscriptions.Any(subscription => subscription.IsAssignableFrom(operation.MessageType)))
                {
                    subscribers[infrastructure.MainPump.inputQueue] = infrastructure.MainPump;
                }
            }

            foreach (var subscriber in subscribers.Values)
            {
                Task.Run(() =>
                    subscriber.Push(operation.Message.Body, operation.Message.MessageId, operation.Message.Headers));
            }
        }

        private void DispatcherOnMessageSent(UnicastTransportOperation operation, string destination)
        {
            List<MessagePump> receivers = new List<MessagePump>();
            foreach (var infrastructure in infrastructures)
            {
                foreach (var pump in infrastructure.messagePumps)
                {
                    if (pump.inputQueue == destination)
                    {
                        receivers.Add(pump);
                    }
                }
            }

            Task.Run(() => receivers
                .FirstOrDefault()?
                .Push(operation.Message.Body, operation.Message.MessageId, operation.Message.Headers));
        }
    }
}