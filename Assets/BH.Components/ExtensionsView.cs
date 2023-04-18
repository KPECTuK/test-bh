using System;
using System.Collections.Generic;
using BH.Model;

namespace BH.Components
{
	public static class ExtensionsView
	{
		public static ModelViewUser GetById(this Queue<ModelViewUser> source, CxId id)
		{
			var size = source.Count;
			for(var index = 0; index < size; index++)
			{
				var model = source.Dequeue();
				source.Enqueue(model);

				// see: ModelViewUser.IEquatable<> also
				if(model.IdUser == id)
				{
					return model;
				}
			}

			return null;
		}

		public static ModelViewServer GetById(this Queue<ModelViewServer> source, CxId id)
		{
			var size = source.Count;
			for(var index = 0; index < size; index++)
			{
				var model = source.Dequeue();
				source.Enqueue(model);

				// see: ModelViewServer.IEquatable<> also
				if(model.IdHost == id)
				{
					return model;
				}
			}

			return null;
		}

		public static ModelViewUser RemoveById(this Queue<ModelViewUser> source, CxId id)
		{
			var size = source.Count;
			for(var index = 0; index < size; index++)
			{
				var model = source.Dequeue();

				// see: ModelViewServer.IEquatable<> also
				if(model.IdUser == id)
				{
					return model;
				}

				source.Enqueue(model);
			}

			return null;
		}

		public static ModelViewServer RemoveById(this Queue<ModelViewServer> source, CxId id)
		{
			var size = source.Count;
			for(var index = 0; index < size; index++)
			{
				var model = source.Dequeue();

				// see: ModelViewServer.IEquatable<> also
				if(model.IdHost == id)
				{
					return model;
				}

				source.Enqueue(model);
			}

			return null;
		}

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

		public static T RemoveBy<T>(this Queue<T> source, Predicate<T> predicate)
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

		public static int GetRecent(this Queue<ModelViewUser> source, ModelViewUser[] into, CxId idHost)
		{
			var size = source.Count;
			for(var index = 0; index < size; index++)
			{
				var model = source.Dequeue();
				source.Enqueue(model);

				if(model.IdAtHost != idHost)
				{
					continue;
				}

				var indexResult = 0;
				for(; indexResult < into.Length; indexResult++)
				{
					if(into[indexResult] == null)
					{
						break;
					}

					if(into[indexResult].FirstUpdated > model.FirstUpdated)
					{
						var indexInsert = into.Length - 1;
						while(indexInsert > indexResult)
						{
							into[indexInsert] = into[indexInsert - 1];
							indexInsert--;
						}
						into[indexResult] = model;
						break;
					}
				}

				if(indexResult < into.Length)
				{
					into[indexResult] = model;
				}
			}

			var count = 0;
			for(var index = 0; index < into.Length; index++)
			{
				count += into[index] == null ? 0 : 1;
			}

			return count;
		}
	}
}
