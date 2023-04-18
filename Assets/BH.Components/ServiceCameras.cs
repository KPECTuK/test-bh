using System;
using System.Collections.Generic;
using BH.Model;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BH.Components
{
	public class ServiceCameras : IService
	{
		private readonly List<(CxId id, ISceneCamera handle)> _cameras = new();

		public Camera CameraAssembler => Camera.main;

		public void Reset()
		{
			// scenes reload purpose

			"initialized: cameras".Log();
		}

		public void Dispose()
		{
			// manageable, due Singleton generic routine

			_cameras.Clear();
		}

		public CxId RegisterCamera(ISceneCamera source)
		{
			if(source == null)
			{
				throw new Exception("can't register camera of null object");
			}

			var pair = (id: CxId.Create(), handle: source);
			_cameras.Add(pair);
			var data = source.CompCamera.GetUniversalAdditionalCameraData();
			data.renderType = CameraRenderType.Overlay;

			var name = source is Component cast ? cast.name : "unknown";
			$"camera is registered: {name} [{pair.id}]".Log();

			return pair.id;
		}

		public void UnregisterCamera(ISceneCamera source)
		{
			var index = 0;
			for(; index < _cameras.Count; index++)
			{
				if(ReferenceEquals(_cameras[index].handle, source))
				{
					break;
				}
			}

			var name = source is Component cast ? cast.name : "unknown";
			if(index == _cameras.Count)
			{
				throw new Exception($"camera not found, by reference: {name}");
			}

			var pair = _cameras[index];
			_cameras.RemoveAt(index);

			$"camera is unregistered: {name} [{pair.id}]".Log();
		}

		public void UnregisterCamera(CxId id)
		{
			var index = 0;
			for(; index < _cameras.Count; index++)
			{
				if(_cameras[index].id == id)
				{
					break;
				}
			}

			if(index == _cameras.Count)
			{
				throw new Exception($"camera not found, by id: {id}");
			}

			var pair = _cameras[index];
			_cameras.RemoveAt(index);

			var name = pair.handle is Component cast ? cast.name : "unknown";
			$"camera is unregistered: {name} [{pair.id}]".Log();
		}

		public void SetSpectator()
		{
			(CxId id, ISceneCamera handle) pair = default;
			var index = 0;
			for(; index < _cameras.Count; index++)
			{
				if(_cameras[index].handle is CompCameraScene)
				{
					pair = _cameras[index];
					_cameras.RemoveAt(index);
					break;
				}
			}

			if(index == _cameras.Count)
			{
				throw new Exception("no scene camera found in scene");
			}

			_cameras.Add(pair);

			ActivateCamera(pair.handle);
		}

		public void SetSpectator(CxId idCamera)
		{
			if(idCamera.IsEmpty)
			{
				SetSpectator();
			}
			else
			{
				for(var index = 0; index < _cameras.Count; index++)
				{
					if(_cameras[index].id == idCamera)
					{
						ActivateCamera(_cameras[index].handle);
						return;
					}
				}
			}
		}

		private void ActivateCamera(ISceneCamera spectator)
		{
			// NOTE: implement accordingly to the renderer implementation

			if(spectator == null)
			{
				throw new Exception("no scene camera found in scene");
			}

			var dataAssembler = CameraAssembler.GetUniversalAdditionalCameraData();
			for(var index = 0; index < _cameras.Count; index++)
			{
				dataAssembler.cameraStack.Remove(_cameras[index].handle.CompCamera);
			}

			var camera = spectator.CompCamera.GetComponent<Camera>();
			dataAssembler.cameraStack.Insert(0, camera);
		}
	}
}
