using BH.Model;
using kcp2k;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BH.Components
{
	[RequireComponent(typeof(CompNetworkManager))]
	[RequireComponent(typeof(KcpTransport))]
	[RequireComponent(typeof(CompNetworkDiscovery))]
	public class CompApp : MonoBehaviour
	{
		private void Awake()
		{
			//Screen.fullScreen = true;
			Application.targetFrameRate = 60;
			Screen.SetResolution(960, 540, false);

			Singleton<ServiceResources>.I.Reset();
			Singleton<ServiceCameras>.I.Reset();
			Singleton<ServiceUI>.I.Reset();
			Singleton<ServiceNetwork>.I.Reset();
			Singleton<ServicePawns>.I.Reset();

			"initialized: app".Log();
		}

		private void OnDestroy()
		{
			// TODO: separate script: the execution order to use

			Singleton<ServicePawns>.Dispose();
			Singleton<ServiceNetwork>.Dispose();
			Singleton<ServiceUI>.Dispose();
			Singleton<ServiceCameras>.Dispose();
			Singleton<ServiceResources>.Dispose();

			//Screen.fullScreen = false;

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

			while(Singleton<ServiceNetwork>.I.Events.TryPeek(out var @event))
			{
				TryRunCommand(@event);
			}
		}

		private void TryRunCommand(ICommand<CompApp> @event)
		{
			if(@event.Assert(this))
			{
				$"running network command: {@event.GetType().NameNice()}".Log();

				@event.Execute(this);
			}
			else
			{
				$"skip network command due conditions: {@event.GetType().NameNice()}".LogWarning();
			}

			Singleton<ServiceNetwork>.I.Events.Dequeue();
		}
	}
}
