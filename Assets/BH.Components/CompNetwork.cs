using Mirror;

namespace BH.Components
{
	public class CompNetwork : NetworkManager
	{
		// ReSharper disable once InconsistentNaming
		public new static CompNetwork singleton { get; private set; }

		public override void Awake()
		{
			base.Awake();
			singleton = this;
		}

		public override void OnValidate()
		{
			base.OnValidate();
		}

		public override void Start()
		{
			base.Start();
		}

		public override void LateUpdate()
		{
			base.LateUpdate();
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
		}

		public override void ConfigureHeadlessFrameRate()
		{
			base.ConfigureHeadlessFrameRate();
		}

		public override void OnApplicationQuit()
		{
			base.OnApplicationQuit();
		}

		// Scene Management

		public override void ServerChangeScene(string newSceneName)
		{
			base.ServerChangeScene(newSceneName);
		}

		public override void OnServerChangeScene(string newSceneName) { }

		public override void OnServerSceneChanged(string sceneName) { }

		public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling) { }

		public override void OnClientSceneChanged()
		{
			base.OnClientSceneChanged();
		}

		// Server System Callbacks

		public override void OnServerConnect(NetworkConnectionToClient conn) { }

		public override void OnServerReady(NetworkConnectionToClient conn)
		{
			base.OnServerReady(conn);
		}

		public override void OnServerAddPlayer(NetworkConnectionToClient conn)
		{
			base.OnServerAddPlayer(conn);
		}

		public override void OnServerDisconnect(NetworkConnectionToClient conn)
		{
			base.OnServerDisconnect(conn);
		}

		public override void OnServerError(NetworkConnectionToClient conn, TransportError transportError, string message) { }

		// Client System Callbacks

		public override void OnClientConnect()
		{
			base.OnClientConnect();
		}

		public override void OnClientDisconnect() { }

		public override void OnClientNotReady() { }

		public override void OnClientError(TransportError transportError, string message) { }

		// Start & Stop Callbacks

		// Since there are multiple versions of StartServer, StartClient and StartHost, to reliably customize
		// their functionality, users would need override all the versions. Instead these callbacks are invoked
		// from all versions, so users only need to implement this one case.

		public override void OnStartHost() { }

		public override void OnStartServer() { }

		public override void OnStartClient() { }

		public override void OnStopHost() { }

		public override void OnStopServer() { }

		public override void OnStopClient() { }
	}
}
