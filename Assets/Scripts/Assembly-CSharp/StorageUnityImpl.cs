using System;
using Chimera.Library.Components.Interfaces;
using UnityEngine;

public class StorageUnityImpl : IStorageService
{
	public bool SetInt(string key, int value)
	{
		PlayerPrefs.SetInt(key, value);
		Save();
		return true;
	}

	public int GetInt(string key)
	{
		return PlayerPrefs.GetInt(key);
	}

	public int GetInt(string key, int standardValue)
	{
		return PlayerPrefs.GetInt(key, standardValue);
	}

	public bool SetFloat(string key, float value)
	{
		PlayerPrefs.SetFloat(key, value);
		Save();
		return true;
	}

	public float GetFloat(string key)
	{
		return PlayerPrefs.GetFloat(key);
	}

	public float GetFloat(string key, float standardValue)
	{
		return PlayerPrefs.GetFloat(key, standardValue);
	}

	public bool SetString(string key, string value)
	{
		PlayerPrefs.SetString(key, value);
		Save();
		return true;
	}

	public string GetString(string key)
	{
		return PlayerPrefs.GetString(key);
	}

	public string GetString(string key, string standardValue)
	{
		return PlayerPrefs.GetString(key, standardValue);
	}

	public bool HasKey(string key)
	{
		return PlayerPrefs.HasKey(key);
	}

	public bool DeleteKey(string key)
	{
		PlayerPrefs.DeleteKey(key);
		Save();
		return true;
	}

	public bool DeleteAll()
	{
		PlayerPrefs.DeleteAll();
		Save();
		return true;
	}

	public bool Save()
	{
		PlayerPrefs.Save();
		return true;
	}

	public byte[] GetBytes(string key)
	{
		throw new NotImplementedException();
	}

	public byte[] GetBytes(string key, byte[] standardValue)
	{
		throw new NotImplementedException();
	}

	public bool SetBytes(string key, byte[] value)
	{
		throw new NotImplementedException();
	}
}
