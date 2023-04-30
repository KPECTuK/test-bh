using System;
using BH.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BH.Components
{
	public sealed class CompScreenLobbyBtnUser : CompScreenLobbyBtnBase
	{
		[NonSerialized] public Action<ModelUser> OnClick;

		public TextMeshProUGUI TextOwner;
		public TextMeshProUGUI TextHostAt;
		public Image ImageIsReady;

		public Color ColorIsReady = "068900".ToColor();
		public Color ColorIsBusy = "DE420A".ToColor();

		private void Awake()
		{
			var button = GetComponent<Button>();
			button.onClick.AddListener(() =>
			{
				ref var model = ref Singleton<ServiceUI>.I.ModelsUser.Get(IdModel, out var contains);
				OnClick?.Invoke(model);
			});
		}

		public override CxId IdModel { get; set; }

		public override void UpdateView()
		{
			ref var modelUser = ref Singleton<ServiceUI>.I.ModelsUser.Get(IdModel, out var contains);
			TextOwner.text = modelUser.RenderIdUser;
			TextHostAt.text = modelUser.RenderIdHostAt;
			ImageIsReady.color = modelUser.IsReady ? ColorIsReady : ColorIsBusy;

		}

		public override void ReleaseAllCallbacks()
		{
			OnClick = null;
		}
	}
}
