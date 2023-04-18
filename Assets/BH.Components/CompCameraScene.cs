using BH.Model;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BH.Components
{
	[RequireComponent(typeof(Camera))]
	public class CompCameraScene : MonoBehaviour, ISceneCamera
	{
		// TODO: implement barrage

		public CxId Id { get; private set; }
		public Camera CompCamera { get; private set; }

		private void Awake()
		{
			CompCamera = GetComponent<Camera>();

			var data = CompCamera.GetUniversalAdditionalCameraData();
			data.renderType = CameraRenderType.Overlay;
		}

		private void OnEnable()
		{
			Id = Singleton<ServiceCameras>.I.RegisterCamera(this);
		}

		private void OnDisable()
		{
			// scene reload purpose
			//Singleton<ServiceCameras>.I.UnregisterCamera(this);
		}
	}
}
