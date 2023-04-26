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
		private enum NetworkMode
		{
			Disabled,
			LobbyClient,
			LobbyServer,
			GameClient,
			GameServer,
		}

		private readonly List<(Type type, NetworkMode mark)> _map = new()
		{
			(typeof(NetworkModeDisabled), NetworkMode.Disabled),
			(typeof(NetworkModeLobbyClient), NetworkMode.LobbyClient),
			(typeof(NetworkModeLobbyServer), NetworkMode.LobbyServer),
			(typeof(NetworkModeGameAsClient), NetworkMode.GameClient),
			(typeof(NetworkModeGameAsServer), NetworkMode.GameServer),
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
			
			if(Singleton<ServiceNetwork>.I.NetworkModeShared.GetType() == typeof(NetworkModeDisabled))
			{
				command = new CmdViewLobbyServerAppend()
				{
					IdModel = CxId.Empty,
				};
			}
			
			if(Singleton<ServiceNetwork>.I.NetworkModeShared.GetType() == typeof(NetworkModeLobbyClient))
			{
				command = new CmdViewLobbyUserAppend()
				{
					IdModel = CxId.Empty,
				};
			}

			if(command != null)
			{
				Singleton<ServiceUI>.I.Events.Enqueue(command);
			}
			else
			{
				$"no command for state: {Singleton<ServiceNetwork>.I.NetworkModeShared.GetType().NameNice()}".LogWarning();
			}

		}

		private void OnLobbyButtonRemove()
		{
			ICommand<CompScreens> command = null;
			
			if(Singleton<ServiceNetwork>.I.NetworkModeShared.GetType() == typeof(NetworkModeDisabled))
			{
				command = new CmdViewLobbyServerRemove()
				{
					IdModel = Singleton<ServiceUI>.I.ModelsServer.FirstOrDefault().IdHost,
				};
			}
			
			if(Singleton<ServiceNetwork>.I.NetworkModeShared.GetType() == typeof(NetworkModeLobbyClient))
			{
				command = new CmdViewLobbyUserRemove()
				{
					IdModel = Singleton<ServiceUI>.I.ModelsUser.FirstOrDefault().IdUser,
				};
			}

			if(command != null)
			{
				Singleton<ServiceUI>.I.Events.Enqueue(command);
			}
			else
			{
				$"no command for state: {Singleton<ServiceNetwork>.I.NetworkModeShared.GetType().NameNice()}".LogWarning();
			}
		}

		private void OnLobbyButtonClear()
		{
			ICommand<CompScreens> command = null;
			
			if(Singleton<ServiceNetwork>.I.NetworkModeShared.GetType() == typeof(NetworkModeDisabled))
			{
				command = new CmdViewLobbyClear()
					{ };
			}
			
			if(Singleton<ServiceNetwork>.I.NetworkModeShared.GetType() == typeof(NetworkModeLobbyClient))
			{
				command = new CmdViewLobbyClear()
					{ };
			}

			if(command != null)
			{
				Singleton<ServiceUI>.I.Events.Enqueue(command);
			}
			else
			{
				$"no command for state: {Singleton<ServiceNetwork>.I.NetworkModeShared.GetType().NameNice()}".LogWarning();
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
					var modeCurrent = NetworkMode.Disabled;
					ActionDeco(() => { modeCurrent = _map.Find(_ => _.type == Singleton<ServiceNetwork>.I.NetworkModeShared.GetType()).mark; }, false);

					var modeNew = (NetworkMode)EditorGUILayout.EnumPopup(new GUIContent("Network mode:"), modeCurrent);
					if(modeCurrent != modeNew)
					{
						ActionDeco(() =>
						{
							var type = _map.Find(_ => _.mark == modeNew).type;
							Singleton<ServiceNetwork>.I.NetworkModeShared = (INetworkMode)Activator.CreateInstance(type);
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

					if(GUILayout.Button("Add bar button"))
					{
						ActionDeco(OnLobbyButtonAdd);
					}

					if(GUILayout.Button("Remove bar button"))
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
