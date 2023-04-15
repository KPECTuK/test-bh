using System;
using BH.Model;
using UnityEngine;

namespace BH.Components
{
	public sealed class BuilderPawnLobby : IBuilderAsset<CompPawn>
	{
		public CompPawn Build(Transform parent, CxOrigin origin)
		{
			var result = Singleton<ServiceResources>.I.LoadAssetAsResources<CompPawn>(
				ServiceResources.ID_RESOURCE_PAWN_LOBBY_S,
				parent,
				origin);

			// pooling
			result.gameObject.SetActive(false);
			result.Set(new DriverPawnLobby());
			result.gameObject.SetActive(true);

			result.View.gameObject.SetActive(true);

			return result;
		}

		public void Destroy(CompPawn instance)
		{
			throw new NotImplementedException();
		}
	}
}
