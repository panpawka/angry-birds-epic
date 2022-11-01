using System;
using UnityEngine;

namespace com.adjust.sdk
{
	public class AdjustAndroid : IAdjust
	{
		private class AttributionChangeListener : AndroidJavaProxy
		{
			private Action<AdjustAttribution> callback;

			public AttributionChangeListener(Action<AdjustAttribution> pCallback)
				: base("com.adjust.sdk.OnAttributionChangedListener")
			{
				callback = pCallback;
			}

			public void onAttributionChanged(AndroidJavaObject attribution)
			{
				if (callback != null)
				{
					AdjustAttribution adjustAttribution = new AdjustAttribution();
					adjustAttribution.trackerName = attribution.Get<string>(AdjustUtils.KeyTrackerName);
					adjustAttribution.trackerToken = attribution.Get<string>(AdjustUtils.KeyTrackerToken);
					adjustAttribution.network = attribution.Get<string>(AdjustUtils.KeyNetwork);
					adjustAttribution.campaign = attribution.Get<string>(AdjustUtils.KeyCampaign);
					adjustAttribution.adgroup = attribution.Get<string>(AdjustUtils.KeyAdgroup);
					adjustAttribution.creative = attribution.Get<string>(AdjustUtils.KeyCreative);
					adjustAttribution.clickLabel = attribution.Get<string>(AdjustUtils.KeyClickLabel);
					callback(adjustAttribution);
				}
			}
		}

		private class DeferredDeeplinkListener : AndroidJavaProxy
		{
			private Action<string> callback;

			public DeferredDeeplinkListener(Action<string> pCallback)
				: base("com.adjust.sdk.OnDeeplinkResponseListener")
			{
				callback = pCallback;
			}

			public bool launchReceivedDeeplink(AndroidJavaObject deeplink)
			{
				if (callback == null)
				{
					return launchDeferredDeeplink;
				}
				string obj = deeplink.Call<string>("toString", new object[0]);
				callback(obj);
				return launchDeferredDeeplink;
			}
		}

		private class EventTrackingSucceededListener : AndroidJavaProxy
		{
			private Action<AdjustEventSuccess> callback;

			public EventTrackingSucceededListener(Action<AdjustEventSuccess> pCallback)
				: base("com.adjust.sdk.OnEventTrackingSucceededListener")
			{
				callback = pCallback;
			}

			public void onFinishedEventTrackingSucceeded(AndroidJavaObject eventSuccessData)
			{
				if (callback != null && eventSuccessData != null)
				{
					AdjustEventSuccess adjustEventSuccess = new AdjustEventSuccess();
					adjustEventSuccess.Adid = eventSuccessData.Get<string>(AdjustUtils.KeyAdid);
					adjustEventSuccess.Message = eventSuccessData.Get<string>(AdjustUtils.KeyMessage);
					adjustEventSuccess.Timestamp = eventSuccessData.Get<string>(AdjustUtils.KeyTimestamp);
					adjustEventSuccess.EventToken = eventSuccessData.Get<string>(AdjustUtils.KeyEventToken);
					try
					{
						AndroidJavaObject androidJavaObject = eventSuccessData.Get<AndroidJavaObject>(AdjustUtils.KeyJsonResponse);
						string jsonResponseString = androidJavaObject.Call<string>("toString", new object[0]);
						adjustEventSuccess.BuildJsonResponseFromString(jsonResponseString);
					}
					catch (Exception)
					{
					}
					callback(adjustEventSuccess);
				}
			}
		}

		private class EventTrackingFailedListener : AndroidJavaProxy
		{
			private Action<AdjustEventFailure> callback;

			public EventTrackingFailedListener(Action<AdjustEventFailure> pCallback)
				: base("com.adjust.sdk.OnEventTrackingFailedListener")
			{
				callback = pCallback;
			}

			public void onFinishedEventTrackingFailed(AndroidJavaObject eventFailureData)
			{
				if (callback != null && eventFailureData != null)
				{
					AdjustEventFailure adjustEventFailure = new AdjustEventFailure();
					adjustEventFailure.Adid = eventFailureData.Get<string>(AdjustUtils.KeyAdid);
					adjustEventFailure.Message = eventFailureData.Get<string>(AdjustUtils.KeyMessage);
					adjustEventFailure.WillRetry = eventFailureData.Get<bool>(AdjustUtils.KeyWillRetry);
					adjustEventFailure.Timestamp = eventFailureData.Get<string>(AdjustUtils.KeyTimestamp);
					adjustEventFailure.EventToken = eventFailureData.Get<string>(AdjustUtils.KeyEventToken);
					try
					{
						AndroidJavaObject androidJavaObject = eventFailureData.Get<AndroidJavaObject>(AdjustUtils.KeyJsonResponse);
						string jsonResponseString = androidJavaObject.Call<string>("toString", new object[0]);
						adjustEventFailure.BuildJsonResponseFromString(jsonResponseString);
					}
					catch (Exception)
					{
					}
					callback(adjustEventFailure);
				}
			}
		}

