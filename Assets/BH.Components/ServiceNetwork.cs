using System;
using System.Collections.Generic;
using System.Net;
using BH.Model;

namespace BH.Components
{
	public class ServiceNetwork : IService
	{
		public readonly CxId IdCurrentUser = CxId.Create();
		public readonly CxId IdCurrentMachine = CxId.Create();

		public INetworkMode NetworkModeShared;

		public readonly Queue<ICommand<CompApp>> Events = new();

		public void Reset()
		{
			// scenes reload purpose

			Events.Enqueue(new CmdNetworkModeChange { Target = new NetworkModeDisabled() });

			"initialized: network".Log();
		}

		public void Dispose()
		{
			// manageable, due Singleton generic routine

			// release components
			NetworkModeShared = null;
		}
	}

	public interface INetworkMode
	{
		CompNetworkManager Manager { get; set; }
		CompNetworkDiscovery Discovery { get; set; }

		CxId IdServerCurrent { get; }

		void Enable();
		void Disable();
	}

	public class NetworkModeDisabled : INetworkMode
	{
		public CompNetworkManager Manager { get; set; }
		public CompNetworkDiscovery Discovery { get; set; }
		public CxId IdServerCurrent => CxId.Empty;

		public void Enable() { }

		public void Disable() { }
	}

	public class NetworkModeLobbyClient : INetworkMode
	{
		public CompNetworkManager Manager { get; set; }
		public CompNetworkDiscovery Discovery { get; set; }

		public CxId IdServerCurrent =>
			Singleton<ServiceUI>.I.ModelsUser
				.Find(_ => _.IdUser == Singleton<ServiceNetwork>.I.IdCurrentUser, out var contains)
				.IdHostAt;

		public void Enable()
		{
			Discovery.OnServerFound = OnServerFound;
			Discovery.OnServerUpdated = OnServerUpdated;
			Discovery.OnServerLost = OnServerLost;

			Singleton<ServiceUI>.I.Events.Enqueue(
				new CmdViewLobbyModeChange<SchedulerTaskModelServer>()
					.BuildForAll(
						Singleton<ServiceUI>.I.ModelsServer,
						Singleton<ServiceUI>.I.ModelsUser));

			Singleton<ServiceUI>.I.ModelsUser.ToText("will call clear <b><color=white>(user)</color></b> in condition").Log();
			Singleton<ServiceUI>.I.ModelsUser.Clear();
			Singleton<ServiceUI>.I.ModelsServer.ToText("will call clear <b><color=white>(server)</color></b> in condition").Log();
			Singleton<ServiceUI>.I.ModelsServer.Clear();

			// build self user, IdHostAt means currently selected
			var idHostAt = CxId.Empty;
			var idUser = Singleton<ServiceNetwork>.I.IdCurrentUser;
			var desc = new ResponseUser
			{
				IdUser = idUser,
				IsReady = false,
				IdFeature = Singleton<ServicePawns>.I.GetNextFeatureAvailableTo(idUser),
			};

			if(ExtensionsView.TryAppendUser(ref desc, ref idHostAt, out idUser))
			{
				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserAppend
					{
						IdModel = idUser,
					});
			}

			Discovery.StartDiscoveryClient();
		}

		public void Disable()
		{
			// sets states as a result

			Discovery.StopDiscoveryClient();

			Discovery.OnServerFound = null;
			Discovery.OnServerUpdated = null;
			Discovery.OnServerLost = null;
		}

		private void OnServerFound(ResponseServer response, IPEndPoint epResponseFrom, Uri uriFrom)
		{
			if(ExtensionsView.TryAppendServer(ref response, epResponseFrom, uriFrom, out var idServer))
			{
				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyServerAppend
					{
						IdModel = idServer,
					});
			}

