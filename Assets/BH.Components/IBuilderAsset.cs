using UnityEngine;

namespace BH.Components
{
	public interface IBuilderAsset<T> where T : Component
	{
		T Build(Transform parent, CxOrigin origin, ModelUser model);
		void Destroy(T instance);
	}
}