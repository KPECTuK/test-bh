using BH.Model;

namespace BH.Components
{
	public class ServiceNetwork : IService
	{
		public IServerMode ServerModeShared = new ServerModeDisabled();

		public void Reset()
		{
			// scenes reload purpose

			"initialized: network".Log();
		}

		public void Dispose()
		{
			// manageable, due Singleton generic routine
		}
	}

	public interface IServerMode { }

	public class ServerModeDisabled : IServerMode { }

	public class ServerModeLobby : IServerMode { }

	public class ServerModeGame : IServerMode { }
}
