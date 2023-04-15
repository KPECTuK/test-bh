using System;
using System.Collections.Generic;
using BH.Model;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BH.Components
{
	public class ServiceCameras : IService
	{
		private readonly List<ISceneCamera> _camerasOrder = new();

		public Camera CameraAssembler => Camera.main;

		public ISceneCamera Spectator { get; private set; }

		public void Reset()
		{
			// scenes reload purpose

			"initialized: cameras".Log();
		}

		public void Dispose()
		{
			// manageable, due Singleton generic routine

			_camerasOrder.Clear();
		}

		public void RegisterCamera(ISceneCamera source)
		{
			if(source != null)
			{
				_camerasOrder.Add(source);
				var data = source.ControllerCamera.GetUniversalAdditionalCameraData();
				data.renderType = CameraRenderType.Overlay;

				$"camera is registered: {(source as Component).name}".Log();
			}
		}

		public void UnregisterCamera(ISceneCamera source)
		{
			if(_camerasOrder.Remove(source))
			{
				$"camera is unregistered: {(source as Component).name}".Log();
			}
		}

		/// <summary> round robin scene cameras </summary>
		public void SetSpectator()
		{
			ISceneCamera spectator = null;
			for(var index = 0; index < _camerasOrder.Count; index++)
			{
				if(_camerasOrder[index] is CompCameraScene cast)
				{
					spectator = cast;
					_camerasOrder.RemoveAt(index);
					break;
				}
			}

			if(spectator == null)
			{
				throw new Exception("no scene camera found in scene");
			}

			_camerasOrder.Add(spectator);

			ActivateCamera(spectator);
		}

		/// <summary> enable camera by id or scene camera round robin if id is zero </summary>
		public void SetSpectator(uint idCamera)
		{
			if(idCamera == 0)
			{
				SetSpectator();
			}
			else
			{
				ISceneCamera spectator = null;
				for(var index = 0; index < _camerasOrder.Count; index++)
				{
					if(_camerasOrder[index].IdCorresponding == idCamera)
					{
						spectator = _camerasOrder[index];
						break;
					}
				}

				ActivateCamera(spectator);
			}
		}

		private void ActivateCamera(ISceneCamera spectator)
		{
			// TODO: implement accordingly to the renderer implementation

			if(spectator == null)
			{
				throw new Exception("no scene camera found in scene");
			}

			var dataAssembler = CameraAssembler.GetUniversalAdditionalCameraData();
			for(var index = 0; index < _camerasOrder.Count; index++)
			{
				dataAssembler.cameraStack.Remove(_camerasOrder[index].ControllerCamera);
			}

			{
				var camera = spectator.ControllerCamera.GetComponent<Camera>();
				//var dataOverlay = camera.GetUniversalAdditionalCameraData();
				//dataOverlay.renderType = CameraRenderType.Overlay;
				//camera.depth = 0;
				dataAssembler.cameraStack.Insert(0, camera);
			}
		}
	}
}
