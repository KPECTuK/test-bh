using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using BH.Model;

namespace Mirror.Discovery
{
	public sealed class ServiceDiscoveryClientSide<TRequest, TResponse> : IDisposable
		where TRequest : NetworkMessage
		where TResponse : NetworkMessage
	{
		private class ThreadStateListen
		{
			public UdpClient Udp;
			public IPEndPoint BroadcastEp;
			public long Handshake;
			public Action<TResponse, IPEndPoint> CallbackResponse;
		}

		private class ThreadStateBroadcast
		{
			public UdpClient Udp;
			public IPEndPoint BroadcastEp;
			public long Handshake;
			public Func<TRequest> CallbackRequestFactory;
			public TimeSpan IntervalBroadcast;
		}

		private readonly TimeSpan _intervalJoin = TimeSpan.FromSeconds(1.0);
		private readonly object _sync = new();
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
			lock(_sync)
			{
				Stop();
			}
		}
		
		/// <param name="settings"></param>
		/// <param name="requestFactory">run in worker thread</param>
		/// <param name="callback">run in worker thread</param>
		public bool TryStart(ISettingsDiscovery settings, Func<TRequest> requestFactory, Action<TResponse, IPEndPoint> callback)
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

				var stateListen = new ThreadStateListen
				{
					CallbackResponse = callback,
					Handshake = settings.SecretHandshake,
				};
				var stateBroadcast = new ThreadStateBroadcast
				{
					Handshake = settings.SecretHandshake,
					CallbackRequestFactory = requestFactory,
					IntervalBroadcast = TimeSpan.FromSeconds(settings.ActiveDiscoveryInterval),
				};

				try
				{
					var ip = IPAddress.Parse(settings.BroadcastAddress);
					var ep = new IPEndPoint(ip, settings.ServerBroadcastListenPort);
					stateBroadcast.BroadcastEp = ep;
					stateListen.BroadcastEp = ep;
				}
				catch(FormatException exception)
				{
					throw new Exception("broadcast address is of illegal format", exception);
				}
				catch(ArgumentNullException)
				{
					var ep = new IPEndPoint(IPAddress.Broadcast, settings.ServerBroadcastListenPort);
					stateBroadcast.BroadcastEp = ep;
					stateListen.BroadcastEp = ep;
				}

				_udpListen = new UdpClient(stateListen.BroadcastEp.Port)
				{
					EnableBroadcast = true,
					MulticastLoopback = false
				};
				stateListen.Udp = _udpListen;

				_threadListen = new Thread(RunListen)
				{
					Name = "service: discovery client listen",
					IsBackground = true,
					CurrentCulture = Thread.CurrentThread.CurrentCulture,
				};

				_udpBroadcast = new UdpClient(0)
				{
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

			_udpBroadcast?.Dispose();
			_udpBroadcast = null;
			_udpListen?.Dispose();
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

		private static void RunBroadcast(object state)
		{
			if(state is ThreadStateBroadcast cast)
			{
				NetworkWriterPooled writer = null;

				while(true)
				{
					try
					{
						//! sync in pool
						writer = NetworkWriterPool.Get();
						writer.WriteLong(cast.Handshake);
						var request = cast.CallbackRequestFactory();
						writer.Write(request);
						var data = writer.ToArraySegment();
						writer.Dispose();
						writer = null;

						if(data.Array == null)
						{
							throw new ArgumentException("send buffer is empty");
						}

						var sent = cast.Udp.Send(data.Array, data.Count, cast.BroadcastEp);

						if(sent != data.Count)
						{
							throw new Exception("no all of bytes was sent");
						}

						Thread.Sleep(cast.IntervalBroadcast);
					}
					catch(SocketException exception)
					{
						exception.ToText().LogError();

						break;
					}
					catch(ObjectDisposedException)
					{
						$"socket closed: {Thread.CurrentThread.Name}".Log();

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

			$"thread complete: {Thread.CurrentThread.Name}".Log();
		}

		private static void RunListen(object state)
		{
			if(state is ThreadStateListen cast)
			{
				// while clientUpdClient to fix: 
				// https://github.com/vis2k/Mirror/pull/2908
				//
				// If, you cancel discovery the clientUdpClient is set to null.
				// However, nothing cancels ClientListenAsync. If we change the if(true)
				// to check if the client is null. You can properly cancel the discovery, 
				// and kill the listen thread.
				//
				// Prior to this fix, if you cancel the discovery search. It crashes the 
				// thread, and is super noisy in the output. As well as causes issues on 
				// the quest.
				NetworkReaderPooled reader = null;

				while(true)
				{
					try
					{
						// only proceed if there is available data in network buffer, or otherwise Receive() will block
						// average time for UdpClient.Available : 10 us

						var ep = new IPEndPoint(IPAddress.Any, 0);
						var result = cast.Udp.Receive(ref ep);

						//! sync in pool
						reader = NetworkReaderPool.Get(result);
						var handshake = reader.ReadLong();

						if(handshake != cast.Handshake)
						{
							throw new ProtocolViolationException("invalid handshake: client");
						}

						var response = reader.Read<TResponse>();
						reader.Dispose();
						reader = null;

						cast.CallbackResponse(response, ep);
					}
					catch(SocketException exception)
					{
						exception.ToText().LogError();

						break;
					}
					catch(ObjectDisposedException)
					{
						$"socket closed: {Thread.CurrentThread.Name}".Log();

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

			$"thread complete: {Thread.CurrentThread.Name}".Log();
		}
	}
}
