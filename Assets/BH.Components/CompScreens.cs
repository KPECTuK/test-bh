using System;
using System.Collections;
using System.Collections.Generic;
using BH.Model;
using UnityEngine;
using UnityEngine.UI;

namespace BH.Components
{
	[RequireComponent(typeof(Canvas))]
	[RequireComponent(typeof(CanvasScaler))]
	public class CompScreens : MonoBehaviour, IWidgetController
	{
		private readonly Queue<IEnumerator> _tasks = new();
		private readonly List<CompScreen> _screens = new();

		private IScheduler _scheduler;

		public IScheduler Scheduler => _scheduler ?? new SchedulerTaskDefault();

		public void SetScheduler<T>() where T : IScheduler, new()
		{
			_scheduler = _tasks.BuildScheduler<T>();
		}

		public void OnWidgetEnable()
		{
			// no top controller
		}

		public void OnWidgetDisable()
		{
			// no top controller
		}

		public void RegisterScreen(CompScreen screen)
		{
			if(screen != null)
			{
				_screens.Add(screen);

				screen.SetScheduler<SchedulerTaskAll>();

				var canvasGroup = screen.GetComponent<CanvasGroup>();
				canvasGroup.alpha = 0f;
				canvasGroup.interactable = false;
				canvasGroup.blocksRaycasts = false;

				screen.OnWidgetEnable();
			}
			else
			{
				throw new Exception("trying to register screen as NULL");
			}
		}

		public void UnregisterScreen(CompScreen screen)
		{
			if(screen != null)
			{
				screen.OnWidgetDisable();
				_screens.Remove(screen);
			}
			else
			{
				throw new Exception("trying to unregister screen as NULL");
			}
		}

		public CompScreen GetActiveScreen()
		{
			for(var index = 0; index < _screens.Count; index++)
			{
				if(_screens[index].IsActiveScreen)
				{
					return _screens[index];
				}
			}

			return null;
		}

		private void Awake()
		{
			SetScheduler<SchedulerTaskAll>();
		}

		private void Update()
		{
			if(!_tasks.ExecuteTasksSequentially())
			{
				Singleton<ServiceUI>.I.Events.TryExecuteCommandQueue(this);
			}
		}

		public IEnumerator TaskScreenChange(string nameScreenTarget)
		{
			// ReSharper disable once InconsistentNaming
			static bool isName(string compName, string comparand)
			{
				return compName.Contains(comparand, StringComparison.InvariantCultureIgnoreCase);
			}

			var screenTarget = _screens.Find(_ => isName(_.name, nameScreenTarget));

			if(screenTarget == null)
			{
				$"unregistered screen: {nameScreenTarget}".LogWarning();
			}
			else
			{
				if(screenTarget.IsActiveScreen)
				{
					yield break;
				}

				var screenSource = _screens.Find(_ => _.IsActiveScreen);
				if(screenSource != null)
				{
					screenSource.Scheduler.Schedule(screenSource.TaskScreenFadeOut);
					yield return screenSource.Scheduler.Wait();
					screenSource.OnWidgetDisable();
				}

				screenTarget.OnWidgetEnable();
				screenTarget.Scheduler.Schedule(screenTarget.TaskScreenFadeIn);
				yield return screenTarget.Scheduler.Wait();
			}
		}
	}
}
