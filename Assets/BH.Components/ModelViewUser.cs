using System;
using BH.Model;

namespace BH.Components
{
	public struct ModelViewUser : IEquatable<CxId>
	{
		/// <summary> immutable </summary>
		public CxId IdUser;
		public CxId IdHostAt;
		public CxId IdFeature;
		public bool IsReady;

		public CxId IdCamera;

		public DateTime LastUpdated;
		public DateTime FirstUpdated;

		public bool IsEmpty => IdUser.IsEmpty;

		public string RenderIdHostAt => IdHostAt.ShortForm(false);
		public string RenderIdUser => IdUser.ShortForm(false);

		public override string ToString()
		{
			return $"( id: {IdUser.ShortForm()} at: {IdHostAt.ShortForm()} )";
		}

		public bool Equals(CxId other)
		{
			return IdUser == other;
		}

		public override bool Equals(object @object)
		{
			return @object is ModelViewUser other && Equals(other.IdUser);
		}

		public override int GetHashCode()
		{
			return IdUser.GetHashCode();
		}

		public bool UpdateFrom(ref ResponseUser response)
		{
			var result = false;
			if(IdUser != response.IdUser)
			{
				throw new Exception($"trying to update a user with another id: ( input: {response.IdUser} over: {IdUser} )");
			}

			if(IdFeature != response.IdFeature)
			{
				result = true;

				IdFeature = response.IdFeature;
			}

			if(IsReady != response.IsReady)
			{
				result = true;

				IsReady = response.IsReady;
			}

			LastUpdated = DateTime.UtcNow;

			return result;
		}
	}
}
