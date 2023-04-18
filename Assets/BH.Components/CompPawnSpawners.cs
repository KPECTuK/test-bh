using System;
using System.Collections;
using System.Collections.Generic;
using BH.Model;
using UnityEngine;

namespace BH.Components
{
	public sealed class CompPawnSpawners : MonoBehaviour
	{
		private readonly Queue<IEnumerator> _tasks = new();
		private Transform[] _locators;

		private void Awake()
		{
			var buffer = new List<Transform>();
			foreach(var component in GetComponentsInChildren<Transform>())
			{
				if(component.name.Contains("loc_spawn_"))
				{
					buffer.Add(component);
				}
			}

			_locators = buffer.ToArray();
			_locators.ToText("spawn points found").Log();
		}

		private void Update()
		{
			// not a resharper issue
			for(int index = 0,
				size = _tasks.Count; index < size; index++)
			{
				var task = _tasks.Dequeue();
				if(task.MoveNext())
				{
					_tasks.Enqueue(task);
				}
			}

			while(Singleton<ServicePawns>.I.Events.TryPeek(out var @event))
			{
				TryRunCommand(@event);
			}
		}

		private void TryRunCommand(ICommand<CompPawnSpawners> @event)
		{
			if(@event.Assert(this))
			{
				$"running pawns command: {@event.GetType().NameNice()}".Log();

				@event.Execute(this);
			}
			else
			{
				$"skip pawns command due conditions: {@event.GetType().NameNice()}".LogWarning();
			}

			Singleton<ServicePawns>.I.Events.Dequeue();
		}

		private Vector3 FindSpawnPoint()
		{
			for(var indexLocator = 0; indexLocator < _locators.Length; indexLocator++)
			{
				var locator = _locators[indexLocator];
				var queue = Singleton<ServicePawns>.I.Instances;
				var size = queue.Count;
				var isOccupied = false;
				for(var indexPawn = 0; indexPawn < size; indexPawn++)
				{
					var instance = queue.Dequeue();
					queue.Enqueue(instance);

					if(instance.Agent.Raycast(instance.transform.position, out var positionPawn))
					{
						throw new Exception("pawn position is out of area");
					}

					if(instance.Agent.Raycast(locator.position, out var positionLocator))
					{
						throw new Exception("spawn position is out of area");
					}

					const float DOUBLE_PAWN_RADIUS_F = 1f;
					var distance = (positionLocator.position - positionPawn.position).magnitude;
					isOccupied = isOccupied || distance > DOUBLE_PAWN_RADIUS_F;
				}

				if(!isOccupied)
				{
					$"spawn point found: {locator.position}".Log();

					return locator.position;
				}

				$"passing spawn: {locator.name} ({locator.position})".LogWarning();
			}

			throw new Exception("no spawn point found");
		}

		public void Schedule(IEnumerator task)
		{
			_tasks.Enqueue(task);
		}

		public IEnumerator TaskDestroy(CompPawn instance)
		{
			instance.Builder.Destroy(instance);

			$"pawn for [{instance.IdModel}] had been destroyed".Log();

			yield break;
		}

		public IEnumerator TaskSpawnForLobby(ModelViewUser model)
		{
			var positionSpawn = FindSpawnPoint();
			var orientationSpawn = Quaternion.LookRotation(-positionSpawn.normalized, Vector3.up);

			var instance = Singleton<ServiceResources>.I
				.BuildPawn<BuilderPawnLobby>(
					null,
					new CxOrigin
					{
						Location = positionSpawn,
						Orientation = orientationSpawn,
					},
					model);

			Singleton<ServicePawns>.I.Instances.Enqueue(instance);

			$"pawn for [{model.IdUser}] created at: {positionSpawn}".Log();

			yield break;
		}

		public IEnumerator TaskSpawnForGame()
		{
			yield break;
		}
	}
}
