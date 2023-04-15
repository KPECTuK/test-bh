using UnityEngine;

namespace BH.Components
{
	public interface IBuilderAsset<T> where T : Component
	{
		T Build(Transform parent, CxOrigin origin);
		void Destroy(T instance);
	}
}