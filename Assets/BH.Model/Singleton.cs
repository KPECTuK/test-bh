using System;

namespace BH.Model
{
	public static class Singleton<T> where T : class, new()
	{
		// ReSharper disable once StaticMemberInGenericType
		private static readonly object _sync = new();
		private static T _instance;

		static Singleton()
		{
			_instance = new T();
		}

		public static T I => Get();

		private static T Get()
		{
			if(_instance == null)
			{
				throw new ObjectDisposedException($"disposed of: {typeof(T).Name}");
			}

			return _instance;
		}

		public static void Dispose()
		{
			lock(_sync)
			{
				if(_instance is IDisposable cast)
				{
					cast.Dispose();
				}
				_instance = null;
			}
		}
	}
}
