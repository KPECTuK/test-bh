using System;
using UnityEngine;

namespace BH.Components
{
	public static class Definitions
	{
		public const string ID_ACTION_MOVE_S = "Move";
		public const string ID_ACTION_LOOK_S = "Look";
		public const string ID_ACTION_FIRE_S = "Fire";

		public const string ID_RESOURCE_PAWN_LOCAL_S = "pfv_pawn_firstP";
		public const string ID_RESOURCE_PAWN_REMOTE_S = "pfv_pawn_thirdP";
		public const string ID_RESOURCE_PAWN_LOBBY_S = "pfv_pawn_thirdP";

		private static readonly IBuilderAsset<CompPawn>[] _factoryBuilderAsset =
		{
			new BuilderPawnLocal(),
			new BuilderPawnRemote(),
			new BuilderPawnLobby(),
		};

		public static CompPawn BuildPawn<T>(Transform parent, CxOrigin origin) where T : class, IBuilderAsset<CompPawn>
		{
			T builder = null;
			for(var index = 0; index < _factoryBuilderAsset.Length; index++)
			{
				if(_factoryBuilderAsset[index] is T cast)
				{
					builder = cast;
					break;
				}
			}

			if(builder == null)
			{
				throw new Exception($"pawn builder is not found: {typeof(T)}");
			}

			return builder.Build(parent, origin);
		}

		public static T LoadAsset<T>(string id, Transform parent, CxOrigin origin) where T : Component
		{
			var proto = Resources.Load<GameObject>(id);
			var instance = UnityEngine.Object.Instantiate(
				proto,
				origin.Location,
				origin.Orientation,
				parent);
			var result = instance.GetComponent<T>();
			if(result == null)
			{
				throw new Exception($"resource has no controller component: {id}");
			}
			Resources.UnloadUnusedAssets();
			return result;
		}
	}
}
