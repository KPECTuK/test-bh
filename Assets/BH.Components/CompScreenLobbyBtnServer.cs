using System;
using BH.Model;
using TMPro;

namespace BH.Components
{
	public sealed class CompScreenLobbyBtnServer : CompScreenLobbyBtnBase
	{
		[NonSerialized] public ModelViewButtonServer Model;

		public TextMeshProUGUI TextOwner;
		public TextMeshProUGUI TextServer;
		public TextMeshProUGUI TextPlayers;
		
		public override bool IsModel(object model)
		{
			return ReferenceEquals(Model, model);
		}
	}

	public sealed class ModelViewButtonServer
	{
		private readonly string _idOwner = "o".MakeUnique();
		private readonly string _idServer = "s".MakeUnique();
		private readonly string _idPlayers = "p".MakeUnique();

		public string IdOwner => $"owner: {_idOwner}";
		public string IdServer => $"server: {_idServer}";
		public string IdPlayers => $"players: {_idPlayers}";

		public override string ToString()
		{
			return "model server";
		}
	}
}
