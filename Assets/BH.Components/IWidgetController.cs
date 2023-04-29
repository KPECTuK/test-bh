using System;
using System.Collections;
using System.Collections.Generic;
using BH.Model;
using UnityEngine;

namespace BH.Components
{
	public interface IWidgetController
	{
		void OnWidgetEnable();
		void OnWidgetDisable();

		IScheduler Scheduler { get; }
		void SetScheduler<T>() where T : IScheduler, new();
	}

	// TODO: extend task name with source class

	/// <summary>
	/// it might be solved by the commands, but it aligned to view, and, more over,
	/// it could serve as any type of filter\provider, not the type only
	/// </summary>
	public interface IScheduler
	{
		bool IsInProgress { get; }

		IScheduler PassThrough { get; }

		void Schedule(Func<IEnumerator> taskFactory);
		
		/// <remarks> NOTE: first arg is a task context </remarks>
		void Schedule<T>(T idOver, Func<T, IEnumerator> taskFactory);

		IEnumerator Wait();

		void Clear();
	}

	/// <summary>
	/// maintains execution queue
	/// </summary>
	public abstract class SchedulerTaskBase
	{
		public Queue<IEnumerator> QueueTasks;

		protected IScheduler Fallback;

		//? not all of them can be canceled
		public bool IsCanceled;

		public bool IsInProgress => IsCanceled || QueueTasks is { Count: > 0 };

		public void Clear()
		{
			QueueTasks?.Clear();
		}

		public IScheduler SetFallBack<T>() where T : IScheduler, new()
		{
			Fallback = QueueTasks.BuildScheduler<T>();
			return Fallback;
		}

		public IEnumerator Wait()
		{
			return new WaitWhile(() => IsInProgress);
		}
	}

	/// <summary>
	/// accepts user ids, executes tasks for users models
	/// </summary>
	public sealed class SchedulerTaskModelUser : SchedulerTaskBase, IScheduler
	{
		public IScheduler PassThrough => QueueTasks.BuildScheduler<SchedulerTaskAll>();
		
		public void Schedule(Func<IEnumerator> taskFactory)
		{
			throw new NotSupportedException("context is unknown");
		}

		public void Schedule<T>(T idOver, Func<T, IEnumerator> taskFactory)
		{
			if(idOver is CxId cast)
			{
				Schedule(cast, taskFactory as Func<CxId, IEnumerator>);
			}
			else
			{
				var name = taskFactory.Method.Name;
				$"pass task '{name}' by scheduler '{GetType().NameNice()}' for '{idOver}': context is of unexpected type".Log();
			}
		}

		private void Schedule(CxId idOver, Func<CxId, IEnumerator> taskFactory)
		{
			var modelUser = Singleton<ServiceUI>.I.ModelsUser.Get(idOver, out var contains);
			// call in server mode
			var aligned = 
				// local user picks any or no server
				modelUser.IdUser == Singleton<ServiceNetwork>.I.IdCurrentUser ||
				// remote users pick is a current server
				modelUser.IdHostAt == Singleton<ServiceNetwork>.I.IdCurrentMachine;

			if(contains && aligned)
			{
				var name = taskFactory.Method.Name;
				$"run task '{name}' by scheduler '{GetType().NameNice()}' for '{idOver.ShortForm()}'".Log();

				QueueTasks.Enqueue(taskFactory.Invoke(idOver));
			}
			else
			{
				var name = taskFactory.Method.Name;
				$"pass task '{name}' by scheduler '{GetType().NameNice()}' for '{idOver.ShortForm()}': conditions are not met (user id)".Log();
			}
		}
	}

	/// <summary>
	/// accepts server ids, executes tasks for server models
	/// </summary>
	public sealed class SchedulerTaskModelServer : SchedulerTaskBase, IScheduler
	{
		public IScheduler PassThrough => QueueTasks.BuildScheduler<SchedulerTaskAll>();

		public void Schedule(Func<IEnumerator> taskFactory)
		{
			throw new NotSupportedException("context is unknown");
		}

		public void Schedule<T>(T idOver, Func<T, IEnumerator> taskFactory)
		{
			if(idOver is CxId cast)
			{
				Schedule(cast, taskFactory as Func<CxId, IEnumerator>);
			}
			else
			{
				var name = taskFactory.Method.Name;
				$"pass task '{name}' by scheduler '{GetType().NameNice()}' for '{idOver}': context is of unexpected type".Log();
			}
		}

		private void Schedule(CxId idOver, Func<CxId, IEnumerator> taskFactory)
		{
			Singleton<ServiceUI>.I.ModelsServer.Get(idOver, out var contains);
			if(contains)
			{
				var name = taskFactory.Method.Name;
				$"run task '{name}' by scheduler '{GetType().NameNice()}' for '{idOver.ShortForm()}'".Log();

				QueueTasks.Enqueue(taskFactory.Invoke(idOver));
			}
			else
			{
				var name = taskFactory?.Method.Name;
				$"pass task '{name}' by scheduler '{GetType().NameNice()}' for '{idOver.ShortForm()}': conditions are not met (serer id)".Log();
			}
		}
	}

	/// <summary>
	/// bypassing id, executes all
	/// </summary>
	public sealed class SchedulerTaskAll : SchedulerTaskBase, IScheduler
	{
		//! avoid loops
		public IScheduler PassThrough => this;

		public void Schedule(Func<IEnumerator> taskFactory)
		{
			var name = taskFactory.Method.Name;
			$"run task '{name}' by scheduler '{GetType().NameNice()}'".Log();

			QueueTasks.Enqueue(taskFactory.Invoke());
		}

		public void Schedule<T>(T idOver, Func<T, IEnumerator> taskFactory)
		{
			var name = taskFactory.Method.Name;
			$"run task '{name}' by scheduler '{GetType().NameNice()}' for context '{idOver}'".Log();

			QueueTasks.Enqueue(taskFactory.Invoke(idOver));
		}
	}

	/// <summary>
	/// bypassing id, executes none
	/// </summary>
	public sealed class SchedulerTaskDefault : IScheduler
	{
		public bool IsInProgress => false;

		public IScheduler PassThrough => throw new NotSupportedException("nothing to pass through by default");

		public void Schedule(Func<IEnumerator> taskFactory)
		{
			var name = taskFactory?.Method.Name;
			$"pass task '{name}' by 'default' scheduler with no context".Log();
		}

		public void Schedule<T>(T idOver, Func<T, IEnumerator> taskFactory)
		{
			var name = taskFactory?.Method.Name;
			$"pass task '{name}' by 'default' scheduler for context '{idOver}'".Log();
		}

		public IEnumerator Wait()
		{
			throw new NotSupportedException("nothing to wait by default");
		}

		public void Clear() { }
	}
}
