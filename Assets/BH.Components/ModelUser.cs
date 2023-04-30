using System;
using BH.Model;

namespace BH.Components
{
	public struct ModelUser : IEquatable<CxId>
	{
		/// <summary> immutable </summary>
		public CxId IdUser;
		public CxId IdFeature;
		/// <summary>
		/// remote ids are always shared by the synchronization,
		/// local id ON CLIENT: is empty or bound to selected server
		/// local id ON SERVER: is always bound to local machine id
		/// </summary>
		public CxId IdHostAt;
		public bool IsReady;
		public bool IsNotifiedStartGame;

		public CxId IdCamera;

		public DataUser CreateData =>
			new()
			{
				IdUser = IdUser,
				IdFeature = IdFeature,
				IdHostAt = IdHostAt,
				IsReady = IsReady,
			};

		// DateTime has no predefined size
		public DateTime TimestampDiscovery;

		public bool IsEmpty => IdUser.IsEmpty;

		public string RenderIdHostAt => IdHostAt.ShortForm(false);
		public string RenderIdUser => IdUser.ShortForm(false);

		public static ModelUser Create(DataUser data)
		{
			return new()
			{
				IdUser = data.IdUser,
				IdFeature = data.IdFeature,
				IdHostAt = data.IdHostAt,
				IsReady = data.IsReady,
				IdCamera = CxId.Empty,
				TimestampDiscovery = DateTime.UtcNow,
			};
		}

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
			return @object is ModelUser other && Equals(other.IdUser);
		}

		public override int GetHashCode()
		{
			return IdUser.GetHashCode();
		}

		public bool UpdateFrom(ref DataUser data)
		{
			var result = false;
			if(IdUser != data.IdUser)
			{
				throw new Exception($"trying to update a user with another id: ( input: {data.IdUser} over: {IdUser} )");
			}

			// server authority
			if(IdFeature != data.IdFeature)
			{
				result = true;

				IdFeature = data.IdFeature;
			}

			// local (not a client or server only) authority
			if(IdHostAt != data.IdHostAt)
			{
				result = true;

				IdHostAt = data.IdHostAt;
			}

			// local (not a client or server only) authority
			if(IsReady != data.IsReady)
			{
				result = true;

				IsReady = data.IsReady;
			}

			return result;
		}
	}
}
