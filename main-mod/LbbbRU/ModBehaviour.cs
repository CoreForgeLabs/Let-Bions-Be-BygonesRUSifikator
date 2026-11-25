using Object = UnityEngine.Object;
using UnityEngine;

namespace LbbbRU;

public class ModBehaviour : MonoBehaviour
{
	private void Update()
	{
		try
		{
			Core.Update();
		}
		catch
		{
		}
	}

	private void LateUpdate()
	{
		try
		{
			Core.LateUpdate();
		}
		catch
		{
		}
	}
}
