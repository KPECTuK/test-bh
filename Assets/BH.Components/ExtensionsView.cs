using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using BH.Model;

namespace BH.Components
{
	public static class ExtensionsView
	{
		// TODO: synchronize GetRecentForHost()

		public static int GetRecentForHost(
			this ListRef<ModelViewUser> source,
			CxId[] into,
			CxId idHost)
		{
			var models = new ModelViewUser[into.Length];
			var size = source.Count;
			for(var index = 0; index < size; index++)
			{
				ref var modelCurrent = ref source[index];

				if(modelCurrent.IdHostAt != idHost)
				{
					continue;
				}

				var indexResult = 0;
				for(; indexResult < models.Length; indexResult++)
				{
					if(models[indexResult].IsEmpty)
					{
						break;
					}

					if(models[indexResult].TimestampDiscovery > modelCurrent.TimestampDiscovery)
					{
						var indexInsert = models.Length - 1;
						while(indexInsert > indexResult)
						{
							into[indexInsert] = into[indexInsert - 1];
							indexInsert--;
						}
						models[indexResult] = modelCurrent;
						break;
					}
				}

				if(indexResult < into.Length)
				{
					models[indexResult] = modelCurrent;
				}
			}

			var count = 0;
			for(var indexIds = 0; indexIds < into.Length; indexIds++)
			{
				if(models[indexIds].IdUser.IsEmpty)
				{
					break;
				}

				count++;
				into[indexIds] = models[indexIds].IdUser;
			}

			return count;
		}

		public static unsafe int GetRecentForHost(
			this ListRef<ModelViewUser> source,
			CxId* intoPtr,
			int numElementsInto,
			CxId idHost)
		{
			// TODO: remove 
			var buffer = new CxId[numElementsInto];
			var numIds = source.GetRecentForHost(buffer, idHost);
			fixed(CxId* bufferPtr = buffer)
			{
				Buffer.MemoryCopy(
					bufferPtr,
					intoPtr,
					numElementsInto * CxId.SIZE_I,
					numIds * CxId.SIZE_I);
			}
			return numIds;
		}

		public static CmdViewLobbyClear BuildForAllExceptSelf(
			this CmdViewLobbyClear target,
			ListRef<ModelViewServer> modelsServer,
			ListRef<ModelViewUser> modelsUser)
		{
			var sizeServers = modelsServer.Count;
			for(var index = 0; index < sizeServers; index++)
			{
				ref var model = ref modelsServer[index];

				if(model.IdHost != Singleton<ServiceNetwork>.I.IdCurrentMachine)
				{
					target.ServersToDelete.Enqueue(model);
				}
			}

			var sizeUsers = modelsServer.Count;
			for(var index = 0; index < sizeUsers; index++)
			{
				ref var model = ref modelsUser[index];

				if(model.IdUser != Singleton<ServiceNetwork>.I.IdCurrentUser)
				{
					target.UsersToDelete.Enqueue(model);
				}
			}

			return target;
		}

		public static CmdViewLobbyClear BuildForAll(
			this CmdViewLobbyClear target,
			ListRef<ModelViewServer> modelsServer,
			ListRef<ModelViewUser> modelsUser)
		{
			var sizeServers = modelsServer.Count;
			for(var index = 0; index < sizeServers; index++)
			{
				ref var model = ref modelsServer[index];
				target.ServersToDelete.Enqueue(model);
			}

			var sizeUsers = modelsUser.Count;
			for(var index = 0; index < sizeUsers; index++)
			{
				ref var model = ref modelsUser[index];
				target.UsersToDelete.Enqueue(model);
			}

			return target;
		}

		/// <summary>
		/// returns operation result, id is returning always
		/// </summary>
		public static bool TryAppendServer(ref Response desc, out CxId idServer)
		{
			if(desc.IdHost.IsEmpty)
			{
				idServer = CxId.Empty;
				return false;
			}

			Singleton<ServiceUI>.I.ModelsUser.Get(desc.IdHost, out var contains);
			if(contains)
			{
				$"user data collision <b><color=white>(server)</color></b> {desc.IdHost.ShortForm()} for operation APPEND".LogError();

				idServer = CxId.Empty;
				return false;
			}

			var modelServer = ModelViewServer.Create(desc);

			Singleton<ServiceUI>.I.ModelsServer.Add(modelServer);
			Singleton<ServiceUI>.I.ModelsServer.ToText($"append <b><color=white>(server)</color></b>: {modelServer}").Log();

			idServer = desc.IdHost;
			return true;
		}

