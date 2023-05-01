using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BH.Model;

namespace BH.Components
{
	public class ServicePawns : IService
	{
		// to link all the parts of code dependant
		public const int MAX_NUMBER_OR_PLAYERS_I = 4;

		public readonly Queue<ICommand<CompPawnSpawners>> Events = new();

		// corresponds to feature id and spawned collections
		private CxId[] _idsUsersMap;
		private CxId[] _idsFeature;
		private bool[] _isSpawned;

		//! variable number of users is not supported by discovery protocol (strict number of parties: 4)

		public int MaxPawnsSpawned => _idsUsersMap.Length;

		public int NumPawnsSpawned
		{
			get
			{
				var count = 0;
				for(var index = 0; index < _idsUsersMap.Length; index++)
				{
					count += _idsUsersMap[index].IsEmpty && _isSpawned[index] ? 1 : 0;
				}

				return count;
			}
		}

		public bool IsSpawned(CxId idUser)
		{
			var index = Array.IndexOf(_idsUsersMap, idUser);
			return index != -1 && _isSpawned[index];
		}

		public unsafe int GetIdsSpawned(CxId* setPtr, int sizeSet)
		{
			var indexSet = 0;
			for(var indexMap = 0; indexMap < _idsUsersMap.Length; indexMap++)
			{
				if(_idsUsersMap[indexMap].IsEmpty)
				{
					continue;
				}

				if(!_isSpawned[indexMap])
				{
					continue;
				}

				if(indexSet == sizeSet)
				{
					throw new Exception("out of buffer");
				}

				setPtr[indexSet] = _idsUsersMap[indexMap];
				indexSet++;
			}

			return indexSet;
		}

		//? feature update from server

		public CxId GetNextFeatureAvailableTo(CxId idUser)
		{
			//? if in server mode: always use first slot for local user - not to use is a game feature

			for(var index = 0; index < _idsUsersMap.Length; index++)
			{
				if(_idsUsersMap[index] == idUser)
				{
					$"feature is in hold already by index: {index} for id user: {idUser.ShortForm()}".LogWarning();

					return _idsFeature[index];
				}
			}

			for(var index = 0; index < _idsUsersMap.Length; index++)
			{
				if(_idsUsersMap[index].IsEmpty)
				{
					_idsUsersMap[index] = idUser;

					$"feature acquired by index: {index} for id user: {idUser.ShortForm()}".Log();

					return _idsFeature[index];
				}
			}

			var builder = new StringBuilder().Append($"can't acquire feature at index for user {idUser.ShortForm()}").AppendLine();
			throw new Exception(DumpTo(builder).ToString());
		}

		public void AssignFeature(CxId idUser, CxId idFeature)
		{
			var indexFeature = 0;
			for(; indexFeature < MAX_NUMBER_OR_PLAYERS_I; indexFeature++)
			{
				if(_idsFeature[indexFeature] == idFeature)
				{
					break;
				}
			}

			if(indexFeature == MAX_NUMBER_OR_PLAYERS_I)
			{
				var builder = new StringBuilder().Append($"can't fund feature id: {idFeature.ShortForm()} for user id: {idUser.ShortForm()}").AppendLine();
				throw new Exception(DumpTo(builder).ToString());
			}

			var indexUser = 0;
			for(; indexUser < MAX_NUMBER_OR_PLAYERS_I; indexUser++)
			{
				if(_idsUsersMap[indexUser] == idUser)
				{
					break;
				}
			}

			if(indexUser == MAX_NUMBER_OR_PLAYERS_I)
			{
				for(var index = 0; index < MAX_NUMBER_OR_PLAYERS_I; index++)
				{
					if(_idsUsersMap[index].IsEmpty)
					{
						_idsUsersMap.Swap(index, indexFeature);
						_isSpawned.Swap(index, indexFeature);
						// but, spawned should be false that case
					}
				}
			}
			else
			{
				_idsUsersMap.Swap(indexUser, indexFeature);
				_isSpawned.Swap(indexUser, indexFeature);
			}
		}

		public void ReleaseFeature(CxId idUser)
		{
			for(var index = 0; index < _idsUsersMap.Length; index++)
			{
				if(_idsUsersMap[index] == idUser)
				{
					if(_isSpawned[index])
					{
						$"feature release fault (asset locked by instance) at index: {index} id user: {idUser.ShortForm()}".LogWarning();
					}
					else
					{
						_idsUsersMap[index] = CxId.Empty;

						$"feature released (asset removed) at index: {index} id user: {idUser.ShortForm()}".Log();
					}

					return;
				}
			}

			var builder = new StringBuilder().Append($"can't release feature at index for user {idUser.ShortForm()}").AppendLine();
			throw new Exception(DumpTo(builder).ToString());
		}

