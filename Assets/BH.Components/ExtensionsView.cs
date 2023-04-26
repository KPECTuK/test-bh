using System;
using System.Collections.Generic;
using System.Net;
using BH.Model;

namespace BH.Components
{
	public static class ExtensionsView
	{
		public static T FindAs<T>(this Queue<T> source, Predicate<T> predicate)
		{
			var size = source.Count;
			for(var index = 0; index < size; index++)
			{
				var item = source.Dequeue();
				source.Enqueue(item);

				// see: ModelViewServer.IEquatable<> also
				if(predicate(item))
				{
					return item;
				}
			}

			return default;
		}

		public static T RemoveItemBy<T>(this Queue<T> source, Predicate<T> predicate)
		{
			var size = source.Count;
			for(var index = 0; index < size; index++)
			{
				var item = source.Dequeue();

				if(predicate(item))
				{
					return item;
				}

				source.Enqueue(item);
			}

			return default;
		}

		public static int GetRecentForHost(this ListRef<ModelViewUser> source, CxId[] into, CxId idHost)
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

					if(models[indexResult].FirstUpdated > modelCurrent.FirstUpdated)
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
		public static bool TryAppendServer(ref ResponseServer desc, IPEndPoint epResponseFrom, Uri uriFrom, out CxId idServer)
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

			var modelServer = new ModelViewServer
			{
				IdHost = desc.IdHost,
				ServerUsersTotal = desc.ServerUsersTotal,
				IdOwner = desc.Owner.IdUser,
				LastUpdated = DateTime.UtcNow,
				HostIp = epResponseFrom,
				HostUri = uriFrom,
			};

			Singleton<ServiceUI>.I.ModelsServer.Add(modelServer);
			Singleton<ServiceUI>.I.ModelsServer.ToText($"append <b><color=white>(server)</color></b>: {modelServer}").Log();

			idServer = desc.IdHost;
			return true;
		}

		/// <summary>
		/// returns operation result, id is returning always
		/// </summary>
		public static bool TryAppendUser(ref ResponseUser response, ref CxId idHostAt, out CxId idUser)
		{
			if(response.IdUser.IsEmpty)
			{
				idUser = CxId.Empty;
				return false;
			}

			Singleton<ServiceUI>.I.ModelsUser.Get(response.IdUser, out var contains);
			if(contains)
			{
				$"user data collision <b><color=white>(user)</color></b> {response.IdUser.ShortForm()} for operation APPEND".LogError();

				idUser = CxId.Empty;
				return false;
			}

			var modelUser = new ModelViewUser
			{
				IdUser = response.IdUser,
				IdFeature = response.IdFeature,
				IsReady = response.IsReady,
				IdCamera = CxId.Empty,
				FirstUpdated = DateTime.UtcNow,
				LastUpdated = DateTime.UtcNow,
				IdHostAt = idHostAt,
			};

			Singleton<ServiceUI>.I.ModelsUser.Add(modelUser);
			Singleton<ServiceUI>.I.ModelsUser.ToText($"append <b><color=white>(user)</color></b>: {modelUser}").Log();

			idUser = response.IdUser;
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

		/// <summary>
		/// returns operation result, id is not a default if data has changed
		/// </summary>
		public static bool TryUpdateServer(ref ResponseServer desc, out CxId idServer)
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
		public static bool TryUpdateUser(ref ResponseUser response, out CxId idUser)
		{
			idUser = CxId.Empty;
			
			if(response.IdUser.IsEmpty)
			{
				return false;
			}

			ref var modelUser = ref Singleton<ServiceUI>.I.ModelsUser.Get(response.IdUser, out var contains);

			if(!contains)
			{
				$"not found <b><color=white>(user)</color></b> {response.IdUser.ShortForm()} for operation UPDATE".LogError();

				return false;
			}

			if(modelUser.UpdateFrom(ref response))
			{
				Singleton<ServiceUI>.I.ModelsUser.ToText($"update <b><color=white>(user)</color></b>: {modelUser})").Log();
				idUser = modelUser.IdUser;
			}

			return true;
		}
	}
}
