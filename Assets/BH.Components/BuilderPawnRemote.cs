using BH.Model;
using UnityEngine;

namespace BH.Components
{
	public sealed class BuilderPawnRemote : IBuilderAsset<CompPawn>
	{
		public CompPawn Build(Transform parent, CxOrigin origin, ModelViewUser model)
		{
			var result = Singleton<ServiceResources>.I.LoadAssetAsResources<CompPawn>(
				ServiceResources.ID_RESOURCE_PAWN_REMOTE_S,
				parent,
				origin);

			result.Builder = this;
			result.IdUser = model.IdUser;
			result.InputReceiver = new InputPawnRemote(result);
			result.InputReceiver.Enable();
			result.Set(new DriverPawnRemote());
			result.SetFeatures(model.IdFeature);
			result.View.gameObject.SetActive(true);

			return result;
		}

		public void Destroy(CompPawn instance)
		{
			instance.InputReceiver.Disable();
		}
	}
}