			if(ExtensionsView.TryAppendUser(ref response.Owner, ref idServer, out var idUser))
			{
				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserAppend
					{
						IdModel = idUser,
					});
			}

			MaintainPartyUsers(response);
		}

		private void OnServerUpdated(ResponseServer response)
		{
			if(ExtensionsView.TryUpdateServer(ref response, out var idServer))
			{
				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyServerUpdate
					{
						IdModel = idServer,
					});
			}

			if(ExtensionsView.TryUpdateUser(ref response.Owner, out var idUser))
			{
				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserUpdate
					{
						IdModel = idUser,
					});
			}

			MaintainPartyUsers(response);
		}

		private void OnServerLost(CxId idServer)
		{
			if(ExtensionsView.TryRemoveServer(idServer))
			{
				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyServerRemove
					{
						IdModel = idServer,
					});
			}

			// remove all pawns connected except self
			var set = Singleton<ServiceUI>.I.ModelsUser;
			var size = set.Count;
			for(var index = 0; index < size; index++)
			{
				var modelUser = set.Dequeue(out var contains);

				// stay intact: local user, update its focus on server
				if(modelUser.IdUser == Singleton<ServiceNetwork>.I.IdCurrentUser)
				{
					modelUser.IdHostAt = CxId.Empty;

					set.Enqueue(modelUser);

					Singleton<ServiceUI>.I.Events.Enqueue(
						new CmdViewLobbyUserUpdate
						{
							IdModel = modelUser.IdUser,
						});

					continue;
				}

				// stay intact: users from another hosts
				if(modelUser.IdHostAt != idServer)
				{
					set.Enqueue(modelUser);
					continue;
				}

				Singleton<ServiceUI>.I.ModelsUser.ToText($"remove <b><color=white>(user)</color></b>: {modelUser}").Log();

				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserRemove
					{
						IdModel = modelUser.IdUser,
					});
			}

			set.DeFragment();
		}

		private void MaintainPartyUsers(ResponseServer response)
		{
			if(ExtensionsView.TryUpdateUser(ref response.Party_01, out var idUser))
			{
				response.Party_01 = default;

				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserUpdate
					{
						IdModel = idUser,
					});
			}

			if(ExtensionsView.TryUpdateUser(ref response.Party_02, out idUser))
			{
				response.Party_02 = default;

				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserUpdate
					{
						IdModel = idUser,
					});
			}

			if(ExtensionsView.TryUpdateUser(ref response.Party_03, out idUser))
			{
				response.Party_03 = default;

				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserUpdate
					{
						IdModel = idUser,
					});
			}

			if(ExtensionsView.TryAppendUser(ref response.Party_01, ref response.IdHost, out idUser))
			{
				response.Party_01 = default;

				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserAppend
					{
						IdModel = idUser,
					});
			}

			if(ExtensionsView.TryAppendUser(ref response.Party_02, ref response.IdHost, out idUser))
			{
				response.Party_01 = default;

				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserAppend
					{
						IdModel = idUser,
					});
			}

			if(ExtensionsView.TryAppendUser(ref response.Party_03, ref response.IdHost, out idUser))
			{
				response.Party_01 = default;

				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserAppend
					{
						IdModel = idUser,
					});
			}

			if(!response.Party_01.IdUser.IsEmpty)
			{
				$"NO operation performed for <color=white>(user)</color> model: {response.Party_01.IdUser}".Log();
			}
			if(!response.Party_02.IdUser.IsEmpty)
			{
				$"NO operation performed for <color=white>(user)</color> model: {response.Party_02.IdUser}".Log();
			}
			if(!response.Party_03.IdUser.IsEmpty)
			{
				$"NO operation performed for <color=white>(user)</color> model: {response.Party_03.IdUser}".Log();
			}
		}
	}

	public class NetworkModeLobbyServer : INetworkMode
	{
		public CompNetworkManager Manager { get; set; }
		public CompNetworkDiscovery Discovery { get; set; }

		public CxId IdServerCurrent => Singleton<ServiceNetwork>.I.IdCurrentMachine;

		public void Enable()
		{
			Discovery.OnUserFound = OnUserFound;
			Discovery.OnUserUpdated = OnUserUpdated;
			Discovery.OnUserLost = OnUserLost;

			Singleton<ServiceUI>.I.Events.Enqueue(
				new CmdViewLobbyModeChange<SchedulerTaskModelUser>()
					.BuildForAll(
						Singleton<ServiceUI>.I.ModelsServer,
						Singleton<ServiceUI>.I.ModelsUser));

			Singleton<ServiceUI>.I.ModelsUser.ToText("will call clear <b><color=white>(user)</color></b> in condition").Log();
			Singleton<ServiceUI>.I.ModelsUser.Clear();
			Singleton<ServiceUI>.I.ModelsServer.ToText("will call clear <b><color=white>(server)</color></b> in condition").Log();
			Singleton<ServiceUI>.I.ModelsServer.Clear();

			// TODO: get from discovery
			IPEndPoint ipLocalAsRemoteEp = null;
			Uri ipLocalAsRemoteUri = null;
			var idHostAt = Singleton<ServiceNetwork>.I.IdCurrentMachine;
			var desc = new ResponseServer
			{
				IdHost = Singleton<ServiceNetwork>.I.IdCurrentMachine,
				ServerUsersTotal = Singleton<ServiceUI>.I.ModelsUser.Count,
				Uri = null,
				Owner = new ResponseUser
				{
					IdUser = Singleton<ServiceNetwork>.I.IdCurrentUser,
					IdFeature = CxId.Empty,
					IsReady = false,
				},
			};

			if(ExtensionsView.TryAppendServer(ref desc, ipLocalAsRemoteEp, ipLocalAsRemoteUri, out var idServer))
			{
				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyServerAppend
					{
						IdModel = idServer,
					});
			}

			if(ExtensionsView.TryAppendUser(ref desc.Owner, ref idHostAt, out var idUser))
			{
				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserAppend
					{
						IdModel = idUser,
					});
			}

			Discovery.StartDiscoveryServer();
		}

		public void Disable()
		{
			// sets states as a result

			Discovery.StopDiscoveryServer();

			Discovery.OnUserFound = null;
			Discovery.OnUserUpdated = null;
			Discovery.OnUserLost = null;
		}

		private void OnUserFound(Request request, IPEndPoint epResponseFrom)
		{
			var idHost = IdServerCurrent;
			var desc = new ResponseUser
			{
				IdUser = request.IdUser,
				IdFeature = Singleton<ServicePawns>.I.GetNextFeatureAvailableTo(request.IdUser),
				IsReady = false,
			};

			if(ExtensionsView.TryAppendUser(ref desc, ref idHost, out var idUser))
			{
				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserAppend
					{
						IdModel = idUser,
					});
			}
		}

		private void OnUserUpdated(Request request)
		{
			var desc = new ResponseUser
			{
				IdUser = request.IdUser,
				IdFeature = Singleton<ServicePawns>.I.GetNextFeatureAvailableTo(request.IdUser),
				IsReady = request.IsReady,
			};

			if(ExtensionsView.TryUpdateUser(ref desc, out var idUser))
			{
				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserUpdate
					{
						IdModel = idUser,
					});
			}
		}

		private void OnUserLost(CxId idUser)
		{
			if(ExtensionsView.TryRemoveUser(idUser))
			{
				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserRemove
					{
						IdModel = idUser,
					});
			}
		}
	}

	public sealed class CmdNetworkModeChange : ICommand<CompApp>
	{
		public INetworkMode Target;

		public bool Assert(CompApp context)
		{
			// settings change
			return true;
		}

		public void Execute(CompApp context)
		{
			// TODO: switching modes must maintain connection monikers (servers, users)

			Target.Manager = context.GetComponent<CompNetworkManager>();
			Target.Discovery = context.GetComponent<CompNetworkDiscovery>();

			var previous = Singleton<ServiceNetwork>.I.NetworkModeShared;
			$"network mode start changing from: <color=#0071A9>{previous?.GetType().NameNice() ?? "unset"}</color>".Log();

			Singleton<ServiceNetwork>.I.NetworkModeShared?.Disable();
			Singleton<ServiceNetwork>.I.NetworkModeShared = Target;
			Singleton<ServiceNetwork>.I.NetworkModeShared.Enable();

			$"network mode has changed to: <color=cyan>{Target.GetType().NameNice()}</color>".Log();
		}
	}

	public class NetworkModeGameAsClient : INetworkMode
	{
		public CompNetworkManager Manager { get; set; }
		public CompNetworkDiscovery Discovery { get; set; }

		public CxId IdServerCurrent =>
			Singleton<ServiceUI>.I.ModelsUser
				.Find(_ => _.IdUser == Singleton<ServiceNetwork>.I.IdCurrentUser, out var contains)
				.IdHostAt;

		public void Enable() { }

		public void Disable() { }
	}

	public class NetworkModeGameAsServer : INetworkMode
	{
		public CompNetworkManager Manager { get; set; }
		public CompNetworkDiscovery Discovery { get; set; }

		public CxId IdServerCurrent => Singleton<ServiceNetwork>.I.IdCurrentMachine;

		public void Enable() { }

		public void Disable() { }
	}
}
