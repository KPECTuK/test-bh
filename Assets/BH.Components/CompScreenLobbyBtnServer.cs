using System;
using BH.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BH.Components
{
	public sealed class CompScreenLobbyBtnServer : CompScreenLobbyBtnBase
	{
		[NonSerialized] public Action<CxId> OnClick;

		public TextMeshProUGUI TextOwner;
		public TextMeshProUGUI TextServer;
		public TextMeshProUGUI TextPlayers;
		public Image Back;

		private Color _initial;

		public override CxId IdModel { get; set; }

		private void Awake()
		{
			_initial = Back.color;

			var button = GetComponent<Button>();
			button.onClick.AddListener(() => { OnClick?.Invoke(IdModel); });
		}

		public override void UpdateView()
		{
			ref var modelServer = ref Singleton<ServiceUI>.I.ModelsServer.Get(IdModel, out var contains);
			if(!contains)
			{
				throw new Exception($"not contains: mode server {IdModel}");
			}

			TextServer.text = modelServer.RenderServer;
			TextOwner.text = modelServer.RenderOwner;
			TextPlayers.text = modelServer.RenderPlayers;

			var modelUser = Singleton<ServiceUI>.I.ModelsUser.Get(Singleton<ServiceNetwork>.I.IdCurrentUser, out contains);
			if(!contains)
			{
				throw new Exception($"not contains: model user {Singleton<ServiceNetwork>.I.IdCurrentUser}");
			}

			Back.color = IdModel == modelUser.IdHostAt
				? Color.Lerp(Color.yellow, Color.black, .2f)
				: _initial;
		}

		public override void ReleaseAllCallbacks()
		{
			OnClick = null;
		}
	}
}
