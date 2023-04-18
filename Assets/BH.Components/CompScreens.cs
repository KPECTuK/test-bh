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
	public class CompScreens : MonoBehaviour
	{
		// alternative to IsBusy request: both are rough implementation
		public class Signal
		{
			public bool Is { get; set; }
		}

		private readonly List<CompScreen> _screens = new();
		private readonly Queue<IEnumerator> _tasks = new();

		private Coroutine _current;

		private void Update()
		{
			// this is busy
			if(_current == null)
			{
				if(_tasks.TryDequeue(out var iterator))
				{
					_current = StartCoroutine(iterator);
				}
				else
				{
					while(Singleton<ServiceUI>.I.Events.TryPeek(out var @event))
					{
						// stop queue to wait screen transition (batching for particular screen)
						if(@event is CmdViewScreenChange)
						{
							var active = GetActiveScreen();
							if(active == null || !active.IsBusy)
							{
								TryRunCommand(@event);
							}

							break;
						}

						TryRunCommand(@event);
					}
				}
			}
		}

		private void TryRunCommand(ICommand<CompScreens> @event)
		{
			if(@event.Assert(this))
			{
				$"running view command: {@event.GetType().NameNice()}".Log();

				@event.Execute(this);
			}
			else
			{
				$"skip view command due conditions: {@event.GetType().NameNice()}".LogWarning();
			}

			Singleton<ServiceUI>.I.Events.Dequeue();
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

		public void RegisterScreen(CompScreen screen)
		{
			if(_screens.Contains(screen))
			{
				throw new Exception("registered already");
			}

			screen.Init();
			_screens.Add(screen);
		}

		public void UnregisterScreen(CompScreen screen)
		{
			_screens.Remove(screen);
		}

		public void Schedule(IEnumerator task)
		{
			_tasks.Enqueue(task);
		}

		public IEnumerator TaskScreenChange(string nameScreenTarget)
		{
			var screenTarget = _screens
				.Find(_ => _
					.name
					.Contains(
						nameScreenTarget,
						StringComparison.InvariantCultureIgnoreCase));

			if(screenTarget == null)
			{
				Debug.LogError($"unregistered screen: {nameScreenTarget}");
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
					var signalFade = new Signal();
					screenSource.GoFade(signalFade);
					while(!signalFade.Is)
					{
						yield return null;
					}
				}

				var signalRaise = new Signal();
				screenTarget.GoRaise(signalRaise);
				while(!signalRaise.Is)
				{
					yield return null;
				}
			}

			_current = null;
		}
	}
}
