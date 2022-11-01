using System;
using System.Collections.Generic;
using ABH.GameDatas;

public class DispatchMessage
{
	public enum Status
	{
		Exception,
		Warning,
		Error,
		Info,
		GlobalLoading,
		LocalLoading,
		LocalLoadingNonBlocking,
		InfoAndIcon,
		InfoAndLoot
	}

	public string m_CompareTag;

	public Status m_DispatchStatus;

	public string m_DispatchMessage;

	public string m_DispatchAsset;

	public float m_DispatchProgress = 1f;

	public List<IInventoryItemGameData> m_DispatchItems;

	public string MessageId { get; private set; }

	public DispatchMessage()
	{
		MessageId = Guid.NewGuid().ToString();
	}

	public override bool Equals(object obj)
	{
		return this == obj;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public static bool operator ==(DispatchMessage a, DispatchMessage b)
	{
		return a.m_CompareTag.Equals(b.m_CompareTag);
	}

	public static bool operator !=(DispatchMessage a, DispatchMessage b)
	{
		return a.m_CompareTag != b.m_CompareTag;
	}
}
