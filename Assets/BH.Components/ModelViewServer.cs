using System;
using System.Net;
using BH.Model;

namespace BH.Components
{
	public struct ModelViewServer : IEquatable<CxId>
	{
		/// <summary> immutable </summary>
		public CxId IdHost;
		public CxId IdOwner;
		public int ServerUsersTotal;

		public IPEndPoint HostIp;
		public Uri HostUri;

		public DateTime LastUpdated;

		public string RenderServer => IdHost.ShortForm(false);
		public string RenderOwner => IdOwner.ShortForm(false);
		public string RenderPlayers => $"{ServerUsersTotal}";

		public bool IsEmpty => IdHost.IsEmpty;

		public override string ToString()
		{
			return $"( host: {IdHost.ShortForm()} owner: {IdOwner.ShortForm()} )";
		}

		public bool Equals(CxId other)
		{
			return IdHost == other;
		}

		public override bool Equals(object @object)
		{
			return @object is ModelViewServer other && Equals(other.IdHost);
		}

		public override int GetHashCode()
		{
			return IdHost.GetHashCode();
		}

		public bool UpdateFrom(ref ResponseServer response)
		{
			var result = false;
			if(IdHost != response.IdHost)
			{
				throw new Exception($"trying to update a server with another id: ( input: {response.IdHost} over: {IdHost} )");
			}

			if(ServerUsersTotal != response.ServerUsersTotal)
			{
				result = true;

				ServerUsersTotal = response.ServerUsersTotal;
			}

			LastUpdated = DateTime.UtcNow;

			return result;
		}
	}
}
