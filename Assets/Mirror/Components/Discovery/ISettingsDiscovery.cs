namespace Mirror.Discovery
{
	public interface ISettingsDiscovery
	{
		long SecretHandshake { get; }
		int ServerBroadcastListenPort { get; }
		float ActiveDiscoveryInterval { get; }
		string BroadcastAddress { get; }
	}
}
