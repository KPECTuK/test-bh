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
		private CxId[] _idsMap;

		public CxId GetNextFeatureAvailableTo(CxId idUser)
		{
			// if its occupied already
			for(var index = 0; index < _idsMap.Length; index++)
			{
				if(_idsMap[index] == idUser)
				{
					return _idsFeature[index];
				}
			}

			// search for first free
			for(var index = 0; index < _idsMap.Length; index++)
			{
				if(_idsMap[index].IsEmpty)
				{
					_idsMap[index] = idUser;
					return _idsFeature[index];
				}
			}

			// return empty
			return CxId.Empty;
		}

		public void ReleaseFeature(CxId idUser)
		{
			var index = 0;
			for(; index < _idsMap.Length; index++)
			{
				if(_idsMap[index] == idUser)
				{
					_idsMap[index] = CxId.Empty;
				}
			}
		}

		public void Reset()
		{
			_idsFeature = Singleton<ServiceResources>.I
				.GetProtoSettingsPawn()
				.Features
				.Select(_ => _.IdFeature)
				.ToArray();
			var size = _idsFeature.Length;
			_idsMap = new CxId[size];
		}

		public void Dispose() { }
	}

	public sealed class CmdPawnDestroy : ICommand<CompPawnSpawners>
	{
		public CxId IdModel;

		public bool Assert(CompPawnSpawners context)
		{
			return Singleton<ServicePawns>.I.Instances.FindAs(_ => _.IdModel == IdModel) != null;
		}

		public void Execute(CompPawnSpawners context)
		{
			var instance = Singleton<ServicePawns>.I.Instances.RemoveItemBy(_ => _.IdModel == IdModel);
			context.Schedule(context.TaskDestroy(instance));
		}
	}

	public sealed class CmdPawnLobbyCreate : ICommand<CompPawnSpawners>
	{
		public CxId IdModel;

		public bool Assert(CompPawnSpawners context)
		{
			var instance = Singleton<ServicePawns>.I.Instances.FindAs(_ => _.IdModel == IdModel);
			var idsUser = new CxId[4];
			var idUserCurrent = Singleton<ServiceNetwork>.I.NetworkModeShared.IdServerCurrent;
			var users = Singleton<ServiceUI>.I.ModelsUser.GetRecentForHost(idsUser, idUserCurrent);
			var isConditions = users < 4 && instance == null;

			var notionPawn = instance == null ? "no pawn" : instance.IdModel.ShortForm();
			$"pawn create conditions ({isConditions}): {notionPawn}; users: {users}".Log();

			return users < 4 && instance == null;
		}

		public void Execute(CompPawnSpawners context)
		{
			var model = Singleton<ServiceUI>.I.ModelsUser.Get(IdModel, out var contains);
			if(contains)
			{
				context.Schedule(context.TaskSpawnForLobby(model));
			}
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
			throw new NotImplementedException();

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

			if(Model.IdHost.IsEmpty)
			{
				var buffer = new CxId[4];
				var users = Singleton<ServiceUI>.I.ModelsUser.GetRecentForHost(buffer, Model.IdHost);
				var isUserCurrent = Singleton<ServiceNetwork>.I.IdCurrentUser;
				for(var index = 0; index < users; index++)
				{
					var idUser = buffer[index];
					if(idUser == isUserCurrent)
					{
						continue;
					}

					Singleton<ServicePawns>.I.Events.Enqueue(
						new CmdPawnLobbyCreate
						{
							IdModel = idUser,
						});
				}
			}
		}
	}
}
