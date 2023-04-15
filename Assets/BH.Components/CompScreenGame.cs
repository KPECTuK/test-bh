using UnityEngine;

namespace BH.Components
{
	[RequireComponent(typeof(CompScreen))]
	public class CompScreenGame : MonoBehaviour, IWidgetController
	{
		public bool IsBusy { get; }

		public void OnScreenEnter()
		{
			Cursor.lockState = CursorLockMode.Locked;
		}

		public void OnScreenExit()
		{
			Cursor.lockState = CursorLockMode.None;
		}
	}
}
