using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using BH.Model;
using Mirror;
using Mirror.Discovery;
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

		public Action<Response, IPEndPoint, Uri> OnServerFound;
		public Action<Response> OnServerUpdated;
		public Action<CxId> OnServerLost;

		public Action<Request, IPEndPoint> OnUserFound;
		public Action<Request> OnUserUpdated;
		public Action<CxId> OnUserLost;

		private Transport _transport;

		public long SecretHandshake { get; private set; }
		public int ServerBroadcastListenPort { get; private set; }
		public float ActiveDiscoveryInterval { get; private set; }
		public string BroadcastAddress { get; private set; }

		private void Awake()
		{
			_transport = GetComponent<Transport>();

			SecretHandshake = 0L.Randomize();
			ServerBroadcastListenPort = 47777;
			ActiveDiscoveryInterval = 3f;
			BroadcastAddress = null;
		}

		private void Update()
		{
			{
				var queue = Singleton<ServiceUI>.I.ModelsServer;
				var size = queue.Count;
				for(var index = 0; index < size; index++)
				{
					var model = queue.Dequeue();
					queue.Enqueue(model);

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
				var queue = Singleton<ServiceUI>.I.ModelsUser;
				var size = queue.Count;
				for(var index = 0; index < size; index++)
				{
					var desc = queue.Dequeue();
					queue.Enqueue(desc);

					// update timeout for current user
					if(desc.IdUser == Singleton<ServiceNetwork>.I.IdCurrentUser)
					{
						desc.LastUpdated = DateTime.UtcNow;
						continue;
					}

					// interval + lag (roughly)
					if((DateTime.UtcNow - desc.LastUpdated).TotalSeconds > ActiveDiscoveryInterval * 2f)
					{
						new StringBuilder()
							.Append("(timeout) [")
							.Append(Thread.CurrentThread.ManagedThreadId)
							.Append("] <color=red>USER drop</color>: ")
							.Append(desc.IdUser.ShortForm())
							.Log();

						OnUserLost?.Invoke(desc.IdUser);
					}
				}
			}
		}

		public void StartDiscoveryServer()
		{
			if(Singleton<ServiceDiscoveryServerSide<Request, Response>>.I.TryStart(this, ProcessRequest))
			{
				"<color=green> discovery is not started: server</color>".LogWarning();
			}
			else
			{
				"<color=red> discovery is not started: server</color>".LogWarning();
			}
		}

		public void StopDiscoveryServer()
		{
			Singleton<ServiceDiscoveryServerSide<Request, Response>>.I.Stop();
			"<color=yellow> discovery stop: server</color>".LogWarning();
		}

		public void StartDiscoveryClient()
		{
			if(Singleton<ServiceDiscoveryClientSide<Request, Response>>.I.TryStart(this, BuildRequest, ProcessResponse))
			{
				"<color=green> discovery is not started: client</color>".LogWarning();
			}
			else
			{
				"<color=red> discovery is not started: server</color>".LogWarning();
			}
		}

		public void StopDiscoveryClient()
		{
			Singleton<ServiceDiscoveryClientSide<Request, Response>>.I.Stop();
			"<color=yellow> discovery stop: client</color>".LogWarning();
		}

		// client side
		private Request BuildRequest()
		{
			var model = Singleton<ServiceUI>.I.ModelsUser
				.GetById(Singleton<ServiceNetwork>.I.IdCurrentUser);
			return new Request
			{
				IdClientUser = model.IdUser,
				IsReady = model.IsReady,
			};
		}

		// server side
		private Response ProcessRequest(Request request, IPEndPoint epRequestFrom)
		{
			// update timeout by event
			var queue = Singleton<ServiceUI>.I.ModelsUser;
			var index = 0;
			for(var size = queue.Count; index < size; index++)
			{
				var model = queue.Dequeue();
				var isMatch = model.IdUser == request.IdClientUser;
				model.LastUpdated = isMatch ? DateTime.UtcNow : model.LastUpdated;
				queue.Enqueue(model);
				if(isMatch)
				{
					break;
				}
			}

			if(index == queue.Count)
			{
				new StringBuilder()
					.Append("Q (request) [")
					.Append(Thread.CurrentThread.ManagedThreadId)
					.Append("] <color=lime>USER found</color>: ")
					.Append(request.IdClientUser.ShortForm())
					.Log();

				OnUserFound?.Invoke(request, epRequestFrom);
			}
			else
			{
				new StringBuilder()
					.Append("Q (request) [")
					.Append(Thread.CurrentThread.ManagedThreadId)
					.Append("] <color=yellow>USER updated</color>: ")
					.Append(request.IdClientUser.ShortForm())
					.Log();

				OnUserUpdated?.Invoke(request);
			}

			// ReSharper disable once InconsistentNaming
			static string dump(ModelViewUser model)
			{
				return $"host: {model.IdAtHost.ShortForm()}; user: {model.IdUser.ShortForm()}";
			}

			// broken Mirror plugin
			var cache = new ModelViewUser[4];
			// new user has been appended already
			var users = Singleton<ServiceUI>.I.ModelsUser.GetRecent(
				cache,
				Singleton<ServiceNetwork>.I.IdCurrentMachine);

			cache.ToText($"responding with users: {users}", dump).Log();
			Singleton<ServiceUI>.I.ModelsUser
				.ToText($"of users total: {Singleton<ServiceUI>.I.ModelsUser.Count}", dump)
				.Log();

			var response = new Response
			{
				IdHost = Singleton<ServiceNetwork>.I.IdCurrentMachine,
				IdOwner = Singleton<ServiceNetwork>.I.IdCurrentUser,
				Uri = _transport == null ? null : _transport.ServerUri(),
				ServerUsersTotal = Singleton<ServiceUI>.I.ModelsUser.Count,
				ServerUsersReady = Singleton<ServiceUI>.I.ModelsUser.Count(_ => _.IsReady),
				IdFeature = Singleton<ServiceUI>.I.ModelsUser.GetById(request.IdClientUser).IdFeature,
				//
				IdParty_01 = users < 1 ? CxId.Empty : cache[1].IdUser,
				IdParty_02 = users < 2 ? CxId.Empty : cache[2].IdUser,
				IdParty_03 = users < 3 ? CxId.Empty : cache[3].IdUser,
				//
				IdFeature_01 = users < 1 ? CxId.Empty : cache[1].IdFeature,
				IdFeature_02 = users < 2 ? CxId.Empty : cache[2].IdFeature,
				IdFeature_03 = users < 3 ? CxId.Empty : cache[3].IdFeature,
				//
				IsReady_01 = users >= 1 && cache[1].IsReady,
				IsReady_02 = users >= 2 && cache[2].IsReady,
				IsReady_03 = users >= 3 && cache[3].IsReady,
			};

			return response;
		}

		// client side
		private void ProcessResponse(Response response, IPEndPoint epResponseFrom)
		{
			// update timeout by event
			var queue = Singleton<ServiceUI>.I.ModelsServer;
			var index = 0;
			for(var size = queue.Count; index < size; index++)
			{
				var model = queue.Dequeue();
				var isMatch = model.IdHost == response.IdHost;
				model.LastUpdated = isMatch ? DateTime.UtcNow : model.LastUpdated;
				queue.Enqueue(model);
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
		public CxId IdClientUser;
		public bool IsReady;
	}

	public struct Response : NetworkMessage
	{
		public CxId IdHost;
		public CxId IdOwner;
		public CxId IdFeature;
		public Uri Uri;
		public int ServerUsersTotal;
		public int ServerUsersReady;

		public CxId IdParty_01;
		public CxId IdParty_02;
		public CxId IdParty_03;

		public CxId IdFeature_01;
		public CxId IdFeature_02;
		public CxId IdFeature_03;

		public bool IsReady_01;
		public bool IsReady_02;
		public bool IsReady_03;
	}
}
