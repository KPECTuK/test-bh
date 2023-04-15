using System;
using BH.Model;
using TMPro;

namespace BH.Components
{
	public sealed class CompScreenLobbyBtnUser : CompScreenLobbyBtnBase
	{
		[NonSerialized] public ModelViewButtonUser Model;

		public TextMeshProUGUI TextOwner;

		public override bool IsModel(object model)
		{
			return ReferenceEquals(Model, model);
		}
	}

	public sealed class ModelViewButtonUser
	{
		private readonly string _idUser = "u".MakeUnique();

		public string IdUser => $"user: {_idUser}";

		public override string ToString()
		{
			return "model user";
		}
	}
}