		/// <summary>
		/// returns operation result, id is returning always
		/// </summary>
		public static bool TryAppendUser(ref DataUser data, CxId idHostInitiator, out CxId idUser)
		{
			//! for client cant be empty or current machine id
			//! idHostInitiator is to update idFeature from server, bound by model (selected one)

			// updates are valid from self or server which is user bound to only
			// initiator will always be a local host id on server
			// initiator will always be a remote host which response had been received on clients every call
			// this is the problem to solve actually - grouping (by server)

			if(data.IdUser.IsEmpty)
			{
				idUser = CxId.Empty;
				return false;
			}

			Singleton<ServiceUI>.I.ModelsUser.Get(data.IdUser, out var contains);
			if(contains)
			{
				$"user data collision <b><color=white>(user)</color></b> {data.IdUser.ShortForm()} for operation APPEND".LogError();

				idUser = CxId.Empty;
				return false;
			}

			var modelUser = ModelViewUser.Create(data);

			Singleton<ServiceUI>.I.ModelsUser.Add(modelUser);
			Singleton<ServiceUI>.I.ModelsUser.ToText($"append <b><color=white>(user)</color></b>: {modelUser}").Log();

			idUser = data.IdUser;
			return true;
		}

		public static bool TryRemoveServer(CxId idServer)
		{
			if(idServer.IsEmpty)
			{
				return false;
			}

			var modelServer = Singleton<ServiceUI>.I.ModelsServer.Remove(idServer, out var contains);

			if(contains)
			{
				Singleton<ServiceUI>.I.ModelsServer.ToText($"remove <b><color=white>(server)</color></b>: {modelServer}").Log();
			}
			else
			{
				$"not found <b><color=white>(server)</color></b> {idServer.ShortForm()} for operation REMOVE".LogError();
			}

			return contains;
		}

		public static bool TryRemoveUser(CxId idUser)
		{
			if(idUser.IsEmpty)
			{
				return false;
			}

			var modelUser = Singleton<ServiceUI>.I.ModelsUser.Remove(idUser, out var contains);

			if(contains)
			{
				Singleton<ServiceUI>.I.ModelsUser.ToText($"remove <b><color=white>(user)</color></b>: {modelUser}").Log();
				Singleton<ServicePawns>.I.ReleaseFeature(idUser);
			}
			else
			{
				$"not found <b><color=white>(user)</color></b> {idUser.ShortForm()} for operation REMOVE".LogError();
			}

			return contains;
		}

		public static void TryRemoveUsersByHost(CxId idServer, bool isRemoveSelf = false)
		{
			var idUserCurrent = Singleton<ServiceNetwork>.I.IdCurrentUser;
			var set = Singleton<ServiceUI>.I.ModelsUser;
			var setSize = set.Count;
			for(var count = 0; count < setSize; count++)
			{
				var modelUser = set.Dequeue(out var contains);

				if(modelUser.IdUser == idUserCurrent && !isRemoveSelf)
				{
					set.Enqueue(modelUser);

					if(!modelUser.IdHostAt.IsEmpty)
					{
						//! TODO: to generalized routine
						modelUser.IdHostAt = CxId.Empty;
						Singleton<ServiceUI>.I.Events.Enqueue(new CmdViewLobbyUserUpdate { IdUser = modelUser.IdUser });
						Singleton<ServiceUI>.I.ModelsUser.ToText($"update <b><color=white>(user)</color></b>: {modelUser})").Log();
					}

					continue;
				}

				if(modelUser.IdHostAt != idServer)
				{
					set.Enqueue(modelUser);

					continue;
				}

				Singleton<ServiceUI>.I.Events.Enqueue(new CmdViewLobbyUserRemove { IdUser = modelUser.IdUser });
				Singleton<ServiceUI>.I.ModelsUser.ToText($"remove batch <b><color=white>(user)</color></b>: {modelUser}").Log();
				Singleton<ServicePawns>.I.ReleaseFeature(modelUser.IdUser);
			}

			if(setSize > 0)
			{
				Singleton<ServiceUI>.I.ModelsUser.DeFragment();
			}
		}

