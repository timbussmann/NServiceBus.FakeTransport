using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.FakeTransport;
using NServiceBus.Settings;
using NServiceBus.TransportTests;

// ReSharper disable once CheckNamespace
// ReSharper disable once UnusedMember.Global
public class ConfigureFakeTransportInfrastructure : IConfigureTransportInfrastructure
{
    public TransportConfigurationResult Configure(SettingsHolder settings, TransportTransactionMode transactionMode)
    {
        var fakeTransportInfrastructure = new FakeTransportInfrastructure();

        // route local sends to the input queue:
        fakeTransportInfrastructure.Dispatcher.MessageSent += (operation, destination) =>
        {
            if (destination == fakeTransportInfrastructure.MainPump.inputQueue)
            {
                Task.Run(() => fakeTransportInfrastructure.MainPump.Push(
                    operation.Message.Body,
                    operation.Message.MessageId,
                    operation.Message.Headers));
            }
        };

        var transportConfigurationResult = new TransportConfigurationResult
        {
            TransportInfrastructure = fakeTransportInfrastructure
        };

        return transportConfigurationResult;
    }

    public Task Cleanup()
    {
        return Task.CompletedTask;
    }
}