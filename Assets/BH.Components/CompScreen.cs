using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BH.Components
{
	[RequireComponent(typeof(Canvas))]
	[RequireComponent(typeof(CanvasGroup))]
	public class CompScreen : MonoBehaviour, IWidgetController
	{
		//? NOTE: TL; TI;
		//? NOTE: can dispatch commands through the ui elements tree
		//? NOTE: consuming particular ones from parents queue by each child
		//? NOTE: to its own queue

		private const float INTERVAL_TRANS_MIN_F = .05f;

		public float IntervalTrans = .1f;

		private CanvasGroup _canvasGroup;
		private CompScreens _controller;

		private readonly Queue<IEnumerator> _tasks = new();

		private IScheduler _scheduler;

		public IScheduler Scheduler => _scheduler ?? new SchedulerTaskDefault();

		public void SetScheduler<T>() where T : IScheduler, new()
		{
			_scheduler = _tasks.BuildScheduler<T>();
		}

		public void OnWidgetEnable() { }

		public void OnWidgetDisable() { }

		public bool IsActiveScreen => _canvasGroup.interactable;

		private void Awake()
		{
			_canvasGroup = GetComponent<CanvasGroup>();
			_controller = GetComponentInParent<CompScreens>();

			if(_controller == null)
			{
				throw new Exception($"no controller for screen: {name}");
			}

			_controller.RegisterScreen(this);
		}

		private void OnDestroy()
		{
			if(_controller != null)
			{
				_controller.UnregisterScreen(this);
			}

			Scheduler.Clear();
		}

		private void Update()
		{
			_tasks.ExecuteTasksSequentially();
		}

		public IEnumerator TaskScreenFadeIn()
		{
			_canvasGroup.interactable = true;
			_canvasGroup.blocksRaycasts = true;

			var value = _canvasGroup.alpha;
			var interval = IntervalTrans < INTERVAL_TRANS_MIN_F
				? INTERVAL_TRANS_MIN_F
				: IntervalTrans;
			var speed = 1f / interval;
			while(value < 1f)
			{
				value += speed * Time.deltaTime;
				_canvasGroup.alpha = value;

				//Debug.Log($"rising: {name}");

				yield return null;
			}

			_canvasGroup.alpha = 1f;

			var customs = GetComponents<IWidgetController>();
			for(var index = 0; index < customs.Length; index++)
			{
				customs[index].OnWidgetEnable();
			}
		}

		public IEnumerator TaskScreenFadeOut()
		{
			var value = _canvasGroup.alpha;
			var interval = IntervalTrans < INTERVAL_TRANS_MIN_F
				? INTERVAL_TRANS_MIN_F
				: IntervalTrans;
			var speed = 1f / interval;
			while(value > 0f)
			{
				value -= speed * Time.deltaTime;
				_canvasGroup.alpha = value;

				//Debug.Log($"fading: {name}");

				yield return null;
			}

			_canvasGroup.alpha = 0f;

			_canvasGroup.interactable = false;
			_canvasGroup.blocksRaycasts = false;

			var customs = GetComponents<IWidgetController>();
			for(var index = 0; index < customs.Length; index++)
			{
				customs[index].OnWidgetDisable();
			}
		}
	}
}
