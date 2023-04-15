using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BH.Components.Editor
{
	/// <summary>
	/// Custom Hold interaction for New Input System.
	/// With this, the .performed callback will be called everytime the Input System updates. 
	/// Allowing a purely callback based approach to a button hold instead of polling it in an Update() loop and using bools
	/// .started will be called when the 'pressPoint' threshold has been met and held for the 'duration'.
	/// .performed will continue to be called each frame after `.started` has triggered.
	/// .cancelled will be called when no-longer actuated (but only if the input has actually 'started' triggering
	/// </summary>
	#if UNITY_EDITOR
	// Allow for the interaction to be utilized outside of Play Mode
	// and so that it will actually show up as an option in the Input Manager
	[InitializeOnLoad]
	#endif
	[Preserve]
	[DisplayName("Holding")]
	[Serializable]
	public class CustomHoldingInteraction : IInputInteraction
	{
		public bool useDefaultSettingsPressPoint;
		public float pressPoint;

		public bool useDefaultSettingsDuration;
		public float duration;

		private float _heldTime;
		private InputInteractionContext _context;

		private float PressPointOrDefault =>
			useDefaultSettingsPressPoint || pressPoint <= 0
				? InputSystem.settings.defaultButtonPressPoint
				: pressPoint;
		private float DurationOrDefault =>
			useDefaultSettingsDuration || duration <= 0
				? InputSystem.settings.defaultHoldTime
				: duration;

		private void OnUpdate()
		{
			var isActuated = _context.ControlIsActuated(PressPointOrDefault);
			var phase = _context.phase;

			// Cancel and cleanup our action if it's no-longer actuated or been externally changed to a stopped state.
			if(phase == InputActionPhase.Canceled || phase == InputActionPhase.Disabled || !_context.action.actionMap.enabled || !isActuated)
			{
				Reset();
				return;
			}

			_heldTime += Time.deltaTime;

			if(_heldTime < DurationOrDefault)
			{
				return;
			}

			// Don't do anything yet, hold time not exceeded
			// We've held for long enough, start triggering the Performed state.

			var @is =
				phase == InputActionPhase.Performed ||
				phase == InputActionPhase.Started ||
				phase == InputActionPhase.Waiting;
			if(@is)
			{
				_context.PerformedAndStayPerformed();
			}
		}

		public void Process(ref InputInteractionContext context)
		{
			// Ensure our Update always has access to the most recently updated context
			_context = context;

			// Actuation changed and thus no longer performed, cancel it all
			if(!_context.ControlIsActuated(PressPointOrDefault))
			{
				Reset();
				return;
			}

			if(_context.phase != InputActionPhase.Performed && _context.phase != InputActionPhase.Started)
			{
				EnableInputHooks();
			}
		}

		private void Cancel(ref InputInteractionContext context)
		{
			DisableInputHooks();

			_heldTime = 0f;

			var @is =
				context.phase == InputActionPhase.Performed ||
				context.phase == InputActionPhase.Started;
			if(@is)
			{
				// Input was being held when this call was made. Trigger the .cancelled event.
				context.Canceled();
			}
		}

		public void Reset()
		{
			Cancel(ref _context);
		}

		private void OnLayoutChange(string layoutName, InputControlLayoutChange change)
		{
			Reset();
		}

		private void OnDeviceChange(InputDevice device, InputDeviceChange change)
		{
			Reset();
		}

		#if UNITY_EDITOR
		private void PlayModeStateChange(PlayModeStateChange state)
		{
			Reset();
		}
		#endif

		private void EnableInputHooks()
		{
			// Safeguard for duplicate registrations
			InputSystem.onAfterUpdate -= OnUpdate;
			InputSystem.onAfterUpdate += OnUpdate;
			// In case layout or device changes, we'll want to trigger
			// a cancelling of the current input action subscription to avoid errors.
			InputSystem.onLayoutChange -= OnLayoutChange;
			InputSystem.onLayoutChange += OnLayoutChange;
			InputSystem.onDeviceChange -= OnDeviceChange;
			InputSystem.onDeviceChange += OnDeviceChange;
			// Prevent the update hook from persisting across a play mode change to avoid errors.

			#if UNITY_EDITOR
			EditorApplication.playModeStateChanged -= PlayModeStateChange;
			EditorApplication.playModeStateChanged += PlayModeStateChange;
			#endif
		}

		private void DisableInputHooks()
		{
			InputSystem.onAfterUpdate -= OnUpdate;
			InputSystem.onLayoutChange -= OnLayoutChange;
			InputSystem.onDeviceChange -= OnDeviceChange;
			#if UNITY_EDITOR
			EditorApplication.playModeStateChanged -= PlayModeStateChange;
			#endif
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
		private static void RegisterInteraction()
		{
			if(InputSystem.TryGetInteraction("CustomHolding") == null)
			{
				// For some reason if this is called again when it already exists,
				// it permanently removes it from the drop-down options... So have to check first
				InputSystem.RegisterInteraction<CustomHoldingInteraction>("CustomHolding");
			}
		}

		/// <summary>
		/// Constructor will be called by our Editor [InitializeOnLoad] attribute when outside Play Mode
		/// </summary>
		static CustomHoldingInteraction()
		{
			RegisterInteraction();
		}
	}
}
