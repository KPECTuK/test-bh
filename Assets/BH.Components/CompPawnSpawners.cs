using System;
using System.Collections;
using System.Collections.Generic;
using BH.Model;
using UnityEngine;

namespace BH.Components
{
	public sealed class CompPawnSpawners : MonoBehaviour
	{
		//? scheduler

		private Transform[] _locators;
		private readonly List<CompPawn> _pawns = new();

		private readonly Queue<IEnumerator> _tasks = new();

		private IScheduler _scheduler;

		public IScheduler Scheduler => _scheduler ?? new SchedulerTaskDefault();

		public void SetScheduler<T>() where T : IScheduler, new()
		{
			_scheduler = _tasks.BuildScheduler<T>();
		}

		private void Awake()
		{
			SetScheduler<SchedulerTaskAll>();

			var buffer = new List<Transform>();
			foreach(var component in GetComponentsInChildren<Transform>())
			{
				if(component.name.Contains("loc_spawn_"))
				{
					buffer.Add(component);
				}
			}

			_locators = buffer.ToArray();
			_locators.ToText("spawn points found", _ => $"'{_.name}': {_.transform.position}").Log();
		}

		private void Update()
		{
			_tasks.ExecuteTasksSimultaneously();

			Singleton<ServicePawns>.I.Events.TryExecuteCommandQueue(this);
		}

		private Vector3 FindSpawnPoint()
		{
			for(var indexLocator = 0; indexLocator < _locators.Length; indexLocator++)
			{
				var locator = _locators[indexLocator];
				var isFree = true;
				for(var indexPawn = 0; indexPawn < _pawns.Count; indexPawn++)
				{
					var instance = _pawns[indexPawn];

					if(instance.Agent.Raycast(instance.transform.position, out var hitPawn))
					{
						throw new Exception("pawn position is out of area");
					}

					if(instance.Agent.Raycast(locator.position, out var hitLocator))
					{
						throw new Exception("spawn position is out of area");
					}

					const float DOUBLE_PAWN_RADIUS_F = 1f;
					var distance = (hitLocator.position - hitPawn.position).magnitude;
					isFree = isFree && distance > DOUBLE_PAWN_RADIUS_F;
				}

				if(isFree)
				{
					_pawns.ToText($"pawn spawn point found: {locator.position}, in pawns collection").Log();

					return locator.position;
				}

				$"passing spawn: {locator.name} ({locator.position})".LogWarning();
			}

			throw new Exception("no spawn point found");
		}

		private CompPawn GetPawnBy(CxId idUser)
		{
			for(var index = 0; index < _pawns.Count; index++)
			{
				if(_pawns[index].IdUser == idUser)
				{
					return _pawns[index];
				}
			}

			return null;
		}

		public IEnumerator TaskPawnRemove(CxId idUser)
		{
			var instance = GetPawnBy(idUser);
			if(instance == null)
			{
				throw new Exception(_pawns.ToText($"can't find pawn instance for user id: {idUser} in"));
			}

			_pawns.Remove(instance);
			instance.Builder.Destroy(instance);

			$"pawn for user id: {idUser.ShortForm()} had been destroyed".Log();

			yield break;
		}

		public IEnumerator TaskPawnAppendOrUpdateLobby(CxId idUser)
		{
			var modeUser = Singleton<ServiceUI>.I.ModelsUser.Get(idUser, out var contains);
			if(!contains)
			{
				throw new Exception($"cant find pawn model for user id: {idUser.ShortForm()}");
			}

			var instance = _pawns.Find(_ => _.IdUser == idUser);
			if(instance != null)
			{
				instance.SetFeatures(modeUser.IdFeature);

				$"pawn for user id: {idUser.ShortForm()} had been updated".Log();
			}
			else
			{
				var positionSpawn = FindSpawnPoint();
				var orientationSpawn = Quaternion.LookRotation(-positionSpawn.normalized, Vector3.up);

				// yield to load
				instance = Singleton<ServiceResources>.I
					.BuildPawn<BuilderPawnLobby>(
						null,
						new CxOrigin
						{
							Location = positionSpawn,
							Orientation = orientationSpawn,
						},
						modeUser);
				_pawns.Add(instance);

				$"pawn for user id: {modeUser.IdUser.ShortForm()} had been created at: {positionSpawn}".Log();
			}

			yield break;
		}

		public IEnumerator TaskPawnAppendGame()
		{
			yield break;
		}
	}
}
