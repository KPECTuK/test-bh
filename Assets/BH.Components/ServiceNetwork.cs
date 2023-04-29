using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
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

			var idUser = Singleton<ServiceNetwork>.I.IdCurrentUser;
			var desc = new DataUser
			{
				IdUser = idUser,
				IsReady = false,
				IdHostAt = CxId.Empty,
				IdFeature = Singleton<ServicePawns>.I.GetNextFeatureAvailableTo(idUser),
			};

			if(ExtensionsView.TryAppendUser(ref desc, Singleton<ServiceNetwork>.I.IdCurrentMachine, out idUser))
			{
				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserAppend
					{
						IdUser = idUser,
					});
			}

			// kind of unknown error
			if(!Discovery.StartDiscoveryClient())
			{
				Singleton<ServiceNetwork>.I.Events.Enqueue(new CmdNetworkModeChange
				{
					Target = new NetworkModeDisabled(),
				});
			}
		}

		public void Disable()
		{
			// sets states as a result

			Discovery.StopDiscoveryClient();

			Discovery.OnServerFound = null;
			Discovery.OnServerUpdated = null;
			Discovery.OnServerLost = null;
		}

		public Request BuildState()
		{
			var idUser = Singleton<ServiceNetwork>.I.IdCurrentUser;
			var modelUser = Singleton<ServiceUI>.I.ModelsUser.Get(idUser, out var contains);
			if(!contains)
			{
				throw new Exception("can't find local user");
			}

			return new Request
			{
				IdClientMachine = Singleton<ServiceNetwork>.I.IdCurrentMachine,
				Owner = modelUser.Data,
			};
		}

		private void OnServerFound(Response response)
		{
			if(ExtensionsView.TryAppendServer(ref response, out var idServer))
			{
				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyServerAppend
					{
						IdServer = idServer,
					});
			}

			MaintainPartyUsers(response);
		}

		private void OnServerUpdated(Response response)
		{
			if(ExtensionsView.TryUpdateServer(ref response, out var idServer))
			{
				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyServerUpdate
					{
						IdServer = idServer,
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
						IdServer = idServer,
					});
			}

			ExtensionsView.TryRemoveUsersByHost(idServer);
		}

		/// <summary>
		/// maintains users collection, commands are rejected by the screen used: client screen displays servers 
		/// </summary>
		/// <remarks>
		/// rough solution
		/// </remarks>
		private void MaintainPartyUsers(Response response)
		{
			// TODO: receive IdFeature from the server, selected currently

			if(ExtensionsView.TryUpdateUser(ref response.Owner, response.IdHost, out var idUser))
			{
				response.Owner = default;

				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserUpdate
					{
						IdUser = idUser,
					});
			}

			if(ExtensionsView.TryUpdateUser(ref response.Party_01, response.IdHost, out idUser))
			{
				response.Party_01 = default;

				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserUpdate
					{
						IdUser = idUser,
					});
			}

			if(ExtensionsView.TryUpdateUser(ref response.Party_02, response.IdHost, out idUser))
			{
				response.Party_02 = default;

				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserUpdate
					{
						IdUser = idUser,
					});
			}

			if(ExtensionsView.TryUpdateUser(ref response.Party_03, response.IdHost, out idUser))
			{
				response.Party_03 = default;

				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserUpdate
					{
						IdUser = idUser,
					});
			}

			if(ExtensionsView.TryAppendUser(ref response.Owner, response.IdHost, out idUser))
			{
				response.Owner = default;

				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserAppend
					{
						IdUser = idUser,
					});
			}

			if(ExtensionsView.TryAppendUser(ref response.Party_01, response.IdHost, out idUser))
			{
				response.Party_01 = default;

				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserAppend
					{
						IdUser = idUser,
					});
			}

			if(ExtensionsView.TryAppendUser(ref response.Party_02, response.IdHost, out idUser))
			{
				response.Party_02 = default;

				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserAppend
					{
						IdUser = idUser,
					});
			}

			if(ExtensionsView.TryAppendUser(ref response.Party_03, response.IdHost, out idUser))
			{
				response.Party_03 = default;

				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserAppend
					{
						IdUser = idUser,
					});
			}

			if(!response.Owner.IdUser.IsEmpty)
			{
				$"NO operation performed for <color=white>(user)</color> model: {response.Owner.IdUser}".Log();
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

			var desc = new Response
			{
				IdHost = Singleton<ServiceNetwork>.I.IdCurrentMachine,
				ServerUsersTotal = Singleton<ServiceUI>.I.ModelsUser.Count,
				Owner = new DataUser
				{
					IdUser = Singleton<ServiceNetwork>.I.IdCurrentUser,
					IdFeature = CxId.Empty,
					IdHostAt = Singleton<ServiceNetwork>.I.IdCurrentMachine,
					IsReady = false,
				},
			};

			if(ExtensionsView.TryAppendServer(ref desc, out var idServer))
			{
				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyServerAppend
					{
						IdServer = idServer,
					});
			}

			if(ExtensionsView.TryAppendUser(ref desc.Owner, Singleton<ServiceNetwork>.I.IdCurrentMachine, out var idUser))
			{
				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserAppend
					{
						IdUser = idUser,
					});
			}

			if(!Discovery.StartDiscoveryServer())
			{
				Singleton<ServiceNetwork>.I.Events.Enqueue(new CmdNetworkModeChange
				{
					Target = new NetworkModeLobbyClient(),
				});
			}
		}

		public void Disable()
		{
			// sets states as a result

			Discovery.StopDiscoveryServer();

			Discovery.OnUserFound = null;
			Discovery.OnUserUpdated = null;
			Discovery.OnUserLost = null;
		}

		public unsafe Response BuildState()
		{
			// ReSharper disable once InconsistentNaming
			static string dump(ModelViewUser model)
			{
				return $"host: {model.IdHostAt.ShortForm()}; user: {model.IdUser.ShortForm()}; feature: {model.IdFeature.ShortForm()}";
			}

			// assemble party
			var cacheIdsPtr = stackalloc CxId[ServicePawns.MAX_NUMBER_OR_PLAYERS_I];
			var users = Singleton<ServiceUI>.I.ModelsUser.GetRecentForHost(
				cacheIdsPtr,
				ServicePawns.MAX_NUMBER_OR_PLAYERS_I,
				Singleton<ServiceNetwork>.I.IdCurrentMachine);
			var cacheModelsPtr = stackalloc ModelViewUser[users];
			for(var indexIds = 0; indexIds < users; indexIds++)
			{
				cacheModelsPtr[indexIds] = Singleton<ServiceUI>.I.ModelsUser.Get(
					cacheIdsPtr[indexIds], 
					out var contains);
			}

			//! noisy
			#if UNITY_EDITOR
			var dumpBuffer = new ModelViewUser[ServicePawns.MAX_NUMBER_OR_PLAYERS_I];
			for(var index = 0; index < users; index++)
			{
				dumpBuffer[index] = cacheModelsPtr[index];
			}
			dumpBuffer.ToText($"responding with users: {users}", dump).Log();
			Singleton<ServiceUI>.I.ModelsUser
				.ToText($"of users total: {Singleton<ServiceUI>.I.ModelsUser.Count}", dump)
				.Log();
			#endif

			// ReSharper disable once UseObjectOrCollectionInitializer
			var response = new Response();
			response.IdHost = Singleton<ServiceNetwork>.I.IdCurrentMachine;
			response.ServerUsersTotal = Singleton<ServiceUI>.I.ModelsUser.Count;

			response.Owner = users < 1 ? default : cacheModelsPtr[0].Data;

			if(response.Owner.IdUser != Singleton<ServiceNetwork>.I.IdCurrentUser)
			{
				throw new Exception($"current user is not an owner: (current: {response.Owner.IdUser} expected: {Singleton<ServiceNetwork>.I.IdCurrentUser})");
			}

			response.Party_01 = users < 2 ? default : cacheModelsPtr[1].Data;
			response.Party_02 = users < 3 ? default : cacheModelsPtr[2].Data;
			response.Party_03 = users < 4 ? default : cacheModelsPtr[3].Data;

			return response;
		}

		private void OnUserFound(Request request)
		{
			var desc = request.Owner;
			desc.IdFeature = Singleton<ServicePawns>.I.GetNextFeatureAvailableTo(desc.IdUser);

			if(ExtensionsView.TryAppendUser(ref desc, Singleton<ServiceNetwork>.I.IdCurrentMachine, out var idUser))
			{
				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserAppend
					{
						IdUser = idUser,
					});
			}
		}

		private void OnUserUpdated(Request request)
		{
			var desc = request.Owner;
			desc.IdFeature = Singleton<ServicePawns>.I.GetNextFeatureAvailableTo(desc.IdUser);

			if(ExtensionsView.TryUpdateUser(ref desc, Singleton<ServiceNetwork>.I.IdCurrentMachine, out var idUser))
			{
				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserUpdate
					{
						IdUser = idUser,
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
						IdUser = idUser,
					});
			}
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

	public sealed class CmdNetworkModeChange : ICommand<CompApp>
	{
		public INetworkMode Target;

		public bool Assert(CompApp context)
		{
			return true;
		}

		public void Execute(CompApp context)
		{
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
}
