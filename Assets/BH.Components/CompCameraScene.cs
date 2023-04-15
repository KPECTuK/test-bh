using BH.Model;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BH.Components
{
	[RequireComponent(typeof(Camera))]
	public class CompCameraScene : MonoBehaviour, ISceneCamera
	{
		public Camera ControllerCamera { get; private set; }
		public uint IdCorresponding { get; } = 0;

		private void Awake()
		{
			ControllerCamera = GetComponent<Camera>();

			var data = ControllerCamera.GetUniversalAdditionalCameraData();
			data.renderType = CameraRenderType.Overlay;
		}

		private void OnEnable()
		{
			Singleton<ServiceCameras>.I.RegisterCamera(this);
		}

		private void OnDisable()
		{
			// scene reload purpose
			//Singleton<ServiceCameras>.I.UnregisterCamera(this);
		}
	}
}
