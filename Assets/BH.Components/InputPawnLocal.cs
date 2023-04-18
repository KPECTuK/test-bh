using System;
using BH.Model;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BH.Components
{
	public sealed class InputPawnLocal : IInputPawn
	{
		private const string ID_ACTION_MOVE_S = "Move";
		private const string ID_ACTION_LOOK_S = "Look";
		private const string ID_ACTION_FIRE_S = "Fire";

		private Action<InputAction.CallbackContext> _actionMovePerformed;
		private Action<InputAction.CallbackContext> _actionMoveCanceled;
		private Action<InputAction.CallbackContext> _actionLookPerformed;
		private Action<InputAction.CallbackContext> _actionLookCanceled;
		private Action<InputAction.CallbackContext> _actionFirePerformed;
		private Action<InputAction.CallbackContext> _actionFireCanceled;

		private readonly CompPawn _component;

		public InputPawnLocal(CompPawn component)
		{
			_component = component;
		}

		public void Enable()
		{
			_actionFirePerformed = OnFirePerformed;
			_component.InputComponent.currentActionMap[ID_ACTION_FIRE_S].performed += _actionFirePerformed;
			_actionFireCanceled = OnFireCanceled;
			_component.InputComponent.currentActionMap[ID_ACTION_FIRE_S].canceled += _actionFireCanceled;

			_actionLookPerformed = OnLookPerformed;
			_component.InputComponent.currentActionMap[ID_ACTION_LOOK_S].performed += _actionLookPerformed;
			_actionLookCanceled = OnLookCanceled;
			_component.InputComponent.currentActionMap[ID_ACTION_LOOK_S].canceled += _actionLookCanceled;

			_actionMovePerformed = OnMovePerformed;
			_component.InputComponent.currentActionMap[ID_ACTION_MOVE_S].performed += _actionMovePerformed;
			_actionMoveCanceled = OnMoveCanceled;
			_component.InputComponent.currentActionMap[ID_ACTION_MOVE_S].canceled += _actionMoveCanceled;

			_component.InputComponent.ActivateInput();
		}

		public void Disable()
		{
			// порядок вызовов методов OnDisable() OnDestroy() во время выхода из приложения - не определена

			if(_component.InputComponent.currentActionMap != null)
			{
				_component.InputComponent.currentActionMap[ID_ACTION_FIRE_S].performed -= _actionFirePerformed;
				_actionFirePerformed = null;
				_component.InputComponent.currentActionMap[ID_ACTION_FIRE_S].canceled -= _actionFireCanceled;
				_actionFireCanceled = null;

				_component.InputComponent.currentActionMap[ID_ACTION_LOOK_S].performed -= _actionLookPerformed;
				_actionLookPerformed = null;
				_component.InputComponent.currentActionMap[ID_ACTION_LOOK_S].canceled -= _actionLookCanceled;
				_actionLookCanceled = null;

				_component.InputComponent.currentActionMap[ID_ACTION_MOVE_S].performed -= _actionMovePerformed;
				_actionMovePerformed = null;
				_component.InputComponent.currentActionMap[ID_ACTION_MOVE_S].canceled -= _actionMoveCanceled;
				_actionMoveCanceled = null;
			}

			_component.InputComponent.DeactivateInput();
		}

		private void OnMovePerformed(InputAction.CallbackContext context)
		{
			var value = context.ReadValue<Vector2>();
			var direct = value.ProjectOnto(Vector2.up).normalized.y * _component.Settings.MaxSpeedMove * Time.deltaTime;
			var side = value.ProjectOnto(Vector2.right).normalized.x * _component.Settings.MaxSpeedStrafe * Time.deltaTime;
			_component.InputSharedMove = new Vector3(side, 0f, direct);
		}

		private void OnMoveCanceled(InputAction.CallbackContext context)
		{
			_component.InputSharedMove = Vector3.zero;
		}

		private void OnLookPerformed(InputAction.CallbackContext context)
		{
			var value = context.ReadValue<Vector2>();
			_component.InputSharedTurn = value.ProjectOnto(Vector2.right).x * _component.Settings.MaxSpeedTurnDeg * Time.deltaTime;
			_component.InputSharedPitch = -value.ProjectOnto(Vector2.up).y * _component.Settings.MaxSpeedPitchDeg * Time.deltaTime;

			#if UNITY_EDITOR
			_component.GizmoShared.InputLookRaw = value;
			#endif
		}

		private void OnLookCanceled(InputAction.CallbackContext context)
		{
			_component.InputSharedTurn = 0;
			_component.InputSharedPitch = 0;

			#if UNITY_EDITOR
			_component.GizmoShared.InputLookRaw = Vector2.zero;
			#endif
		}

		private void OnFirePerformed(InputAction.CallbackContext context)
		{
			// TODO: strafe blocks fire gen sometimes

			_component.Push(new DriverPawnUltimate(_component));
		}

		private void OnFireCanceled(InputAction.CallbackContext context) { }
	}

	public sealed class InputPawnRemote : IInputPawn
	{
		private readonly CompPawn _component;

		public InputPawnRemote(CompPawn component)
		{
			_component = component;
		}

		public void Enable() { }

		public void Disable() { }
	}
}
