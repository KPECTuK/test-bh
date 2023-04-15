#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using BH.Model;
using UnityEditor;
using UnityEngine;

namespace BH.Components.Editor
{
	[CustomEditor(typeof(CompScreens))]
	public class DrawerCompScreens : UnityEditor.Editor
	{
		private enum ServerMode
		{
			ServerDisabled,
			ServerEnabled,
			Game,
		}

		private readonly List<(Type type, ServerMode mark)> _map = new()
		{
			(typeof(ServerModeDisabled), ServerMode.ServerDisabled),
			(typeof(ServerModeLobby), ServerMode.ServerEnabled),
			(typeof(ServerModeGame), ServerMode.Game),
		};

		private readonly string[] _names =
		{
			ServiceUI.SCREEN_LOADING_S,
			ServiceUI.SCREEN_LOBBY_S,
			ServiceUI.SCREEN_GAME_S,
			ServiceUI.SCREEN_WIN_S,
			ServiceUI.SCREEN_LOSE_S,
		};

		private static void ActionDeco(Action intent, bool isWarning = true)
		{
			if(Application.isPlaying)
			{
				intent?.Invoke();
			}
			else
			{
				if(isWarning)
				{
					"not in play mode.. passing by".LogWarning();
				}
			}
		}

		private void OnLobbyButtonAdd()
		{
			ICommand<CompScreens> command = null;
			
			if(Singleton<ServiceNetwork>.I.ServerModeShared.GetType() == typeof(ServerModeDisabled))
			{
				command = new CmdViewLobbyAppendServer()
				{
					Model = new ModelViewButtonServer(),
				};
			}
			
			if(Singleton<ServiceNetwork>.I.ServerModeShared.GetType() == typeof(ServerModeLobby))
			{
				command = new CmdViewLobbyAppendUser()
				{
					Model = new ModelViewButtonUser(),
				};
			}

			if(command != null)
			{
				Singleton<ServiceUI>.I.EventsView.Enqueue(command);
			}
			else
			{
				$"no command for state: {Singleton<ServiceNetwork>.I.ServerModeShared.GetType().NameNice()}".LogWarning();
			}

		}

		private void OnLobbyButtonRemove()
		{
			ICommand<CompScreens> command = null;
			
			if(Singleton<ServiceNetwork>.I.ServerModeShared.GetType() == typeof(ServerModeDisabled))
			{
				command = new CmdViewLobbyRemoveServer()
				{
					Model = Singleton<ServiceUI>.I.ModelsServer.FirstOrDefault(),
				};
			}
			
			if(Singleton<ServiceNetwork>.I.ServerModeShared.GetType() == typeof(ServerModeLobby))
			{
				command = new CmdViewLobbyRemoveUser()
				{
					Model = Singleton<ServiceUI>.I.ModelsUser.FirstOrDefault(),
				};
			}

			if(command != null)
			{
				Singleton<ServiceUI>.I.EventsView.Enqueue(command);
			}
			else
			{
				$"no command for state: {Singleton<ServiceNetwork>.I.ServerModeShared.GetType().NameNice()}".LogWarning();
			}
		}

		private void OnLobbyButtonClear()
		{
			ICommand<CompScreens> command = null;
			
			if(Singleton<ServiceNetwork>.I.ServerModeShared.GetType() == typeof(ServerModeDisabled))
			{
				command = new CmdViewLobbyClear()
					{ };
			}
			
			if(Singleton<ServiceNetwork>.I.ServerModeShared.GetType() == typeof(ServerModeLobby))
			{
				command = new CmdViewLobbyClear()
					{ };
			}

			if(command != null)
			{
				Singleton<ServiceUI>.I.EventsView.Enqueue(command);
			}
			else
			{
				$"no command for state: {Singleton<ServiceNetwork>.I.ServerModeShared.GetType().NameNice()}".LogWarning();
			}
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if(target is CompScreens cast)
			{
				GUILayout.BeginVertical(GUI.skin.box);
				{
					GUILayout.Label("Screens toggle:", GUILayout.ExpandWidth(false));

					for(var index = 0; index < _names.Length; index++)
					{
						if(GUILayout.Button(_names[index]))
						{
							var nameScreen = _names[index];
							ActionDeco(() => cast.Schedule(cast.TaskScreenChange(nameScreen)));
						}
					}
				}
				GUILayout.EndVertical();

				GUILayout.BeginVertical(GUI.skin.box);
				{
					var modeCurrent = ServerMode.ServerDisabled;
					ActionDeco(() => { modeCurrent = _map.Find(_ => _.type == Singleton<ServiceNetwork>.I.ServerModeShared.GetType()).mark; }, false);

					var modeNew = (ServerMode)EditorGUILayout.EnumPopup(new GUIContent("Server mode:"), modeCurrent);
					if(modeCurrent != modeNew)
					{
						ActionDeco(() =>
						{
							var type = _map.Find(_ => _.mark == modeNew).type;
							Singleton<ServiceNetwork>.I.ServerModeShared = (IServerMode)Activator.CreateInstance(type);
						});
					}
				}
				GUILayout.EndVertical();

				GUILayout.BeginHorizontal(GUI.skin.box);
				{
					GUILayout.Label("Screen 'Lobby':", GUILayout.ExpandWidth(false));

					if(GUILayout.Button("Clear bar"))
					{
						ActionDeco(OnLobbyButtonClear);
					}

					if(GUILayout.Button("Add button"))
					{
						ActionDeco(OnLobbyButtonAdd);
					}

					if(GUILayout.Button("Remove button"))
					{
						ActionDeco(OnLobbyButtonRemove);
					}
				}
				GUILayout.EndHorizontal();
			}
		}
	}
}
#endif
