using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Mirror;

namespace BH.Model
{
	/// <summary>
	/// listens client broadcasts, occupies port as a service ep
	/// </summary>
	public sealed class ServiceDiscoveryServerSide<TRequest, TResponse> : IService
		where TRequest : NetworkMessage
		where TResponse : NetworkMessage
	{
		// TODO: job implementation

		private class ThreadState
		{
			public long Handshake;
			public UdpClient Udp;
			public int PortServerResponseListen;
			public Func<TRequest, IPEndPoint, TResponse> CallbackResponseBuilder;
		}

		private readonly TimeSpan _intervalJoin = TimeSpan.FromSeconds(1.0);
		private readonly object _syncDispose = new();
		private bool _disposed;

		private int _portServer = -1;
		private volatile UdpClient _udp;
		private MultiCatsLock _lock;
		private Thread _thread;

		private volatile bool _isEnabled;

		/// <summary>
		/// deprecated: TODO: remove.. to store that flag in component is actually the same 
		/// </summary>
		public bool IsEnabled => _isEnabled;

		~ServiceDiscoveryServerSide()
		{
			Disposing();
			_disposed = true;
		}

		public void Dispose()
		{
			Disposing();
			GC.SuppressFinalize(this);
			_disposed = true;
		}

		public void Reset()
		{
			"initialized: discovery server".Log();
		}

		private void Disposing()
		{
			lock(_syncDispose)
			{
				Stop();
			}
		}

		/// <param name="settings"></param>
		/// <param name="callback">run in worker thread</param>
		public bool TryStart(
			ISettingsDiscovery settings,
			Func<TRequest, IPEndPoint, TResponse> callback)
		{
			if(_disposed)
			{
				throw new ObjectDisposedException(GetType().Name);
			}

			if(_thread != null)
			{
				if(_portServer == settings.PortServerBroadcastListen)
				{
					return true;
				}

				Stop();
			}

			try
			{
				ExtensionsDiscovery.AssertPlatform();

				_lock = new MultiCatsLock();

				$"discovery trying to open 'server listen' ep on port: {settings.PortServerBroadcastListen}".Log();

				_udp = new UdpClient(settings.PortServerBroadcastListen)
				{
					EnableBroadcast = false,
					MulticastLoopback = false,
				};

				var state = new ThreadState
				{
					Udp = _udp,
					Handshake = settings.SecretHandshake,
					CallbackResponseBuilder = callback,
					PortServerResponseListen = settings.PortServerResponseListen,
				};

				_thread = new Thread(Run)
				{
					Name = "service: discovery server",
					IsBackground = true,
					CurrentCulture = Thread.CurrentThread.CurrentCulture,
				};

				var portActual = ((IPEndPoint)_udp.Client.LocalEndPoint).Port;
				$"discovery server start to listen on port: {portActual}".Log();

				_thread.Start(state);
			}
			catch(Exception exception)
			{
				exception.ToText().LogError();

				Stop();

				return false;
			}

			_isEnabled = true;

			return true;
		}

		public void Stop()
		{
			if(_disposed)
			{
				throw new ObjectDisposedException(GetType().Name);
			}

			if(_udp != null)
			{
				if(_udp.Client is { Connected: true })
				{
					_udp.Client.Shutdown(SocketShutdown.Both);
				}

				_udp.Close();
				_udp.Dispose();
			}

			_lock?.Dispose();
			_lock = null;

			_isEnabled = false;
			_portServer = -1;

			//! check
			if(!_thread?.Join(_intervalJoin) ?? false)
			{
				_thread.Abort();
			}

			_thread = null;
		}

		// long living task, see link for details: https://docs.unity3d.com/2021.3/Documentation/Manual/overview-of-dot-net-in-unity.html
		private static void Run(object state)
		{
			if(state is ThreadState cast)
			{
				NetworkReaderPooled reader = null;
				NetworkWriterPooled writer = null;

				while(true)
				{
					try
					{
						//? use packet header as a struct

						var ep = new IPEndPoint(IPAddress.Broadcast, cast.PortServerResponseListen);
						var result = cast.Udp.Receive(ref ep);

						reader = NetworkReaderPool.Get(result);

						var header = reader.ReadLong();
						if(header != cast.Handshake)
						{
							throw new ProtocolViolationException($"invalid handshake: server (got: {header} awaiting: {cast.Handshake}) received from: {ep}");
						}
						var port = reader.ReadInt();

						var request = reader.Read<TRequest>();
						reader.Dispose();
						reader = null;

						var epRemote = new IPEndPoint(ep.Address, port);
						var response = cast.CallbackResponseBuilder.Invoke(request, epRemote);

						writer = NetworkWriterPool.Get();
						writer.WriteLong(header);
						// can be client listen port
						const int ID_PACKET_DISCOVERY_RESPONSE = 0;
						writer.WriteInt(ID_PACKET_DISCOVERY_RESPONSE);
						writer.Write(response);
						var data = writer.ToArraySegment();
						writer.Dispose();
						writer = null;

						if(data.Array == null)
						{
							throw new ArgumentException("send buffer is empty");
						}

						cast.Udp.Send(data.Array, data.Count, epRemote);
					}
					catch(SocketException exception)
					{
						// https://learn.microsoft.com/ru-ru/windows/win32/winsock/windows-sockets-error-codes-2
						// socket cancellation is a primary cause of interruption, no object dispose will be called
						exception.ToText().LogError();

						break;
					}
					catch(ObjectDisposedException)
					{
						$"socket closed: [{Thread.CurrentThread.Name}]".Log();

						break;
					}
					catch(Exception exception)
					{
						exception.ToText().LogError();
					}
					finally
					{
						reader?.Dispose();
						reader = null;

						writer?.Dispose();
						writer = null;
					}
				}
			}

			$"thread complete: [{Thread.CurrentThread.Name}]".Log();
		}
	}
}
