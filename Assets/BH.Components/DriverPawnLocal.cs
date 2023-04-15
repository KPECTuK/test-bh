using BH.Model;
using UnityEngine;

namespace BH.Components
{
	public sealed class DriverPawnLocal : IDriverPawn
	{
		private readonly CompPawn _component;

		public DriverPawnLocal(CompPawn pawn)
		{
			_component = pawn;
		}

		public void Update()
		{
			// pawn
			var cachedPawn = _component.transform;
			var pawnRotation = Quaternion.AngleAxis(_component.InputSharedTurn, cachedPawn.up) * cachedPawn.localRotation;
			cachedPawn.localRotation = pawnRotation;
			cachedPawn.localPosition += pawnRotation * _component.InputSharedMove;

			// camera
			var cachedCamera = _component.Camera.transform;
			var cameraRotation = Quaternion.AngleAxis(_component.InputSharedPitch, Vector3.right) * cachedCamera.localRotation;
			cameraRotation = cameraRotation.Clamp(_component.Settings.PitchMinDeg, _component.Settings.PitchMaxDeg);
			cachedCamera.localRotation = cameraRotation;
			cachedCamera.localPosition = cameraRotation * _component.CameraAnchor;
		}

		public void DrawGizmo()
		{
			#if UNITY_EDITOR
			var cachedTransform = _component.transform;
			var camera = _component.Camera;

			const float RADIUS = 1f;
			var distance = camera.nearClipPlane;
			var width = Screen.width * .5f;
			var height = Screen.height * .5f;
			var origin = new Vector3(width, height, distance);
			var target = new Vector3(
				width + _component.GizmoShared.InputLookRaw.x * RADIUS,
				height + _component.GizmoShared.InputLookRaw.y * RADIUS,
				distance);

			var originWorld = camera.ScreenToWorldPoint(origin);
			var offsetWorld = camera.ScreenToWorldPoint(target) - originWorld;
			var radiusWorld = camera.ScreenToWorldPoint(origin + Vector3.up * RADIUS);
			var forward = cachedTransform.forward;
			var delta = Quaternion.FromToRotation(_component.GizmoShared.Previous, forward);
			_component.GizmoShared.Previous = forward;

			_component.GizmoShared.IndexStart = ++_component.GizmoShared.IndexStart % _component.GizmoShared.Trace.Length;
			_component.GizmoShared.Trace[_component.GizmoShared.IndexStart] = offsetWorld;
			_component.GizmoShared.Size = ++_component.GizmoShared.Size > _component.GizmoShared.Trace.Length
				? _component.GizmoShared.Trace.Length
				: _component.GizmoShared.Size;

			var index = _component.GizmoShared.IndexStart;
			index -= _component.GizmoShared.Size;
			index += _component.GizmoShared.Trace.Length;
			index %= _component.GizmoShared.Trace.Length;
			var colorStep = 1f / _component.GizmoShared.Size;
			var colorLerp = 0f;
			while((index = ++index % _component.GizmoShared.Trace.Length) != _component.GizmoShared.IndexStart)
			{
				_component.GizmoShared.Trace[index] = delta * _component.GizmoShared.Trace[index];
				Debug.DrawLine(
					originWorld,
					originWorld + _component.GizmoShared.Trace[index],
					Color.Lerp(Color.blue, Color.cyan, colorLerp));
				colorLerp += colorStep;
			}

			Debug.DrawLine(
				originWorld,
				originWorld + _component.GizmoShared.Trace[_component.GizmoShared.IndexStart],
				Color.cyan);

			new CxOrigin
				{
					Location = originWorld,
				}
				.ToMeta()
				.SetUpVector(forward)
				.SetSize((originWorld - radiusWorld).magnitude)
				.DrawCircle();

			var angleFovHorizontal = .5f *
				Camera.VerticalToHorizontalFieldOfView(
					camera.fieldOfView,
					camera.aspect);

			distance /= Mathf.Cos(angleFovHorizontal * Mathf.Deg2Rad);
			forward = Vector3.ProjectOnPlane(forward, _component.GizmoShared.Horizon.normal).normalized;
			var originHorizon = _component.Camera.transform.position + forward * distance;
			var half = cachedTransform.right * (distance * .5f);
			var right = originHorizon + half;
			var left = originHorizon - half;

			Debug.DrawLine(right, left, Color.gray);
			#endif
		}
	}
}
