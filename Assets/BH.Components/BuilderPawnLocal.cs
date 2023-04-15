using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BH.Components
{
	public sealed class BuilderPawnLocal : IBuilderAsset<CompPawn>
	{
		public CompPawn Build(Transform parent, CxOrigin origin)
		{
			var result = Definitions.LoadAsset<CompPawn>(
				Definitions.ID_RESOURCE_PAWN_LOCAL_S,
				parent,
				origin);

			result.View.gameObject.SetActive(false);

			result.Builder = this;

			result.InputReceiver = new InputPawnLocal(result);
			result.InputReceiver.Enable();

			result.Set(new DriverPawnLocal(result));

			var dataPawn = result.Camera.GetUniversalAdditionalCameraData();
			dataPawn.renderType = CameraRenderType.Overlay;
			var dataMain = Camera.main.GetUniversalAdditionalCameraData();
			dataMain.cameraStack.Add(result.Camera);
			result.Camera.gameObject.SetActive(true);
			result.CameraAnchor = result.Camera.transform.localPosition;

			#if UNITY_EDITOR
			var cachedCamera = result.Camera.transform;
			result.GizmoShared.Horizon = new Plane(cachedCamera.up, Vector3.zero);
			result.GizmoShared.Previous = cachedCamera.forward;
			#endif

			return result;
		}

		public void Destroy(CompPawn pawn)
		{
			// порядок вызовов методов OnDisable() OnDestroy() во время выхода из приложения - не определена

			pawn.InputReceiver.Disable();

			if(Camera.main != null)
			{
				var dataMain = Camera.main.GetUniversalAdditionalCameraData();
				dataMain.cameraStack.Remove(pawn.Camera);
			}
		}
	}
}
