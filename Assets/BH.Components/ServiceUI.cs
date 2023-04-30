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
		public readonly ListRef<ModelServer> ModelsServer = new();
		public readonly ListRef<ModelUser> ModelsUser = new();

		public void Reset()
		{
			Events.Enqueue(
				new CmdViewScreenChange
				{
					NameScreen = SCREEN_LOBBY_S
				});

			"initialized: ui".Log();
		}

		public void Dispose() { }
	}

	// TODO: all contains might be in Assert()

	public class CmdViewScreenChange : ICommandBreak<CompScreens>
	{
		public string NameScreen;

		public bool Assert(CompScreens context)
		{
			//? is the same screen constraint
			var active = context.GetActiveScreen();
			return active == null || !active.name.Contains(NameScreen);
		}

		public void Execute(CompScreens context)
		{
			context.Scheduler.Schedule(NameScreen, context.TaskScreenChange);
		}
	}

	public class CmdViewLobbyClear : ICommand<CompScreens>
	{
		public readonly Queue<ModelServer> ServersToDelete = new();
		public readonly Queue<ModelUser> UsersToDelete = new();

		public virtual bool Assert(CompScreens context)
		{
			var active = context.GetActiveScreen();
			return active != null && active.name.Contains(ServiceUI.SCREEN_LOBBY_S);
		}

		public virtual void Execute(CompScreens context)
		{
			var controller = context.GetActiveScreen().GetComponent<CompScreenLobby>();

			controller.Bar.GetButtons().ToText("button, found on bar").Log();
			ServersToDelete.ToText("buttons, scheduled for remove (servers)").Log();
			UsersToDelete.ToText("buttons, scheduled for remove (users)").Log();

			DestroyAllServerButtonsIfAny(controller);
			DestroyAllUserButtonsIfAny(controller);

			controller.Scheduler.Schedule(controller.TaskUpdateButtons);
		}

		private void DestroyAllServerButtonsIfAny(CompScreenLobby controller)
		{
			var size = ServersToDelete.Count;
			for(var index = 0; index < size; index++)
			{
				var modelServer = ServersToDelete.Dequeue();
				controller.Bar.Scheduler.PassThrough.Schedule(
					modelServer.IdHost,
					controller.Bar.TaskButtonRemove);
			}
		}

		private void DestroyAllUserButtonsIfAny(CompScreenLobby controller)
		{
			var size = UsersToDelete.Count;
			for(var index = 0; index < size; index++)
			{
				var modelUser = UsersToDelete.Dequeue();
				controller.Bar.Scheduler.PassThrough.Schedule(
					modelUser.IdUser,
					controller.Bar.TaskButtonRemove);
			}
		}
	}

	public class CmdViewLobbyModeChange<T> : CmdViewLobbyClear where T : IScheduler, new()
	{
		public override void Execute(CompScreens context)
		{
			base.Execute(context);

			var controller = context.GetActiveScreen().GetComponent<CompScreenLobby>();
			controller.Bar.SetScheduler<T>();
		}
	}

	public class CmdViewLobbyUserAppend : ICommand<CompScreens>
	{
		public CxId IdUser;

		public bool Assert(CompScreens context)
		{
			var active = context.GetActiveScreen();
			return !IdUser.IsEmpty && active != null && active.name.Contains(ServiceUI.SCREEN_LOBBY_S);
		}

		public void Execute(CompScreens context)
		{
			var controller = context.GetActiveScreen().GetComponent<CompScreenLobby>();
			controller.Bar.Scheduler.Schedule(IdUser, controller.Bar.TaskButtonAppendOrUpdateUser);
			controller.Scheduler.Schedule(controller.TaskUpdateButtons);
		}
	}

	public class CmdViewLobbyUserUpdate : ICommand<CompScreens>
	{
		public CxId IdUser;

		public bool Assert(CompScreens context)
		{
			var active = context.GetActiveScreen();
			return !IdUser.IsEmpty && active != null && active.name.Contains(ServiceUI.SCREEN_LOBBY_S);
		}

		public void Execute(CompScreens context)
		{
			var controller = context.GetActiveScreen().GetComponent<CompScreenLobby>();
			controller.Bar.Scheduler.Schedule(IdUser, controller.Bar.TaskButtonAppendOrUpdateUser);
			controller.Scheduler.Schedule(controller.TaskUpdateButtons);
		}
	}

	public class CmdViewLobbyUserRemove : ICommand<CompScreens>
	{
		public CxId IdUser;

		public bool Assert(CompScreens context)
		{
			var active = context.GetActiveScreen();
			return !IdUser.IsEmpty && active != null && active.name.Contains(ServiceUI.SCREEN_LOBBY_S);
		}

		public void Execute(CompScreens context)
		{
			var controller = context.GetActiveScreen().GetComponent<CompScreenLobby>();
			controller.Bar.Scheduler.PassThrough.Schedule(IdUser, controller.Bar.TaskButtonRemove);
			controller.Scheduler.Schedule(controller.TaskUpdateButtons);
		}
	}

	public class CmdViewLobbyServerAppend : ICommand<CompScreens>
	{
		public CxId IdServer;

		public bool Assert(CompScreens context)
		{
			var active = context.GetActiveScreen();
			return !IdServer.IsEmpty && active != null && active.name.Contains(ServiceUI.SCREEN_LOBBY_S);
		}

		public void Execute(CompScreens context)
		{
			var controller = context.GetActiveScreen().GetComponent<CompScreenLobby>();
			controller.Bar.Scheduler.Schedule(IdServer, controller.Bar.TaskButtonAppendServer);
			controller.Scheduler.Schedule(controller.TaskUpdateButtons);
		}
	}

	public class CmdViewLobbyServerUpdate : ICommand<CompScreens>
	{
		public CxId IdServer;

		public bool Assert(CompScreens context)
		{
			var active = context.GetActiveScreen();
			return !IdServer.IsEmpty && active != null && active.name.Contains(ServiceUI.SCREEN_LOBBY_S);
		}

		public void Execute(CompScreens context)
		{
			var controller = context.GetActiveScreen().GetComponent<CompScreenLobby>();
			controller.Bar.Scheduler.Schedule(IdServer, controller.Bar.TaskButtonUpdate);
			controller.Scheduler.Schedule(controller.TaskUpdateButtons);
		}
	}

	public class CmdViewLobbyServerRemove : ICommand<CompScreens>
	{
		public CxId IdServer;

		public bool Assert(CompScreens context)
		{
			var active = context.GetActiveScreen();
			return !IdServer.IsEmpty && active != null && active.name.Contains(ServiceUI.SCREEN_LOBBY_S);
		}

		public void Execute(CompScreens context)
		{
			var controller = context.GetActiveScreen().GetComponent<CompScreenLobby>();
			controller.Bar.Scheduler.PassThrough.Schedule(IdServer, controller.Bar.TaskButtonRemove);
			controller.Scheduler.Schedule(controller.TaskUpdateButtons);
		}
	}
}