		private class SessionTrackingSucceededListener : AndroidJavaProxy
		{
			private Action<AdjustSessionSuccess> callback;

			public SessionTrackingSucceededListener(Action<AdjustSessionSuccess> pCallback)
				: base("com.adjust.sdk.OnSessionTrackingSucceededListener")
			{
				callback = pCallback;
			}

			public void onFinishedSessionTrackingSucceeded(AndroidJavaObject sessionSuccessData)
			{
				if (callback != null && sessionSuccessData != null)
				{
					AdjustSessionSuccess adjustSessionSuccess = new AdjustSessionSuccess();
					adjustSessionSuccess.Adid = sessionSuccessData.Get<string>(AdjustUtils.KeyAdid);
					adjustSessionSuccess.Message = sessionSuccessData.Get<string>(AdjustUtils.KeyMessage);
					adjustSessionSuccess.Timestamp = sessionSuccessData.Get<string>(AdjustUtils.KeyTimestamp);
					try
					{
						AndroidJavaObject androidJavaObject = sessionSuccessData.Get<AndroidJavaObject>(AdjustUtils.KeyJsonResponse);
						string jsonResponseString = androidJavaObject.Call<string>("toString", new object[0]);
						adjustSessionSuccess.BuildJsonResponseFromString(jsonResponseString);
					}
					catch (Exception)
					{
					}
					callback(adjustSessionSuccess);
				}
			}
		}

		private class SessionTrackingFailedListener : AndroidJavaProxy
		{
			private Action<AdjustSessionFailure> callback;

			public SessionTrackingFailedListener(Action<AdjustSessionFailure> pCallback)
				: base("com.adjust.sdk.OnSessionTrackingFailedListener")
			{
				callback = pCallback;
			}

			public void onFinishedSessionTrackingFailed(AndroidJavaObject sessionFailureData)
			{
				if (callback != null && sessionFailureData != null)
				{
					AdjustSessionFailure adjustSessionFailure = new AdjustSessionFailure();
					adjustSessionFailure.Adid = sessionFailureData.Get<string>(AdjustUtils.KeyAdid);
					adjustSessionFailure.Message = sessionFailureData.Get<string>(AdjustUtils.KeyMessage);
					adjustSessionFailure.WillRetry = sessionFailureData.Get<bool>(AdjustUtils.KeyWillRetry);
					adjustSessionFailure.Timestamp = sessionFailureData.Get<string>(AdjustUtils.KeyTimestamp);
					try
					{
						AndroidJavaObject androidJavaObject = sessionFailureData.Get<AndroidJavaObject>(AdjustUtils.KeyJsonResponse);
						string jsonResponseString = androidJavaObject.Call<string>("toString", new object[0]);
						adjustSessionFailure.BuildJsonResponseFromString(jsonResponseString);
					}
					catch (Exception)
					{
					}
					callback(adjustSessionFailure);
				}
			}
		}

		private class DeviceIdsReadListener : AndroidJavaProxy
		{
			private Action<string> onPlayAdIdReadCallback;

			public DeviceIdsReadListener(Action<string> pCallback)
				: base("com.adjust.sdk.OnDeviceIdsRead")
			{
				onPlayAdIdReadCallback = pCallback;
			}

			public void onGoogleAdIdRead(string playAdId)
			{
				if (onPlayAdIdReadCallback != null)
				{
					onPlayAdIdReadCallback(playAdId);
				}
			}

			public void onGoogleAdIdRead(AndroidJavaObject ajoAdId)
			{
				if (ajoAdId == null)
				{
					string playAdId = null;
					onGoogleAdIdRead(playAdId);
				}
				else
				{
					onGoogleAdIdRead(ajoAdId.Call<string>("toString", new object[0]));
				}
			}
		}

		private const string sdkPrefix = "unity4.10.2";

		private static bool launchDeferredDeeplink = true;

		private static AndroidJavaClass ajcAdjust;

		private AndroidJavaObject ajoCurrentActivity;

		private DeferredDeeplinkListener onDeferredDeeplinkListener;

		private AttributionChangeListener onAttributionChangedListener;

		private EventTrackingFailedListener onEventTrackingFailedListener;

		private EventTrackingSucceededListener onEventTrackingSucceededListener;

		private SessionTrackingFailedListener onSessionTrackingFailedListener;

		private SessionTrackingSucceededListener onSessionTrackingSucceededListener;

