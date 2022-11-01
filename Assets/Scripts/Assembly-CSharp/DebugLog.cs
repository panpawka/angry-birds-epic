using System;
using UnityEngine;

internal static class DebugLog
{
	private static string m_timestamp
	{
		get
		{
			return DateTime.Now.ToString("HH:mm:ss.ffffff");
		}
	}

	public static void ForceLog(Type tag, string msg)
	{
		Debug.Log(m_timestamp + ": [" + tag.Name + "] " + msg);
	}

	public static void Log(Type tag, string msg)
	{
	}

	public static void Log(object msg)
	{
	}

	public static void Log(string tag, string msg)
	{
	}

	public static void Log(string tag, string msg, string hexColor)
	{
	}

	public static void Log(Type tag, string msg, LogPlatform platform)
	{
	}

	public static void Log(string tag, string msg, LogPlatform platform)
	{
	}

	public static void Error(object msg)
	{
	}

	public static void Error(Type tag, string msg)
	{
	}

	public static void Error(string tag, string msg)
	{
	}

	public static void Error(Type tag, string msg, LogPlatform platform)
	{
	}

	public static void Error(string tag, string msg, LogPlatform platform)
	{
	}

	public static void ForceWarn(Type tag, string msg)
	{
		Debug.LogWarning(m_timestamp + ": [" + tag.Name + "] " + msg);
	}

	public static void Warn(Type tag, string msg, LogPlatform platform)
	{
	}

	public static void Warn(string tag, string msg, LogPlatform platform)
	{
	}

	public static void Warn(Type tag, string msg)
	{
	}

	public static void Warn(string tag, string msg)
	{
	}

	public static void Warn(object msg)
	{
	}
}
