using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BH.Model;
using UnityEngine;

namespace BH.Components
{
	[RequireComponent(typeof(RectTransform))]
	public class CompScreenLobbyCtrlBar : MonoBehaviour, IWidgetController
	{
		//? bar is focused on users (? observer switchable)
		// TODO: TL; TI; use view modes to display server modes, so can get rid of commands from ServiceNetwork

		[Range(0f, 3f)] public float ButtonSpeedSlideSec = 1f;
		[Range(0f, 1f)] public float ButtonSpeedScaleSec = .2f;

		private readonly Queue<IEnumerator> _tasks = new();
		private readonly List<CompScreenLobbyBtnBase> _buttons = new();

		private RectTransform _transform;

		private IScheduler _scheduler;

		public void OnWidgetEnable() { }

		public void OnWidgetDisable() { }

		public IScheduler Scheduler => _scheduler ?? new SchedulerTaskDefault();

		public void SetScheduler<T>() where T : IScheduler, new()
		{
			_scheduler = new T();
			if(_scheduler is SchedulerTaskBase cast)
			{
				cast.QueueTasks = _tasks;
			}
		}

		private Vector3 GetPointLocalOnAnimLine(float normalized)
		{
			// to enable scroll bar: start and stop need to be changed accordingly
			// Vector3 is for widget rotation, not used at a time
			var rect = _transform.rect;
			var pivot = _transform.pivot;
			// strict the middle of the widget
			var correction = new Vector3(rect.width * pivot.x, 0f);
			var baseStart = (new Vector3(rect.xMax, rect.yMin) + new Vector3(rect.xMax, rect.yMax)) * .5f;
			var baseStop = (new Vector3(rect.xMin, rect.yMin) + new Vector3(rect.xMin, rect.yMax)) * .5f;
			return (baseStop - baseStart) * normalized + correction;
		}

		private void ButtonWidgetInit(CompScreenLobbyBtnBase component)
		{
			var transformWidget = component.GetComponent<RectTransform>();
			transformWidget.localPosition = GetPointLocalOnAnimLine(0f);
			transformWidget.localRotation = _transform.localRotation;

			var stop = GetPointLocalOnAnimLine(1f);
			var start = GetPointLocalOnAnimLine(0f);

			component.SizeLocalInitial = transformWidget.rect.size;
			component.PositionLocalTarget = stop;
			var stepNormalized = (start - stop).normalized;

			for(var index = 0; index < _buttons.Count; index++)
			{
				component.PositionLocalTarget += stepNormalized * _buttons[index].SizeLocalInitial.x;
			}

			component.UpdateView();

			_buttons.Add(component);
		}

		public IEnumerator TaskAppendButtonServer(CxId idModel)
		{
			var load = Singleton<ServiceResources>.I.LoadAssetAsLibrary<CompScreenLobbyBtnServer>(
				ServiceResources.ID_RESOURCE_UI_LOBBY_ITEM_SERVER_S,
				transform,
				CxOrigin.Identity);

			load.IdModel = idModel;
			ButtonWidgetInit(load);
			load.OnClick = OnButtonItemServer;

			yield break;
		}

		public IEnumerator TaskAppendOrUpdateButtonUser(CxId idModel)
		{
			var instance = _buttons.Find(_ => _.IdModel == idModel);
			if(instance is CompScreenLobbyBtnUser cast)
			{
				cast.UpdateView();
			}
			else
			{
				var load = Singleton<ServiceResources>.I.LoadAssetAsLibrary<CompScreenLobbyBtnUser>(
					ServiceResources.ID_RESOURCE_UI_LOBBY_ITEM_USER_S,
					transform,
					new CxOrigin());

				load.IdModel = idModel;
				ButtonWidgetInit(load);
				load.OnClick = OnButtonItem;
			}

			yield break;
		}

		public IEnumerator TaskUpdateButton(CxId idModel)
		{
			var button = _buttons.Find(_ => _.IdModel == idModel);
			if(button != null)
			{
				button.UpdateView();
			}
			else
			{
				_buttons.ToText(
						$"button is not found: {idModel.ShortForm()} of buttons:",
						_ => _.IdModel.ShortForm())
					.LogWarning();
			}

			yield break;
		}

