using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BH.Components
{
	public class CompApp : MonoBehaviour
	{
		private CompPawn _local;

		private void Awake()
		{
			Cursor.lockState = CursorLockMode.Locked;
			Screen.fullScreen = true;
			Application.targetFrameRate = 60;

			_local = Definitions.BuildPawn<BuilderPawnLocal>(null, new CxOrigin());
		}

		private void OnDestroy()
		{
			_local.Builder.Destroy(_local);

			Cursor.lockState = CursorLockMode.None;
			Screen.fullScreen = false;
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
