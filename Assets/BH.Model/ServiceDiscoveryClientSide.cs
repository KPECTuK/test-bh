using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Mirror;

namespace BH.Model
{
	public sealed class ServiceDiscoveryClientSide<TRequest, TResponse> : IService
		where TRequest : NetworkMessage
		where TResponse : NetworkMessage
	{
		// TODO: job implementation

		private class ThreadStateBroadcast
		{
			public UdpClient Udp;
			public IPEndPoint BroadcastEp;
			public long Handshake;
			public int PortServerResponseListen;
			public Func<TRequest> CallbackRequestFactory;
			public TimeSpan IntervalBroadcast;
		}

		private class ThreadStateListen
		{
			public UdpClient Udp;
			public int PortServerResponseListen;
			public long Handshake;
			public Action<TResponse, IPEndPoint> CallbackResponse;
		}

		private TimeSpan _intervalJoin = TimeSpan.FromSeconds(1.0);
		private readonly object _syncDispose = new();
		private bool _disposed;

		private UdpClient _udpListen;
		private Thread _threadListen;
		private UdpClient _udpBroadcast;
		private Thread _threadBroadcast;

		~ServiceDiscoveryClientSide()
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

		private void Disposing()
		{
			lock(_syncDispose)
			{
				Stop();
			}
		}

		public void Reset()
		{
			"initialized: discovery client".Log();
		}

		/// <param name="settings"></param>
		/// <param name="requestFactory">run in worker thread</param>
		/// <param name="callback">run in worker thread</param>
		public bool TryStart(
			ISettingsDiscovery settings,
			Func<TRequest> requestFactory,
			Action<TResponse, IPEndPoint> callback)
		{
			if(_disposed)
			{
				throw new ObjectDisposedException(GetType().Name);
			}

			if(_threadListen != null)
			{
				return true;
			}

			if(_threadBroadcast != null)
			{
				return true;
			}

			try
			{
				ExtensionsDiscovery.AssertPlatform();

				var stateBroadcast = new ThreadStateBroadcast
				{
					Handshake = settings.SecretHandshake,
					IntervalBroadcast = TimeSpan.FromSeconds(settings.ActiveDiscoveryInterval),
					CallbackRequestFactory = requestFactory,
					PortServerResponseListen = settings.PortServerResponseListen,
				};

				var stateListen = new ThreadStateListen
				{
					Handshake = settings.SecretHandshake,
					CallbackResponse = callback,
					PortServerResponseListen = settings.PortServerResponseListen,
				};

				_intervalJoin = stateBroadcast.IntervalBroadcast;

				try
				{
					var ip = IPAddress.Parse(settings.BroadcastAddress);
					var ep = new IPEndPoint(ip, settings.PortServerBroadcastListen);
					stateBroadcast.BroadcastEp = ep;
				}
				catch(FormatException exception)
				{
					throw new Exception("broadcast address is of illegal format", exception);
				}
				catch(ArgumentNullException)
				{
					var ep = new IPEndPoint(IPAddress.Broadcast, settings.PortServerBroadcastListen);
					stateBroadcast.BroadcastEp = ep;
				}

				"discovery trying to open 'client broadcast' socket on port: 0".Log();

				_udpBroadcast = new UdpClient(0)
				{
					//! the only socket which sends broadcasts
					EnableBroadcast = true,
					MulticastLoopback = false
				};
				stateBroadcast.Udp = _udpBroadcast;

				_threadBroadcast = new Thread(RunBroadcast)
				{
					Name = "service: discovery client broadcast",
					IsBackground = true,
					CurrentCulture = Thread.CurrentThread.CurrentCulture,
				};

				$"discovery trying to open 'client listen' ep on port: {stateListen.PortServerResponseListen}".Log();

				_udpListen = new UdpClient(stateListen.PortServerResponseListen)
				{
					EnableBroadcast = false,
					MulticastLoopback = false,
				};
				stateListen.Udp = _udpListen;

				_threadListen = new Thread(RunListen)
				{
					Name = "service: discovery client listen",
					IsBackground = true,
					CurrentCulture = Thread.CurrentThread.CurrentCulture,
				};

				var portActual = ((IPEndPoint)_udpListen.Client.LocalEndPoint).Port;
				stateListen.PortServerResponseListen = portActual;
				stateBroadcast.PortServerResponseListen = portActual;

				$"discovery client start to broadcast on port: {((IPEndPoint)_udpBroadcast.Client.LocalEndPoint).Port}".Log();
				$"discovery client start to listen on port: {stateListen.PortServerResponseListen}".Log();

				_threadListen.Start(stateListen);
				_threadBroadcast.Start(stateBroadcast);
			}
			catch(Exception exception)
			{
				exception.ToText().LogError();

				Stop();

				return false;
			}

			return true;
		}

		public void Stop()
		{
			if(_disposed)
			{
				throw new ObjectDisposedException(GetType().Name);
			}

			if(_udpBroadcast != null)
			{
				if(_udpBroadcast.Client is {Connected: true })
				{
					_udpBroadcast.Client.Shutdown(SocketShutdown.Both);
				}

				_udpBroadcast.Close();
				_udpBroadcast.Dispose();
			}

			_udpBroadcast = null;

			if(_udpListen != null)
			{
				if(_udpListen.Client is { Connected: true })
				{
					_udpListen.Client.Shutdown(SocketShutdown.Both);
				}

				_udpListen.Close();
				_udpListen.Dispose();
			}

			_udpListen = null;

			//! check
			if(!_threadBroadcast?.Join(_intervalJoin) ?? false)
			{
				_threadBroadcast.Abort();
			}

			if(!_threadListen?.Join(_intervalJoin) ?? false)
			{
				_threadListen.Abort();
			}

			_threadBroadcast = null;
			_threadListen = null;
		}

		// long living task, see link for details: https://docs.unity3d.com/2021.3/Documentation/Manual/overview-of-dot-net-in-unity.html
		private static void RunBroadcast(object state)
		{
			if(state is ThreadStateBroadcast cast)
			{
				NetworkWriterPooled writer = null;

				while(true)
				{
					try
					{
						writer = NetworkWriterPool.Get();
						writer.WriteLong(cast.Handshake);
						writer.WriteInt(cast.PortServerResponseListen);
						var request = cast.CallbackRequestFactory();
						writer.Write(request);
						var data = writer.ToArraySegment();

						writer.Dispose();
						writer = null;

						if(data.Array == null)
						{
							throw new Exception("send buffer is empty");
						}

						var sent = cast.Udp.Send(data.Array, data.Count, cast.BroadcastEp);

						if(sent != data.Count)
						{
							throw new Exception($"no all of bytes was sent: (got: {sent} awaiting: {data.Count})");
						}

						Thread.Sleep(cast.IntervalBroadcast);
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
						writer?.Dispose();
						writer = null;
					}
				}
			}

			$"thread complete: [{Thread.CurrentThread.Name}]".Log();
		}

		// long living task, see link for details: https://docs.unity3d.com/2021.3/Documentation/Manual/overview-of-dot-net-in-unity.html
		private static void RunListen(object state)
		{
			if(state is ThreadStateListen cast)
			{
				//var dnsName = Dns.GetHostName();
				//var dnsEntry = Dns.GetHostEntry(dnsName);
				//var ips = dnsEntry.AddressList;
				//ips.ToText("local interfaces found").Log();

				NetworkReaderPooled reader = null;

				while(true)
				{
					try
					{
						var ep = new IPEndPoint(IPAddress.Any, cast.PortServerResponseListen);
						var result = cast.Udp.Receive(ref ep);

						reader = NetworkReaderPool.Get(result);
						var header = reader.ReadLong();

						if(header != cast.Handshake)
						{
							throw new ProtocolViolationException($"invalid handshake: client (got: {header} awaiting: {cast.Handshake}) received from: {ep}");
						}

						// can be client listen port
						const int ID_PACKET_DISCOVERY_RESPONSE = 0;
						var idPacket = reader.ReadInt();
						if(idPacket != ID_PACKET_DISCOVERY_RESPONSE)
						{
							$"received from: {ep} - local address, skipping ..".Log();

							continue;
						}

						var response = reader.Read<TResponse>();
						reader.Dispose();
						reader = null;

						cast.CallbackResponse(response, ep);
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
					}
				}
			}

			$"thread complete: [{Thread.CurrentThread.Name}]".Log();
		}
	}
}
