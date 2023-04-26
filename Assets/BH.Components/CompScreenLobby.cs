using System;
using System.Text;
using BH.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BH.Components
{
	[RequireComponent(typeof(CompScreen))]
	public class CompScreenLobby : MonoBehaviour, IWidgetController
	{
		private const string CONTENT_MODE_SERVER_S = "SERVER STOP";
		private const string CONTENT_MODE_CLIENT_S = "SERVER START";

		private const string CONTENT_USER_STATE_READY_S = "TO BUSY";
		private const string CONTENT_USER_STATE_NOT_READY_S = "TO READY";

		public float CameraIntervalSec = 10f;

		public Button ButtonOwn;
		public Button ButtonReady;
		public CompScreenLobbyCtrlBar Bar;
		public TextMeshProUGUI TextInfo;

		private TextMeshProUGUI _textButtonOwn;
		private TextMeshProUGUI _textButtonReady;

		private CompScreen _screen;
		private Action _callbackOnOwn;
		private Action _callbackOnReady;

		private float _screenInitialTime;

		// not implemented: due time limit
		public bool IsBusy { get; }

		public void OnScreenEnter()
		{
			Singleton<ServiceCameras>.I.SetSpectator();
			_screenInitialTime = Time.time;

			_callbackOnOwn = OnButtonOwn;
			_callbackOnReady = OnButtonReady;

			_textButtonOwn.text = CONTENT_MODE_CLIENT_S;
			Singleton<ServiceNetwork>.I.Events.Enqueue(
				new CmdNetworkModeChange { Target = new NetworkModeLobbyClient() });
		}

		public void OnScreenExit()
		{
			// exit is to game state only

			_callbackOnOwn = OnButtonOwn;
			_callbackOnReady = OnButtonReady;
		}

		private void Awake()
		{
			_screen = GetComponent<CompScreen>();
			ButtonOwn.onClick.AddListener(() => { _callbackOnOwn?.Invoke(); });
			ButtonReady.onClick.AddListener(() => { _callbackOnReady?.Invoke(); });

			_textButtonOwn = ButtonOwn.transform.GetComponentInChildren<TextMeshProUGUI>();
			_textButtonReady = ButtonReady.transform.GetComponentInChildren<TextMeshProUGUI>();
		}

		private void Update()
		{
			if(!_screen.IsActiveScreen)
			{
				return;
			}

			UpdateSpectator();

			var idUser = Singleton<ServiceNetwork>.I.IdCurrentUser;
			var idServerLocal = Singleton<ServiceNetwork>.I.IdCurrentMachine;
			var idServerRemote = Singleton<ServiceNetwork>.I.NetworkModeShared.IdServerCurrent;
			TextInfo.text = new StringBuilder()
				.Append("id.user: ")
				.Append(idUser.ShortForm())
				.AppendLine()
				.Append("id.server (machine): ")
				.Append(idServerLocal.ShortForm())
				.AppendLine()
				.Append("id.server (remote): ")
				.Append(idServerRemote.ShortForm())
				.AppendLine()
				.ToString();
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

			// shared by discovery
			ref var model = ref Singleton<ServiceUI>.I.ModelsUser.Get(Singleton<ServiceNetwork>.I.IdCurrentUser, out var contains);
			if(!contains)
			{
				throw new Exception("not contains");
			}

			model.IsReady = !model.IsReady;
			_textButtonReady.text = model.IsReady
				? CONTENT_USER_STATE_READY_S
				: CONTENT_USER_STATE_NOT_READY_S;
		}

		private void OnButtonOwn()
		{
			"press: 'Own'".Log();

			if(Singleton<ServiceNetwork>.I.NetworkModeShared is NetworkModeLobbyClient)
			{
				_textButtonOwn.text = CONTENT_MODE_SERVER_S;
				Singleton<ServiceNetwork>.I.Events.Enqueue(
					new CmdNetworkModeChange
					{
						Target = new NetworkModeLobbyServer(),
					});
			}

			if(Singleton<ServiceNetwork>.I.NetworkModeShared is NetworkModeLobbyServer)
			{
				_textButtonOwn.text = CONTENT_MODE_CLIENT_S;
				Singleton<ServiceNetwork>.I.Events.Enqueue(
					new CmdNetworkModeChange
					{
						Target = new NetworkModeLobbyClient(),
					});
			}
		}
	}
}