		/// <summary>
		/// returns operation result, id is not a default if data has changed
		/// </summary>
		public static bool TryUpdateServer(ref Response desc, out CxId idServer)
		{
			idServer = CxId.Empty;
			if(desc.IdHost.IsEmpty)
			{
				return false;
			}

			ref var modelServer = ref Singleton<ServiceUI>.I.ModelsServer.Get(desc.IdHost, out var contains);

			if(!contains)
			{
				$"not found <b><color=white>(server)</color></b> {desc.IdHost.ShortForm()} for operation UPDATE".LogError();

				return false;
			}

			if(modelServer.UpdateFrom(ref desc))
			{
				Singleton<ServiceUI>.I.ModelsServer.ToText($"update <b><color=white>(server)</color></b>: {modelServer})").Log();
				idServer = modelServer.IdHost;
			}

			return true;
		}

		/// <summary>
		/// returns operation result, id is not a default if data has changed
		/// </summary>
		public static bool TryUpdateUser(ref DataUser data, CxId idHostInitiator, out CxId idUser)
		{
			//! for client cant be empty or current machine id
			//! idHostInitiator is to update idFeature from server, bound by model (selected one)

			// updates are valid from self or server which is user bound to only
			// initiator will always be a local host id on server
			// initiator will always be a remote host which response had been received on clients every call
			// this is the problem to solve actually - grouping (by server)

			idUser = CxId.Empty;

			if(data.IdUser.IsEmpty)
			{
				return false;
			}

			ref var modelUser = ref Singleton<ServiceUI>.I.ModelsUser.Get(data.IdUser, out var contains);

			if(!contains)
			{
				// warning is ok because append goes after update
				$"not found <b><color=white>(user)</color></b> {data.IdUser.ShortForm()} for operation UPDATE".LogWarning();

				return false;
			}

			if(modelUser.UpdateFrom(ref data))
			{
				Singleton<ServiceUI>.I.ModelsUser.ToText($"update <b><color=white>(user)</color></b>: {modelUser})").Log();
				idUser = modelUser.IdUser;
			}

			return true;
		}

		//

		public static bool TryExecuteCommandQueue<T>(this Queue<ICommand<T>> source, T context)
		{
			var notionContext = context?.GetType().NameNice() ?? "undefined";

			while(source.TryPeek(out var @event))
			{
				try
				{
					var notionEvent = @event?.GetType().NameNice() ?? "undefined";

					if(@event == null)
					{
						"skip command due conditions: command is NULL".LogError();
					}
					else
					{
						if(@event.Assert(context))
						{
							$"run command <color=white>{notionEvent}</color> in context <color=white>{notionContext}</color>".Log();

							@event.Execute(context);
						}
						else
						{
							$"skip command due conditions <color=white>{notionEvent}</color> in context <color=white>{notionContext}</color>".LogWarning();
						}
					}
				}
				catch(Exception exception)
				{
					exception.ToText().Log();
				}

				source.Dequeue();

				// stop queue to wait screen transition for example (batching for particular screen)
				if(@event is ICommandBreak<CompScreens>)
				{
					$"stop command queue in context: <color=white>{notionContext}</color>".Log();

					break;
				}
			}

			return source.Count > 0;
		}

		//

		public static IScheduler BuildScheduler<T>(this Queue<IEnumerator> tasks) where T : IScheduler, new()
		{
			var result = new T();
			if(result is SchedulerTaskBase cast)
			{
				cast.QueueTasks = tasks;
			}

			return result;
		}

		/// <summary>
		/// enumerator like behaviour
		/// </summary>
		public static bool ExecuteTasksSimultaneously(this Queue<IEnumerator> tasks)
		{
			var size = tasks.Count;
			for(var index = 0; index < size; index++)
			{
				var task = tasks.Dequeue();
				if(task.MoveNext())
				{
					tasks.Enqueue(task);
				}
			}

			return tasks.Count != 0;
		}

		public static bool ExecuteTasksSequentially(this Queue<IEnumerator> tasks)
		{
			var size = tasks.Count;
			for(var index = 0; index < size; index++)
			{
				var task = tasks.Dequeue();
				if(task.MoveNext())
				{
					tasks.Enqueue(task);
					break;
				}
			}

			return tasks.Count != 0;
		}
	}
}
