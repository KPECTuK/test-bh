using System;
using UnityEngine;
using UnityEngine.UI;

namespace BH.Components
{
	[RequireComponent(typeof(Button))]
	public abstract class CompScreenLobbyBtnBase : MonoBehaviour
	{
		public RectTransform GroupMove;
		public RectTransform GroupScale;

		[NonSerialized] public Vector2 SizeLocalInitial;
		[NonSerialized] public Vector3 PositionLocalTarget;

		public abstract bool IsModel(object model);
	}
}