		public AdjustAndroid()
		{
			if (ajcAdjust == null)
			{
				ajcAdjust = new AndroidJavaClass("com.adjust.sdk.Adjust");
			}
			AndroidJavaClass androidJavaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			ajoCurrentActivity = androidJavaClass.GetStatic<AndroidJavaObject>("currentActivity");
		}

		public void start(AdjustConfig adjustConfig)
		{
			AndroidJavaObject androidJavaObject = ((adjustConfig.environment != 0) ? new AndroidJavaClass("com.adjust.sdk.AdjustConfig").GetStatic<AndroidJavaObject>("ENVIRONMENT_PRODUCTION") : new AndroidJavaClass("com.adjust.sdk.AdjustConfig").GetStatic<AndroidJavaObject>("ENVIRONMENT_SANDBOX"));
			bool? allowSuppressLogLevel = adjustConfig.allowSuppressLogLevel;
			AndroidJavaObject androidJavaObject3;
			if (allowSuppressLogLevel.HasValue)
			{
				AndroidJavaObject androidJavaObject2 = new AndroidJavaObject("java.lang.Boolean", adjustConfig.allowSuppressLogLevel.Value);
				androidJavaObject3 = new AndroidJavaObject("com.adjust.sdk.AdjustConfig", ajoCurrentActivity, adjustConfig.appToken, androidJavaObject, androidJavaObject2);
			}
			else
			{
				androidJavaObject3 = new AndroidJavaObject("com.adjust.sdk.AdjustConfig", ajoCurrentActivity, adjustConfig.appToken, androidJavaObject);
			}
			launchDeferredDeeplink = adjustConfig.launchDeferredDeeplink;
			AdjustLogLevel? logLevel = adjustConfig.logLevel;
			if (logLevel.HasValue)
			{
				AndroidJavaObject @static = new AndroidJavaClass("com.adjust.sdk.LogLevel").GetStatic<AndroidJavaObject>(adjustConfig.logLevel.Value.uppercaseToString());
				if (@static != null)
				{
					androidJavaObject3.Call("setLogLevel", @static);
				}
			}
			double? delayStart = adjustConfig.delayStart;
			if (delayStart.HasValue)
			{
				androidJavaObject3.Call("setDelayStart", adjustConfig.delayStart);
			}
			bool? eventBufferingEnabled = adjustConfig.eventBufferingEnabled;
			if (eventBufferingEnabled.HasValue)
			{
				AndroidJavaObject androidJavaObject4 = new AndroidJavaObject("java.lang.Boolean", adjustConfig.eventBufferingEnabled.Value);
				androidJavaObject3.Call("setEventBufferingEnabled", androidJavaObject4);
			}
			bool? sendInBackground = adjustConfig.sendInBackground;
			if (sendInBackground.HasValue)
			{
				androidJavaObject3.Call("setSendInBackground", adjustConfig.sendInBackground.Value);
			}
			if (adjustConfig.userAgent != null)
			{
				androidJavaObject3.Call("setUserAgent", adjustConfig.userAgent);
			}
			if (!string.IsNullOrEmpty(adjustConfig.processName))
			{
				androidJavaObject3.Call("setProcessName", adjustConfig.processName);
			}
			if (adjustConfig.defaultTracker != null)
			{
				androidJavaObject3.Call("setDefaultTracker", adjustConfig.defaultTracker);
			}
			if (adjustConfig.attributionChangedDelegate != null)
			{
				onAttributionChangedListener = new AttributionChangeListener(adjustConfig.attributionChangedDelegate);
				androidJavaObject3.Call("setOnAttributionChangedListener", onAttributionChangedListener);
			}
			if (adjustConfig.eventSuccessDelegate != null)
			{
				onEventTrackingSucceededListener = new EventTrackingSucceededListener(adjustConfig.eventSuccessDelegate);
				androidJavaObject3.Call("setOnEventTrackingSucceededListener", onEventTrackingSucceededListener);
			}
			if (adjustConfig.eventFailureDelegate != null)
			{
				onEventTrackingFailedListener = new EventTrackingFailedListener(adjustConfig.eventFailureDelegate);
				androidJavaObject3.Call("setOnEventTrackingFailedListener", onEventTrackingFailedListener);
			}
			if (adjustConfig.sessionSuccessDelegate != null)
			{
				onSessionTrackingSucceededListener = new SessionTrackingSucceededListener(adjustConfig.sessionSuccessDelegate);
				androidJavaObject3.Call("setOnSessionTrackingSucceededListener", onSessionTrackingSucceededListener);
			}
			if (adjustConfig.sessionFailureDelegate != null)
			{
				onSessionTrackingFailedListener = new SessionTrackingFailedListener(adjustConfig.sessionFailureDelegate);
				androidJavaObject3.Call("setOnSessionTrackingFailedListener", onSessionTrackingFailedListener);
			}
			if (adjustConfig.deferredDeeplinkDelegate != null)
			{
				onDeferredDeeplinkListener = new DeferredDeeplinkListener(adjustConfig.deferredDeeplinkDelegate);
				androidJavaObject3.Call("setOnDeeplinkResponseListener", onDeferredDeeplinkListener);
			}
			androidJavaObject3.Call("setSdkPrefix", "unity4.10.2");
			ajcAdjust.CallStatic("onCreate", androidJavaObject3);
			ajcAdjust.CallStatic("onResume");
		}

