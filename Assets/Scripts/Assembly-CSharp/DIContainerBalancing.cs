using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using ABH.Shared.BalancingData;
using ABH.Shared.Models;
using Chimera.Library.Components.Interfaces;
using Chimera.Library.Components.Models;
using Chimera.Library.Components.Services;
using UnityEngine;

public class DIContainerBalancing
{
	private static readonly string m_serializedBalancingDataContainerFileExtension = ".bytes";

	private static IBalancingDataLoaderService m_service;

	private static bool m_isInitializing;

	public static Action<string> ReportError = DebugLog.Error;

	private static LootTableBalancingDataProvider m_lootTableBalancingDataPovider;

	private static InventoryItemBalancingDataPovider m_inventoryItemBalancingDataPovider;

	private static IBalancingDataLoaderService m_eventBalancingService;

	public static string BalancingDataAssetFilename
	{
		get
		{
			return DIContainerInfrastructure.GetTargetBuildGroup() + "_" + BalancingDataResourceFilename + "_" + DIContainerInfrastructure.GetVersionService().StoreVersion + ".bytes";
		}
	}

	public static string EventBalancingDataAssetFilename
	{
		get
		{
			return DIContainerInfrastructure.GetTargetBuildGroup() + "_" + EventBalancingDataResourceFilename + ".bytes";
		}
	}

