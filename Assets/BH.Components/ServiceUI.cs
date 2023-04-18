using System.Collections.Generic;
using BH.Model;

namespace BH.Components
{
	public class ServiceUI : IService
	{
		public const string SCREEN_LOADING_S = "loading";
		public const string SCREEN_LOBBY_S = "lobby";
		public const string SCREEN_GAME_S = "game";
		public const string SCREEN_WIN_S = "win";
		public const string SCREEN_LOSE_S = "lose";

		public readonly Queue<ICommand<CompScreens>> Events = new();

		// TL; TI; i do now want to make copies of sets and cannot implement cyclic list
		// with random access due limited time, hadn't found any of mine
		public readonly Queue<ModelViewServer> ModelsServer = new();
		public readonly Queue<ModelViewUser> ModelsUser = new();

		public void Reset()
		{
			Events.Enqueue(
				new CmdViewScreenChange
				{
					NameScreen = SCREEN_LOBBY_S
				});

			"initialized: ui".Log();
		}

		public void Dispose()
		{
		}
	}

	public class CmdViewScreenChange : ICommand<CompScreens>
	{
		public string NameScreen;

		public bool Assert(CompScreens context)
		{
			var active = context.GetActiveScreen();
			return active == null || !active.name.Contains(NameScreen);
		}

		public void Execute(CompScreens context)
		{
			context.Schedule(context.TaskScreenChange(NameScreen));
		}
	}

	public class CmdViewLobbyClear : ICommand<CompScreens>
	{
		public bool RemoveAllFromServiceServer = true;
		public bool KeepSelfInServiceServer = true;
		public bool RemoveAllFromServiceUser = true;
		public bool KeepSelfInServiceUser = true;

		public bool Assert(CompScreens context)
		{
			// check active screen is LOBBY
			var active = context.GetActiveScreen();
			return active != null && active.name.Contains(ServiceUI.SCREEN_LOBBY_S);
		}

		public void Execute(CompScreens context)
		{
			var controller = context.GetActiveScreen().GetComponent<CompScreenLobby>();
			DestroyAllServerButtonsIfAny(controller);
			DestroyAllUserButtonsIfAny(controller);
		}

		private void DestroyAllServerButtonsIfAny(CompScreenLobby controller)
		{
			var queue = Singleton<ServiceUI>.I.ModelsServer;
			var size = queue.Count;
			for(var index = 0; index < size; index++)
			{
				// based on models existing
				var modelServer = queue.Dequeue();

				if(!RemoveAllFromServiceServer)
				{
					queue.Enqueue(modelServer);

					continue;
				}

				if(KeepSelfInServiceServer && modelServer.IdHost == Singleton<ServiceNetwork>.I.IdCurrentMachine)
				{
					queue.Enqueue(modelServer);
				}
				else
				{
					Singleton<ServiceUI>.I.ModelsServer.ToText($"remove <b><color=white>(server)</color></b>: {modelServer}").Log();
				}

				controller.Bar.Schedule(controller.Bar.TaskRemoveButtonAnim(modelServer));
			}
		}

		private void DestroyAllUserButtonsIfAny(CompScreenLobby controller)
		{
			var queue = Singleton<ServiceUI>.I.ModelsUser;
			var size = queue.Count;
			for(var index = 0; index < size; index++)
			{
				// based on models existing
				var modelUser = queue.Dequeue();

				if(!RemoveAllFromServiceUser)
				{
					queue.Enqueue(modelUser);

					continue;
				}

				if(KeepSelfInServiceUser && modelUser.IdUser == Singleton<ServiceNetwork>.I.IdCurrentUser)
				{
					queue.Enqueue(modelUser);
				}
				else
				{
					Singleton<ServiceUI>.I.ModelsUser.ToText($"remove <b><color=white>(user)</color></b>: {modelUser}").Log();
				}

				controller.Bar.Schedule(controller.Bar.TaskRemoveButtonAnim(modelUser));
			}
		}
	}

	public class CmdViewLobbyUserAppend : ICommand<CompScreens>
	{
		public ModelViewUser Model;

		public bool Assert(CompScreens context)
		{
			var active = context.GetActiveScreen();
			return active != null && active.name.Contains(ServiceUI.SCREEN_LOBBY_S);
		}

