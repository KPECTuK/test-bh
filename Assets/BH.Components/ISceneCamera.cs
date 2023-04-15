using BH.Model;
using UnityEngine;

namespace BH.Components
{
	public interface ISceneCamera
	{
		Camera ControllerCamera { get; }

		uint IdCorresponding { get; }
	}
}