		public IEnumerator TaskRemoveButton(CxId idModel)
		{
			var button = _buttons.Find(_ => _.IdModel == idModel);
			if(button != null)
			{
				button.ReleaseAllCallbacks();

				_buttons.Remove(button);

				var current = 1f;
				var transformWidget = button.GroupScale;
				while(current > 0f)
				{
					current -= 1f / ButtonSpeedScaleSec * Time.deltaTime;
					transformWidget.localScale = Vector3.one * current;

					yield return null;
				}

				Destroy(button.gameObject);
			}
			else
			{
				_buttons.ToText(
						$"button is not found: {idModel.ShortForm()} of buttons:",
						_ => _.IdModel.ShortForm())
					.LogWarning();
			}
		}

		public IEnumerator TaskUpdateFocus()
		{
			for(var index = 0; index < _buttons.Count; index++)
			{
				_buttons[index].UpdateView();
			}

			yield break;
		}

		private void OnButtonItemServer(CxId idServer)
		{
			"press: 'Server'".Log();

			var idUser = Singleton<ServiceNetwork>.I.IdCurrentUser;
			ref var modelUser = ref Singleton<ServiceUI>.I.ModelsUser.Get(idUser, out var contains);
			if(!contains)
			{
				throw new Exception($"can't find local user with id: {idServer.ShortForm()}");
			}

			if(modelUser.IdHostAt == idServer)
			{
				return;
			}

			//TODO: the only place where user desc data is not sufficient: remove direct assignment using command
			modelUser.IdHostAt = idServer;
			Scheduler.PassThrough.Schedule(TaskUpdateFocus);

			Singleton<ServicePawns>.I.Events.Enqueue(
				new CmdPawnLobbySetChangeTo
				{
					IdServer = idServer,
				});
		}

		private void OnButtonItem(ModelViewUser model)
		{
			"press: 'User'".Log();

			// TODO: remove user from participants
		}

		public IEnumerable<CxId> GetButtons()
		{
			return _buttons.Select(_ => _.IdModel);
		}

		private void Awake()
		{
			_transform = GetComponent<RectTransform>();
		}

		private void Update()
		{
			_tasks.ExecuteTasksSimultaneously();

			BarWidgetScroll();

			#if UNITY_EDITOR
			DrawGizmo();
			#endif
		}

		private void BarWidgetScroll()
		{
			var stop = GetPointLocalOnAnimLine(1f);
			var start = GetPointLocalOnAnimLine(0f);

			var current = stop;
			var stepNormalized = (start - stop).normalized;
			var length = (start - stop).magnitude;
			for(var index = 0; index < _buttons.Count; index++)
			{
				var transformWidget = _buttons[index].GroupMove;

				_buttons[index].PositionLocalTarget = current;
				current += stepNormalized * _buttons[index].SizeLocalInitial.x;

				var position = transformWidget.localPosition;
				var step = -stepNormalized * (length / ButtonSpeedSlideSec * Time.deltaTime);
				var next = position + step;
				var isStop = Vector3.Dot((_buttons[index].PositionLocalTarget - next).normalized, stepNormalized) >= 0f;

				transformWidget.localPosition = isStop
					? _buttons[index].PositionLocalTarget
					: next;
			}
		}

		#if UNITY_EDITOR
		private void DrawGizmo()
		{
			var animLineStart = transform.TransformPoint(GetPointLocalOnAnimLine(0f));
			var animLineStop = transform.TransformPoint(GetPointLocalOnAnimLine(1f));
			const float DIVIDED_F = 12f;
			var step = (animLineStop - animLineStart) / DIVIDED_F;
			var segment = 0f;
			while(segment < DIVIDED_F)
			{
				Debug.DrawLine(
					animLineStart + step * segment,
					animLineStart + step * (segment + 1f),
					Color.Lerp(Color.black, Color.yellow, segment / DIVIDED_F));
				segment += 1f;
			}
			new CxOrigin { Location = animLineStart }.ToMeta().SetSize(.02f).Draw();
			new CxOrigin { Location = animLineStop }.ToMeta().SetSize(.05f).Draw();

			for(var index = 0; index < _buttons.Count; index++)
			{
				var world = _transform.TransformPoint(_buttons[index].PositionLocalTarget);
				new CxOrigin { Location = world }.ToMeta().SetSize(.03f).SetColor(Color.cyan).Draw();
			}
		}
		#endif
	}
}
