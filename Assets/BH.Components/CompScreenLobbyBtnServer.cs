using System;
using BH.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BH.Components
{
	public sealed class CompScreenLobbyBtnServer : CompScreenLobbyBtnBase
	{
		[NonSerialized] public ModelViewServer Model;
		[NonSerialized] public Action<ModelViewServer> OnClick;

		public TextMeshProUGUI TextOwner;
		public TextMeshProUGUI TextServer;
		public TextMeshProUGUI TextPlayers;

		public Image Back;

		private Color _initial;

		private void Awake()
		{
			_initial = Back.color;

			var button = GetComponent<Button>();
			button.onClick.AddListener(() => { OnClick?.Invoke(Model); });
		}

		public override void UpdateView()
		{
			TextServer.text = Model.RenderServer;
			TextOwner.text = Model.RenderOwner;
			TextPlayers.text = Model.RenderPlayers;

			var modelUser = Singleton<ServiceUI>.I.ModelsUser
				.GetById(Singleton<ServiceNetwork>.I.IdCurrentUser);
			Back.color = Model.IdHost == modelUser.IdAtHost
				? Color.Lerp(Color.yellow, Color.black, .5f)
				: _initial;
		}

		public override bool IsModel(object model)
		{
			return Model.Equals(model);
		}
	}
}
