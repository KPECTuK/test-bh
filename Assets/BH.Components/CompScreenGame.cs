using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BH.Components
{
	[RequireComponent(typeof(CompScreen))]
	public class CompScreenGame : MonoBehaviour, IWidgetController
	{
		public void OnWidgetEnable()
		{
			Cursor.lockState = CursorLockMode.Locked;
		}

		public void OnWidgetDisable()
		{
			Cursor.lockState = CursorLockMode.None;
		}

		private readonly Queue<IEnumerator> _tasks = new();

		private IScheduler _scheduler;

		public IScheduler Scheduler => _scheduler ?? new SchedulerTaskDefault();

		public void SetScheduler<T>() where T : IScheduler, new()
		{
			_scheduler = _tasks.BuildScheduler<T>();
		}
	}
}
