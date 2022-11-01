using System.Collections.Generic;
using com.adjust.sdk;

public class AdjustServiceiOSAndroidImpl : IAppAttributionService
{
	private Dictionary<AdjustTrackingEvent, string> m_eventTrackingTokenMapping = new Dictionary<AdjustTrackingEvent, string>();

	private bool m_initialized;

	public void Init()
	{
		if (m_initialized)
		{
			DebugLog.Warn(GetType(), "Duplicate Init call for Adjust!");
			return;
		}
		m_initialized = true;
		DebugLog.Log(GetType(), "Init");
		AdjustConfig adjustConfig = new AdjustConfig("5zdfhy2zp6qa", AdjustEnvironment.Production);
		adjustConfig.setLogLevel(AdjustLogLevel.Info);
		adjustConfig.setLogDelegate(delegate(string logMessage)
		{
			DebugLog.Log(GetType(), logMessage);
		});
		Adjust.start(adjustConfig);
		m_eventTrackingTokenMapping.Add(AdjustTrackingEvent.Signup, "j9r2ym");
		m_eventTrackingTokenMapping.Add(AdjustTrackingEvent.TutorialPassed, "j9u5ph");
		m_eventTrackingTokenMapping.Add(AdjustTrackingEvent.Sale, "d1jcjq");
		m_eventTrackingTokenMapping.Add(AdjustTrackingEvent.PlayerProgress1, "lu0b1l");
		m_eventTrackingTokenMapping.Add(AdjustTrackingEvent.PlayerProgress2, "hqrqq9");
		m_eventTrackingTokenMapping.Add(AdjustTrackingEvent.PlayerProgress3, "hjbj14");
		m_eventTrackingTokenMapping.Add(AdjustTrackingEvent.PlayerProgress4, "sumtkc");
		m_eventTrackingTokenMapping.Add(AdjustTrackingEvent.PlayerProgress5, "3mc4b8");
	}

	public void TrackEvent(AdjustTrackingEvent adjustTrackingEvent)
	{
		DebugLog.Log(GetType(), string.Format("TrackEvent: '{0}'", adjustTrackingEvent));
		string value = string.Empty;
		if (m_eventTrackingTokenMapping.TryGetValue(adjustTrackingEvent, out value))
		{
			Adjust.trackEvent(new AdjustEvent(value));
		}
		else
		{
			DebugLog.Error(GetType(), string.Format("Tracking the event: '{0}' was not possible, because no tracking token was found!", adjustTrackingEvent));
		}
	}

	public void TrackPlayerLevelProgress(int playerLevel)
	{
		AdjustTrackingEvent adjustTrackingEvent = AdjustTrackingEvent.None;
		switch (playerLevel)
		{
		case 5:
			adjustTrackingEvent = AdjustTrackingEvent.PlayerProgress1;
			break;
		case 10:
			adjustTrackingEvent = AdjustTrackingEvent.PlayerProgress2;
			break;
		case 15:
			adjustTrackingEvent = AdjustTrackingEvent.PlayerProgress3;
			break;
		case 20:
			adjustTrackingEvent = AdjustTrackingEvent.PlayerProgress4;
			break;
		case 25:
			adjustTrackingEvent = AdjustTrackingEvent.PlayerProgress5;
			break;
		}
		if (adjustTrackingEvent != 0)
		{
			TrackEvent(adjustTrackingEvent);
		}
	}

	public void TrackSaleEvent(double price, string transactionId)
	{
		DebugLog.Log(GetType(), string.Format("TrackSaleEvent. price: '{0}', transactionId: '{1}'", price, transactionId));
		string value = string.Empty;
		if (m_eventTrackingTokenMapping.TryGetValue(AdjustTrackingEvent.Sale, out value))
		{
			AdjustEvent adjustEvent = new AdjustEvent(value);
			adjustEvent.setRevenue(price, "USD");
			adjustEvent.setTransactionId(transactionId);
			Adjust.trackEvent(adjustEvent);
		}
		else
		{
			DebugLog.Error(GetType(), string.Format("Tracking the sale event was not possible, because no tracking token was found!"));
		}
	}
}
