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

		public readonly Queue<ICommand<CompScreens>> EventsView = new();

		public readonly List<ModelViewButtonServer> ModelsServer = new();
		public readonly List<ModelViewButtonUser> ModelsUser = new();

		public void Reset()
		{
			//? clear events queue

			EventsView.Enqueue(
				new CmdViewScreenChange { NameScreen = SCREEN_LOBBY_S });
			EventsView.Enqueue(
				new CmdViewLobbyClear());

			"initialized: ui".Log();
		}

		public void Dispose()
		{
			// manageable, due Singleton generic routine
		}
	}

	public interface ICommand<in T>
	{
		bool Assert(T context);
		void Execute(T context);
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
		public bool Assert(CompScreens context)
		{
			var active = context.GetActiveScreen();
			return active != null && active.name.Contains(ServiceUI.SCREEN_LOBBY_S);
		}

		public void Execute(CompScreens context)
		{
			var controller = context.GetActiveScreen().GetComponent<CompScreenLobby>();

			{
				var list = Singleton<ServiceUI>.I.ModelsServer;
				for(var index = 0; index < list.Count; index++)
				{
					controller.Bar.Schedule(controller.Bar.TaskRemoveButtonAnim(list[index]));
				}
				list.Clear();
			}

			{
				var list = Singleton<ServiceUI>.I.ModelsUser;
				for(var index = 0; index < list.Count; index++)
				{
					controller.Bar.Schedule(controller.Bar.TaskRemoveButtonAnim(list[index]));
				}
				list.Clear();
			}
		}
	}

	public class CmdViewLobbyAppendUser : ICommand<CompScreens>
	{
		public ModelViewButtonUser Model;

		public bool Assert(CompScreens context)
		{
			var active = context.GetActiveScreen();
			return active != null && active.name.Contains(ServiceUI.SCREEN_LOBBY_S);
		}

		public void Execute(CompScreens context)
		{
			var controller = context.GetActiveScreen().GetComponent<CompScreenLobby>();
			controller.Bar.Schedule(controller.Bar.TaskAppendButtonAnim(Model));
			Singleton<ServiceUI>.I.ModelsUser.Add(Model);
		}
	}

	public class CmdViewLobbyAppendServer : ICommand<CompScreens>
	{
		public ModelViewButtonServer Model;

		public bool Assert(CompScreens context)
		{
			var active = context.GetActiveScreen();
			return active != null && active.name.Contains(ServiceUI.SCREEN_LOBBY_S);
		}

		public void Execute(CompScreens context)
		{
			var controller = context.GetActiveScreen().GetComponent<CompScreenLobby>();
			controller.Bar.Schedule(controller.Bar.TaskAppendButtonAnim(Model));
			Singleton<ServiceUI>.I.ModelsServer.Add(Model);
		}
	}

	public class CmdViewLobbyRemoveUser : ICommand<CompScreens>
	{
		public ModelViewButtonUser Model;

		public bool Assert(CompScreens context)
		{
			var active = context.GetActiveScreen();
			return active != null && active.name.Contains(ServiceUI.SCREEN_LOBBY_S);
		}

		public void Execute(CompScreens context)
		{
			var controller = context.GetActiveScreen().GetComponent<CompScreenLobby>();
			controller.Bar.Schedule(controller.Bar.TaskRemoveButtonAnim(Model));
			Singleton<ServiceUI>.I.ModelsUser.Remove(Model);
		}
	}

	public class CmdViewLobbyRemoveServer : ICommand<CompScreens>
	{
		public ModelViewButtonServer Model;

		public bool Assert(CompScreens context)
		{
			var active = context.GetActiveScreen();
			return active != null && active.name.Contains(ServiceUI.SCREEN_LOBBY_S);
		}

		public void Execute(CompScreens context)
		{
			var controller = context.GetActiveScreen().GetComponent<CompScreenLobby>();
			controller.Bar.Schedule(controller.Bar.TaskRemoveButtonAnim(Model));
			Singleton<ServiceUI>.I.ModelsServer.Remove(Model);
		}
	}
}
