using System;
using System.Reflection;
using BH.Model;
using UnityEngine;

namespace BH.Components
{
	public class ServiceResources : IService
	{
		// TL; TI; use custom Url scheme here:
		// it's just a place to control resources routine

		public const string ID_RESOURCE_PAWN_LOCAL_S = "pfv_pawn_firstP";
		public const string ID_RESOURCE_PAWN_REMOTE_S = "pfv_pawn_thirdP";
		public const string ID_RESOURCE_PAWN_LOBBY_S = "pfv_pawn_thirdP";

		public const string ID_RESOURCE_UI_LOBBY_ITEM_SERVER_S = "pfu_item_server";
		public const string ID_RESOURCE_UI_LOBBY_ITEM_USER_S = "pfu_item_user";

		private SettingsResources _proto;

		private static readonly IBuilderAsset<CompPawn>[] _factoryBuilderAsset =
		{
			new BuilderPawnLocal(),
			new BuilderPawnRemote(),
			new BuilderPawnLobby(),
		};

		public void Reset()
		{
			if(!ReferenceEquals(null, _proto))
			{
				UnityEngine.Object.DestroyImmediate(_proto);
			}

			_proto = Resources.Load<SettingsResources>("settings_service_resources");

			"initialized: resources".Log();
		}

		public CompPawn BuildPawn<T>(Transform parent, CxOrigin origin, ModelUser model) where T : class, IBuilderAsset<CompPawn>
		{
			for(var index = 0; index < _factoryBuilderAsset.Length; index++)
			{
				if(_factoryBuilderAsset[index] is T cast)
				{
					return cast.Build(parent, origin, model);
				}
			}

			throw new Exception($"pawn builder is not found: {typeof(T)}");
		}

		public T LoadAssetAsResources<T>(string id, Transform parent, CxOrigin origin) where T : Component
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

		public T LoadAssetAsLibrary<T>(string id, Transform parent, CxOrigin origin) where T : Component
		{
			var proto = FindLibraryProto<T>(id);
			var instance = UnityEngine.Object.Instantiate(
				proto.gameObject,
				origin.Location,
				origin.Orientation,
				parent);
			return instance.GetComponent<T>();
		}

		public SettingsPawn GetProtoSettingsPawn()
		{
			return _proto.SettingsPawn;
		}

		private T FindLibraryProto<T>(string id) where T : Component
		{
			var field = typeof(SettingsResources).GetFields(BindingFlags.Public | BindingFlags.Instance);
			foreach(var info in field)
			{
				var proto = info.GetValue(_proto);
				if(proto is Component cast)
				{
					if(cast.name.Contains(id))
					{
						return cast.GetComponent<T>();
					}
				}
			}

			return null;
		}

		public void Dispose() { }
	}
}
