using NServiceBus.Settings;
using NServiceBus.Transport;

namespace NServiceBus.FakeTransport
{
    public class FakeTransport : TransportDefinition
    {
        public override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            return settings.GetOrDefault<FakeTransportInfrastructure>() ?? new FakeTransportInfrastructure();
        }

        public override bool RequiresConnectionString { get; } = false;

        public override string ExampleConnectionStringForErrorMessage { get; } = string.Empty;
    }
}