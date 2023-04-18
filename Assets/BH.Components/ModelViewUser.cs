using System;
using BH.Model;

namespace BH.Components
{
	public sealed class ModelViewUser : IEquatable<ModelViewUser>
	{
		public const string CONTENT_STATE_USER_READY_S = "ready";
		public const string CONTENT_STATE_USER_NOT_READY_S = "not ready";

		public CxId IdUser;
		public CxId IdAtHost;
		public CxId IdFeature;
		public bool IsReady;

		public CxId IdCamera;

		public DateTime LastUpdated;
		public DateTime FirstUpdated;

		public string RenderIdUser => $"user: {IdUser.ShortForm()}";
		public string RenderIsReady =>
			IsReady
				? CONTENT_STATE_USER_READY_S
				: CONTENT_STATE_USER_NOT_READY_S;

		public override string ToString()
		{
			return $"( id: {IdUser.ShortForm()} at: {IdAtHost.ShortForm()} )";
		}

		public bool Equals(ModelViewUser other)
		{
			if(ReferenceEquals(null, other))
			{
				return false;
			}

			if(ReferenceEquals(this, other))
			{
				return true;
			}

			return IdUser == other.IdUser;
		}

		public override bool Equals(object @object)
		{
			return
				ReferenceEquals(this, @object) ||
				@object is ModelViewUser other && Equals(other);
		}

		public override int GetHashCode()
		{
			throw new NotSupportedException("not applicable for hash collections");
		}
	}
}
