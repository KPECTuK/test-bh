using BH.Model;
using kcp2k;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BH.Components
{
	[RequireComponent(typeof(CompNetwork))]
	[RequireComponent(typeof(KcpTransport))]
	public class CompApp : MonoBehaviour
	{
		//private CompPawn _local;

		private void Awake()
		{
			Screen.fullScreen = true;
			Application.targetFrameRate = 60;

			Singleton<ServiceResources>.I.Reset();
			Singleton<ServiceCameras>.I.Reset();
			Singleton<ServiceUI>.I.Reset();
			Singleton<ServiceNetwork>.I.Reset();

			//_local = ServiceResources.BuildPawn<BuilderPawnLocal>(null, new CxOrigin());

			"initialized: app".Log();
		}

		private void OnDestroy()
		{
			//_local.Builder.Destroy(_local);

			// TODO: separate script: the execution order to use

			Singleton<ServiceNetwork>.Dispose();
			Singleton<ServiceUI>.Dispose();
			Singleton<ServiceCameras>.Dispose();
			Singleton<ServiceResources>.Dispose();

			Screen.fullScreen = false;

			"disposed".Log();
		}

		private void Update()
		{
			if(Input.GetKey(KeyCode.Escape))
			{
				#if UNITY_EDITOR
				EditorApplication.isPlaying = false;
				#else
				Application.Quit();
				#endif
			}
		}
	}
}
