using System;
using UnityEngine;

namespace com.adjust.sdk
{
	public class Adjust : MonoBehaviour
	{
		private const string errorMessage = "adjust: SDK not started. Start it manually using the 'start' method.";

		private static IAdjust instance;

		private static Action<string> deferredDeeplinkDelegate;

		private static Action<AdjustEventSuccess> eventSuccessDelegate;

		private static Action<AdjustEventFailure> eventFailureDelegate;

		private static Action<AdjustSessionSuccess> sessionSuccessDelegate;

		private static Action<AdjustSessionFailure> sessionFailureDelegate;

		private static Action<AdjustAttribution> attributionChangedDelegate;

		public bool startManually = true;

		public bool eventBuffering;

		public bool printAttribution = true;

		public bool sendInBackground;

		public bool launchDeferredDeeplink = true;

		public string appToken = "{Your App Token}";

		public AdjustLogLevel logLevel = AdjustLogLevel.Info;

		public AdjustEnvironment environment;

		private void Awake()
		{
			if (instance != null)
			{
				return;
			}
			UnityEngine.Object.DontDestroyOnLoad(base.transform.gameObject);
			if (!startManually)
			{
				AdjustConfig adjustConfig = ((logLevel == AdjustLogLevel.Suppress) ? new AdjustConfig(appToken, environment, true) : new AdjustConfig(appToken, environment));
				adjustConfig.setLogLevel(logLevel);
				adjustConfig.setSendInBackground(sendInBackground);
				adjustConfig.setEventBufferingEnabled(eventBuffering);
				adjustConfig.setLaunchDeferredDeeplink(launchDeferredDeeplink);
				if (printAttribution)
				{
					adjustConfig.setEventSuccessDelegate(EventSuccessCallback);
					adjustConfig.setEventFailureDelegate(EventFailureCallback);
					adjustConfig.setSessionSuccessDelegate(SessionSuccessCallback);
					adjustConfig.setSessionFailureDelegate(SessionFailureCallback);
					adjustConfig.setDeferredDeeplinkDelegate(DeferredDeeplinkCallback);
					adjustConfig.setAttributionChangedDelegate(AttributionChangedCallback);
				}
				start(adjustConfig);
			}
		}

		private void OnApplicationPause(bool pauseStatus)
		{
			if (instance != null)
			{
				if (pauseStatus)
				{
					instance.onPause();
				}
				else
				{
					instance.onResume();
				}
			}
		}

		public static void start(AdjustConfig adjustConfig)
		{
			if (instance != null)
			{
				Debug.Log("adjust: Error, SDK already started.");
				return;
			}
			if (adjustConfig == null)
			{
				Debug.Log("adjust: Missing config to start.");
				return;
			}
			instance = new AdjustAndroid();
			if (instance == null)
			{
				Debug.Log("adjust: SDK can only be used in Android, iOS, Windows Phone 8 or Windows Store apps.");
				return;
			}
			eventSuccessDelegate = adjustConfig.getEventSuccessDelegate();
			eventFailureDelegate = adjustConfig.getEventFailureDelegate();
			sessionSuccessDelegate = adjustConfig.getSessionSuccessDelegate();
			sessionFailureDelegate = adjustConfig.getSessionFailureDelegate();
			deferredDeeplinkDelegate = adjustConfig.getDeferredDeeplinkDelegate();
			attributionChangedDelegate = adjustConfig.getAttributionChangedDelegate();
			instance.start(adjustConfig);
		}

		public static void trackEvent(AdjustEvent adjustEvent)
		{
			if (instance == null)
			{
				Debug.Log("adjust: SDK not started. Start it manually using the 'start' method.");
			}
			else if (adjustEvent == null)
			{
				Debug.Log("adjust: Missing event to track.");
			}
			else
			{
				instance.trackEvent(adjustEvent);
			}
		}

		public static void setEnabled(bool enabled)
		{
			if (instance == null)
			{
				Debug.Log("adjust: SDK not started. Start it manually using the 'start' method.");
			}
			else
			{
				instance.setEnabled(enabled);
			}
		}

		public static bool isEnabled()
		{
			if (instance == null)
			{
				Debug.Log("adjust: SDK not started. Start it manually using the 'start' method.");
				return false;
			}
			return instance.isEnabled();
		}

		public static void setOfflineMode(bool enabled)
		{
			if (instance == null)
			{
				Debug.Log("adjust: SDK not started. Start it manually using the 'start' method.");
			}
			else
			{
				instance.setOfflineMode(enabled);
			}
		}

