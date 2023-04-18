using System;
using System.Collections.Generic;
using System.Linq;
using BH.Model;

namespace BH.Components
{
	public class ServicePawns : IService
	{
		// TL; TI; not a good idea to store components in static
		public readonly Queue<CompPawn> Instances = new();
		public readonly Queue<ICommand<CompPawnSpawners>> Events = new();

		private CxId[] _idsFeature;

		public CxId GetNextFeatureAvailable()
		{
			var buffer = new ModelViewUser[4];
			var users = Singleton<ServiceUI>.I.ModelsUser.GetRecent(
				buffer,
				Singleton<ServiceNetwork>.I.IdCurrentMachine);
			for(var index = 0; index < users; index++)
			{
				if(Array.IndexOf(_idsFeature, buffer[index].IdFeature) == -1)
				{
					return buffer[index].IdFeature;
				}
			}

			throw new Exception("no feature found");
		}

		public void Reset()
		{
			_idsFeature = Singleton<ServiceResources>.I
				.GetProtoSettingsPawn()
				.Features
				.Select(_ => _.IdFeature)
				.ToArray();
		}

		public void Dispose() { }
	}

	public sealed class CmdPawnDestroy : ICommand<CompPawnSpawners>
	{
		public ModelViewUser Model;

		public bool Assert(CompPawnSpawners context)
		{
			return Singleton<ServicePawns>.I.Instances.FindAs(_ => _.IdModel == Model.IdUser) != null;
		}

		public void Execute(CompPawnSpawners context)
		{
			var instance = Singleton<ServicePawns>.I.Instances.RemoveBy(_ => _.IdModel == Model.IdUser);
			context.Schedule(context.TaskDestroy(instance));
		}
	}

	public sealed class CmdPawnLobbyCreate : ICommand<CompPawnSpawners>
	{
		public ModelViewUser Model;

		public bool Assert(CompPawnSpawners context)
		{
			var instance = Singleton<ServicePawns>.I.Instances.FindAs(_ => _.IdModel == Model.IdUser);
			var models = new ModelViewUser[4];
			var users = Singleton<ServiceUI>.I.ModelsUser
				.GetRecent(models, Singleton<ServiceNetwork>.I.NetworkModeShared.IdServerCurrent);

			$"pawn create conditions ({users < 4 && instance == null}): {(instance == null ? "no pawn" : instance.IdModel.ShortForm())}; users: {users}".Log();
			
			return users < 4 && instance == null;
		}

		public void Execute(CompPawnSpawners context)
		{
			context.Schedule(context.TaskSpawnForLobby(Model));
		}
	}

	public sealed class CmdPawnLobbySetChangeTo : ICommand<CompPawnSpawners>
	{
		public ModelViewServer Model;

		public bool Assert(CompPawnSpawners context)
		{
			return true;
		}

		public void Execute(CompPawnSpawners context)
		{
			var queue = Singleton<ServicePawns>.I.Instances;
			var size = queue.Count;
			for(var index = 0; index < size; index++)
			{
				var instance = queue.Dequeue();
				if(instance.IdModel != Singleton<ServiceNetwork>.I.IdCurrentUser)
				{
					context.Schedule(context.TaskDestroy(instance));
				}
				else
				{
					queue.Enqueue(instance);
				}
			}

			if(Model != null)
			{
				var buffer = new ModelViewUser[4];
				var users = Singleton<ServiceUI>.I.ModelsUser.GetRecent(buffer, Model.IdHost);
				for(var index = 0; index < users; index++)
				{
					var modelUser = buffer[index];
					if(modelUser.IdUser == Singleton<ServiceNetwork>.I.IdCurrentUser)
					{
						continue;
					}

					Singleton<ServicePawns>.I.Events.Enqueue(
						new CmdPawnLobbyCreate
						{
							Model = modelUser,
						});
				}
			}
		}
	}

	public sealed class CmdPawnLobbySetUpdate : ICommand<CompPawnSpawners>
	{
		public ModelViewServer Model;

		public bool Assert(CompPawnSpawners context)
		{
			return true;
		}

		public void Execute(CompPawnSpawners context)
		{
			"not implemented".LogWarning();
		}
	}

	public sealed class CmdPawnGameCreate : ICommand<CompPawnSpawners>
	{
		public ModelViewUser Model;

		public bool Assert(CompPawnSpawners context)
		{
			//? ?
			return true;
		}

		public void Execute(CompPawnSpawners context) { }
	}
}
