using System; 
using TMPro;
using UnityEngine.UI;

namespace BH.Components
{
	public sealed class CompScreenLobbyBtnUser : CompScreenLobbyBtnBase
	{
		[NonSerialized] public ModelViewUser Model;
		[NonSerialized] public Action<ModelViewUser> OnClick;

		public TextMeshProUGUI TextOwner;
		public TextMeshProUGUI TextIsReady;

		private void Awake()
		{
			var button = GetComponent<Button>();
			button.onClick.AddListener(() => { OnClick?.Invoke(Model); });
		}

		public override void UpdateView()
		{
			TextOwner.text = Model.RenderIdUser;
			TextIsReady.text = Model.RenderIsReady;
		}

		public override bool IsModel(object model)
		{
			return Model.Equals(model);
		}
	}
}
