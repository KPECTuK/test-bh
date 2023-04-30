using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BH.Model;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace BH.Components
{
	[RequireComponent(typeof(CapsuleCollider))]
	[RequireComponent(typeof(Rigidbody))]
	[RequireComponent(typeof(PlayerInput))]
	[RequireComponent(typeof(NavMeshAgent))]
	//[RequireComponent(typeof(NetworkIdentity))]
	//[RequireComponent(typeof(NetworkTransform))]
	public class CompPawn : MonoBehaviour
	{
		public Transform View;
		public Camera Camera;
		public SettingsPawn Settings;

		//[NonSerialized] public NetworkIdentity Identity;
		[NonSerialized] public NavMeshAgent Agent;
		[NonSerialized] public PlayerInput InputComponent;
		[NonSerialized] public IBuilderAsset<CompPawn> Builder;
		[NonSerialized] public IInputPawn InputReceiver;

		[NonSerialized] public Vector3 InputSharedMove;
		[NonSerialized] public float InputSharedTurn;
		[NonSerialized] public float InputSharedPitch;

		[NonSerialized] public Vector3 CameraAnchor;

		[NonSerialized] public CxId IdUser;

		//? sync source
		private readonly Stack<IDriverPawn> _driver = new();

		public void Set(IDriverPawn driver)
		{
			_driver.Clear();
			_driver.Push(driver);
		}

		public void Push(IDriverPawn driver)
		{
			var type = driver.GetType();
			if(_driver.All(_ => _.GetType() != type))
			{
				_driver.Push(driver);
			}
		}

		public void Pop()
		{
			_driver.Pop();
		}

		public void SetFeatures(CxId idFeature)
		{
			for(var index = 0; index < Settings.Features.Length; index++)
			{
				if(Settings.Features[index].IdFeature == idFeature)
				{
					View.GetComponent<Renderer>().material.color = Settings.Features[index].PawnColor;

					$"pawn feature set: ( id user : {IdUser.ShortForm()} id feature: {idFeature.ShortForm()} )".Log();

					return;
				}
			}

			// View.GetComponent<Renderer>().material.color = Settings.Features[0].PawnColor;

			var builder = new StringBuilder($"pawn feature set fault: ( id user : {IdUser.ShortForm()} id feature: {idFeature.ShortForm()} )");
			throw new Exception(Singleton<ServicePawns>.I.DumpTo(builder).ToString());
		}

		#if UNITY_EDITOR
		private void DrawGizmo()
		{
			foreach(var driver in _driver)
			{
				driver.DrawGizmo();
			}
		}

		public struct GizmoData
		{
			public Plane Horizon;
			public Vector3 Previous;
			public Vector2 InputLookRaw;
			public int IndexStart;
			public int Size;
			public Vector3[] Trace;

			public static GizmoData Create()
			{
				return new()
				{
					InputLookRaw = Vector2.zero,
					Size = 0,
					IndexStart = -1,
					Trace = new Vector3[60],
				};
			}
		}

		public GizmoData GizmoShared = GizmoData.Create();
		#endif

		private void Awake()
		{
			//Identity = GetComponent<NetworkIdentity>();
			Agent = GetComponent<NavMeshAgent>();
			InputComponent = GetComponent<PlayerInput>();
		}

		private void Update()
		{
			if(_driver.TryPeek(out var controller))
			{
				controller.Update();
			}
		}

		private void LateUpdate()
		{
			#if UNITY_EDITOR
			DrawGizmo();
			#endif
		}
	}
}
