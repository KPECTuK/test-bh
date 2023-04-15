using System;
using BH.Model;
using UnityEngine;
using UnityEngine.UI;

namespace BH.Components
{
	[RequireComponent(typeof(CompScreen))]
	public class CompScreenLobby : MonoBehaviour, IWidgetController
	{
		public float CameraIntervalSec = 10f;

		public Button ButtonOwn;
		public Button ButtonReady;
		public CompScreenLobbyCtrlBar Bar;

		private CompScreen _screen;
		private Action _callbackOnOwn;
		private Action _callbackOnReady;

		private float _screenInitialTime;

		public bool IsBusy { get; }

		public void OnScreenEnter()
		{
			Singleton<ServiceCameras>.I.SetSpectator();
			_screenInitialTime = Time.time;

			_callbackOnOwn = OnButtonOwn;
			_callbackOnReady = OnButtonReady;
		}

		public void OnScreenExit()
		{
			_callbackOnOwn = OnButtonOwn;
			_callbackOnReady = OnButtonReady;
		}

		private void Awake()
		{
			_screen = GetComponent<CompScreen>();
			ButtonOwn.onClick.AddListener(() => { _callbackOnOwn?.Invoke(); });
			ButtonReady.onClick.AddListener(() => { _callbackOnReady?.Invoke(); });
		}

		private void Update()
		{
			if(!_screen.IsActiveScreen)
			{
				return;
			}

			UpdateSpectator();
		}

		private void UpdateSpectator()
		{
			var delta = Time.time - _screenInitialTime;
			if(delta > CameraIntervalSec)
			{
				Singleton<ServiceCameras>.I.SetSpectator();
				_screenInitialTime = Time.time;
			}
		}

		private void OnButtonReady()
		{
			"press: 'Ready'".Log();
		}

		private void OnButtonOwn()
		{
			"press: 'Own'".Log();
		}

		private void OnButtonItem(ModelViewButtonServer model)
		{
			"press: 'Server'".Log();
		}

		private void OnButtonItem(ModelViewButtonUser model)
		{
			"press: 'User'".Log();
		}
	}
}