		public void trackEvent(AdjustEvent adjustEvent)
		{
			AndroidJavaObject androidJavaObject = new AndroidJavaObject("com.adjust.sdk.AdjustEvent", adjustEvent.eventToken);
			double? revenue = adjustEvent.revenue;
			if (revenue.HasValue && adjustEvent.currency != null)
			{
				object[] array = new object[2];
				double? revenue2 = adjustEvent.revenue;
				array[0] = revenue2.Value;
				array[1] = adjustEvent.currency;
				androidJavaObject.Call("setRevenue", array);
			}
			if (adjustEvent.callbackList != null)
			{
				for (int i = 0; i < adjustEvent.callbackList.Count; i += 2)
				{
					string text = adjustEvent.callbackList[i];
					string text2 = adjustEvent.callbackList[i + 1];
					androidJavaObject.Call("addCallbackParameter", text, text2);
				}
			}
			if (adjustEvent.partnerList != null)
			{
				for (int j = 0; j < adjustEvent.partnerList.Count; j += 2)
				{
					string text3 = adjustEvent.partnerList[j];
					string text4 = adjustEvent.partnerList[j + 1];
					androidJavaObject.Call("addPartnerParameter", text3, text4);
				}
			}
			ajcAdjust.CallStatic("trackEvent", androidJavaObject);
		}

		public bool isEnabled()
		{
			return ajcAdjust.CallStatic<bool>("isEnabled", new object[0]);
		}

		public void setEnabled(bool enabled)
		{
			ajcAdjust.CallStatic("setEnabled", enabled);
		}

		public void setOfflineMode(bool enabled)
		{
			ajcAdjust.CallStatic("setOfflineMode", enabled);
		}

		public void sendFirstPackages()
		{
			ajcAdjust.CallStatic("sendFirstPackages");
		}

		public void setDeviceToken(string deviceToken)
		{
			ajcAdjust.CallStatic("setPushToken", deviceToken);
		}

		public static void addSessionPartnerParameter(string key, string value)
		{
			if (ajcAdjust == null)
			{
				ajcAdjust = new AndroidJavaClass("com.adjust.sdk.Adjust");
			}
			ajcAdjust.CallStatic("addSessionPartnerParameter", key, value);
		}

		public static void addSessionCallbackParameter(string key, string value)
		{
			if (ajcAdjust == null)
			{
				ajcAdjust = new AndroidJavaClass("com.adjust.sdk.Adjust");
			}
			ajcAdjust.CallStatic("addSessionCallbackParameter", key, value);
		}

		public static void removeSessionPartnerParameter(string key)
		{
			if (ajcAdjust == null)
			{
				ajcAdjust = new AndroidJavaClass("com.adjust.sdk.Adjust");
			}
			ajcAdjust.CallStatic("removeSessionPartnerParameter", key);
		}

		public static void removeSessionCallbackParameter(string key)
		{
			if (ajcAdjust == null)
			{
				ajcAdjust = new AndroidJavaClass("com.adjust.sdk.Adjust");
			}
			ajcAdjust.CallStatic("removeSessionCallbackParameter", key);
		}

		public static void resetSessionPartnerParameters()
		{
			if (ajcAdjust == null)
			{
				ajcAdjust = new AndroidJavaClass("com.adjust.sdk.Adjust");
			}
			ajcAdjust.CallStatic("resetSessionPartnerParameters");
		}

		public static void resetSessionCallbackParameters()
		{
			if (ajcAdjust == null)
			{
				ajcAdjust = new AndroidJavaClass("com.adjust.sdk.Adjust");
			}
			ajcAdjust.CallStatic("resetSessionCallbackParameters");
		}

		public void onPause()
		{
			ajcAdjust.CallStatic("onPause");
		}

		public void onResume()
		{
			ajcAdjust.CallStatic("onResume");
		}

		public void setReferrer(string referrer)
		{
			ajcAdjust.CallStatic("setReferrer", referrer);
		}

		public void getGoogleAdId(Action<string> onDeviceIdsRead)
		{
			DeviceIdsReadListener deviceIdsReadListener = new DeviceIdsReadListener(onDeviceIdsRead);
			ajcAdjust.CallStatic("getGoogleAdId", ajoCurrentActivity, deviceIdsReadListener);
		}

		public string getIdfa()
		{
			return null;
		}
	}
}
