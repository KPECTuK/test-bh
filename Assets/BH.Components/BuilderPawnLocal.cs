using BH.Model;
using UnityEngine;

namespace BH.Components
{
	public sealed class BuilderPawnLocal : IBuilderAsset<CompPawn>
	{
		public CompPawn Build(Transform parent, CxOrigin origin, ModelViewUser model)
		{
			var result = Singleton<ServiceResources>.I.LoadAssetAsResources<CompPawn>(
				ServiceResources.ID_RESOURCE_PAWN_LOCAL_S,
				parent,
				origin);

			model.IdCamera = result.Camera.GetComponent<ISceneCamera>().Id;
			result.CameraAnchor = result.Camera.transform.localPosition;
			result.Builder = this;
			result.IdModel = model.IdUser;
			result.InputReceiver = new InputPawnLocal(result);
			result.InputReceiver.Enable();
			result.Set(new DriverPawnLocal(result));
			result.View.gameObject.SetActive(false);
			Singleton<ServiceCameras>.I.SetSpectator(model.IdCamera);

			#if UNITY_EDITOR
			var cachedCamera = result.Camera.transform;
			result.GizmoShared.Horizon = new Plane(cachedCamera.up, Vector3.zero);
			result.GizmoShared.Previous = cachedCamera.forward;
			#endif

			return result;
		}

		public void Destroy(CompPawn instance)
		{
			instance.InputReceiver.Disable();
			Singleton<ServiceCameras>.I.SetSpectator();
			
			//? destroy
		}
	}
}
