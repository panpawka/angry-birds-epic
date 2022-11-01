using UnityEngine;

public class MonoBehaviourContainerBase : MonoBehaviour, IMonoBehaviourContainer
{
	public void AddComponentSafely<T>(ref T member) where T : MonoBehaviour
	{
		if ((Object)member == (Object)null)
		{
			member = base.gameObject.GetComponent<T>();
		}
		if ((Object)member == (Object)null)
		{
			member = base.gameObject.AddComponent<T>();
		}
	}
}