		public static void sendFirstPackages()
		{
			if (instance == null)
			{
				Debug.Log("adjust: SDK not started. Start it manually using the 'start' method.");
			}
			else
			{
				instance.sendFirstPackages();
			}
		}

		public static void addSessionPartnerParameter(string key, string value)
		{
			AdjustAndroid.addSessionPartnerParameter(key, value);
		}

		public static void addSessionCallbackParameter(string key, string value)
		{
			AdjustAndroid.addSessionCallbackParameter(key, value);
		}

		public static void removeSessionPartnerParameter(string key)
		{
			AdjustAndroid.removeSessionPartnerParameter(key);
		}

		public static void removeSessionCallbackParameter(string key)
		{
			AdjustAndroid.removeSessionCallbackParameter(key);
		}

		public static void resetSessionPartnerParameters()
		{
			AdjustAndroid.resetSessionPartnerParameters();
		}

		public static void resetSessionCallbackParameters()
		{
			AdjustAndroid.resetSessionCallbackParameters();
		}

		public static void setDeviceToken(string deviceToken)
		{
			if (instance == null)
			{
				Debug.Log("adjust: SDK not started. Start it manually using the 'start' method.");
			}
			else
			{
				instance.setDeviceToken(deviceToken);
			}
		}

		public static string getIdfa()
		{
			if (instance == null)
			{
				Debug.Log("adjust: SDK not started. Start it manually using the 'start' method.");
				return null;
			}
			return instance.getIdfa();
		}

		public static void setReferrer(string referrer)
		{
			if (instance == null)
			{
				Debug.Log("adjust: SDK not started. Start it manually using the 'start' method.");
			}
			else
			{
				instance.setReferrer(referrer);
			}
		}

		public static void getGoogleAdId(Action<string> onDeviceIdsRead)
		{
			if (instance == null)
			{
				Debug.Log("adjust: SDK not started. Start it manually using the 'start' method.");
			}
			else
			{
				instance.getGoogleAdId(onDeviceIdsRead);
			}
		}

		public void GetNativeAttribution(string attributionData)
		{
			if (instance == null)
			{
				Debug.Log("adjust: SDK not started. Start it manually using the 'start' method.");
				return;
			}
			if (attributionChangedDelegate == null)
			{
				Debug.Log("adjust: Attribution changed delegate was not set.");
				return;
			}
			AdjustAttribution obj = new AdjustAttribution(attributionData);
			attributionChangedDelegate(obj);
		}

		public void GetNativeEventSuccess(string eventSuccessData)
		{
			if (instance == null)
			{
				Debug.Log("adjust: SDK not started. Start it manually using the 'start' method.");
				return;
			}
			if (eventSuccessDelegate == null)
			{
				Debug.Log("adjust: Event success delegate was not set.");
				return;
			}
			AdjustEventSuccess obj = new AdjustEventSuccess(eventSuccessData);
			eventSuccessDelegate(obj);
		}

		public void GetNativeEventFailure(string eventFailureData)
		{
			if (instance == null)
			{
				Debug.Log("adjust: SDK not started. Start it manually using the 'start' method.");
				return;
			}
			if (eventFailureDelegate == null)
			{
				Debug.Log("adjust: Event failure delegate was not set.");
				return;
			}
			AdjustEventFailure obj = new AdjustEventFailure(eventFailureData);
			eventFailureDelegate(obj);
		}

		public void GetNativeSessionSuccess(string sessionSuccessData)
		{
			if (instance == null)
			{
				Debug.Log("adjust: SDK not started. Start it manually using the 'start' method.");
				return;
			}
			if (sessionSuccessDelegate == null)
			{
				Debug.Log("adjust: Session success delegate was not set.");
				return;
			}
			AdjustSessionSuccess obj = new AdjustSessionSuccess(sessionSuccessData);
			sessionSuccessDelegate(obj);
		}

		public void GetNativeSessionFailure(string sessionFailureData)
		{
			if (instance == null)
			{
				Debug.Log("adjust: SDK not started. Start it manually using the 'start' method.");
				return;
			}
			if (sessionFailureDelegate == null)
			{
				Debug.Log("adjust: Session failure delegate was not set.");
				return;
			}
			AdjustSessionFailure obj = new AdjustSessionFailure(sessionFailureData);
			sessionFailureDelegate(obj);
		}

		public void GetNativeDeferredDeeplink(string deeplinkURL)
		{
			if (instance == null)
			{
				Debug.Log("adjust: SDK not started. Start it manually using the 'start' method.");
			}
			else if (deferredDeeplinkDelegate == null)
			{
				Debug.Log("adjust: Deferred deeplink delegate was not set.");
			}
			else
			{
				deferredDeeplinkDelegate(deeplinkURL);
			}
		}

