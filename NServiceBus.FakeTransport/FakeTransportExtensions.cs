using NServiceBus.Configuration.AdvanceExtensibility;

namespace NServiceBus.FakeTransport
{
    public static class FakeTransportExtensions
    {
        public static void Use(this TransportExtensions<FakeTransport> config, FakeTransportInfrastructure infrastrcuture)
        {
            config.GetSettings().Set<FakeTransportInfrastructure>(infrastrcuture);
        }
    }
}