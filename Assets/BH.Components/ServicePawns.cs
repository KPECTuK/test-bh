using System;
using System.Collections.Generic;
using System.Linq;
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

		//! variable number of users is not supported by discovery protocol

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

		public CxId GetNextFeatureAvailableTo(CxId idUser)
		{
			//? if in server mode: always use first slot for local user - not to use is a game feature

			// if its occupied already
			for(var index = 0; index < _idsUsersMap.Length; index++)
			{
				if(_idsUsersMap[index] == idUser)
				{
					return _idsFeature[index];
				}
			}

			// search for first free
			for(var index = 0; index < _idsUsersMap.Length; index++)
			{
				if(_idsUsersMap[index].IsEmpty)
				{
					_idsUsersMap[index] = idUser;
					return _idsFeature[index];
				}
			}

			// return empty
			return CxId.Empty;
		}

		public void ReleaseFeature(CxId idUser)
		{
			for(var index = 0; index < _idsUsersMap.Length; index++)
			{
				if(_idsUsersMap[index] == idUser)
				{
					_idsUsersMap[index] = CxId.Empty;
				}
			}
		}

		public void ReleaseAsset(CxId idUser)
		{
			for(var index = 0; index < _idsUsersMap.Length; index++)
			{
				if(_idsUsersMap[index] == idUser)
				{
					_isSpawned[index] = false;
				}
			}
		}

		public void AcquireAsset(CxId idUser)
		{
			for(var index = 0; index < _idsUsersMap.Length; index++)
			{
				if(_idsUsersMap[index] == idUser)
				{
					_isSpawned[index] = true;
				}
			}
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
	}

	public sealed class CmdPawnDestroy : ICommand<CompPawnSpawners>
	{
		public CxId IdUser;

		public bool Assert(CompPawnSpawners context)
		{
			return Singleton<ServicePawns>.I.IsSpawned(IdUser);
		}

		public void Execute(CompPawnSpawners context)
		{
			Singleton<ServicePawns>.I.ReleaseAsset(IdUser);
			context.Schedule(context.TaskDestroy(IdUser));
		}
	}

	public sealed class CmdPawnLobbyCreate : ICommand<CompPawnSpawners>
	{
		public CxId IdUser;

		public bool Assert(CompPawnSpawners context)
		{
			if(IdUser.IsEmpty)
			{
				"pawn create conditions: user id is empty".LogWarning();

				return false;
			}

			if(Singleton<ServicePawns>.I.IsSpawned(IdUser))
			{
				$"pawn create conditions: exists ({IdUser})".Log();
				
				return false;
			}

			if(Singleton<ServicePawns>.I.NumPawnsSpawned == Singleton<ServicePawns>.I.MaxPawnsSpawned)
			{
				$"pawn create conditions: spawn cap reached ({Singleton<ServicePawns>.I.NumPawnsSpawned})".Log();

				return false;
			}

			return true;
		}

		public void Execute(CompPawnSpawners context)
		{
			Singleton<ServicePawns>.I.AcquireAsset(IdUser);
			context.Schedule(context.TaskSpawnForLobby(IdUser));
		}
	}

	public sealed class CmdPawnLobbySetChangeTo : ICommand<CompPawnSpawners>
	{
		public CxId IdServer;

		public bool Assert(CompPawnSpawners context)
		{
			return !IdServer.IsEmpty;
		}

		public unsafe void Execute(CompPawnSpawners context)
		{
			var max = Singleton<ServicePawns>.I.MaxPawnsSpawned;
			var idsBufferPtr = stackalloc CxId[max];

			var numSpawned = Singleton<ServicePawns>.I.GetIdsSpawned(idsBufferPtr, max);
			for(var index = 0; index < numSpawned; index++)
			{
				Singleton<ServicePawns>.I.Events.Enqueue(
					new CmdPawnDestroy
					{
						IdUser = idsBufferPtr[index],
					});
			}

			var numRecent = Singleton<ServiceUI>.I.ModelsUser.GetRecentForHost(idsBufferPtr, max, IdServer);
			for(var index = 0; index < numRecent; index++)
			{
				Singleton<ServicePawns>.I.Events.Enqueue(
					new CmdPawnLobbyCreate
					{
						IdUser = idsBufferPtr[index],
					});
			}
		}
	}
}
