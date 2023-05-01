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

			$"current machine id: <color=yellow>{IdCurrentMachine.ShortForm()}</color>".Log();
			$"current user id: <color=yellow>{IdCurrentUser.ShortForm()}</color>".Log();

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
				// not selected
				IdHostAt = CxId.Empty,
				// will be given by server
				IdFeature = CxId.Empty,
			};

			if(ExtensionsView.TryAppendUser(ref desc, out idUser))
			{
				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserAppend
					{
						IdUser = idUser,
					});
			}

			Singleton<ServicePawns>.I.Events.Enqueue(
				new CmdPawnLobbySetChangeTo()
				{
					// for local only: remote server is not selected
					IdServer = CxId.Empty,
				});

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
			var idUserCurrent = Singleton<ServiceNetwork>.I.IdCurrentUser;
			var modelUser = Singleton<ServiceUI>.I.ModelsUser.Get(idUserCurrent, out var contains);

			// not selected
			if(!modelUser.IdHostAt.IsEmpty)
			{
				Manager.networkAddress = Discovery.GetEpFor(modelUser.IdHostAt).Address.ToString();
			}

			// sets states as a result for next mode (game) - not good but MVP
			// will be better to build process result as a separate object

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
				Owner = modelUser.CreateData,
			};
		}

		private void OnServerFound(Response response)
		{
			if(ExtensionsView.TryAppendServer(ref response, out var idServer))
			{
				$"operation scheduled APPEND over <color=white>(server)</color> model: {idServer.ShortForm()}".Log();

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
				$"operation scheduled UPDATE over <color=white>(server)</color> model: {idServer.ShortForm()}".Log();

				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyServerUpdate
					{
						IdServer = idServer,
					});
			}

			MaintainPartyUsers(response);

			Singleton<ServiceNetwork>.I.Events.Enqueue(
				new CmdNetworkTryStartGame());
		}

		private void OnServerLost(CxId idServer)
		{
			if(ExtensionsView.TryRemoveServer(idServer))
			{
				$"operation scheduled REMOVE over <color=white>(server)</color> model: {idServer.ShortForm()}".Log();

				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyServerRemove
					{
						IdServer = idServer,
					});

				var idUserCurrent = Singleton<ServiceNetwork>.I.IdCurrentUser;
				var set = Singleton<ServiceUI>.I.ModelsUser;
				var setSize = set.Count;
				for(var count = 0; count < setSize; count++)
				{
					var modelUser = set.Dequeue(out var contains);

					if(modelUser.IdHostAt != idServer)
					{
						set.Enqueue(modelUser);

						continue;
					}

					if(modelUser.IdUser == idUserCurrent)
					{
						modelUser.IdHostAt = CxId.Empty;

						set.Enqueue(modelUser);

						$"operation scheduled UPDATE over <color=white>(user: local)</color> model: {modelUser.IdUser.ShortForm()}".Log();

						Singleton<ServiceUI>.I.Events.Enqueue(
							new CmdViewLobbyUserUpdate
							{
								IdUser = modelUser.IdUser
							});

						Singleton<ServicePawns>.I.Events.Enqueue(
							new CmdPawnLobbySetChangeTo()
							{
								IdServer = modelUser.IdHostAt,
							});

						continue;
					}

					$"operation scheduled REMOVE over <color=white>(user: remote)</color> model: {modelUser.IdUser.ShortForm()}".Log();

					Singleton<ServiceUI>.I.Events.Enqueue(
						new CmdViewLobbyUserRemove
						{
							IdUser = modelUser.IdUser
						});
				}

				if(setSize > 0)
				{
					Singleton<ServiceUI>.I.ModelsUser.DeFragment();
				}
			}
		}

		private bool UpdatePartyUser(DataUser data, string userNote)
		{
			if(ExtensionsView.TryUpdateUser(ref data, out var idUser))
			{
				$"operation scheduled UPDATE over <color=white>(user: {userNote})</color> model: {idUser.ShortForm()}".Log();

				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserUpdate
					{
						IdUser = idUser,
					});

				// server pawn is constant
				// pawn set selection authority is local
				// so no need to create pawns here

				// update server authority values
				if(idUser == Singleton<ServiceNetwork>.I.IdCurrentUser)
				{
					// can be rejected by no update, have to use that result to create buttons on update
					Singleton<ServicePawns>.I.Events.Enqueue(
						new CmdPawnAppendOrUpdateLobby
						{
							IdUser = idUser,
						});
				}

				return true;
			}

			return false;
		}

		private bool AppendPartyUser(DataUser data, string userNote)
		{
			if(ExtensionsView.TryAppendUser(ref data, out var idUser))
			{
				$"operation scheduled APPEND over <color=white>(user: {userNote})</color> model: {idUser}".Log();

				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserAppend
					{
						IdUser = idUser,
					});

				// server pawn is constant
				// pawn set selection authority is local
				// so no need to create pawns here

				// can be rejected by no update, have to use that result to create buttons on update
				//Singleton<ServicePawns>.I.Events.Enqueue(
				//	new CmdPawnAppendOrUpdateLobby
				//	{
				//		IdUser = idUser,
				//	});

				return true;
			}

			return false;
		}

		/// <summary>
		/// maintains users collection, commands are rejected by the screen used: client screen displays servers 
		/// </summary>
		/// <remarks>
		/// rough solution
		/// </remarks>
		private void MaintainPartyUsers(Response response)
		{
			// authority
			var idUserCurrent = Singleton<ServiceNetwork>.I.IdCurrentUser;
			var modelUser = Singleton<ServiceUI>.I.ModelsUser.Get(idUserCurrent, out var contains);

			if(contains && response.Party_01.IdUser == idUserCurrent)
			{
				response.Party_01.IsReady = modelUser.IsReady;
				response.Party_01.IdHostAt = modelUser.IdHostAt;
			}

			if(contains && response.Party_02.IdUser == idUserCurrent)
			{
				response.Party_02.IsReady = modelUser.IsReady;
				response.Party_02.IdHostAt = modelUser.IdHostAt;
			}

			if(contains && response.Party_03.IdUser == idUserCurrent)
			{
				response.Party_03.IsReady = modelUser.IsReady;
				response.Party_03.IdHostAt = modelUser.IdHostAt;
			}

			// update
			response.Owner = UpdatePartyUser(response.Owner, "owner") ? default : response.Owner;
			response.Party_01 = UpdatePartyUser(response.Party_01, "party 1") ? default : response.Party_01;
			response.Party_02 = UpdatePartyUser(response.Party_02, "party 2") ? default : response.Party_02;
			response.Party_03 = UpdatePartyUser(response.Party_03, "party 3") ? default : response.Party_03;

			// append
			response.Owner = AppendPartyUser(response.Owner, "owner") ? default : response.Owner;
			response.Party_01 = AppendPartyUser(response.Party_01, "party 1") ? default : response.Party_01;
			response.Party_02 = AppendPartyUser(response.Party_02, "party 2") ? default : response.Party_02;
			response.Party_03 = AppendPartyUser(response.Party_03, "party 3") ? default : response.Party_03;

			// check
			if(!response.Owner.IdUser.IsEmpty)
			{
				$"NO operation performed for <color=white>(user: owner)</color> model: {response.Owner.IdUser}".Log();
			}

			if(!response.Party_01.IdUser.IsEmpty)
			{
				$"NO operation performed for <color=white>(user: party 1)</color> model: {response.Party_01.IdUser}".Log();
			}

			if(!response.Party_02.IdUser.IsEmpty)
			{
				$"NO operation performed for <color=white>(user: party 2)</color> model: {response.Party_02.IdUser}".Log();
			}

			if(!response.Party_03.IdUser.IsEmpty)
			{
				$"NO operation performed for <color=white>(user: party 3)</color> model: {response.Party_03.IdUser}".Log();
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
					IdFeature = Singleton<ServicePawns>.I.GetNextFeatureAvailableTo(Singleton<ServiceNetwork>.I.IdCurrentUser),
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

			if(ExtensionsView.TryAppendUser(ref desc.Owner, out var idUser))
			{
				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserAppend
					{
						IdUser = idUser,
					});
			}

			Singleton<ServicePawns>.I.Events.Enqueue(
				new CmdPawnLobbySetChangeTo()
				{
					IdServer = Singleton<ServiceNetwork>.I.IdCurrentMachine,
				});

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
			// sets states as a result for next mode (game) - not good but MVP
			// will be better to build process result as a separate object

			Discovery.StopDiscoveryServer();

			Discovery.OnUserFound = null;
			Discovery.OnUserUpdated = null;
			Discovery.OnUserLost = null;
		}

		public unsafe Response BuildState()
		{
			// ReSharper disable once InconsistentNaming
			static string dump(ModelUser model)
			{
				return $"host: {model.IdHostAt.ShortForm()}; user: {model.IdUser.ShortForm()}; feature: {model.IdFeature.ShortForm()}";
			}

			// assemble party
			var cacheIdsPtr = stackalloc CxId[ServicePawns.MAX_NUMBER_OR_PLAYERS_I];
			var users = Singleton<ServiceUI>.I.ModelsUser.GetRecentForHost(
				cacheIdsPtr,
				ServicePawns.MAX_NUMBER_OR_PLAYERS_I,
				Singleton<ServiceNetwork>.I.IdCurrentMachine);
			var cacheModelsPtr = stackalloc ModelUser[users];
			for(var indexIds = 0; indexIds < users; indexIds++)
			{
				cacheModelsPtr[indexIds] = Singleton<ServiceUI>.I.ModelsUser.Get(
					cacheIdsPtr[indexIds], 
					out var contains);
			}

			#if UNITY_EDITOR
			var dumpBuffer = new ModelUser[ServicePawns.MAX_NUMBER_OR_PLAYERS_I];
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

			response.Owner = users < 1 ? default : cacheModelsPtr[0].CreateData;

			if(response.Owner.IdUser != Singleton<ServiceNetwork>.I.IdCurrentUser)
			{
				throw new Exception($"current user is not an owner: (current: {response.Owner.IdUser} expected: {Singleton<ServiceNetwork>.I.IdCurrentUser})");
			}

			response.Party_01 = users < 2 ? default : cacheModelsPtr[1].CreateData;
			response.Party_02 = users < 3 ? default : cacheModelsPtr[2].CreateData;
			response.Party_03 = users < 4 ? default : cacheModelsPtr[3].CreateData;

			return response;
		}

		private void OnUserFound(Request request)
		{
			// authority
			request.Owner.IdFeature = Singleton<ServicePawns>.I.GetNextFeatureAvailableTo(request.Owner.IdUser);

			// append
			if(ExtensionsView.TryAppendUser(ref request.Owner, out var idUser))
			{
				$"operation scheduled APPEND over <color=white>(user)</color> model: {idUser.ShortForm()}".Log();

				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserAppend
					{
						IdUser = idUser,
					});

				Singleton<ServicePawns>.I.Events.Enqueue(
					new CmdPawnAppendOrUpdateLobby
					{
						IdUser = idUser,
					});
			}
		}

		private void OnUserUpdated(Request request)
		{
			// authority
			request.Owner.IdFeature = Singleton<ServicePawns>.I.GetNextFeatureAvailableTo(request.Owner.IdUser);
			
			var modelUser = Singleton<ServiceUI>.I.ModelsUser.Get(request.Owner.IdUser, out var contains);

			// update
			if(ExtensionsView.TryUpdateUser(ref request.Owner, out var idUser))
			{
				if(contains && modelUser.IdHostAt != request.Owner.IdHostAt && request.Owner.IdHostAt.IsEmpty)
				{
					$"operation scheduled REMOVE over <color=white>(user)</color> model: {idUser.ShortForm()}".Log();

					Singleton<ServiceUI>.I.Events.Enqueue(
						new CmdViewLobbyUserRemove
						{
							IdUser = idUser,
						});

					Singleton<ServicePawns>.I.Events.Enqueue(
						new CmdPawnDestroy
						{
							IdUser = idUser,
						});
				}
				else
				{
					$"operation scheduled UPDATE over <color=white>(user)</color> model: {idUser.ShortForm()}".Log();

					Singleton<ServiceUI>.I.Events.Enqueue(
						new CmdViewLobbyUserUpdate
						{
							IdUser = idUser,
						});

					Singleton<ServicePawns>.I.Events.Enqueue(
						new CmdPawnAppendOrUpdateLobby
						{
							IdUser = idUser,
						});
				}
			}

			Singleton<ServiceNetwork>.I.Events.Enqueue(
				new CmdNetworkTryStartGame());
		}

		private void OnUserLost(CxId idUser)
		{
			// remove
			if(ExtensionsView.TryRemoveUser(idUser))
			{
				$"operation scheduled REMOVE over <color=white>(user)</color> model: {idUser.ShortForm()}".Log();

				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserRemove
					{
						IdUser = idUser,
					});
				
				Singleton<ServicePawns>.I.Events.Enqueue(
					new CmdPawnDestroy
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

		public unsafe void Enable()
		{
			Singleton<ServiceUI>.I.Events.Enqueue(
				new CmdViewScreenChange
				{
					NameScreen = ServiceUI.SCREEN_GAME_S,
				});

			var modelUserCurrent = Singleton<ServiceUI>.I.ModelsUser.Get(Singleton<ServiceNetwork>.I.IdCurrentUser, out var contains);
			var idsRecentPtr = stackalloc CxId[ServicePawns.MAX_NUMBER_OR_PLAYERS_I];
			var numPlayers = Singleton<ServiceUI>.I.ModelsUser.GetRecentForHost(
				idsRecentPtr,
				ServicePawns.MAX_NUMBER_OR_PLAYERS_I,
				modelUserCurrent.IdHostAt);

			for(var index = 0; index < numPlayers; index++)
			{
				Singleton<ServicePawns>.I.Events.Enqueue(
					new CmdPawnDestroy
					{
						IdUser = idsRecentPtr[index],
					});
			}

			Manager.StartClient();
		}

		public void Disable()
		{
			Manager.StopClient();
		}
	}

	public class NetworkModeGameAsServer : INetworkMode
	{
		public CompNetworkManager Manager { get; set; }
		public CompNetworkDiscovery Discovery { get; set; }

		public CxId IdServerCurrent => Singleton<ServiceNetwork>.I.IdCurrentMachine;

		public unsafe void Enable()
		{
			Singleton<ServiceUI>.I.Events.Enqueue(
				new CmdViewScreenChange
				{
					NameScreen = ServiceUI.SCREEN_GAME_S,
				});

			var modelUserCurrent = Singleton<ServiceUI>.I.ModelsUser.Get(Singleton<ServiceNetwork>.I.IdCurrentUser, out var contains);
			var idsRecentPtr = stackalloc CxId[ServicePawns.MAX_NUMBER_OR_PLAYERS_I];
			var numPlayers = Singleton<ServiceUI>.I.ModelsUser.GetRecentForHost(
				idsRecentPtr,
				ServicePawns.MAX_NUMBER_OR_PLAYERS_I,
				modelUserCurrent.IdHostAt);

			for(var index = 0; index < numPlayers; index++)
			{
				Singleton<ServicePawns>.I.Events.Enqueue(
					new CmdPawnDestroy
					{
						IdUser = idsRecentPtr[index],
					});
			}

			Manager.StartHost();
		}

		public void Disable()
		{
			Manager.StopHost();
		}
	}

	public class CmdNetworkModeChange : ICommand<CompApp>
	{
		public INetworkMode Target;

		public virtual bool Assert(CompApp context)
		{
			return true;
		}

		public virtual void Execute(CompApp context)
		{
			Target.Manager = context.GetComponent<CompNetworkManager>();
			Target.Discovery = context.GetComponent<CompNetworkDiscovery>();

			var previous = Singleton<ServiceNetwork>.I.NetworkModeShared;
			$"network mode start changing from: <color=#0071A9>{previous?.GetType().NameNice() ?? "unset"}</color>".Log();

			Singleton<ServiceNetwork>.I.NetworkModeShared?.Disable();
			Singleton<ServiceNetwork>.I.NetworkModeShared = Target;
			Singleton<ServiceNetwork>.I.NetworkModeShared.Enable();

			$"network mode has changed to: <color=cyan>{Target.GetType().NameNice()}</color>".Log();

			if(Target is NetworkModeLobbyServer or NetworkModeLobbyClient)
			{
				context.IsGameStart = false;
			}
		}
	}

	public sealed class CmdNetworkTryStartGame : CmdNetworkModeChange
	{
		public override unsafe bool Assert(CompApp context)
		{
			if(!base.Assert(context))
			{
				return false;
			}

			var modelUserCurrent = Singleton<ServiceUI>.I.ModelsUser.Get(Singleton<ServiceNetwork>.I.IdCurrentUser, out var contains);
			var idsRecentPtr = stackalloc CxId[ServicePawns.MAX_NUMBER_OR_PLAYERS_I];
			var numPlayers = Singleton<ServiceUI>.I.ModelsUser.GetRecentForHost(
				idsRecentPtr,
				ServicePawns.MAX_NUMBER_OR_PLAYERS_I,
				modelUserCurrent.IdHostAt);

			var @is = numPlayers > 1;

			for(var index = 0; index < numPlayers; index++)
			{
				var modelUser = Singleton<ServiceUI>.I.ModelsUser.Get(idsRecentPtr[index], out contains);
				@is = @is && modelUser.IsReady;
			}

			if(!@is)
			{
				var builder = new StringBuilder($"try start game condition for players: {numPlayers}\n");
				for(var index = 0; index < numPlayers; index++)
				{
					var modelUser = Singleton<ServiceUI>.I.ModelsUser.Get(idsRecentPtr[index], out contains);
					builder
						.Append($"for user {modelUser.IdUser.ShortForm()} status: {(modelUser.IsReady ? "ready" : "busy")}")
						.AppendLine();
				}
				builder.Log();
			}

			return @is;
		}

		public override void Execute(CompApp context)
		{
			if(Singleton<ServiceNetwork>.I.NetworkModeShared is NetworkModeLobbyServer && !context.IsGameStart)
			{
				// bicycle )) to notify all clients: game start
				context.StartCoroutine(context.TaskWaitToNotify());

				return;
			}

			if(Singleton<ServiceNetwork>.I.NetworkModeShared is NetworkModeLobbyClient)
			{
				Target = new NetworkModeGameAsClient();
			}

			base.Execute(context);
		}
	}
}
