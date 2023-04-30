using System;
using System.Collections;
using System.Collections.Generic;
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
		private const string CONTENT_MODE_SERVER_S = "TO CLIENT MODE";
		private const string CONTENT_MODE_CLIENT_S = "TO SERVER MODE";

		private const string CONTENT_USER_STATE_READY_S = "TO BUSY STATE";
		private const string CONTENT_USER_STATE_NOT_READY_S = "TO READY STATE";

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

		private readonly Queue<IEnumerator> _tasks = new();

		private IScheduler _scheduler;

		public IScheduler Scheduler => _scheduler ?? new SchedulerTaskDefault();

		public void SetScheduler<T>() where T : IScheduler, new()
		{
			_scheduler = _tasks.BuildScheduler<T>();
		}

		public void OnWidgetEnable()
		{
			SetScheduler<SchedulerTaskAll>();

			Singleton<ServiceCameras>.I.SetSpectator();
			_screenInitialTime = Time.time;

			_callbackOnOwn = OnButtonOwn;
			_callbackOnReady = OnButtonReady;

			Singleton<ServiceNetwork>.I.Events.Enqueue(
				new CmdNetworkModeChange
				{
					Target = new NetworkModeLobbyClient()
				});
		}

		public void OnWidgetDisable()
		{
			// exit is to game state only

			_callbackOnOwn = OnButtonOwn;
			_callbackOnReady = OnButtonReady;

			Scheduler.Clear();
		}

		private void Awake()
		{
			_screen = GetComponent<CompScreen>();

			ButtonOwn.onClick.AddListener(() => { _callbackOnOwn?.Invoke(); });
			ButtonReady.onClick.AddListener(() => { _callbackOnReady?.Invoke(); });

			_textButtonOwn = ButtonOwn.transform.GetComponentInChildren<TextMeshProUGUI>();
			_textButtonReady = ButtonReady.transform.GetComponentInChildren<TextMeshProUGUI>();

			SetScheduler<SchedulerTaskModelUser>();
		}

		private void Update()
		{
			if(!_screen.IsActiveScreen)
			{
				return;
			}

			_tasks.ExecuteTasksSimultaneously();

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

		public IEnumerator TaskUpdateButtons()
		{
			var idUser = Singleton<ServiceNetwork>.I.IdCurrentUser;
			var model = Singleton<ServiceUI>.I.ModelsUser.Get(idUser, out var contains);
			if(!contains)
			{
				throw new Exception("not contains");
			}

			_textButtonReady.text = model.IsReady
				? CONTENT_USER_STATE_READY_S
				: CONTENT_USER_STATE_NOT_READY_S;

			// ReSharper disable once ConvertIfStatementToSwitchExpression
			if(Singleton<ServiceNetwork>.I.NetworkModeShared is NetworkModeLobbyServer)
			{
				_textButtonOwn.text = CONTENT_MODE_SERVER_S;
			}

			if(Singleton<ServiceNetwork>.I.NetworkModeShared is NetworkModeLobbyClient)
			{
				_textButtonOwn.text = CONTENT_MODE_CLIENT_S;
			}

			yield break;
		}

		//? might be a task also
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

			var idUser = Singleton<ServiceNetwork>.I.IdCurrentUser;
			ref var model = ref Singleton<ServiceUI>.I.ModelsUser.Get(idUser, out var contains);
			if(!contains)
			{
				throw new Exception("not contains");
			}

			var desc = model.CreateData;
			desc.IsReady = !desc.IsReady;

			if(ExtensionsView.TryUpdateUser(ref desc, out idUser))
			{
				Singleton<ServiceUI>.I.Events.Enqueue(
					new CmdViewLobbyUserUpdate
					{
						IdUser = idUser
					});
			}
		}

		private void OnButtonOwn()
		{
			"press: 'Own'".Log();

			if(Singleton<ServiceNetwork>.I.NetworkModeShared is NetworkModeLobbyClient)
			{
				Singleton<ServiceNetwork>.I.Events.Enqueue(
					new CmdNetworkModeChange
					{
						Target = new NetworkModeLobbyServer(),
					});
			}

			if(Singleton<ServiceNetwork>.I.NetworkModeShared is NetworkModeLobbyServer)
			{
				Singleton<ServiceNetwork>.I.Events.Enqueue(
					new CmdNetworkModeChange
					{
						Target = new NetworkModeLobbyClient(),
					});
			}
		}
	}
}
