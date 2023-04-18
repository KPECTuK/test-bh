using System.Collections;
using System.Collections.Generic;
using BH.Model;
using UnityEngine;

namespace BH.Components
{
	[RequireComponent(typeof(RectTransform))]
	public class CompScreenLobbyCtrlBar : MonoBehaviour
	{
		//? bar is focused on users (? observer switchable)
		// TODO: TL; TI; use view modes to display server modes, so can get rid of commands from ServiceNetwork

		[Range(0f, 3f)] public float ButtonSpeedSlideSec = 1f;
		[Range(0f, 1f)] public float ButtonSpeedScaleSec = .2f;

		private readonly Queue<IEnumerator> _tasks = new();
		private readonly List<CompScreenLobbyBtnBase> _buttons = new();

		private RectTransform _transform;

		private void Awake()
		{
			_transform = GetComponent<RectTransform>();
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

		public void Schedule(IEnumerator task)
		{
			_tasks.Enqueue(task);
		}

		public IEnumerator TaskAppendButtonAnim(ModelViewServer model)
		{
			var load = Singleton<ServiceResources>.I.LoadAssetAsLibrary<CompScreenLobbyBtnServer>(
				ServiceResources.ID_RESOURCE_UI_LOBBY_ITEM_SERVER_S,
				transform,
				CxOrigin.Identity);

			load.Model = model;
			ButtonWidgetInit(load);
			load.OnClick = OnButtonItem;

			yield break;
		}

		public IEnumerator TaskAppendButtonAnim(ModelViewUser model)
		{
			var load = Singleton<ServiceResources>.I.LoadAssetAsLibrary<CompScreenLobbyBtnUser>(
				ServiceResources.ID_RESOURCE_UI_LOBBY_ITEM_USER_S,
				transform,
				new CxOrigin());

			load.Model = model;
			ButtonWidgetInit(load);
			load.OnClick = OnButtonItem;

			yield break;
		}

		public IEnumerator TaskUpdateButtonAnim(ModelViewServer model)
		{
			var button = _buttons.Find(_ => _.IsModel(model));
			if(button != null)
			{
				button.UpdateView();
			}

			yield break;
		}

		public IEnumerator TaskUpdateButtonAnim(ModelViewUser model)
		{
			var button = _buttons.Find(_ => _.IsModel(model));
			if(button != null)
			{
				button.UpdateView();
			}

			yield break;
		}

		public IEnumerator TaskRemoveButtonAnim(ModelViewServer model)
		{
			var button = _buttons.Find(_ => _.IsModel(model)) as CompScreenLobbyBtnServer;
			if(button == null)
			{
				$"button is not found (server): {model.IdHost}".LogWarning();

				yield break;
			}

			button.OnClick = null;

			var current = 1f;
			var transformWidget = button.GroupScale;
			while(current > 0f)
			{
				current -= 1f / ButtonSpeedScaleSec * Time.deltaTime;
				transformWidget.localScale = Vector3.one * current;

				yield return null;
			}

			_buttons.Remove(button);
			Destroy(button.gameObject);
		}

		public IEnumerator TaskRemoveButtonAnim(ModelViewUser model)
		{
			var button = _buttons.Find(_ => _.IsModel(model)) as CompScreenLobbyBtnUser;
			if(button == null)
			{
				$"button is not found (user): {model.IdUser}".LogWarning();

				yield break;
			}

			button.OnClick = null;

			var current = 1f;
			var transformWidget = button.GroupScale;
			while(current > 0f)
			{
				current -= 1f / ButtonSpeedScaleSec * Time.deltaTime;
				transformWidget.localScale = Vector3.one * current;

				yield return null;
			}

			_buttons.Remove(button);
			Destroy(button.gameObject);
		}

		public IEnumerator TaskUpdateFocus()
		{
			for(var index = 0; index < _buttons.Count; index++)
			{
				_buttons[index].UpdateView();
			}

			yield break;
		}

		private void Update()
		{
			for(int index = 0,
				size = _tasks.Count; index < size; index++)
			{
				var task = _tasks.Dequeue();
				if(task.MoveNext())
				{
					_tasks.Enqueue(task);
				}
			}

			BarWidgetScroll();

			#if UNITY_EDITOR
			DrawGizmo();
			#endif
		}

		private void OnButtonItem(ModelViewServer model)
		{
			"press: 'Server'".Log();
			
			var modelUser = Singleton<ServiceUI>.I.ModelsUser
				.GetById(Singleton<ServiceNetwork>.I.IdCurrentUser);
			modelUser.IdAtHost = model.IdHost;

			Schedule(TaskUpdateFocus());

			Singleton<ServicePawns>.I.Events.Enqueue(
				new CmdPawnLobbySetChangeTo
				{
					Model = model,
				});
		}

		private void OnButtonItem(ModelViewUser model)
		{
			"press: 'User'".Log();

			// TODO: remove user from participants
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
