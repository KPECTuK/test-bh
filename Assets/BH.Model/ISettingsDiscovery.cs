namespace BH.Model
{
	public interface ISettingsDiscovery
	{
		long SecretHandshake { get; }
		int PortServerBroadcastListen { get; }
		int PortServerResponseListen { get; }
		float ActiveDiscoveryInterval { get; }
		string BroadcastAddress { get; }
	}
}
