using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace BH.Components
{
	[RequireComponent(typeof(Canvas))]
	[RequireComponent(typeof(CanvasGroup))]
	public class CompScreen : MonoBehaviour
	{
		//? NOTE: TL; TI;
		//? NOTE: can dispatch commands through the ui elements tree
		//? NOTE: consuming particular ones from parents queue by each child
		//? NOTE: to its own queue

		private const float INTERVAL_TRANS_MIN_F = .05f;

		public float IntervalTrans = .1f;

		private Canvas _canvas;
		private CanvasGroup _canvasGroup;
		private CompScreens _controller;

		private Coroutine _transCurrent;
		private CompScreens.Signal _signalCurrent;

		public bool IsActiveScreen => _canvasGroup.interactable;

		public bool IsBusy =>
			_signalCurrent != null &&
			GetComponents<IWidgetController>().All(_ => !_.IsBusy);

		private void Awake()
		{
			_canvas = GetComponent<Canvas>();
			_canvasGroup = GetComponent<CanvasGroup>();
			_controller = GetComponentInParent<CompScreens>();

			if(_controller == null)
			{
				throw new Exception($"no controller for screen: {name}");
			}

			_controller.RegisterScreen(this);
		}

		private void OnDestroy()
		{
			if(_controller != null)
			{
				_controller.UnregisterScreen(this);
			}
		}

		public void GoRaise(CompScreens.Signal signal)
		{
			if(_transCurrent != null)
			{
				StopCoroutine(_transCurrent);
			}

			if(_signalCurrent != null)
			{
				_signalCurrent.Is = true;
			}

			_signalCurrent = signal;
			_transCurrent = StartCoroutine(Rise(_signalCurrent));
		}

		public void GoFade(CompScreens.Signal signal)
		{
			if(_transCurrent != null)
			{
				StopCoroutine(_transCurrent);
			}

			if(_signalCurrent != null)
			{
				_signalCurrent.Is = true;
			}

			_signalCurrent = signal;
			_transCurrent = StartCoroutine(Fade(_signalCurrent));
		}

		public void Init()
		{
			_canvasGroup.alpha = 0f;
			_canvasGroup.interactable = false;
			_canvasGroup.blocksRaycasts = false;
		}

		private IEnumerator Rise(CompScreens.Signal signal)
		{
			_canvasGroup.interactable = true;
			_canvasGroup.blocksRaycasts = true;

			var value = _canvasGroup.alpha;
			var interval = IntervalTrans < INTERVAL_TRANS_MIN_F
				? INTERVAL_TRANS_MIN_F
				: IntervalTrans;
			var speed = 1f / interval;
			while(value < 1f)
			{
				value += speed * Time.deltaTime;
				_canvasGroup.alpha = value;

				//Debug.Log($"rising: {name}");

				yield return null;
			}

			_canvasGroup.alpha = 1f;

			var customs = GetComponents<IWidgetController>();
			for(var index = 0; index < customs.Length; index++)
			{
				customs[index].OnScreenEnter();
			}

			signal.Is = true;

			_signalCurrent = null;
			_transCurrent = null;
		}

		private IEnumerator Fade(CompScreens.Signal signal)
		{
			var value = _canvasGroup.alpha;
			var interval = IntervalTrans < INTERVAL_TRANS_MIN_F
				? INTERVAL_TRANS_MIN_F
				: IntervalTrans;
			var speed = 1f / interval;
			while(value > 0f)
			{
				value -= speed * Time.deltaTime;
				_canvasGroup.alpha = value;

				//Debug.Log($"fading: {name}");

				yield return null;
			}

			_canvasGroup.alpha = 0f;

			_canvasGroup.interactable = false;
			_canvasGroup.blocksRaycasts = false;

			var customs = GetComponents<IWidgetController>();
			for(var index = 0; index < customs.Length; index++)
			{
				customs[index].OnScreenExit();
			}

			signal.Is = true;

			_signalCurrent = null;
			_transCurrent = null;
		}
	}
}