	public static string BalancingDataResourceFilename
	{
		get
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(typeof(SerializedBalancingDataContainer).Name);
			return stringBuilder.ToString();
		}
	}

	public static string EventBalancingDataResourceFilename
	{
		get
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("SerializedEventBalancingDataContainer");
			return stringBuilder.ToString();
		}
	}

	public static IBalancingDataLoaderService Service
	{
		get
		{
			if (m_isInitializing)
			{
				ReportError("Balancing Service is initializing, please try again!");
			}
			if (m_service == null)
			{
				ReportError("Balancing Service not initialized!");
			}
			return m_service;
		}
	}

	public static LootTableBalancingDataProvider LootTableBalancingDataPovider
	{
		get
		{
			return m_lootTableBalancingDataPovider ?? (m_lootTableBalancingDataPovider = new LootTableBalancingDataProvider());
		}
		set
		{
			m_lootTableBalancingDataPovider = value;
		}
	}

	public static IBalancingDataLoaderService EventBalancingService
	{
		get
		{
			if (m_eventBalancingService == null)
			{
				DebugLog.Log("Event Balancing Service not initialized!");
			}
			return m_eventBalancingService;
		}
	}

	public static bool EventBalancingLoadingPending { get; private set; }

	public static bool IsInitialized { get; private set; }

	[method: MethodImpl(32)]
	public static event Action OnBalancingDataInitialized;

	public static bool Init(Action<BalancingInitErrorCode> errorCallback = null, bool restart = false)
	{
		//Discarded unreachable code: IL_0276
		if (restart)
		{
			IsInitialized = false;
		}
		if (m_isInitializing)
		{
			if (errorCallback != null)
			{
				errorCallback(BalancingInitErrorCode.INIT_IN_PROGRESS);
			}
			return false;
		}
		if (IsInitialized)
		{
			if (DIContainerBalancing.OnBalancingDataInitialized != null)
			{
				DIContainerBalancing.OnBalancingDataInitialized();
			}
			return true;
		}
		DebugLog.Log("[DIContainerBalancing] Init");
		bool flag = false;
		m_isInitializing = true;
		AssetInfo assetInfoFor = DIContainerInfrastructure.GetAssetData().GetAssetInfoFor(BalancingDataAssetFilename);
		byte[] outBytes;
		if (assetInfoFor == null)
		{
			DebugLog.Log(typeof(DIContainerBalancing), "Asset info for " + BalancingDataAssetFilename + " is null. Loading from local: " + BalancingDataResourceFilename);
			string path = "SerializedBalancingData/" + BalancingDataResourceFilename;
			TextAsset textAsset = Resources.Load(path) as TextAsset;
			if (textAsset == null)
			{
				string text = "Could not load " + BalancingDataResourceFilename + "! (#1)";
				ReportError(text);
				DebugLog.Error(typeof(DIContainerBalancing), text);
				if (errorCallback != null)
				{
					errorCallback(BalancingInitErrorCode.FILE_NOT_FOUND);
				}
				return false;
			}
			outBytes = textAsset.bytes;
		}
		else
		{
			string path = assetInfoFor.FilePath;
			if (!File.Exists(path))
			{
				ReportError("[DIContainerBalancing] Could not load " + BalancingDataResourceFilename + "! (file does not exist: " + path + ")");
				if (errorCallback != null)
				{
					errorCallback(BalancingInitErrorCode.FILE_NOT_FOUND);
				}
				return false;
			}
			outBytes = FileHelper.ReadAllBytes(path);
		}
		if (flag)
		{
			DebugLog.Log("[DIContainerBalancing] Trying to decrypt asset file");
			TryDecrypt(outBytes, out outBytes);
		}
		DebugLog.Log("[DIContainerBalancing] Trying to decompress asset file, Info = " + assetInfoFor);
		byte[] array = DIContainerInfrastructure.GetCompressionService().DecompressIfNecessary(outBytes);
		if (array != null)
		{
			outBytes = array;
		}
		DebugLog.Log("[DIContainerBalancing] Loaded " + outBytes.Length + " bytes of possibly originally compressed and " + ((!flag) ? "un" : string.Empty) + "encrypted asset data.");
		try
		{
			m_service = new BalancingDataLoaderServiceProtobufImpl(outBytes, DIContainerInfrastructure.GetBalancingDataSerializer().Deserialize, delegate(string msg)
			{
				DebugLog.Log(typeof(BalancingDataLoaderServiceProtobufImpl), msg);
			}, delegate(string msg)
			{
				DebugLog.Error(typeof(BalancingDataLoaderServiceProtobufImpl), msg);
			});
		}
		catch (Exception ex)
		{
			DebugLog.Error(ex.ToString());
			if (flag)
			{
				DebugLog.Error("Maybe you chose the wrong decryption key and/or -algorithm?");
			}
			throw ex;
		}
		m_isInitializing = false;
		IsInitialized = true;
		if (DIContainerBalancing.OnBalancingDataInitialized != null)
		{
			DIContainerBalancing.OnBalancingDataInitialized();
		}
		return true;
	}

	private static bool TryDecrypt(byte[] inBytes, out byte[] outBytes)
	{
		//Discarded unreachable code: IL_003c
		try
		{
			outBytes = DIContainerInfrastructure.GetEncryptionService().Decrypt3DES(inBytes, DIContainerConfig.Key, DIContainerConfig.GetConstants().EncryptionAlgo);
		}
		catch (Exception ex)
		{
			DebugLog.Error("[DIContainerBalancing] " + ex);
			outBytes = inBytes;
			return false;
		}
		return true;
	}

	public static void Reset()
	{
		m_service = null;
		m_inventoryItemBalancingDataPovider = null;
		m_lootTableBalancingDataPovider = null;
		m_eventBalancingService = null;
	}

	public static InventoryItemBalancingDataPovider GetInventoryItemBalancingDataPovider()
	{
		if (m_inventoryItemBalancingDataPovider == null)
		{
			m_inventoryItemBalancingDataPovider = new InventoryItemBalancingDataPovider();
		}
		return m_inventoryItemBalancingDataPovider;
	}

	public static bool GetEventBalancingDataPoviderAsynch(Action<IBalancingDataLoaderService> callback)
	{
		if (EventBalancingLoadingPending)
		{
			DebugLog.Error("Event balancing already loading! Stopped to prevent skynest crash");
			return false;
		}
		EventBalancingLoadingPending = true;
		if (DIContainerInfrastructure.GetAssetsService().NeedToDownloadAsset(EventBalancingDataAssetFilename))
		{
			DIContainerInfrastructure.GetAssetsService().Load(EventBalancingDataAssetFilename, delegate(string result)
			{
				if (result != null)
				{
					EventBalancingLoadingPending = false;
					FinishWithEventBalancingInit(callback);
				}
				else
				{
					EventBalancingLoadingPending = false;
					callback(null);
				}
			}, SetDownloadProgress, SetSlowProgress);
			return true;
		}
		if (m_eventBalancingService != null)
		{
			EventBalancingLoadingPending = false;
			if (callback != null)
			{
				callback(m_eventBalancingService);
			}
			return false;
		}
		EventBalancingLoadingPending = false;
		FinishWithEventBalancingInit(callback);
		return false;
	}

	public static void SetDownloadProgress(float loadingProgress)
	{
	}

	private static void SetSlowProgress(bool isSlow)
	{
	}

	private static bool FinishWithEventBalancingInit(Action<IBalancingDataLoaderService> callback)
	{
		//Discarded unreachable code: IL_01af
		AssetInfo assetInfoFor = DIContainerInfrastructure.GetAssetData().GetAssetInfoFor(EventBalancingDataAssetFilename);
		if (assetInfoFor == null)
		{
			DebugLog.Log(EventBalancingDataAssetFilename + " asset data does not exist, contents: " + DIContainerInfrastructure.GetAssetData().Assets.Aggregate(string.Empty, (string acc, KeyValuePair<string, AssetInfo> kvp) => string.Concat(acc, "[", kvp.Key, " => ", kvp.Value, "]")));
		}
		byte[] data;
		if (assetInfoFor == null)
		{
			DebugLog.Log("[DIContainerBalancing] Asset info for " + EventBalancingDataAssetFilename + " is null. Loading from local: " + EventBalancingDataResourceFilename);
			string path = "SerializedBalancingData/" + EventBalancingDataResourceFilename;
			TextAsset textAsset = Resources.Load(path) as TextAsset;
			if (textAsset == null)
			{
				string obj = "Could not load " + EventBalancingDataResourceFilename + "! (#1)";
				ReportError(obj);
				DebugLog.Error("[DIContainerBalancing] error");
				callback(null);
				return false;
			}
			data = textAsset.bytes;
		}
		else
		{
			string path = assetInfoFor.FilePath;
			if (!File.Exists(path))
			{
				ReportError("Could not load " + EventBalancingDataResourceFilename + "! (file does not exist: " + path + ")");
				callback(null);
				return false;
			}
			data = FileHelper.ReadAllBytes(path);
		}
		DebugLog.Log("[DIContainerBalancing] Trying to decompress asset file, Info = " + assetInfoFor);
		data = DIContainerInfrastructure.GetCompressionService().DecompressIfNecessary(data);
		DebugLog.Log("[DIContainerBalancing] Loaded " + data.Length + " bytes of possibly originally compressed");
		try
		{
			m_eventBalancingService = new BalancingDataLoaderServiceProtobufImpl(data, DIContainerInfrastructure.GetBalancingDataSerializer().Deserialize, null, null);
		}
		catch (Exception ex)
		{
			DebugLog.Error(ex.ToString());
			throw ex;
		}
		DIContainerInfrastructure.GetCurrentPlayer().RemoveInvalidTrophyFix();
		callback(m_eventBalancingService);
		return true;
	}
}