		private void AttributionChangedCallback(AdjustAttribution attributionData)
		{
			Debug.Log("Attribution changed!");
			if (attributionData.trackerName != null)
			{
				Debug.Log("trackerName " + attributionData.trackerName);
			}
			if (attributionData.trackerToken != null)
			{
				Debug.Log("trackerToken " + attributionData.trackerToken);
			}
			if (attributionData.network != null)
			{
				Debug.Log("network " + attributionData.network);
			}
			if (attributionData.campaign != null)
			{
				Debug.Log("campaign " + attributionData.campaign);
			}
			if (attributionData.adgroup != null)
			{
				Debug.Log("adgroup " + attributionData.adgroup);
			}
			if (attributionData.creative != null)
			{
				Debug.Log("creative " + attributionData.creative);
			}
			if (attributionData.clickLabel != null)
			{
				Debug.Log("clickLabel" + attributionData.clickLabel);
			}
		}

		private void EventSuccessCallback(AdjustEventSuccess eventSuccessData)
		{
			Debug.Log("Event tracked successfully!");
			if (eventSuccessData.Message != null)
			{
				Debug.Log("Message: " + eventSuccessData.Message);
			}
			if (eventSuccessData.Timestamp != null)
			{
				Debug.Log("Timestamp: " + eventSuccessData.Timestamp);
			}
			if (eventSuccessData.Adid != null)
			{
				Debug.Log("Adid: " + eventSuccessData.Adid);
			}
			if (eventSuccessData.EventToken != null)
			{
				Debug.Log("EventToken: " + eventSuccessData.EventToken);
			}
			if (eventSuccessData.JsonResponse != null)
			{
				Debug.Log("JsonResponse: " + eventSuccessData.GetJsonResponse());
			}
		}

		private void EventFailureCallback(AdjustEventFailure eventFailureData)
		{
			Debug.Log("Event tracking failed!");
			if (eventFailureData.Message != null)
			{
				Debug.Log("Message: " + eventFailureData.Message);
			}
			if (eventFailureData.Timestamp != null)
			{
				Debug.Log("Timestamp: " + eventFailureData.Timestamp);
			}
			if (eventFailureData.Adid != null)
			{
				Debug.Log("Adid: " + eventFailureData.Adid);
			}
			if (eventFailureData.EventToken != null)
			{
				Debug.Log("EventToken: " + eventFailureData.EventToken);
			}
			Debug.Log("WillRetry: " + eventFailureData.WillRetry);
			if (eventFailureData.JsonResponse != null)
			{
				Debug.Log("JsonResponse: " + eventFailureData.GetJsonResponse());
			}
		}

		private void SessionSuccessCallback(AdjustSessionSuccess sessionSuccessData)
		{
			Debug.Log("Session tracked successfully!");
			if (sessionSuccessData.Message != null)
			{
				Debug.Log("Message: " + sessionSuccessData.Message);
			}
			if (sessionSuccessData.Timestamp != null)
			{
				Debug.Log("Timestamp: " + sessionSuccessData.Timestamp);
			}
			if (sessionSuccessData.Adid != null)
			{
				Debug.Log("Adid: " + sessionSuccessData.Adid);
			}
			if (sessionSuccessData.JsonResponse != null)
			{
				Debug.Log("JsonResponse: " + sessionSuccessData.GetJsonResponse());
			}
		}

		private void SessionFailureCallback(AdjustSessionFailure sessionFailureData)
		{
			Debug.Log("Session tracking failed!");
			if (sessionFailureData.Message != null)
			{
				Debug.Log("Message: " + sessionFailureData.Message);
			}
			if (sessionFailureData.Timestamp != null)
			{
				Debug.Log("Timestamp: " + sessionFailureData.Timestamp);
			}
			if (sessionFailureData.Adid != null)
			{
				Debug.Log("Adid: " + sessionFailureData.Adid);
			}
			Debug.Log("WillRetry: " + sessionFailureData.WillRetry);
			if (sessionFailureData.JsonResponse != null)
			{
				Debug.Log("JsonResponse: " + sessionFailureData.GetJsonResponse());
			}
		}

		private void DeferredDeeplinkCallback(string deeplinkURL)
		{
			Debug.Log("Deferred deeplink reported!");
			if (deeplinkURL != null)
			{
				Debug.Log("Deeplink URL: " + deeplinkURL);
			}
			else
			{
				Debug.Log("Deeplink URL is null!");
			}
		}
	}
}
