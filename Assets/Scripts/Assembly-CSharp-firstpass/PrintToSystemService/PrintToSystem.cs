using System.Runtime.InteropServices;
using UnityEngine;

namespace PrintToSystemService
{
	public class PrintToSystem : MonoBehaviour
	{
		[DllImport("__Internal")]
		private static extern void _PrintToSystem(string message);

		public static void Print(string message)
		{
		}
	}
}