		public void Execute(CompScreens context)
		{
			var controller = context.GetActiveScreen().GetComponent<CompScreenLobby>();
			controller.Bar.Schedule(controller.Bar.TaskAppendButtonAnim(Model));
		}
	}

	public class CmdViewLobbyUserUpdate : ICommand<CompScreens>
	{
		public ModelViewUser Model;

		public bool Assert(CompScreens context)
		{
			var active = context.GetActiveScreen();
			return active != null && active.name.Contains(ServiceUI.SCREEN_LOBBY_S);
		}

		public void Execute(CompScreens context)
		{
			var controller = context.GetActiveScreen().GetComponent<CompScreenLobby>();
			controller.Bar.Schedule(controller.Bar.TaskUpdateButtonAnim(Model));

			//var queue = Singleton<ServiceUI>.I.ModelsUser;
			//for(int index = 0,
			//	size = queue.Count; index < size; index++)
			//{
			//	var model = queue.Dequeue();
			//	if(model.Equals(Model))
			//	{
			//		//? should view re-acquire its model
			//		model.CopyFrom(Model);
			//		var controller = context.GetActiveScreen().GetComponent<CompScreenLobby>();
			//		controller.Bar.Schedule(controller.Bar.TaskUpdateButtonAnim(Model));
			//	}
			//	queue.Enqueue(model);
			//}
		}
	}

	public class CmdViewLobbyUserRemove : ICommand<CompScreens>
	{
		public ModelViewUser Model;

		public bool Assert(CompScreens context)
		{
			var active = context.GetActiveScreen();
			return active != null && active.name.Contains(ServiceUI.SCREEN_LOBBY_S);
		}

		public void Execute(CompScreens context)
		{
			var controller = context.GetActiveScreen().GetComponent<CompScreenLobby>();
			controller.Bar.Schedule(controller.Bar.TaskRemoveButtonAnim(Model));
		}
	}

	public class CmdViewLobbyServerAppend : ICommand<CompScreens>
	{
		public ModelViewServer Model;

		public bool Assert(CompScreens context)
		{
			var active = context.GetActiveScreen();
			return active != null && active.name.Contains(ServiceUI.SCREEN_LOBBY_S);
		}

		public void Execute(CompScreens context)
		{
			var controller = context.GetActiveScreen().GetComponent<CompScreenLobby>();
			controller.Bar.Schedule(controller.Bar.TaskAppendButtonAnim(Model));
		}
	}

	public class CmdViewLobbyServerUpdate : ICommand<CompScreens>
	{
		public ModelViewServer Model;

		public bool Assert(CompScreens context)
		{
			var active = context.GetActiveScreen();
			return active != null && active.name.Contains(ServiceUI.SCREEN_LOBBY_S);
		}

		public void Execute(CompScreens context)
		{
			var controller = context.GetActiveScreen().GetComponent<CompScreenLobby>();
			controller.Bar.Schedule(controller.Bar.TaskUpdateButtonAnim(Model));

			//var queue = Singleton<ServiceUI>.I.ModelsServer;
			//for(int index = 0,
			//	size = queue.Count; index < size; index++)
			//{
			//	var model = queue.Dequeue();
			//	if(model.Equals(Model))
			//	{
			//		//? should view re-acquire its model
			//		model.CopyFrom(Model);
			//		var controller = context.GetActiveScreen().GetComponent<CompScreenLobby>();
			//		controller.Bar.Schedule(controller.Bar.TaskUpdateButtonAnim(Model));
			//	}
			//	queue.Enqueue(model);
			//}
		}
	}

	public class CmdViewLobbyServerRemove : ICommand<CompScreens>
	{
		public ModelViewServer Model;

		public bool Assert(CompScreens context)
		{
			var active = context.GetActiveScreen();
			return active != null && active.name.Contains(ServiceUI.SCREEN_LOBBY_S);
		}

		public void Execute(CompScreens context)
		{
			var controller = context.GetActiveScreen().GetComponent<CompScreenLobby>();
			controller.Bar.Schedule(controller.Bar.TaskRemoveButtonAnim(Model));
		}
	}
}
