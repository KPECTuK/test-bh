//#define UNITY_ANDROID

// ReSharper Disable All
using System;
using UnityEngine;

namespace Mirror.Discovery
{
	public sealed class MultiCatsLock : IDisposable
	{
		//? is AndroidJavaObject{} thread safe

		#if UNITY_ANDROID
		private static AndroidJavaObject _handle;
		private readonly object _sync = new();

		#endif

		public MultiCatsLock()
		{
			#if UNITY_ANDROID
			if(_handle != null)
			{
				return;
			}

			if(Application.platform != RuntimePlatform.Android)
			{
				return;
			}

			try
			{
				using var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
					.GetStatic<AndroidJavaObject>("currentActivity");
				using var service = activity
					.Call<AndroidJavaObject>("getSystemService", "wifi");

				var @lock = service.Call<AndroidJavaObject>("createMulticastLock", "lock");
				@lock.Call("acquire");

				_handle = @lock;
			}
			catch(Exception exception)
			{
				Dispose();

				throw new ExceptionMulticastLock(exception);
			}

			#endif
		}

		~MultiCatsLock()
		{
			Dispose();
		}

		public void Dispose()
		{
			Disposing();
			GC.SuppressFinalize(this);
		}

		// ReSharper disable once MemberCanBeMadeStatic.Local
		private void Disposing()
		{
			#if UNITY_ANDROID
			lock(_sync)
			{
				_handle?.Call("release");
				_handle = null;
			}

			#endif
		}
	}

	#if UNITY_ANDROID
	public sealed class ExceptionMulticastLock : Exception
	{
		public ExceptionMulticastLock(Exception inner) : base("multicast lock acquisition fault", inner) { }
	}

	#endif
}
