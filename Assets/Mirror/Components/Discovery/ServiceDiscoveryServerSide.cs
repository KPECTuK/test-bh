using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using BH.Model;

namespace Mirror.Discovery
{
	/// <summary>
	/// listens client broadcasts, occupies port as a service ep
	/// </summary>
	public sealed class ServiceDiscoveryServerSide<TRequest, TResponse> : IDisposable
		where TRequest : NetworkMessage
		where TResponse : NetworkMessage
	{
		private class ThreadState
		{
			public long Handshake;
			public UdpClient Udp;
			public Func<TRequest, IPEndPoint, TResponse> CallbackResponseBuilder;
		}

		private readonly TimeSpan _intervalJoin = TimeSpan.FromSeconds(1.0);
		private readonly object _sync = new();
		private bool _disposed;

		private int _epPort = -1;
		private volatile UdpClient _udp;
		private MultiCatsLock _lock;
		private Thread _thread;

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

		private void Disposing()
		{
			lock(_sync)
			{
				Stop();
			}
		}

		/// <param name="settings"></param>
		/// <param name="callback">run in worker thread</param>
		public bool TryStart(ISettingsDiscovery settings, Func<TRequest, IPEndPoint, TResponse> callback)
		{
			if(_disposed)
			{
				throw new ObjectDisposedException(GetType().Name);
			}

			if(_thread != null)
			{
				if(_epPort == settings.ServerBroadcastListenPort)
				{
					return true;
				}

				Stop();
			}

			try
			{
				ExtensionsDiscovery.AssertPlatform();

				_lock = new MultiCatsLock();

				_udp = new UdpClient(settings.ServerBroadcastListenPort)
				{
					EnableBroadcast = true,
					MulticastLoopback = false,
				};

				var state = new ThreadState
				{
					Udp = _udp,
					Handshake = settings.SecretHandshake,
					CallbackResponseBuilder = callback,
				};

				_thread = new Thread(Run)
				{
					Name = "service: discovery server",
					IsBackground = true,
					CurrentCulture = Thread.CurrentThread.CurrentCulture,
				};

				_thread.Start(state);
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

			_udp?.Dispose();
			_udp = null;
			_lock?.Dispose();
			_lock = null;

			_epPort = -1;

			//! check
			if(!_thread?.Join(_intervalJoin) ?? false)
			{
				_thread.Abort();
			}

			_thread = null;
		}

		// long live task, see link for details: https://docs.unity3d.com/2021.3/Documentation/Manual/overview-of-dot-net-in-unity.html
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
						var ep = new IPEndPoint(IPAddress.Any, 0);
						var result = cast.Udp.Receive(ref ep);

						//! sync in pool
						reader = NetworkReaderPool.Get(result);

						var header = reader.ReadLong();

						if(header != cast.Handshake)
						{
							throw new ProtocolViolationException("invalid handshake: server");
						}

						var request = reader.Read<TRequest>();
						reader.Dispose();
						reader = null;

						var response = cast.CallbackResponseBuilder.Invoke(request, ep);

						//! sync in pool
						writer = NetworkWriterPool.Get();
						writer.WriteLong(header);
						writer.Write(response);
						var data = writer.ToArraySegment();
						writer.Dispose();
						writer = null;

						if(data.Array == null)
						{
							throw new ArgumentException("send buffer is empty");
						}

						cast.Udp.Send(data.Array, data.Count, ep);
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
						writer?.Dispose();
						writer = null;
					}
				}
			}

			$"thread complete: {Thread.CurrentThread.Name}".Log();
		}
	}
}
