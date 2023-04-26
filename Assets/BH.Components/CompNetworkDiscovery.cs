using System;
using System.Net;
using System.Text;
using System.Threading;
using BH.Model;
using Mirror;
using UnityEngine;

namespace BH.Components
{
	public sealed class CompNetworkDiscovery : MonoBehaviour, ISettingsDiscovery
	{
		// any server is allowed to response, no ProcessClientRequest()

		// cannot call OnFrameUpdate()\OnIntervalUpdate():
		// discovery is a part of algorithm where callback sequence is undefined
		// discovery is a persistent service app lifetime (game and lobby)
		// call Update() means in time (undefined), call callback means any time (undefined)

		public Action<ResponseServer, IPEndPoint, Uri> OnServerFound;
		public Action<ResponseServer> OnServerUpdated;
		public Action<CxId> OnServerLost;

		public Action<Request, IPEndPoint> OnUserFound;
		public Action<Request> OnUserUpdated;
		public Action<CxId> OnUserLost;

		private Transport _transport;

		public long SecretHandshake { get; private set; }
		public int PortServerBroadcastListen { get; private set; }
		public int PortServerResponseListen { get; private set; }
		public float ActiveDiscoveryInterval { get; private set; }
		public string BroadcastAddress { get; private set; }

		private void Awake()
		{
			_transport = GetComponent<Transport>();

			Writer<Request>.write = Request.Writer;
			Reader<Request>.read = Request.Reader;
			Writer<ResponseServer>.write = ResponseServer.Writer;
			Reader<ResponseServer>.read = ResponseServer.Reader;
			Writer<CxId>.write = CxId.Writer;
			Reader<CxId>.read = CxId.Reader;
			Writer<ResponseUser>.write = ResponseUser.Writer;
			Reader<ResponseUser>.read = ResponseUser.Reader;

			SecretHandshake = -1;
			PortServerBroadcastListen = 47777;
			PortServerResponseListen = 0;
			ActiveDiscoveryInterval = 1f;
			BroadcastAddress = null;
		}

		private void Update()
		{
			{
				var list = Singleton<ServiceUI>.I.ModelsServer;
				var size = list.Count;
				for(var index = 0; index < size; index++)
				{
					ref var model = ref list[index];

					// update timeout for current machine
					if(model.IdHost == Singleton<ServiceNetwork>.I.IdCurrentMachine)
					{
						model.LastUpdated = DateTime.UtcNow;
						continue;
					}

					// interval + lag (roughly)
					if((DateTime.UtcNow - model.LastUpdated).TotalSeconds > ActiveDiscoveryInterval * 2f)
					{
						new StringBuilder()
							.Append("(timeout) [")
							.Append(Thread.CurrentThread.ManagedThreadId)
							.Append("] <color=red>SERVER drop</color>: ")
							.Append(model.IdHost.ShortForm())
							.Log();

						OnServerLost?.Invoke(model.IdHost);
					}
				}
			}

			{
				var list = Singleton<ServiceUI>.I.ModelsUser;
				var size = list.Count;
				for(var index = 0; index < size; index++)
				{
					ref var model = ref list[index];

					// update timeout for current user
					if(model.IdUser == Singleton<ServiceNetwork>.I.IdCurrentUser)
					{
						model.LastUpdated = DateTime.UtcNow;
						continue;
					}

					// interval + lag (roughly)
					if((DateTime.UtcNow - model.LastUpdated).TotalSeconds > ActiveDiscoveryInterval * 2f)
					{
						new StringBuilder()
							.Append("(timeout) [")
							.Append(Thread.CurrentThread.ManagedThreadId)
							.Append("] <color=red>USER drop</color>: ")
							.Append(model.IdUser.ShortForm())
							.Log();

						OnUserLost?.Invoke(model.IdUser);
					}
				}
			}
		}

		public void StartDiscoveryServer()
		{
			if(Singleton<ServiceDiscoveryServerSide<Request, ResponseServer>>.I.TryStart(this, ProcessRequest))
			{
				"<color=lime>discovery is started: server</color>".Log();
			}
			else
			{
				"<color=red>discovery is not started: server</color>".LogWarning();
			}
		}

		public void StopDiscoveryServer()
		{
			Singleton<ServiceDiscoveryServerSide<Request, ResponseServer>>.I.Stop();
			"<color=yellow>discovery is stopped: server</color>".Log();
		}