		public void ReleaseAsset(CxId idUser)
		{
			for(var index = 0; index < _idsUsersMap.Length; index++)
			{
				if(_idsUsersMap[index] == idUser)
				{
					_isSpawned[index] = false;


					if(_idsFeature[index].IsEmpty)
					{
						_idsUsersMap[index] = CxId.Empty;

						$"asset released (removed) at index: {index} id user: {idUser.ShortForm()}".Log();
					}
					else
					{
						$"asset released (locked by feature id) at index: {index} id user: {idUser.ShortForm()}".Log();
					}

					return;
				}
			}

			var builder = new StringBuilder().Append($"can't release asset at index for user {idUser.ShortForm()}").AppendLine();
			throw new Exception(DumpTo(builder).ToString());
		}

		public void AcquireAsset(CxId idUser)
		{
			for(var index = 0; index < _idsUsersMap.Length; index++)
			{
				if(_idsUsersMap[index] == idUser)
				{
					if(_isSpawned[index])
					{
						$"asset acquired already at index: {index} for user id : {idUser.ShortForm()}".Log();
					}
					else
					{
						_isSpawned[index] = true;

						$"asset (by feature) acquired at index: {index} for user id : {idUser.ShortForm()}".Log();
					}

					return;
				}
			}

			for(var index = 0; index < _idsUsersMap.Length; index++)
			{
				if(_idsUsersMap[index].IsEmpty)
				{
					_isSpawned[index] = true;
					_idsUsersMap[index] = idUser;

					$"asset (no feature, set to: {_idsFeature[index].ShortForm()}) acquired at index: {index} for user id : {idUser.ShortForm()}".Log();

					return;
				}
			}

			var builder = new StringBuilder().Append($"can't acquire asset at index for user {idUser.ShortForm()}").AppendLine();
			throw new Exception(DumpTo(builder).ToString());
		}

		public void Reset()
		{
			var idsFeature = Singleton<ServiceResources>.I
				.GetProtoSettingsPawn()
				.Features
				.Select(_ => _.IdFeature)
				.ToArray();
			_idsFeature = new CxId[MAX_NUMBER_OR_PLAYERS_I];
			_idsUsersMap = new CxId[MAX_NUMBER_OR_PLAYERS_I];
			_isSpawned = new bool[MAX_NUMBER_OR_PLAYERS_I];

			for(var index = 0; index < MAX_NUMBER_OR_PLAYERS_I; index++)
			{
				if(index < idsFeature.Length)
				{
					_idsFeature[index] = idsFeature[index];
				}
			}

			// or default values
		}

		public void Dispose() { }

		public StringBuilder DumpTo(StringBuilder builder)
		{
			for(var index = 0; index < MAX_NUMBER_OR_PLAYERS_I; index++)
			{
				builder
					.Append(" user id: ")
					.Append(_idsUsersMap[index].ShortForm())
					.Append(" feature id: ")
					.Append(_idsFeature[index].ShortForm())
					.Append(_isSpawned[index] ? string.Empty : " not")
					.Append(" spawned")
					.AppendLine();
			}

			return builder;
		}
	}

	public sealed class CmdPawnAppendOrUpdateLobby : ICommand<CompPawnSpawners>
	{
		public CxId IdUser;

