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
				.FindAs(_ => _.IdUser == Singleton<ServiceNetwork>.I.IdCurrentUser)
				?.IdAtHost ??
			CxId.Empty;

		public void Enable()
		{
			Discovery.OnServerFound = OnServerFound;
			Discovery.OnServerUpdated = OnServerUpdated;
			Discovery.OnServerLost = OnServerLost;

			Singleton<ServiceUI>.I.Events.Enqueue(
				new CmdViewLobbyClear { KeepSelfInServiceServer = false });

			// build self user, IdAtHost is currently selected

			var modelUser = Singleton<ServiceUI>.I.ModelsUser.GetById(Singleton<ServiceNetwork>.I.IdCurrentUser);
			if(modelUser == null)
			{
				modelUser = new ModelViewUser
				{
					IdUser = Singleton<ServiceNetwork>.I.IdCurrentUser,
					IdAtHost = CxId.Empty,
					IsReady = false,
					IdCamera = CxId.Empty,
					IdFeature = CxId.Empty,
					LastUpdated = DateTime.UtcNow,
					FirstUpdated = DateTime.UtcNow,
				};

				Singleton<ServiceUI>.I.ModelsUser.Enqueue(modelUser);

				Singleton<ServiceUI>.I.ModelsUser.ToText( $"append <b><color=white>(user)</color></b>: {modelUser}").Log();
			}
			else
			{
				modelUser.IdAtHost = CxId.Empty;

				Singleton<ServiceUI>.I.ModelsUser.ToText( $"update <b><color=white>(user)</color></b>: {modelUser}").Log();
			}

			Singleton<ServicePawns>.I.Events.Enqueue(
				new CmdPawnLobbyCreate
				{
					Model = modelUser,
				});

			Manager.StartClient();
			Discovery.StartDiscovery();
		}

		public void Disable()
		{
			// sets states as a result

			Discovery.StopDiscovery();
			Manager.StopClient();

			Discovery.OnServerFound = null;
			Discovery.OnServerUpdated = null;
			Discovery.OnServerLost = null;
		}

		private void OnServerFound(Response response, IPEndPoint epResponseFrom, Uri uriFrom)
		{
			var modelServer = new ModelViewServer
			{
				IdOwner = response.IdOwner,
				IdHost = response.IdHost,
				PlayersTotal = response.ServerUsersReady,
				LastUpdated = DateTime.UtcNow,
				HostIp = epResponseFrom,
				HostUri = uriFrom,
			};

			Singleton<ServiceUI>.I.ModelsServer.Enqueue(modelServer);

			Singleton<ServiceUI>.I.ModelsServer.ToText($"append <b><color=white>(server)</color></b>: {modelServer}").Log();

			Singleton<ServiceUI>.I.Events.Enqueue(
				new CmdViewLobbyServerAppend
				{
					Model = modelServer,
				});

			UpdateUsers(response);

			// not selected, no need to update pawns
		}

		private void OnServerUpdated(Response response)
		{
			var modelServer = Singleton<ServiceUI>.I.ModelsServer
				.GetById(response.IdHost);

			if(modelServer == null)
			{
				throw new Exception($"server model is not found: {response.IdHost}");
			}

			modelServer.PlayersTotal = response.ServerUsersTotal;

			Singleton<ServiceUI>.I.ModelsServer.ToText($"update <b><color=white>(server)</color></b>: {modelServer}").Log();

			Singleton<ServiceUI>.I.Events.Enqueue(
				new CmdViewLobbyServerUpdate
				{
					Model = modelServer,
				});

			UpdateUsers(response);

			var modelUser = Singleton<ServiceUI>.I.ModelsUser
				.GetById(Singleton<ServiceNetwork>.I.IdCurrentUser);
			if(modelUser.IdAtHost == modelServer.IdHost)
			{
				Singleton<ServicePawns>.I.Events.Enqueue(
					new CmdPawnLobbySetUpdate
					{
						Model = modelServer,
					});
			}
		}

		private void OnServerLost(CxId idServer)
		{
			var modelServer = Singleton<ServiceUI>.I.ModelsServer.RemoveById(idServer);

			Singleton<ServiceUI>.I.ModelsServer.ToText($"remove <b><color=white>(server)</color></b>: {modelServer}").Log();

			Singleton<ServiceUI>.I.Events.Enqueue(
				new CmdViewLobbyServerRemove
				{
					Model = modelServer,
				});

			// remove all pawns connected except self
			var queue = Singleton<ServiceUI>.I.ModelsUser;
			var size = queue.Count;
			for(var index = 0; index < size; index++)
			{
				var modelUser = queue.Dequeue();

				// stay intact: local user, update its focus on server
				if(modelUser.IdUser == Singleton<ServiceNetwork>.I.IdCurrentUser)
				{
					queue.Enqueue(modelUser);
					if(modelUser.IdAtHost == modelServer.IdHost)
					{
						modelUser.IdAtHost = CxId.Empty;
					}

					continue;
				}

				// stay intact: users from another hosts
				if(modelUser.IdAtHost != modelServer.IdHost)
				{
					queue.Enqueue(modelUser);

					continue;
				}

				Singleton<ServiceUI>.I.ModelsUser.ToText($"remove <b><color=white>(user)</color></b>: {modelUser}").Log();
			}

			Singleton<ServicePawns>.I.Events.Enqueue(
				new CmdPawnLobbySetChangeTo
				{
					Model = null,
				});
		}

		private void UpdateUsers(Response response)
		{
			var queue = Singleton<ServiceUI>.I.ModelsUser;
			var size = queue.Count;

			// update
			for(var index = 0; index < size; index++)
			{
				var modelUser = queue.Dequeue();
				queue.Enqueue(modelUser);

				if(modelUser.IdUser == response.IdParty_03)
				{
					modelUser.IdUser = response.IdParty_03;
					modelUser.IsReady = response.IsReady_03;
					modelUser.IdFeature = response.IdFeature_03;
					// no need to update others (local assignment)

					Singleton<ServiceUI>.I.ModelsUser.ToText($"update <b><color=white>(user)</color></b>: {modelUser})").Log();

					response.IdParty_03 = CxId.Empty;

					Singleton<ServiceUI>.I.Events.Enqueue(
						new CmdViewLobbyUserUpdate
						{
							Model = modelUser,
						});
				}
				if(modelUser.IdUser == response.IdParty_02)
				{
					modelUser.IdUser = response.IdParty_02;
					modelUser.IsReady = response.IsReady_02;
					modelUser.IdFeature = response.IdFeature_02;
					// no need to update others (local assignment)

					Singleton<ServiceUI>.I.ModelsUser.ToText($"update <b><color=white>(user)</color></b>: {modelUser}").Log();

					response.IdParty_02 = CxId.Empty;

					Singleton<ServiceUI>.I.Events.Enqueue(
						new CmdViewLobbyUserUpdate
						{
							Model = modelUser,
						});
				}
				if(modelUser.IdUser == response.IdParty_01)
				{
					modelUser.IdUser = response.IdParty_01;
					modelUser.IsReady = response.IsReady_01;
					modelUser.IdFeature = response.IdFeature_01;
					// no need to update others (local assignment)

					Singleton<ServiceUI>.I.ModelsUser.ToText($"update <b><color=white>(user)</color></b>: {modelUser}").Log();

					response.IdParty_01 = CxId.Empty;

					Singleton<ServiceUI>.I.Events.Enqueue(
						new CmdViewLobbyUserUpdate
						{
							Model = modelUser,
						});
				}
			}

			// delete: remove on game start and server lost

			// append
			{
				if(!response.IdParty_03.IsEmpty)
				{
					var modelUser = new ModelViewUser
					{
						IdUser = response.IdParty_03,
						IsReady = response.IsReady_03,
						IdFeature = response.IdFeature_03,
						IdCamera = CxId.Empty,
						FirstUpdated = DateTime.UtcNow,
						LastUpdated = DateTime.UtcNow,
						IdAtHost = response.IdHost,
					};

					Singleton<ServiceUI>.I.ModelsUser.Enqueue(modelUser);

					Singleton<ServiceUI>.I.ModelsUser.ToText($"append <b><color=white>(user)</color></b>: {modelUser}").Log();
				}
				if(!response.IdParty_02.IsEmpty)
				{
					var modelUser = new ModelViewUser
					{
						IdUser = response.IdParty_02,
						IsReady = response.IsReady_02,
						IdFeature = response.IdFeature_02,
						IdCamera = CxId.Empty,
						FirstUpdated = DateTime.UtcNow,
						LastUpdated = DateTime.UtcNow,
						IdAtHost = response.IdHost,
					};

					Singleton<ServiceUI>.I.ModelsUser.Enqueue(modelUser);

					Singleton<ServiceUI>.I.ModelsUser.ToText($"append <b><color=white>(user)</color></b>: {modelUser}").Log();
				}
				if(!response.IdParty_01.IsEmpty)
				{
					var modelUser = new ModelViewUser
					{
						IdUser = response.IdParty_01,
						IsReady = response.IsReady_01,
						IdFeature = response.IdFeature_01,
						IdCamera = CxId.Empty,
						FirstUpdated = DateTime.UtcNow,
						LastUpdated = DateTime.UtcNow,
						IdAtHost = response.IdHost,
					};

					Singleton<ServiceUI>.I.ModelsUser.Enqueue(modelUser);

					Singleton<ServiceUI>.I.ModelsUser.ToText($"append <b><color=white>(user)</color></b>: {modelUser}").Log();
				}
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
				new CmdViewLobbyClear());

			// append user self
			var modelUser = Singleton<ServiceUI>.I.ModelsUser.GetById(Singleton<ServiceNetwork>.I.IdCurrentUser);
			if(modelUser == null)
			{
				modelUser = new ModelViewUser
				{
					IdUser = Singleton<ServiceNetwork>.I.IdCurrentUser,
					IdAtHost = IdServerCurrent,
					IsReady = false,
					IdCamera = CxId.Empty,
					IdFeature = CxId.Empty,
					LastUpdated = DateTime.UtcNow,
					FirstUpdated = DateTime.UtcNow,
				};

				Singleton<ServiceUI>.I.ModelsUser.Enqueue(modelUser);

				Singleton<ServiceUI>.I.ModelsUser.ToText($"append <b><color=white>(user)</color></b>: {modelUser}").Log();
			}
			else
			{
				modelUser.IdAtHost = IdServerCurrent;

				Singleton<ServiceUI>.I.ModelsUser.ToText( $"update <b><color=white>(user)</color></b>: {modelUser}").Log();
			}

			Singleton<ServiceUI>.I.Events.Enqueue(
				new CmdViewLobbyUserAppend
				{
					Model = modelUser,
				});

			Singleton<ServicePawns>.I.Events.Enqueue(
				new CmdPawnLobbyCreate
				{
					Model = modelUser,
				});

			// append server self
			{
				var modeServer = Singleton<ServiceUI>.I.ModelsServer.GetById(Singleton<ServiceNetwork>.I.IdCurrentMachine);
				if(modeServer == null)
				{
					modeServer = new ModelViewServer
					{
						IdOwner = Singleton<ServiceNetwork>.I.IdCurrentUser,
						IdHost = IdServerCurrent,
						PlayersTotal = Singleton<ServiceUI>.I.ModelsUser.Count,
						LastUpdated = DateTime.UtcNow,
						HostIp = null,
						HostUri = null,
					};

					Singleton<ServiceUI>.I.ModelsServer.Enqueue(modeServer);

					Singleton<ServiceUI>.I.ModelsServer.ToText($"append <b><color=white>(server)</color></b>: {modeServer}").Log();
				}
				else
				{
					modeServer.IdOwner = Singleton<ServiceNetwork>.I.IdCurrentUser;
					modeServer.IdHost = IdServerCurrent;
					modeServer.PlayersTotal = Singleton<ServiceUI>.I.ModelsUser.Count;
					modeServer.LastUpdated = DateTime.UtcNow;

					Singleton<ServiceUI>.I.ModelsServer.ToText($"update <b><color=white>(server)</color></b>: {modeServer}").Log();
				}
			}

			Manager.OnStartHost();
			Discovery.AdvertiseServer();
		}

		public void Disable()
		{
			// sets states as a result

			Discovery.StopAdvertiseServer();
			Manager.StopHost();

			Discovery.OnUserFound = null;
			Discovery.OnUserUpdated = null;
			Discovery.OnUserLost = null;
		}

		private void OnUserFound(Request request, IPEndPoint epResponseFrom)
		{
			var modelUser = new ModelViewUser
			{
				IdUser = request.IdClientUser,
				IsReady = request.IsReady,
				IdCamera = CxId.Empty,
				IdFeature = Singleton<ServicePawns>.I.GetNextFeatureAvailable(),
				LastUpdated = DateTime.UtcNow,
				FirstUpdated = DateTime.UtcNow,
				IdAtHost = IdServerCurrent,
			};

			Singleton<ServiceUI>.I.ModelsUser.Enqueue(modelUser);

			Singleton<ServiceUI>.I.ModelsUser.ToText($"append <b><color=white>(user)</color></b>: {modelUser}").Log();

			Singleton<ServiceUI>.I.Events.Enqueue(
				new CmdViewLobbyUserAppend
				{
					Model = modelUser,
				});

			Singleton<ServicePawns>.I.Events.Enqueue(
				new CmdPawnLobbyCreate
				{
					Model = modelUser,
				});
		}

		private void OnUserUpdated(Request request)
		{
			var modelUser = Singleton<ServiceUI>.I.ModelsUser.GetById(request.IdClientUser);

			if(modelUser == null)
			{
				throw new Exception($"user model is not found: {request.IdClientUser}");
			}

			modelUser.IsReady = request.IsReady;

			Singleton<ServiceUI>.I.ModelsUser.ToText($"update <b><color=white>(user)</color></b>: {modelUser})").Log();

			Singleton<ServiceUI>.I.Events.Enqueue(
				new CmdViewLobbyUserUpdate
				{
					Model = modelUser,
				});
		}

		private void OnUserLost(CxId idUser)
		{
			var modelUser = Singleton<ServiceUI>.I.ModelsUser.RemoveById(idUser);

			Singleton<ServiceUI>.I.ModelsUser.ToText($"remove <b><color=white>(user)</color></b>: {modelUser})").Log();

			Singleton<ServiceUI>.I.Events.Enqueue(
				new CmdViewLobbyUserRemove
				{
					Model = modelUser,
				});

			Singleton<ServicePawns>.I.Events.Enqueue(
				new CmdPawnDestroy
				{
					Model = modelUser,
				});
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

			$"network mode start changing from: <color=#0071A9>{Target.GetType().NameNice()}</color>".Log();

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
				.FindAs(_ => _.IdUser == Singleton<ServiceNetwork>.I.IdCurrentUser)
				?.IdAtHost ??
			CxId.Empty;

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
