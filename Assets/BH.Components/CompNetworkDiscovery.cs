using System;
using System.Collections.Generic;
using System.Linq;
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

		//! TODO: synchronize collections assess ModelsUser\ModelsServer
		//! TODO: ip tables by machine id
		//! TODO: implement flood attack prevention at server part

		private sealed class Info
		{
			public IPEndPoint Ep;
			public DateTime Updated;
			public CxId IdUser;
		}

		// contextual on server\client
		private readonly Dictionary<CxId, Info> _info = new();
		private readonly object _stateLock = new();
		// synchronized block start (custom synchronization context)
		private Request _requestCurrent;
		private Response _responseCurrent;
		private readonly Queue<(Request request, IPEndPoint ep)> _eventsRequest = new();
		private readonly Queue<(Response response, IPEndPoint ep)> _eventsResponse = new();
		// synchronized block end

		public Action<Response> OnServerFound;
		public Action<Response> OnServerUpdated;
		public Action<CxId> OnServerLost;

		public Action<Request> OnUserFound;
		public Action<Request> OnUserUpdated;
		public Action<CxId> OnUserLost;

		public long SecretHandshake { get; private set; }
		public int PortServerBroadcastListen { get; private set; }
		public int PortServerResponseListen { get; private set; }
		public float ActiveDiscoveryInterval { get; private set; }
		public string BroadcastAddress { get; private set; }

		public bool StartDiscoveryServer()
		{
			if(Singleton<ServiceNetwork>.I.NetworkModeShared is NetworkModeLobbyServer cast)
			{
				_responseCurrent = cast.BuildState();

				if(Singleton<ServiceDiscoveryServerSide<Request, Response>>.I.TryStart(this, OnServerRequestReceived))
				{
					"<color=lime>discovery is started: server</color>".Log();

					return true;
				}

				"<color=red>discovery does not start: service error</color>".LogWarning();
			}
			else
			{
				"<color=red>discovery does not start: cannot start with inappropriate handler</color>".LogError();
			}

			return false;
		}

		public void StopDiscoveryServer()
		{
			Singleton<ServiceDiscoveryServerSide<Request, Response>>.I.Stop();

			_eventsRequest.Clear();
			_eventsResponse.Clear();
			_info.Clear();

			"<color=yellow>discovery is stopped: server</color>".Log();
		}

		public bool StartDiscoveryClient()
		{
			if(Singleton<ServiceNetwork>.I.NetworkModeShared is NetworkModeLobbyClient cast)
			{
				_requestCurrent = cast.BuildState();

				if(Singleton<ServiceDiscoveryClientSide<Request, Response>>.I.TryStart(this, OnClientRequestRequired, OnClientResponseReceived))
				{
					"<color=lime>discovery is started: client</color>".Log();

					return true;
				}

				"<color=red>discovery does not start: service error</color>".LogWarning();
			}
			else
			{
				"<color=red>discovery does not start: cannot start with inappropriate handler</color>".LogError();
			}

			return false;
		}

		public void StopDiscoveryClient()
		{
			Singleton<ServiceDiscoveryClientSide<Request, Response>>.I.Stop();

			_eventsRequest.Clear();
			_eventsResponse.Clear();
			_info.Clear();

			"<color=yellow>discovery is stopped: client</color>".Log();
		}

		private void Awake()
		{
			Writer<Request>.write = Request.Writer;
			Reader<Request>.read = Request.Reader;
			Writer<Response>.write = Response.Writer;
			Reader<Response>.read = Response.Reader;
			Writer<CxId>.write = CxId.Writer;
			Reader<CxId>.read = CxId.Reader;
			Writer<DataUser>.write = DataUser.Writer;
			Reader<DataUser>.read = DataUser.Reader;

			SecretHandshake = -1;
			PortServerBroadcastListen = 47777;
			PortServerResponseListen = 0;
			ActiveDiscoveryInterval = 1f;
			BroadcastAddress = null;
		}

		private void Update()
		{
			// over, per server\user:
			// update collections from queues including timeouts + cleanup timeouts
			// if any operation had performed, rebuild responses

			// TODO: log statistics

			//? direct call
			var isEnabledServer = Singleton<ServiceDiscoveryServerSide<Request, Response>>.I.IsEnabled;
			if(isEnabledServer && Singleton<ServiceNetwork>.I.NetworkModeShared is NetworkModeLobbyServer castServer)
			{
				if(Monitor.TryEnter(_stateLock))
				{
					try
					{
						var updatesCount = _eventsRequest.Count;
						while(_eventsRequest.TryDequeue(out var @event))
						{
							if(_info.TryGetValue(@event.request.IdClientMachine, out var info))
							{
								info.Ep = @event.ep;
								info.Updated = DateTime.UtcNow;
								//info.IdUser = @event.request.Owner.IdUser;

								new StringBuilder()
									.Append("Q (request) [")
									.Append(Thread.CurrentThread.ManagedThreadId)
									.Append("] <color=yellow>USER updated</color>: ")
									.Append(@event.request.Owner.IdUser.ShortForm())
									.Log();

								OnUserUpdated?.Invoke(@event.request);
							}
							else
							{
								_info.Add(@event.request.IdClientMachine,
									new Info
									{
										Ep = @event.ep,
										IdUser = @event.request.Owner.IdUser,
										Updated = DateTime.UtcNow,
									});

								new StringBuilder()
									.Append("Q (request) [")
									.Append(Thread.CurrentThread.ManagedThreadId)
									.Append("] <color=lime>USER found</color>: ")
									.Append(@event.request.Owner.IdUser.ShortForm())
									.Log();

								OnUserFound?.Invoke(@event.request);
							}
						}

						// all the client machines on server side (only)
						var keys = _info
							.Where(_ => (DateTime.UtcNow - _.Value.Updated).TotalSeconds > ActiveDiscoveryInterval * 2f)
							.ToArray();

						foreach(var key in keys)
						{
							new StringBuilder()
								.Append("(timeout) [")
								.Append(Thread.CurrentThread.ManagedThreadId)
								.Append("] <color=red>USER drop</color>: ")
								.Append(key.Key.ShortForm())
								.Log();

							_info.Remove(key.Key, out var info);
							OnUserLost?.Invoke(info.IdUser);
							updatesCount++;
						}

						if(updatesCount > 0)
						{
							_responseCurrent = castServer.BuildState();
						}
					}
					finally
					{
						Monitor.Exit(_stateLock);
					}
				}
			}

			//? direct call
			var isEnabledClient = Singleton<ServiceDiscoveryClientSide<Request, Response>>.I.IsEnabled;
			if(isEnabledClient && Singleton<ServiceNetwork>.I.NetworkModeShared is NetworkModeLobbyClient castClient)
			{
				if(Monitor.TryEnter(_stateLock))
				{
					try
					{
						var updatesCount = _eventsResponse.Count;
						while(_eventsResponse.TryDequeue(out var @event))
						{
							// from original source
							//var ipText = epResponseFrom.Address.ToString();
							//var builder = new UriBuilder(response.Uri) { Host = ipText };

							if(_info.TryGetValue(@event.response.IdHost, out var info))
							{
								info.Ep = @event.ep;
								info.Updated = DateTime.UtcNow;

								new StringBuilder()
									.Append("Q (response) [")
									.Append(Thread.CurrentThread.ManagedThreadId)
									.Append("] <color=yellow>SERVER updated</color>: ")
									.Append(@event.response.IdHost.ShortForm())
									.Log();

								OnServerUpdated?.Invoke(@event.response);
							}
							else
							{
								_info.Add(@event.response.IdHost,
									new Info
									{
										Ep = @event.ep,
										Updated = DateTime.UtcNow,
									});

								new StringBuilder()
									.Append("R (response) [")
									.Append(Thread.CurrentThread.ManagedThreadId)
									.Append("] <color=lime>SERVER found</color>: ")
									.Append(@event.response.IdHost.ShortForm())
									.Log();

								OnServerFound?.Invoke(@event.response);
							}
						}

						// all the server machines on client side (only)
						var keys = _info
							.Where(_ => (DateTime.UtcNow - _.Value.Updated).TotalSeconds > ActiveDiscoveryInterval * 2f)
							.ToArray();

						foreach(var key in keys)
						{
							new StringBuilder()
								.Append("(timeout) [")
								.Append(Thread.CurrentThread.ManagedThreadId)
								.Append("] <color=red>SERVER drop</color>: ")
								.Append(key.Key.ShortForm())
								.Log();

							_info.Remove(key.Key);
							OnServerLost?.Invoke(key.Key);
							updatesCount++;
						}

						if(updatesCount > 0)
						{
							_requestCurrent = castClient.BuildState();
						}
					}
					finally
					{
						Monitor.Exit(_stateLock);
					}
				}
			}
		}

		// client side
		private Request OnClientRequestRequired()
		{
			lock(_stateLock)
			{
				return _requestCurrent;
			}
		}

		// server side
		private Response OnServerRequestReceived(Request request, IPEndPoint epRequestFrom)
		{
			lock(_stateLock)
			{
				_eventsRequest.Enqueue((request, epRequestFrom));

				return _responseCurrent;
			}
		}

		// client side
		private void OnClientResponseReceived(Response response, IPEndPoint epResponseFrom)
		{
			lock(_stateLock)
			{
				_eventsResponse.Enqueue((response, epResponseFrom));
			}
		}
	}

	public struct Request : NetworkMessage
	{
		public CxId IdClientMachine;
		public DataUser Owner;

		public static void Writer(NetworkWriter writer, Request source)
		{
			writer.Write(source.IdClientMachine);
			writer.Write(source.Owner);
		}

		public static Request Reader(NetworkReader reader)
		{
			var result = new Request
			{
				IdClientMachine = reader.Read<CxId>(),
				Owner = reader.Read<DataUser>(),
			};

			return result;
		}
	}

	public struct Response : NetworkMessage
	{
		//? Uri might be used in iOS

		public CxId IdHost;
		public int ServerUsersTotal;

		public DataUser Owner;
		public DataUser Party_01;
		public DataUser Party_02;
		public DataUser Party_03;

		public static void Writer(NetworkWriter writer, Response source)
		{
			writer.Write(source.IdHost);
			writer.Write(source.ServerUsersTotal);
			// 
			writer.Write(source.Owner);
			writer.Write(source.Party_01);
			writer.Write(source.Party_02);
			writer.Write(source.Party_03);
		}

		public static Response Reader(NetworkReader reader)
		{
			var result = new Response
			{
				IdHost = reader.Read<CxId>(),
				ServerUsersTotal = reader.ReadInt(),
				//
				Owner = reader.Read<DataUser>(),
				Party_01 = reader.Read<DataUser>(),
				Party_02 = reader.Read<DataUser>(),
				Party_03 = reader.Read<DataUser>(),
			};
			return result;
		}
	}

	public struct DataUser
	{
		public CxId IdUser;
		public CxId IdFeature;
		public CxId IdHostAt;
		public bool IsReady;

		public static void Writer(NetworkWriter writer, DataUser source)
		{
			writer.Write(source.IdUser);
			writer.Write(source.IdFeature);
			writer.Write(source.IdHostAt);
			writer.Write(source.IsReady);
		}

		public static DataUser Reader(NetworkReader reader)
		{
			var result = new DataUser
			{
				IdUser = reader.Read<CxId>(),
				IdFeature = reader.Read<CxId>(),
				IdHostAt = reader.Read<CxId>(),
				IsReady = reader.ReadBool(),
			};
			return result;
		}
	}
}
