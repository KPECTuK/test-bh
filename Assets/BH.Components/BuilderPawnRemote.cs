using UnityEngine;

namespace BH.Components
{
	public sealed class BuilderPawnRemote : IBuilderAsset<CompPawn>
	{
		public CompPawn Build(Transform parent, CxOrigin origin)
		{
			var result = Definitions.LoadAsset<CompPawn>(
				Definitions.ID_RESOURCE_PAWN_REMOTE_S,
				parent,
				origin);

			// pooling
			result.gameObject.SetActive(false);
			result.Set(new DriverPawnRemote());
			result.gameObject.SetActive(true);

			result.View.gameObject.SetActive(true);

			return result;
		}

		public void Destroy(CompPawn instance)
		{
			throw new System.NotImplementedException();
		}
	}
}