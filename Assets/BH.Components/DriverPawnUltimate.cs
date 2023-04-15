using UnityEngine;

namespace BH.Components
{
	public sealed class DriverPawnUltimate : IDriverPawn
	{
		private readonly CompPawn _component;

		public Vector3 Target;
		public Vector3 Forward;

		public DriverPawnUltimate(CompPawn pawn)
		{
			_component = pawn;

			var cached = _component.transform;
			Forward = cached.forward;
			Target = cached.position + Forward * _component.Settings.MaxUltDistance;
			if(_component.Agent.Raycast(Target, out var hit))
			{
				Target = hit.position;
			}
		}

		public void Update()
		{
			var delta = Forward * (_component.Settings.MaxUltSpeed * Time.deltaTime);
			var next = _component.transform.position + delta;

			if(Vector3.Dot((Target - next).normalized, _component.transform.forward) < 0f)
			{
				_component.transform.position = Target;
				_component.Pop();
			}
			else
			{
				_component.transform.position = next;
			}
		}

		public void DrawGizmo()
		{
			#if UNITY_EDITOR
			var orientation = _component.transform.rotation;
			new CxOrigin { Location = Target, Orientation = orientation }
				.ToMeta()
				.SetUpVector(_component.transform.up)
				.SetShape(Meta<CxOrigin>.Shape.Circle)
				.SetSize(1f)
				.Draw();
			new CxOrigin { Location = Target, Orientation = orientation }
				.ToMeta()
				.SetShape(Meta<CxOrigin>.Shape.Cross)
				.SetSize(.1f)
				.Draw();
			#endif
		}
	}
}
