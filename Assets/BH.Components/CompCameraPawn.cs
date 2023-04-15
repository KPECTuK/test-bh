using System;
using BH.Model;
using Mirror;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BH.Components
{
	[RequireComponent(typeof(Camera))]
	public class CompCameraPawn : MonoBehaviour, ISceneCamera
	{
		public Camera ControllerCamera { get; private set; }
		public uint IdCorresponding { get; private set; }

		private void Awake()
		{
			ControllerCamera = GetComponent<Camera>();

			var data = ControllerCamera.GetUniversalAdditionalCameraData();
			data.renderType = CameraRenderType.Overlay;
		}

		private void OnEnable()
		{
			// due unity callback sequence
			var identity = GetComponentInParent<NetworkIdentity>();
			if(identity == null)
			{
				throw new Exception("pawn camera is not in pawn asset structure: no identity found");
			}
			IdCorresponding = identity.netId;

			Singleton<ServiceCameras>.I.RegisterCamera(this);
		}

		private void OnDisable()
		{
			//! newer disabled from within script
			Singleton<ServiceCameras>.I.UnregisterCamera(this);
		}
	}
}