		public void StartDiscoveryClient()
		{
			if(Singleton<ServiceDiscoveryClientSide<Request, ResponseServer>>.I.TryStart(this, BuildRequest, ProcessResponse))
			{
				"<color=lime>discovery is started: client</color>".Log();
			}
			else
			{
				"<color=red>discovery is not started: client</color>".LogWarning();
			}
		}

		public void StopDiscoveryClient()
		{
			Singleton<ServiceDiscoveryClientSide<Request, ResponseServer>>.I.Stop();
			"<color=yellow>discovery is stopped: client</color>".Log();
		}

		// client side
		private Request BuildRequest()
		{
			var idUser = Singleton<ServiceNetwork>.I.IdCurrentUser;
			ref var modelUser = ref Singleton<ServiceUI>.I.ModelsUser.Get(idUser, out var contains);
			if(!contains)
			{
				throw new Exception("can't find local user");
			}

			return new Request
			{
				IdUser = idUser,
				IdClientMachine = Singleton<ServiceNetwork>.I.IdCurrentMachine,
				IsReady = modelUser.IsReady,
			};
		}

		// server side
		private ResponseServer ProcessRequest(Request request, IPEndPoint epRequestFrom)
		{
			// update timeout by event
			var list = Singleton<ServiceUI>.I.ModelsUser;
			var index = 0;
			for(var size = list.Count; index < size; index++)
			{
				ref var model = ref list[index];
				var isMatch = model.IdUser == request.IdUser;
				model.LastUpdated = isMatch ? DateTime.UtcNow : model.LastUpdated;
				if(isMatch)
				{
					break;
				}
			}

			if(index == list.Count)
			{
				new StringBuilder()
					.Append("Q (request) [")
					.Append(Thread.CurrentThread.ManagedThreadId)
					.Append("] <color=lime>USER found</color>: ")
					.Append(request.IdUser.ShortForm())
					.Log();

				OnUserFound?.Invoke(request, epRequestFrom);
			}
			else
			{
				new StringBuilder()
					.Append("Q (request) [")
					.Append(Thread.CurrentThread.ManagedThreadId)
					.Append("] <color=yellow>USER updated</color>: ")
					.Append(request.IdUser.ShortForm())
					.Log();

				OnUserUpdated?.Invoke(request);
			}

			// ReSharper disable once InconsistentNaming
			static string dump(ModelViewUser model)
			{
				return $"host: {model.IdHostAt.ShortForm()}; user: {model.IdUser.ShortForm()}; feature: {model.IdFeature.ShortForm()}";
			}

			var cacheIds = new CxId[4];
			var users = Singleton<ServiceUI>.I.ModelsUser.GetRecentForHost(
				cacheIds,
				Singleton<ServiceNetwork>.I.IdCurrentMachine);

			var cacheModels = new ModelViewUser[users];
			for(var indexIds = 0; indexIds < users; indexIds++)
			{
				cacheModels[indexIds] = Singleton<ServiceUI>.I.ModelsUser.Get(cacheIds[indexIds], out var contains);
			}

			cacheModels.ToText($"responding with users: {users}", dump).Log();
			Singleton<ServiceUI>.I.ModelsUser
				.ToText($"of users total: {Singleton<ServiceUI>.I.ModelsUser.Count}", dump)
				.Log();

			// ReSharper disable once UseObjectOrCollectionInitializer
			var response = new ResponseServer();
			response.IdHost = Singleton<ServiceNetwork>.I.IdCurrentMachine;
			response.Uri = _transport == null ? null : _transport.ServerUri();
			response.ServerUsersTotal = Singleton<ServiceUI>.I.ModelsUser.Count;

			response.Owner.IdUser = users < 1 ? CxId.Empty : cacheModels[0].IdUser;
			response.Owner.IdFeature = users < 1 ? CxId.Empty : cacheModels[0].IdFeature;
			response.Owner.IsReady = users >= 1 && cacheModels[1].IsReady;

			if(response.Owner.IdUser != Singleton<ServiceNetwork>.I.IdCurrentUser)
			{
				throw new Exception($"current user is not an owner: (current: {response.Owner.IdUser} expected: {Singleton<ServiceNetwork>.I.IdCurrentUser})");
			}

			response.Party_01.IdUser = users < 2 ? CxId.Empty : cacheModels[1].IdUser;
			response.Party_01.IdFeature = users < 2 ? CxId.Empty : cacheModels[1].IdFeature;
			response.Party_01.IsReady = users >= 2 && cacheModels[1].IsReady;

			response.Party_02.IdUser = users < 3 ? CxId.Empty : cacheModels[2].IdUser;
			response.Party_02.IdFeature = users < 3 ? CxId.Empty : cacheModels[2].IdFeature;
			response.Party_02.IsReady = users >= 3 && cacheModels[2].IsReady;

			response.Party_03.IdUser = users < 4 ? CxId.Empty : cacheModels[3].IdUser;
			response.Party_03.IdFeature = users < 4 ? CxId.Empty : cacheModels[3].IdFeature;
			response.Party_03.IsReady = users >= 4 && cacheModels[3].IsReady;

			return response;
		}

