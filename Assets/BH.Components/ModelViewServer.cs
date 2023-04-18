using System;
using System.Net;
using BH.Model;

namespace BH.Components
{
	public sealed class ModelViewServer : IEquatable<ModelViewServer>
	{
		public CxId IdHost;
		public CxId IdOwner;
		public int PlayersTotal;

		public IPEndPoint HostIp;
		public Uri HostUri;

		public DateTime LastUpdated;

		public string RenderServer => $"server: {IdHost.ShortForm()}";
		public string RenderOwner => $"owner: {IdOwner.ShortForm()}";
		public string RenderPlayers => $"players: {PlayersTotal}";

		public override string ToString()
		{
			return $"( host: {IdHost.ShortForm()} owner: {IdOwner.ShortForm()} )";
		}

		public bool Equals(ModelViewServer other)
		{
			if(ReferenceEquals(null, other))
			{
				return false;
			}

			if(ReferenceEquals(this, other))
			{
				return true;
			}

			return IdHost == other.IdHost;
		}

		public override bool Equals(object @object)
		{
			return
				ReferenceEquals(this, @object) ||
				@object is ModelViewServer other && Equals(other);
		}

		public override int GetHashCode()
		{
			throw new NotSupportedException("not applicable for hash collections");
		}
	}
}
