using BH.Model;
using UnityEngine;

namespace BH.Components
{
	public sealed class BuilderPawnLobby : IBuilderAsset<CompPawn>
	{
		public CompPawn Build(Transform parent, CxOrigin origin, ModelUser model)
		{
			var result = Singleton<ServiceResources>.I.LoadAssetAsResources<CompPawn>(
				ServiceResources.ID_RESOURCE_PAWN_LOBBY_S,
				parent,
				origin);

			result.name = $"{result.name.CleanUpName()}_{model.IdUser.ShortForm()}";
			result.Builder = this;
			result.IdUser = model.IdUser;
			result.InputReceiver = null;
			result.Set(new DriverPawnLobby());
			result.SetFeatures(model.IdFeature);
			result.View.gameObject.SetActive(true);

			return result;
		}

		public void Destroy(CompPawn instance)
		{
			Object.Destroy(instance.gameObject);
		}
	}
}