		public bool Assert(CompPawnSpawners context)
		{
			if(IdUser.IsEmpty)
			{
				"pawn create conditions: user id is empty".LogWarning();

				return false;
			}

			// removed due update routine in command
			//if(Singleton<ServicePawns>.I.IsSpawned(IdUser))
			//{
			//	$"pawn create conditions: exists ({IdUser.ShortForm()})".Log();

			//	return false;
			//}

			if(Singleton<ServicePawns>.I.NumPawnsSpawned == Singleton<ServicePawns>.I.MaxPawnsSpawned)
			{
				$"pawn create conditions: spawn cap reached ({Singleton<ServicePawns>.I.NumPawnsSpawned})".Log();

				return false;
			}

			var idUserLocal = Singleton<ServiceNetwork>.I.IdCurrentUser;
			var modelUserLocal = Singleton<ServiceUI>.I.ModelsUser.Get(idUserLocal, out var contains);
			var modelUserCurrent = Singleton<ServiceUI>.I.ModelsUser.Get(IdUser, out contains);
			var @is = contains && (idUserLocal == IdUser || modelUserCurrent.IdHostAt == modelUserLocal.IdHostAt);

			if(@is)
			{
				$"pawn create conditions: approved for id user: {IdUser.ShortForm()} ({(idUserLocal == IdUser ? "local" : "remote")}) at host: {modelUserCurrent.IdHostAt.ShortForm()}".Log();
			}
			else
			{
				$"pawn create conditions: lobby presentation filter remoteHostAt: {modelUserCurrent.IdHostAt.ShortForm()} localHostAt: {modelUserLocal.IdHostAt.ShortForm()}".Log();
			}

			return @is;
		}

		public void Execute(CompPawnSpawners context)
		{
			Singleton<ServicePawns>.I.AcquireAsset(IdUser);

			//! problem
			ref var modelUser = ref Singleton<ServiceUI>.I.ModelsUser.Get(IdUser, out var contains);
			modelUser.IdFeature = modelUser.IdFeature.IsEmpty
				? Singleton<ServicePawns>.I.GetNextFeatureAvailableTo(IdUser)
				: modelUser.IdFeature;

			context.Scheduler.Schedule(IdUser, context.TaskPawnAppendOrUpdateLobby);
		}
	}

	public sealed class CmdPawnAppendGame : ICommand<CompPawnSpawners>
	{
		public CxId IdUser;

		public bool Assert(CompPawnSpawners context)
		{
			if(IdUser.IsEmpty)
			{
				"pawn create conditions: user id is empty".LogWarning();

				return false;
			}

			return true;
		}

		public void Execute(CompPawnSpawners context)
		{
			context.Scheduler.Schedule(IdUser, context.TaskPawnAppendGame);
		}
	}

	public sealed class CmdPawnDestroy : ICommand<CompPawnSpawners>
	{
		public CxId IdUser;

		public bool Assert(CompPawnSpawners context)
		{
			var result = Singleton<ServicePawns>.I.IsSpawned(IdUser);
			var builder = new StringBuilder($"pawn destroy condition: {result} for user id: {IdUser.ShortForm()}").AppendLine();
			Singleton<ServicePawns>.I.DumpTo(builder).Log();
			return result;
		}

		public void Execute(CompPawnSpawners context)
		{
			Singleton<ServicePawns>.I.ReleaseAsset(IdUser);
			Singleton<ServicePawns>.I.ReleaseFeature(IdUser);
			context.Scheduler.Schedule(IdUser, context.TaskPawnRemove);
		}
	}

	public sealed class CmdPawnLobbySetChangeTo : ICommand<CompPawnSpawners>
	{
		public CxId IdServer;

		public bool Assert(CompPawnSpawners context)
		{
			// server could be empty for client which does not select its alignment, so:
			// - any existing in server mode which is actually the only local machine in set
			// - any existing and empty in client mode
			return true;
		}

		public unsafe void Execute(CompPawnSpawners context)
		{
			var max = Singleton<ServicePawns>.I.MaxPawnsSpawned;
			var idsSpawnedPtr = stackalloc CxId[max];
			var idsRecentPtr = stackalloc CxId[max];

			var numSpawned = Singleton<ServicePawns>.I.GetIdsSpawned(idsSpawnedPtr, max);
			var numRecent = Singleton<ServiceUI>.I.ModelsUser.GetRecentForHost(idsRecentPtr, max, IdServer);

			for(var index = 0; index < numSpawned; index++)
			{
				var indexRecent = 0;
				for(; indexRecent < numRecent; indexRecent++)
				{
					if(idsSpawnedPtr[index] == idsRecentPtr[indexRecent])
					{
						break;
					}
				}

				if(indexRecent == numRecent)
				{
					Singleton<ServicePawns>.I.Events.Enqueue(
						new CmdPawnDestroy
						{
							IdUser = idsSpawnedPtr[index],
						});
				}
			}

			for(var index = 0; index < numRecent; index++)
			{
				Singleton<ServicePawns>.I.Events.Enqueue(
					new CmdPawnAppendOrUpdateLobby
					{
						IdUser = idsRecentPtr[index],
					});
			}
		}
	}
}