		// client side
		private void ProcessResponse(ResponseServer response, IPEndPoint epResponseFrom)
		{
			// update timeout by event
			var queue = Singleton<ServiceUI>.I.ModelsServer;
			var index = 0;
			for(var size = queue.Count; index < size; index++)
			{
				ref var model = ref queue[index];
				var isMatch = model.IdHost == response.IdHost;
				model.LastUpdated = isMatch ? DateTime.UtcNow : model.LastUpdated;
				if(isMatch)
				{
					break;
				}
			}

			var ipText = epResponseFrom.Address.ToString();
			var builder = new UriBuilder(response.Uri) { Host = ipText };

			if(index == queue.Count)
			{
				new StringBuilder()
					.Append("R (response) [")
					.Append(Thread.CurrentThread.ManagedThreadId)
					.Append("] <color=lime>SERVER found</color>: ")
					.Append(response.IdHost.ShortForm())
					.Log();

				OnServerFound?.Invoke(response, epResponseFrom, builder.Uri);
			}
			else
			{
				new StringBuilder()
					.Append("Q (response) [")
					.Append(Thread.CurrentThread.ManagedThreadId)
					.Append("] <color=yellow>SERVER updated</color>: ")
					.Append(response.IdHost.ShortForm())
					.Log();

				OnServerUpdated?.Invoke(response);
			}
		}
	}

	public struct Request : NetworkMessage
	{
		public CxId IdUser;
		public CxId IdClientMachine;
		public bool IsReady;

		public static void Writer(NetworkWriter writer, Request source)
		{
			writer.Write(source.IdUser);
			writer.Write(source.IdClientMachine);
			writer.Write(source.IsReady);
		}

		public static Request Reader(NetworkReader reader)
		{
			var result = new Request
			{
				IdUser = reader.Read<CxId>(),
				IdClientMachine = reader.Read<CxId>(),
				IsReady = reader.ReadBool(),
			};
			return result;
		}
	}

	public struct ResponseServer : NetworkMessage
	{
		public CxId IdHost;
		public Uri Uri;
		public int ServerUsersTotal;

		public ResponseUser Owner;
		public ResponseUser Party_01;
		public ResponseUser Party_02;
		public ResponseUser Party_03;

		public static void Writer(NetworkWriter writer, ResponseServer source)
		{
			writer.Write(source.IdHost);
			writer.Write(source.Uri);
			writer.Write(source.ServerUsersTotal);
			// 
			writer.Write(source.Owner);
			writer.Write(source.Party_01);
			writer.Write(source.Party_02);
			writer.Write(source.Party_03);
		}

		public static ResponseServer Reader(NetworkReader reader)
		{
			var result = new ResponseServer
			{
				IdHost = reader.Read<CxId>(),
				Uri = reader.ReadUri(),
				ServerUsersTotal = reader.ReadInt(),
				//
				Owner = reader.Read<ResponseUser>(),
				Party_01 = reader.Read<ResponseUser>(),
				Party_02 = reader.Read<ResponseUser>(),
				Party_03 = reader.Read<ResponseUser>(),
			};
			return result;
		}
	}

	public struct ResponseUser
	{
		public CxId IdUser;
		public CxId IdFeature;
		public bool IsReady;

		public static void Writer(NetworkWriter writer, ResponseUser source)
		{
			writer.Write(source.IdUser);
			writer.Write(source.IdFeature);
			writer.Write(source.IsReady);
		}

		public static ResponseUser Reader(NetworkReader reader)
		{
			var result = new ResponseUser
			{
				IdUser = reader.Read<CxId>(),
				IdFeature = reader.Read<CxId>(),
				IsReady = reader.ReadBool(),
			};
			return result;
		}
	}
}
