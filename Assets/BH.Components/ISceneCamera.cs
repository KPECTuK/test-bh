using BH.Model;
using UnityEngine;

namespace BH.Components
{
	public interface ISceneCamera
	{
		CxId Id { get; }
		Camera CompCamera { get; }
	}
}
