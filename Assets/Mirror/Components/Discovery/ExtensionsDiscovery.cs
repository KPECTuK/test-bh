using System;
using UnityEngine;

namespace Mirror.Discovery
{
	public static class ExtensionsDiscovery
	{
		public static void AssertPlatform()
		{
			if(Application.platform == RuntimePlatform.WebGLPlayer)
			{
				throw new PlatformNotSupportedException($"discovery service is not supported: {Application.platform}");
			}
		}

		public static long Randomize(this long source)
		{
			var value1 = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
			var value2 = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
			return value1 + ((long)value2 << 32);
		}
	}
}
