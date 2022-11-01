using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ABH.GameDatas;
using ABH.Shared.BalancingData;
using ABH.Shared.DTOs;
using ABH.Shared.Events.BalancingData;
using ABH.Shared.Generic;
using ABH.Shared.Models;
using ABH.Shared.Models.Generic;
using Chimera.Library.Components.Interfaces;
using Rcs;
using UnityEngine;

namespace ABH.Services.Logic
{
	public class PvPSeasonService
	{
		private Action<string> DebugLog;

		private Action<string> ErrorLog;

		public TimingInjectableServiceImpl m_TimingServiceInjectable;

		private ITimingService m_timingService;

		public PvPSeasonService SetTimingService(ITimingService timingService)
		{
			m_timingService = timingService;
			return this;
		}

		public PvPSeasonService SetDebugLog(Action<string> debugLog)
		{
			DebugLog = debugLog;
			return this;
		}

		public PvPSeasonService SetErrorLog(Action<string> errorLog)
		{
			ErrorLog = errorLog;
			return this;
		}

		private void LogDebug(string message)
		{
			if (DebugLog != null)
			{
				DebugLog(message);
			}
		}

		private void LogError(string message)
		{
			if (ErrorLog != null)
			{
				ErrorLog(message);
			}
		}

		public bool IsCurrentPvPTurnAvailable(PlayerGameData currentPlayer)
		{
			return currentPlayer.CurrentPvPSeasonGameData != null && currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn != null && currentPlayer.CurrentPvPSeasonGameData.IsValid && currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.IsValid;
		}

		public bool IsPvPTurnOverNow(PvPSeasonManagerGameData currentPvPManagerGameData)
		{
			return m_timingService.IsAfter(m_timingService.GetDateTimeFromTimestamp(GetPvPTurnEndTimeStamp(currentPvPManagerGameData))) && !IsWaitingForConfirmation(currentPvPManagerGameData);
		}

		public bool IsWaitingForConfirmation(PvPSeasonManagerGameData currentPvPManagerGameData)
		{
			if (currentPvPManagerGameData == null || currentPvPManagerGameData.CurrentSeasonTurn == null)
			{
				return false;
			}
			return currentPvPManagerGameData.CurrentSeasonTurn.CurrentPvPTurnManagerState >= EventManagerState.Finished;
		}

		public void TriggerSeasonEnd()
		{
			PlayerGameData currentPlayer = DIContainerInfrastructure.GetCurrentPlayer();
			string text = DIContainerInfrastructure.GetLocaService().Tr(currentPlayer.CurrentPvPSeasonGameData.Balancing.LocaBaseId + "_name");
			if (!(currentPlayer.Data.m_CachedSeasonName == text))
			{
				currentPlayer.Data.m_CachedSeasonName = text;
				currentPlayer.Data.HasPendingSeasonendPopup = true;
			}
		}

		public EventManagerState FinishCurrentPvPTurn(PvPSeasonManagerGameData currentPvPManagerGameData)
		{
			PlayerGameData currentPlayer = DIContainerInfrastructure.GetCurrentPlayer();
			if (currentPvPManagerGameData == null || currentPvPManagerGameData.CurrentSeasonTurn == null)
			{
				return EventManagerState.FinishedWithoutPoints;
			}
			currentPvPManagerGameData.CurrentSeasonTurn.CurrentPvPTurnManagerState = ((currentPvPManagerGameData.Data.CurrentSeasonTurn.CurrentScore == 0) ? EventManagerState.FinishedWithoutPoints : EventManagerState.Finished);
			EventManagerState currentPvPTurnManagerState = currentPvPManagerGameData.CurrentSeasonTurn.CurrentPvPTurnManagerState;
			PvPSeasonManagerBalancingData currentSeason = GetCurrentSeason();
			if (currentSeason != null && currentSeason.NameId != currentPvPManagerGameData.Balancing.NameId)
			{
				TriggerSeasonEnd();
			}
			else if (currentPvPManagerGameData.Data.CurrentSeason == currentPvPManagerGameData.Balancing.SeasonTurnAmount)
			{
				TriggerSeasonEnd();
			}
			LogDebug(string.Concat("Finish Current PvP turn: ", currentPvPManagerGameData.Balancing.NameId, " with state: ", currentPvPTurnManagerState, " and Score: ", currentPvPManagerGameData.CurrentSeasonTurn.Data.CurrentScore));
			CurrentGlobalEventState newState = ((currentPvPTurnManagerState == EventManagerState.Finished) ? CurrentGlobalEventState.FinishedEvent : CurrentGlobalEventState.NoEvent);
			currentPlayer.RegisterGlobalPvPStateChanged(CurrentGlobalEventState.RunningEvent, newState);
			currentPlayer.SavePlayerData();
			GetLeaderboardScores(currentPlayer, currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.Data.CurrentOpponents, currentPlayer.CurrentPvPSeasonGameData);
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			dictionary.Add("PvPTurnNameId", currentPvPManagerGameData.CurrentSeasonTurn.Data.NameId);
			dictionary.Add("CurrentScore", currentPvPManagerGameData.CurrentSeasonTurn.Data.CurrentScore.ToString("0"));
			dictionary.Add("CurrentState", currentPvPManagerGameData.CurrentSeasonTurn.CurrentPvPTurnManagerState.ToString());
			dictionary.Add("MatchmakingScore", currentPvPManagerGameData.CurrentSeasonTurn.Data.MatchmakingScore.ToString("0"));
			DIContainerInfrastructure.GetAnalyticsSystem(false).LogEventWithParameters("PvPTurnStateChanged", dictionary);
			int num = ((currentPvPTurnManagerState != EventManagerState.FinishedWithoutPoints) ? GetRankChangesOnPvPTurnFinish(currentPvPManagerGameData, false) : GetRankChangesOnPvPTurnFinish(currentPvPManagerGameData, true));
			int num2 = currentPvPManagerGameData.Data.CurrentLeague + num;
			if (currentPlayer.Data.HighestFinishedLeague < num2)
			{
				currentPlayer.Data.HighestFinishedLeague = num2;
			}
			if (currentPvPManagerGameData.CurrentSeasonTurn.CurrentPvPTurnManagerState == EventManagerState.FinishedWithoutPoints)
			{
				currentPlayer.RemovePvPTurnManager();
				currentPvPManagerGameData.Data.HasPendingDemotionPopup = num < 0;
			}
			return currentPvPTurnManagerState;
		}

		public PvPSeasonManagerBalancingData GetCurrentSeason()
		{
			foreach (PvPSeasonManagerBalancingData balancingData in DIContainerBalancing.EventBalancingService.GetBalancingDataList<PvPSeasonManagerBalancingData>())
			{
				if (IsSeasonRunning(balancingData))
				{
					return balancingData;
				}
			}
			return null;
		}

		public void SetDemotionPopupShown(PlayerGameData player)
		{
			player.CurrentPvPSeasonGameData.Data.HasPendingDemotionPopup = false;
			player.SavePlayerData();
		}

		public void SetPvPResultValid(PlayerGameData currentPlayer, PvPSeasonManagerGameData pvpSeasonManagerGameData)
		{
			if (pvpSeasonManagerGameData.CurrentSeasonTurn == null)
			{
				return;
			}
			pvpSeasonManagerGameData.CurrentSeasonTurn.ResultRank = pvpSeasonManagerGameData.CurrentSeasonTurn.GetCurrentRank;
			int wheelIndex = 0;
			if (pvpSeasonManagerGameData.CurrentSeasonTurn.ResultStars > 0)
			{
				if (pvpSeasonManagerGameData.CurrentSeasonTurn.Data.CachedRolledResultWheelIndex > -1)
				{
					wheelIndex = pvpSeasonManagerGameData.CurrentSeasonTurn.Data.CachedRolledResultWheelIndex;
					pvpSeasonManagerGameData.CurrentSeasonTurn.RolledResultLoot = DIContainerLogic.GetLootOperationService().GenerateLootForcedWheelIndex(new Dictionary<string, int> { 
					{
						pvpSeasonManagerGameData.GetSeasonTurnLootTableWheel(),
						1
					} }, currentPlayer.Data.Level, pvpSeasonManagerGameData.CurrentSeasonTurn.ResultStars, ref wheelIndex);
				}
				else
				{
					pvpSeasonManagerGameData.CurrentSeasonTurn.RolledResultLoot = DIContainerLogic.GetLootOperationService().GenerateLoot(new Dictionary<string, int> { 
					{
						pvpSeasonManagerGameData.GetSeasonTurnLootTableWheel(),
						1
					} }, currentPlayer.Data.Level, pvpSeasonManagerGameData.CurrentSeasonTurn.ResultStars, ref wheelIndex);
				}
			}
			pvpSeasonManagerGameData.CurrentSeasonTurn.Data.CachedRolledResultWheelIndex = wheelIndex;
			if (pvpSeasonManagerGameData.Balancing.PvPBonusLootTablesPerRank.Count >= pvpSeasonManagerGameData.CurrentSeasonTurn.ResultRank)
			{
				pvpSeasonManagerGameData.CurrentSeasonTurn.FinalRankBonusLoot = DIContainerLogic.GetLootOperationService().GenerateLoot(new Dictionary<string, int> { 
				{
					pvpSeasonManagerGameData.CurrentSeasonTurn.GetScalingRankRewardLootTable(),
					1
				} }, pvpSeasonManagerGameData.Data.CurrentSeason);
			}
			pvpSeasonManagerGameData.CurrentSeasonTurn.IsResultValid = true;
			currentPlayer.SavePlayerData();
			DIContainerInfrastructure.GetCurrentPlayer().RegisterGlobalPvPStateChanged(CurrentGlobalEventState.FinishedEvent, CurrentGlobalEventState.FinishedEventAndResultValid);
		}

		public bool ConfirmCurrentPvPTurn(PlayerGameData currentPlayer)
		{
			if (!IsCurrentPvPTurnAvailable(currentPlayer) || !IsWaitingForConfirmation(currentPlayer.CurrentPvPSeasonGameData))
			{
				LogError("There is no finished pvp turn to confirm");
				return false;
			}
			LogDebug("Confirmed Current pvp turn: " + currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.Data.NameId);
			if (currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.ResultStars > 0)
			{
				DIContainerLogic.GetLootOperationService().RewardLoot(currentPlayer.InventoryGameData, 1, currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.RolledResultLoot, new Dictionary<string, string>
				{
					{
						"PvPTurnName",
						currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.Data.NameId
					},
					{ "LootType", "Wheel" }
				});
			}
			if (currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.FinalRankBonusLoot.Count > 0)
			{
				DIContainerLogic.GetLootOperationService().RewardLoot(currentPlayer.InventoryGameData, 1, currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.FinalRankBonusLoot, new Dictionary<string, string>
				{
					{
						"PvPTurnName",
						currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.Data.NameId
					},
					{ "LootType", "RankBonus" }
				});
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			dictionary.Add("PvPTurnNameId", currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.Data.NameId);
			dictionary.Add("CurrentScore", currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.Data.CurrentScore.ToString("0"));
			dictionary.Add("CurrentState", EventManagerState.FinishedAndConfirmed.ToString());
			dictionary.Add("MatchmakingScore", currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.Data.MatchmakingScore.ToString("0"));
			if (currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.Data.CurrentOpponents != null)
			{
				for (int i = 0; i < currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.Data.CurrentOpponents.Count; i++)
				{
					string value = currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.Data.CurrentOpponents[i];
					if (!string.IsNullOrEmpty(value))
					{
						dictionary.Add("Opponent" + i.ToString("00"), value);
					}
				}
			}
			dictionary.Add("Rank", currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.ResultRank.ToString("0"));
			DIContainerInfrastructure.GetAnalyticsSystem(false).LogEventWithParameters("PvPTurnStateChanged", dictionary);
			GetRankChangesOnPvPTurnFinish(currentPlayer.CurrentPvPSeasonGameData, true);
			ClearPvPTurn(currentPlayer);
			return true;
		}

		public int GetCurrentSeasonTurn(PvPSeasonManagerBalancingData pvPSeasonManagerBalancingDataData)
		{
			uint num = Math.Max(0u, m_timingService.GetCurrentTimestamp() - pvPSeasonManagerBalancingDataData.SeasonStartTimeStamp);
			uint num2 = (uint)Mathf.RoundToInt((long)(pvPSeasonManagerBalancingDataData.SeasonEndTimeStamp - pvPSeasonManagerBalancingDataData.SeasonStartTimeStamp) / (long)pvPSeasonManagerBalancingDataData.SeasonTurnAmount);
			return Mathf.FloorToInt((float)num / (float)num2) + 1;
		}

		public int GetRankChangesOnPvPTurnFinish(PvPSeasonManagerGameData pvpManager, bool applyChanges)
		{
			if (pvpManager.CurrentSeasonTurn.CurrentPvPTurnManagerState < EventManagerState.Finished)
			{
				return 0;
			}
			int rankChangeByRanking = GetRankChangeByRanking(pvpManager);
			int rankChangeByMissedSeasons = GetRankChangeByMissedSeasons(pvpManager);
			if (!applyChanges)
			{
				return rankChangeByRanking + rankChangeByMissedSeasons;
			}
			pvpManager.LastLeagueChangeByRanking = rankChangeByRanking;
			pvpManager.LastLeagueChangeByTime = rankChangeByMissedSeasons;
			pvpManager.Data.CurrentLeague = Mathf.Max(1, pvpManager.Data.CurrentLeague + rankChangeByRanking + rankChangeByMissedSeasons);
			IInventoryItemGameData data = null;
			if (DIContainerLogic.InventoryService.TryGetItemGameData(DIContainerInfrastructure.GetCurrentPlayer().InventoryGameData, "pvp_league_crown", out data))
			{
				data.ItemData.Level = pvpManager.Data.CurrentLeague;
			}
			else
			{
				DIContainerLogic.InventoryService.AddItem(DIContainerInfrastructure.GetCurrentPlayer().InventoryGameData, pvpManager.Data.CurrentLeague, 1, "pvp_league_crown", 1, "SeasonStart");
			}
			if (DIContainerLogic.InventoryService.TryGetItemGameData(DIContainerInfrastructure.GetCurrentPlayer().InventoryGameData, "pvp_league_crown_max", out data))
			{
				data.ItemData.Level = Mathf.Max(data.ItemData.Level, pvpManager.Data.CurrentLeague);
			}
			else
			{
				DIContainerLogic.InventoryService.AddItem(DIContainerInfrastructure.GetCurrentPlayer().InventoryGameData, pvpManager.Data.CurrentLeague, 1, "pvp_league_crown_max", 1, "SeasonStart");
			}
			return rankChangeByRanking + rankChangeByMissedSeasons;
		}

		public int GetRankChangeByRanking(PvPSeasonManagerGameData pvpManager)
		{
			int num = 0;
			pvpManager.CurrentSeasonTurn.ResultRank = pvpManager.CurrentSeasonTurn.GetCurrentRank;
			if (pvpManager.CurrentSeasonTurn.ResultStars <= 0 || pvpManager.CurrentSeasonTurn.CurrentPvPTurnManagerState == EventManagerState.FinishedWithoutPoints)
			{
				num--;
			}
			else if (pvpManager.CurrentSeasonTurn.ResultStars >= 3)
			{
				num++;
			}
			if (pvpManager.Data.CurrentLeague + num > pvpManager.Balancing.MaxLeague || pvpManager.Data.CurrentLeague + num < 1)
			{
				return 0;
			}
			return num;
		}

		public int GetRankChangeByMissedSeasons(PvPSeasonManagerGameData pvpManager)
		{
			int num = 0;
			if (GetCurrentSeasonTurn(pvpManager.Balancing) > pvpManager.Data.CurrentSeason + 1)
			{
				num -= GetCurrentSeasonTurn(pvpManager.Balancing) - pvpManager.Data.CurrentSeason + 1;
			}
			if (pvpManager.Data.CurrentLeague + num > pvpManager.Balancing.MaxLeague || pvpManager.Data.CurrentLeague + num < 1)
			{
				return 0;
			}
			return num;
		}

		public void ClearPvPTurn(PlayerGameData currentPlayer)
		{
			currentPlayer.RemovePvPTurnManager();
			currentPlayer.RegisterGlobalPvPStateChanged(CurrentGlobalEventState.FinishedEvent, CurrentGlobalEventState.NoEvent);
			DIContainerInfrastructure.GetCurrentPlayer().SavePlayerData();
		}

		public bool IsPvPTurnRunning(PvPSeasonManagerGameData currentPvPManagerGameData)
		{
			return currentPvPManagerGameData != null && m_timingService.IsAfter(m_timingService.GetDateTimeFromTimestamp(GetPvPTurnStartTime(currentPvPManagerGameData))) && m_timingService.IsBefore(m_timingService.GetDateTimeFromTimestamp(GetPvPTurnEndTimeStamp(currentPvPManagerGameData)));
		}

		public DateTime GetPvpTurnEndTime(PvPSeasonManagerGameData currentPvPManagerGameData)
		{
			return m_timingService.GetDateTimeFromTimestamp(GetPvPTurnEndTimeStamp(currentPvPManagerGameData));
		}

		private static uint GetPvPTurnEndTimeStamp(PvPSeasonManagerGameData currentPvPManagerGameData)
		{
			return currentPvPManagerGameData.Balancing.SeasonStartTimeStamp + (uint)(int)((long)(currentPvPManagerGameData.Balancing.SeasonEndTimeStamp - currentPvPManagerGameData.Balancing.SeasonStartTimeStamp) / (long)currentPvPManagerGameData.Balancing.SeasonTurnAmount * currentPvPManagerGameData.Data.CurrentSeason);
		}

		private uint GetPvPTurnStartTime(PvPSeasonManagerGameData currentPvPManagerGameData)
		{
			return currentPvPManagerGameData.Balancing.SeasonStartTimeStamp + (uint)(int)((long)(currentPvPManagerGameData.Balancing.SeasonEndTimeStamp - currentPvPManagerGameData.Balancing.SeasonStartTimeStamp) / (long)currentPvPManagerGameData.Balancing.SeasonTurnAmount * (currentPvPManagerGameData.Data.CurrentSeason - 1));
		}

		public void StartNewPvPTurn(PvPSeasonManagerGameData pvpManager, PlayerGameData playerGameData)
		{
			LogDebug(string.Concat("Start New PvP: ", pvpManager.Balancing.NameId, " with start Time: ", m_timingService.GetDateTimeFromTimestamp(GetPvPTurnStartTime(pvpManager)), " at time: ", m_timingService.GetPresentTime()));
			pvpManager.SetCurrentSeasonTurn();
			pvpManager.CurrentSeasonTurn.CurrentPvPTurnManagerState = EventManagerState.Running;
			pvpManager.CurrentSeasonTurn.Data.StartingPlayerLevel = DIContainerInfrastructure.GetCurrentPlayer().Data.Level;
			PlayerGameData currentPlayer = DIContainerInfrastructure.GetCurrentPlayer();
			currentPlayer.RegisterGlobalPvPStateChanged(CurrentGlobalEventState.NoEvent, CurrentGlobalEventState.RunningEvent);
			pvpManager.CurrentSeasonTurn.Data.CurrentScore = 0u;
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			dictionary.Add("PvPTurnNameId", pvpManager.CurrentSeasonTurn.Data.NameId);
			dictionary.Add("CurrentScore", playerGameData.CurrentPvPSeasonGameData.CurrentSeasonTurn.Data.CurrentScore.ToString("0"));
			dictionary.Add("CurrentState", playerGameData.CurrentPvPSeasonGameData.CurrentSeasonTurn.CurrentPvPTurnManagerState.ToString());
			dictionary.Add("MatchmakingScore", playerGameData.CurrentPvPSeasonGameData.CurrentSeasonTurn.Data.MatchmakingScore.ToString("0"));
			DIContainerInfrastructure.GetAnalyticsSystem(false).LogEventWithParameters("PvPTurnStateChanged", dictionary);
			currentPlayer.SavePlayerData();
		}

		public bool StartPvPTurn(PvPSeasonManagerGameData currentPvPManagerGameData)
		{
			if (currentPvPManagerGameData != null && currentPvPManagerGameData.CurrentSeasonTurn != null && currentPvPManagerGameData.CurrentSeasonTurn.CurrentPvPTurnManagerState != EventManagerState.Running)
			{
				LogDebug(string.Concat("Start PvP turn: ", currentPvPManagerGameData.Balancing.NameId, " with start Time: ", m_timingService.GetDateTimeFromTimestamp(GetPvPTurnStartTime(currentPvPManagerGameData)), " at time: ", m_timingService.GetPresentTime()));
				currentPvPManagerGameData.CurrentSeasonTurn.CurrentPvPTurnManagerState = EventManagerState.Running;
				currentPvPManagerGameData.CurrentSeasonTurn.Data.StartingPlayerLevel = DIContainerInfrastructure.GetCurrentPlayer().Data.Level;
				DIContainerInfrastructure.GetCurrentPlayer().RegisterGlobalPvPStateChanged(CurrentGlobalEventState.NoEvent, CurrentGlobalEventState.RunningEvent);
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				dictionary.Add("PvPTurnNameId", currentPvPManagerGameData.Balancing.NameId);
				dictionary.Add("CurrentScore", currentPvPManagerGameData.CurrentSeasonTurn.Data.CurrentScore.ToString("0"));
				dictionary.Add("CurrentState", currentPvPManagerGameData.CurrentSeasonTurn.CurrentPvPTurnManagerState.ToString());
				dictionary.Add("MatchmakingScore", currentPvPManagerGameData.CurrentSeasonTurn.Data.MatchmakingScore.ToString("0"));
				DIContainerInfrastructure.GetAnalyticsSystem(false).LogEventWithParameters("PvPTurnStateChanged", dictionary);
				DIContainerInfrastructure.GetCurrentPlayer().SavePlayerData();
				return true;
			}
			return false;
		}

		public void SubmitPvPTurnScore(PlayerGameData player, PvPSeasonManagerGameData pvpManager, Action<RESTResultEnum> UiCallback = null)
		{
			PvPTurnManagerGameData currentSeasonTurn = pvpManager.CurrentSeasonTurn;
			if (currentSeasonTurn != null && IsSubmitAllowed(player))
			{
				Leaderboard.Score score = new Leaderboard.Score(currentSeasonTurn.Data.NameId);
				score.SetPoints(currentSeasonTurn.Data.CurrentScore);
				LogDebug("Final Score: " + currentSeasonTurn.Data.CurrentScore);
				DIContainerLogic.BackendService.AddPvpScore(pvpManager.Balancing.NameId, currentSeasonTurn.Data.CurrentSeason, (PvpLeague)pvpManager.Data.CurrentLeague, currentSeasonTurn.Data.CurrentScore, player.Data.Level, OnPvpScoreSubmittedSuccess, OnErrorsubmittingPvPTurnScore);
			}
		}

		private void OnErrorsubmittingPvPTurnScore(int errorCode)
		{
			LogError("Error submitting pvp turn score: " + errorCode);
		}

		public bool IsSubmitAllowed(PlayerGameData player)
		{
			return IsCurrentPvPTurnAvailable(player) && player.CurrentPvPSeasonGameData.CurrentSeasonTurn.Data.CurrentState == EventManagerState.Running && player.CurrentPvPSeasonGameData.CurrentSeasonTurn.Data.CurrentScore != 0;
		}

		public void SubmitOfflineMatchmakingAttributes(PlayerGameData player, string pvpTurnManagerId)
		{
			Dictionary<string, object> offlineMatchmakingAttributes = GetOfflineMatchmakingAttributes(player, pvpTurnManagerId);
			DIContainerInfrastructure.MatchmakingService.SetOfflineAttributes(offlineMatchmakingAttributes, delegate(OfflineMatchmaker.ResultCode result)
			{
				OnSubmittedOfflineAttributes(player, result);
			});
		}

		private Dictionary<string, object> GetOfflineMatchmakingAttributes(PlayerGameData player, string pvpTurnManagerId)
		{
			int num = 0;
			if (IsCurrentPvPTurnAvailable(player))
			{
				num = player.CurrentPvPSeasonGameData.Data.CurrentLeague;
			}
			LogDebug("League Param: " + num);
			int level = player.Data.Level;
			LogDebug("Level Param: " + level);
			int num2 = (int)(TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalHours + 13.0);
			LogDebug("Timezone Indicator: " + num2);
			int num3 = (int)((DIContainerLogic.GetDeviceTimingService().GetPresentTime() - new DateTime(2014, 1, 1)).TotalDays + (double)player.Data.ActivityIndicator);
			LogDebug("Activity Indicator: " + num3);
			return AddOfflineMatchmakingAttributes(pvpTurnManagerId, num, level, num2, num3);
		}

		private Dictionary<string, object> AddOfflineMatchmakingAttributes(string pvpTurnManagerId, int leagueParameter, int levelParameter, int timeZoneIndicator, int activity)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary.Add("pvpturn", pvpTurnManagerId);
			dictionary.Add("league", leagueParameter);
			dictionary.Add("timezone", timeZoneIndicator);
			dictionary.Add("activity", activity);
			dictionary.Add("variance", UnityEngine.Random.Range(0, 1000));
			dictionary.Add("level", levelParameter);
			dictionary.Add("powerlevel", DIContainerInfrastructure.GetPowerLevelCalculator().GetPvPTeamPowerLevel(DIContainerInfrastructure.GetCurrentPlayer().PublicPlayer, null));
			return dictionary;
		}

		private void OnSubmittedOfflineAttributes(PlayerGameData player, OfflineMatchmaker.ResultCode result)
		{
			LogDebug(string.Concat("OnSubmittedOfflineAttributes callback received: ", result, "; now searching for match!"));
		}

		private void OnPvpScoreSubmittedSuccess(AddPvpScoreResponseDto scoreResponse)
		{
			UpdatePvpLeaderboard(scoreResponse.LeaderboardId, scoreResponse.Leaderboard);
		}

		public float GetAverageEquipmentLevel(PlayerGameData player)
		{
			List<float> list = new List<float>();
			foreach (BirdGameData bird in player.Birds)
			{
				list.Add(bird.MainHandItem.Data.Level);
				list.Add(bird.OffHandItem.Data.Level);
			}
			float num = list.Average();
			list.Clear();
			list.Add(player.BannerGameData.BannerTip.Data.Level);
			list.Add(player.BannerGameData.BannerCenter.Data.Level);
			list.Add(player.BannerGameData.BannerEmblem.Data.Level);
			float num2 = list.Average();
			return (num + num2) / 2f;
		}

		public void PullLeaderboardUpdate(PvPSeasonManagerGameData pvpModel, Action<RESTResultEnum> uiCallback = null)
		{
			if (pvpModel == null || pvpModel.CurrentSeasonTurn == null || (string.IsNullOrEmpty(pvpModel.CurrentSeasonTurn.Data.LeaderboardId) && !pvpModel.CurrentSeasonTurn.IsLegacyLeaderboard))
			{
				global::DebugLog.Warn(GetType(), "PullLeaderboardUpdate: invalid eventManagerGameData LeaderboardId!");
				if (uiCallback != null)
				{
					uiCallback(RESTResultEnum.Fail);
				}
				return;
			}
			PlayerGameData player = DIContainerInfrastructure.GetCurrentPlayer();
			if (pvpModel.CurrentSeasonTurn.IsLegacyLeaderboard)
			{
				DebugLog("PullLeaderboardUpdate: We have a legacy leaderboard. Turning to Beacon for direct scores");
				GetLeaderboardScores(player, player.CurrentPvPSeasonGameData.CurrentSeasonTurn.Data.CurrentOpponents, player.CurrentPvPSeasonGameData, uiCallback);
				return;
			}
			DIContainerLogic.BackendService.GetPvpLeaderboard(pvpModel.CurrentSeasonTurn.Data.LeaderboardId, delegate(GetLeaderboardResponseDto response)
			{
				UpdatePvpLeaderboard(pvpModel.CurrentSeasonTurn.Data.LeaderboardId, response.Leaderboard, uiCallback);
			}, delegate(int errorCode)
			{
				DebugLog("GetPvpLeaderboard for id " + pvpModel.CurrentSeasonTurn.Data.LeaderboardId + " failed with errorCode = " + errorCode);
				if (player == null || player.CurrentPvPSeasonGameData == null || player.CurrentPvPSeasonGameData.CurrentSeasonTurn == null || player.CurrentPvPSeasonGameData.CurrentSeasonTurn.Data.CurrentOpponents == null)
				{
					if (uiCallback != null)
					{
						uiCallback((RESTResultEnum)errorCode);
					}
				}
				else
				{
					GetLeaderboardScores(player, player.CurrentPvPSeasonGameData.CurrentSeasonTurn.Data.CurrentOpponents, player.CurrentPvPSeasonGameData, uiCallback);
				}
			});
		}

		internal void UpdatePvpLeaderboard(string leaderboardId, Dictionary<string, bool> lbPlayerIds, Action<RESTResultEnum> uiCallback = null)
		{
			DebugLog("UpdatePvpLeaderboard: GOGOGO");
			PlayerGameData currentPlayer = DIContainerInfrastructure.GetCurrentPlayer();
			if (string.IsNullOrEmpty(leaderboardId) || lbPlayerIds == null || lbPlayerIds.Count <= 1 || currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.IsLegacyLeaderboard)
			{
				global::DebugLog.Error(GetType(), "[EPIC SERVER] UpdateEventLeaderboard: Got false parameters: id=" + leaderboardId + " and playerIds=" + lbPlayerIds);
				if (uiCallback != null)
				{
					uiCallback(RESTResultEnum.Success);
				}
				return;
			}
			PvPTurnManagerGameData currentSeasonTurn = currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn;
			if (currentSeasonTurn.Data.CurrentOpponents == null)
			{
				currentSeasonTurn.Data.CurrentOpponents = new List<string>();
			}
			if (currentSeasonTurn.Data.CheatingOpponents == null)
			{
				currentSeasonTurn.Data.CheatingOpponents = new List<string>();
			}
			global::DebugLog.Log(GetType(), "UpdatePvpLeaderboard: setting new leaderboardID: " + leaderboardId);
			currentSeasonTurn.Data.LeaderboardId = leaderboardId;
			currentSeasonTurn.Data.CurrentOpponents.Clear();
			currentSeasonTurn.Data.CheatingOpponents.Clear();
			currentSeasonTurn.ScoresByPlayer.Clear();
			foreach (KeyValuePair<string, bool> lbPlayerId in lbPlayerIds)
			{
				string key = lbPlayerId.Key;
				if (lbPlayerId.Value && !currentSeasonTurn.Data.CheatingOpponents.Contains(key))
				{
					currentSeasonTurn.Data.CheatingOpponents.Add(key);
				}
				if (key == DIContainerInfrastructure.IdentityService.SharedId || currentSeasonTurn.Data.CurrentOpponents.Contains(key))
				{
					DebugLog("UpdatePvpLeaderboard: ignore own sharedId in the idList");
					continue;
				}
				currentSeasonTurn.Data.CurrentOpponents.Add(key);
				if (!currentSeasonTurn.ScoresByPlayer.ContainsKey(key))
				{
					currentSeasonTurn.ScoresByPlayer.Add(key, new Leaderboard.Score(currentSeasonTurn.Data.NameId, key));
				}
			}
			global::DebugLog.Log(GetType(), "UpdatePvpLeaderboard: Getting leaderboard scores for LB " + leaderboardId);
			GetLeaderboardScores(currentPlayer, currentSeasonTurn.Data.CurrentOpponents, currentPlayer.CurrentPvPSeasonGameData, uiCallback);
		}

		public void GetLeaderboardScores(PlayerGameData player, List<string> ids, PvPSeasonManagerGameData pvpManagerGameData, Action<RESTResultEnum> uiCallback = null)
		{
			DebugLog("GetLeaderboardScores: getting scores for season pvp season");
			List<string> list = ids ?? new List<string>();
			if (player.CurrentPvPSeasonGameData == null || player.CurrentPvPSeasonGameData.CurrentSeasonTurn == null || list == null)
			{
				return;
			}
			string text = string.Empty;
			foreach (string item in list)
			{
				text = text + item + "; ";
			}
			DIContainerInfrastructure.RemoteStorageService.GetPublicPlayerDatas(list.ToArray(), delegate(Dictionary<string, PublicPlayerData> publicPlayers)
			{
				OnSuccessfulGotPublicPlayers(player, publicPlayers);
			}, OnErrorGetPublicPlayers);
			DIContainerInfrastructure.ScoringService.FetchScores(list.ToArray(), pvpManagerGameData.CurrentSeasonTurn.Data.NameId, delegate(Dictionary<string, int> scores)
			{
				OnSuccessfullFetchedScore(player, scores);
				if (uiCallback != null)
				{
					uiCallback(RESTResultEnum.Success);
				}
			}, delegate(int errorCode)
			{
				OnErrorFechingScores(errorCode);
				if (uiCallback != null)
				{
					uiCallback(RESTResultEnum.Fail);
				}
			});
		}

		private void OnSuccessfulGotPublicPlayers(PlayerGameData player, Dictionary<string, PublicPlayerData> playerDatas)
		{
			LogDebug("successful got profiles of other players");
			if (player.CurrentPvPSeasonGameData.CurrentSeasonTurn != null)
			{
				player.CurrentPvPSeasonGameData.CurrentSeasonTurn.UpdateOpponents(playerDatas);
			}
		}

		private void OnErrorGetPublicPlayers(string error)
		{
			LogError("Error getting profiles of other players: " + error);
		}

		private void OnSuccessfullFetchedScore(PlayerGameData player, Dictionary<string, int> scores)
		{
			LogDebug("successful got scores of other players");
			if (player.CurrentPvPSeasonGameData == null || player.CurrentPvPSeasonGameData.CurrentSeasonTurn == null)
			{
				return;
			}
			LeaderboardResults leaderboardResults = new LeaderboardResults(scores.Count);
			foreach (KeyValuePair<string, int> score2 in scores)
			{
				Leaderboard.Score score = new Leaderboard.Score(string.Empty, score2.Key);
				score.SetPoints(score2.Value);
				Leaderboard.Result x = new Leaderboard.Result(0L, score);
				leaderboardResults.Add(x);
			}
			player.CurrentPvPSeasonGameData.CurrentSeasonTurn.UpdateOpponentScores(Enumerable.ToList(leaderboardResults));
			player.RegisterGlobalPvPScoresUpdated();
			if (IsWaitingForConfirmation(player.CurrentPvPSeasonGameData) && !player.CurrentPvPSeasonGameData.CurrentSeasonTurn.IsResultValid)
			{
				LogDebug("PvP turn is over and valid to show result!");
				SetPvPResultValid(player, player.CurrentPvPSeasonGameData);
				player.RegisterShowPvPResult(player.CurrentPvPSeasonGameData);
			}
		}

		private void OnErrorFechingScores(int errorCode)
		{
			LogError("Error getting scores of other players: " + errorCode);
			if ((errorCode == 1 || errorCode == 0) && IsWaitingForConfirmation(DIContainerInfrastructure.GetCurrentPlayer().CurrentPvPSeasonGameData) && !DIContainerInfrastructure.GetCurrentPlayer().CurrentPvPSeasonGameData.CurrentSeasonTurn.IsResultValid)
			{
				LogDebug("Pvp turn is over and valid to show result!");
				SetPvPResultValid(DIContainerInfrastructure.GetCurrentPlayer(), DIContainerInfrastructure.GetCurrentPlayer().CurrentPvPSeasonGameData);
				DIContainerInfrastructure.GetCurrentPlayer().RegisterShowPvPResult(DIContainerInfrastructure.GetCurrentPlayer().CurrentPvPSeasonGameData);
			}
		}

		public bool IsResultRerollPossible(PvPSeasonManagerGameData pvPSeasonManagerGame, InventoryGameData inventoryGameData)
		{
			Requirement rerollRequirement = GetRerollRequirement(pvPSeasonManagerGame);
			return rerollRequirement == null || (float)DIContainerLogic.InventoryService.GetItemValue(inventoryGameData, rerollRequirement.NameId) >= rerollRequirement.Value;
		}

		public bool ExecuteResultRerollCost(PvPSeasonManagerGameData pvPSeasonManagerGame, InventoryGameData inventoryGameData)
		{
			Requirement rerollRequirement = GetRerollRequirement(pvPSeasonManagerGame);
			return rerollRequirement == null || DIContainerLogic.InventoryService.RemoveItem(inventoryGameData, rerollRequirement.NameId, (int)rerollRequirement.Value, "pvpresult_reroll");
		}

		public void RerollPvPSeasonResultLoot(PvPSeasonManagerGameData seasonManagerGameData, PlayerGameData playerGameData)
		{
			LootTableBalancingData balancing = null;
			string seasonTurnLootTableWheel = seasonManagerGameData.GetSeasonTurnLootTableWheel();
			if (string.IsNullOrEmpty(seasonTurnLootTableWheel))
			{
				LogError("PvPRewardLootTablesPerLeague is missing!");
			}
			else if (DIContainerBalancing.LootTableBalancingDataPovider.TryGetBalancingData(seasonTurnLootTableWheel, out balancing))
			{
				if (balancing.LootTableEntries.Count != 8)
				{
					LogError("Wheel LootTable for Battles does not contains 8 entrys instead it has " + balancing.LootTableEntries.Count);
					return;
				}
				int wheelIndex = seasonManagerGameData.CurrentSeasonTurn.Data.CachedRolledResultWheelIndex;
				seasonManagerGameData.CurrentSeasonTurn.RolledResultLoot = DIContainerLogic.GetLootOperationService().GenerateLoot(new Dictionary<string, int> { { balancing.NameId, 1 } }, playerGameData.Data.Level, seasonManagerGameData.CurrentSeasonTurn.ResultStars, ref wheelIndex);
				seasonManagerGameData.CurrentSeasonTurn.Data.CachedRolledResultWheelIndex = wheelIndex;
			}
			else
			{
				LogError("No Wheel LootTable set for battle ");
			}
		}

		public Requirement GetRerollRequirement(PvPSeasonManagerGameData pvpSeasonManager)
		{
			if (pvpSeasonManager.Balancing.RerollResultRequirement == null)
			{
				LogError("No Reroll Requirement set!");
				return null;
			}
			if (pvpSeasonManager.Balancing.RerollResultRequirement.Count < pvpSeasonManager.Data.CurrentSeason)
			{
				return pvpSeasonManager.Balancing.RerollResultRequirement.LastOrDefault();
			}
			return pvpSeasonManager.Balancing.RerollResultRequirement[pvpSeasonManager.Data.CurrentSeason - 1];
		}

		public bool IsSeasonRunning(PvPSeasonManagerBalancingData seasonManager)
		{
			return seasonManager != null && m_timingService.IsAfter(m_timingService.GetDateTimeFromTimestamp(seasonManager.SeasonStartTimeStamp)) && m_timingService.IsBefore(m_timingService.GetDateTimeFromTimestamp(seasonManager.SeasonEndTimeStamp));
		}

		public PvPSeasonManagerGameData StartNewSeason()
		{
			foreach (PvPSeasonManagerBalancingData balancingData in DIContainerBalancing.EventBalancingService.GetBalancingDataList<PvPSeasonManagerBalancingData>())
			{
				if (IsSeasonRunning(balancingData))
				{
					return StartNewSeason(balancingData, DIContainerInfrastructure.GetCurrentPlayer());
				}
			}
			return null;
		}

		public PvPSeasonManagerGameData StartNewSeason(PvPSeasonManagerBalancingData seasonManager, PlayerGameData playerGameData)
		{
			PvPSeasonManagerGameData pvPSeasonManagerGameData = new PvPSeasonManagerGameData().CreateNewInstance(seasonManager.NameId);
			playerGameData.Data.PvpSeasonManager = pvPSeasonManagerGameData.Data;
			playerGameData.GeneratePvPManagerFromProfile();
			playerGameData.CurrentPvPSeasonGameData.CurrentPvPSeasonState = (IsSeasonRunning(playerGameData.CurrentPvPSeasonGameData.Balancing) ? PvPSeasonState.Running : PvPSeasonState.Pending);
			IInventoryItemGameData data = null;
			if (DIContainerLogic.InventoryService.TryGetItemGameData(DIContainerInfrastructure.GetCurrentPlayer().InventoryGameData, "pvp_league_crown", out data))
			{
				data.ItemData.Level = playerGameData.CurrentPvPSeasonGameData.Data.CurrentLeague;
			}
			else
			{
				DIContainerLogic.InventoryService.AddItem(DIContainerInfrastructure.GetCurrentPlayer().InventoryGameData, playerGameData.CurrentPvPSeasonGameData.Data.CurrentLeague, 1, "pvp_league_crown", 1, "SeasonStart");
			}
			if (DIContainerLogic.InventoryService.TryGetItemGameData(DIContainerInfrastructure.GetCurrentPlayer().InventoryGameData, "pvp_league_crown_max", out data))
			{
				data.ItemData.Level = Mathf.Max(data.ItemData.Level, playerGameData.CurrentPvPSeasonGameData.Data.CurrentLeague);
			}
			else
			{
				DIContainerLogic.InventoryService.AddItem(DIContainerInfrastructure.GetCurrentPlayer().InventoryGameData, playerGameData.CurrentPvPSeasonGameData.Data.CurrentLeague, 1, "pvp_league_crown_max", 1, "SeasonStart");
			}
			return playerGameData.CurrentPvPSeasonGameData;
		}

		public bool MatchmakeForSinglePvPOpponent(PlayerGameData player, PvPSeasonManagerGameData pvpManagerGameData)
		{
			if (pvpManagerGameData == null || pvpManagerGameData.CurrentSeasonTurn == null)
			{
				return false;
			}
			LogDebug("Start getting a pvp opponent");
			if (IsCurrentPvPTurnAvailable(player))
			{
				pvpManagerGameData.CurrentSeasonTurn.LastOpponentUpdateTime = DIContainerLogic.GetDeviceTimingService().GetPresentTime();
				MatchmakeForSinglePvPOpponentOffline(player, pvpManagerGameData);
				return true;
			}
			return false;
		}

		private void MatchmakeForSinglePvPOpponentOffline(PlayerGameData player, PvPSeasonManagerGameData pvpManagerGameData)
		{
			LogDebug("Offline Matchmake a pvp opponent: With used function " + pvpManagerGameData.Balancing.OfflineGetBattleFunction);
			Dictionary<string, object> offlineMatchmakingAttributes = GetOfflineMatchmakingAttributes(player, pvpManagerGameData.CurrentSeasonTurn.Data.NameId);
			StringBuilder stringBuilder = new StringBuilder("MatchmakeForSinglePvPOpponentOffline: matchmaking parameters sent to server are " + offlineMatchmakingAttributes);
			foreach (KeyValuePair<string, object> item in offlineMatchmakingAttributes)
			{
				stringBuilder.AppendLine(item.Key + " : " + item.Value);
			}
			DIContainerInfrastructure.MatchmakingService.MatchOfflineUsers(pvpManagerGameData.Balancing.OfflineGetBattleFunction.Replace("{value_1}", pvpManagerGameData.CurrentSeasonTurn.Data.CurrentMatchingDifficulty.ToString("00")), offlineMatchmakingAttributes, delegate(OfflineMatchmaker.ResultCode result, List<string> ids)
			{
				OnSinglePvPOpponentMatchedOffline(player, result, ids);
			}, 1);
		}

		private void OnSinglePvPOpponentMatchedOffline(PlayerGameData player, OfflineMatchmaker.ResultCode result, List<string> ids)
		{
			switch (result)
			{
			case OfflineMatchmaker.ResultCode.Success:
				OnSuccessfullyMatchedSingleOpponentOffline(player, ids.ToArray());
				break;
			case OfflineMatchmaker.ResultCode.ErrorNetworkFailure:
			case OfflineMatchmaker.ResultCode.ErrorOtherReason:
				OnErrorMatchmakingSingleOpponentOffline(player, result.ToString());
				break;
			default:
				throw new ArgumentOutOfRangeException("result");
			}
		}

		private void OnErrorMatchmakingSingleOpponentOffline(PlayerGameData player, string error)
		{
			LogDebug("Offline Matchmake a pvp opponent Error: " + error + " selecting fallback");
			if (IsCurrentPvPTurnAvailable(player))
			{
				PublicPlayerData fallbackPvPOpponent = GetFallbackPvPOpponent(player);
				player.CurrentPvPSeasonGameData.CurrentSeasonTurn.CurrentPvPOpponent = fallbackPvPOpponent;
				player.CurrentPvPSeasonGameData.CurrentSeasonTurn.FallbackOpponentReason = "MATCHMAKING_ERROR";
				player.CurrentPvPSeasonGameData.CurrentSeasonTurn.CurrentBirdIndices = fallbackPvPOpponent.PvPIndices;
				TrackPvpOpponentFound();
			}
		}

		private void OnSuccessfullyMatchedSingleOpponentOffline(PlayerGameData player, string[] ids)
		{
			LogDebug("Offline Matchmake a pvp opponent succes");
			List<string> list = new List<string>();
			if (ids != null && ids.Length > 0)
			{
				string text = ids[0];
				if (!string.IsNullOrEmpty(text) && text != DIContainerInfrastructure.IdentityService.SharedId)
				{
					list.Add(text);
				}
			}
			if (list.Count <= 0)
			{
				LogDebug("Offline Matchmake a pvp opponent succes but no id returned, selecting fallback");
				PublicPlayerData fallbackPvPOpponent = GetFallbackPvPOpponent(player);
				player.CurrentPvPSeasonGameData.CurrentSeasonTurn.CurrentPvPOpponent = fallbackPvPOpponent;
				player.CurrentPvPSeasonGameData.CurrentSeasonTurn.FallbackOpponentReason = "NO_OPPONENT_ID";
				player.CurrentPvPSeasonGameData.CurrentSeasonTurn.CurrentBirdIndices = fallbackPvPOpponent.PvPIndices;
				TrackPvpOpponentFound();
				ResetIndices(player);
			}
			else
			{
				DIContainerInfrastructure.RemoteStorageService.GetPublicPlayerDatas(list.ToArray(), delegate(Dictionary<string, PublicPlayerData> publicPlayers)
				{
					OnSuccessfullGotPublicOpponent(player, publicPlayers);
				}, delegate(string error)
				{
					OnErrorGetPublicOpponent(player, error);
				});
			}
		}

		private void OnErrorGetPublicOpponent(PlayerGameData player, string error)
		{
			if (IsCurrentPvPTurnAvailable(player))
			{
				PublicPlayerData fallbackPvPOpponent = GetFallbackPvPOpponent(player);
				player.CurrentPvPSeasonGameData.CurrentSeasonTurn.CurrentPvPOpponent = fallbackPvPOpponent;
				player.CurrentPvPSeasonGameData.CurrentSeasonTurn.FallbackOpponentReason = "NO_PLAYER_PROFILE";
				player.CurrentPvPSeasonGameData.CurrentSeasonTurn.CurrentBirdIndices = fallbackPvPOpponent.PvPIndices;
				TrackPvpOpponentFound();
			}
		}

		private void TrackPvpOpponentFound()
		{
			PvPSeasonManagerGameData currentPvPSeasonGameData = DIContainerInfrastructure.GetCurrentPlayer().CurrentPvPSeasonGameData;
			if (currentPvPSeasonGameData != null)
			{
				bool flag = currentPvPSeasonGameData.CurrentSeasonTurn.FallbackOpponentReason.Contains("CHEAT");
				bool flag2 = currentPvPSeasonGameData.CurrentSeasonTurn.FallbackOpponentReason != "NONE";
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				dictionary.Add("Fallback", flag2.ToString());
				dictionary.Add("Cheater", flag.ToString());
				dictionary.Add("FallbackReason", currentPvPSeasonGameData.CurrentSeasonTurn.FallbackOpponentReason);
				DIContainerInfrastructure.GetAnalyticsSystem(true).LogEventWithParameters("PvpBattleMatchmaking", dictionary);
			}
		}

		private void OnSuccessfullGotPublicOpponent(PlayerGameData player, Dictionary<string, PublicPlayerData> publicPlayers)
		{
			LogDebug("Matchmake a pvp opponent public profile succes");
			if (!IsCurrentPvPTurnAvailable(player))
			{
				return;
			}
			if (publicPlayers == null)
			{
				PublicPlayerData fallbackPvPOpponent = GetFallbackPvPOpponent(player);
				player.CurrentPvPSeasonGameData.CurrentSeasonTurn.CurrentPvPOpponent = fallbackPvPOpponent;
				player.CurrentPvPSeasonGameData.CurrentSeasonTurn.FallbackOpponentReason = "NO_PLAYER_PROFILE";
				player.CurrentPvPSeasonGameData.CurrentSeasonTurn.CurrentBirdIndices = fallbackPvPOpponent.PvPIndices;
			}
			else
			{
				player.CurrentPvPSeasonGameData.CurrentSeasonTurn.CurrentPvPOpponent = publicPlayers.Values.FirstOrDefault();
				if (IsCheating(player.CurrentPvPSeasonGameData.CurrentSeasonTurn.CurrentPvPOpponent) || player.CurrentPvPSeasonGameData.CurrentSeasonTurn.CurrentPvPOpponent == null)
				{
					PublicPlayerData fallbackPvPOpponent2 = GetFallbackPvPOpponent(player);
					player.CurrentPvPSeasonGameData.CurrentSeasonTurn.CurrentPvPOpponent = fallbackPvPOpponent2;
					player.CurrentPvPSeasonGameData.CurrentSeasonTurn.CurrentBirdIndices = fallbackPvPOpponent2.PvPIndices;
				}
				else
				{
					player.CurrentPvPSeasonGameData.CurrentSeasonTurn.FallbackOpponentReason = "NONE";
					player.CurrentPvPSeasonGameData.CurrentSeasonTurn.CurrentBirdIndices = player.CurrentPvPSeasonGameData.CurrentSeasonTurn.CurrentPvPOpponent.PvPIndices;
				}
			}
			TrackPvpOpponentFound();
		}

		public bool IsCheating(PublicPlayerData opponent)
		{
			if (opponent == null || opponent.PvPIndices == null)
			{
				global::DebugLog.Error(GetType(), "IsCheating - opponent null or pvpIndices not set");
				return false;
			}
			List<int> pvPIndices = opponent.PvPIndices;
			PlayerGameData currentPlayer = DIContainerInfrastructure.GetCurrentPlayer();
			List<BirdGameData> list = new List<BirdGameData>();
			for (int i = 0; i < pvPIndices.Count; i++)
			{
				BirdGameData giantEnemyBird = new BirdGameData(opponent.Birds[pvPIndices[i]]);
				if (giantEnemyBird.ClassItem == null || !giantEnemyBird.ClassItem.IsValidForBird(giantEnemyBird))
				{
					InventoryGameData inventoryGameData = new InventoryGameData(opponent.Inventory);
					DIContainerLogic.InventoryService.EquipBirdWithItem(new List<IInventoryItemGameData> { inventoryGameData.Items[InventoryItemType.Class].FirstOrDefault((IInventoryItemGameData item) => item.IsValidForBird(giantEnemyBird)) }, InventoryItemType.Class, giantEnemyBird.InventoryGameData);
				}
				list.Add(giantEnemyBird);
				if (giantEnemyBird.Level > DIContainerLogic.PlayerOperationsService.GetPlayerMaxLevel())
				{
					global::DebugLog.Log("IsCheating - level = " + giantEnemyBird.Level);
					currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.FallbackOpponentReason = "CHEAT_LEVEL";
					return true;
				}
				if (giantEnemyBird.ClassItem.Data.Level > giantEnemyBird.ClassItem.MasteryMaxRank())
				{
					global::DebugLog.Log("IsCheating - mastery = " + giantEnemyBird.ClassItem.Data.Level);
					currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.FallbackOpponentReason = "CHEAT_MASTERY";
					return true;
				}
				if (giantEnemyBird.MainHandItem.Data.Level > giantEnemyBird.Level + 2)
				{
					global::DebugLog.Log("IsCheating - mainhand = " + giantEnemyBird.MainHandItem.Data.Level);
					currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.FallbackOpponentReason = "CHEAT_EQUIPMENT";
					return true;
				}
				if (giantEnemyBird.OffHandItem.Data.Level > giantEnemyBird.Level + 2)
				{
					global::DebugLog.Log("IsCheating - offhand = " + giantEnemyBird.OffHandItem.Data.Level);
					currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.FallbackOpponentReason = "CHEAT_EQUIPMENT";
					return true;
				}
				if (giantEnemyBird.MainHandItem.EnchantementLevel > DIContainerLogic.EnchantmentLogic.GetMaxEnchantmentLevel(giantEnemyBird.MainHandItem))
				{
					global::DebugLog.Log("IsCheating - mainhand enchant = " + giantEnemyBird.MainHandItem.EnchantementLevel);
					currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.FallbackOpponentReason = "CHEAT_ENCHANTMENT";
					return true;
				}
				if (giantEnemyBird.OffHandItem.EnchantementLevel > DIContainerLogic.EnchantmentLogic.GetMaxEnchantmentLevel(giantEnemyBird.OffHandItem))
				{
					global::DebugLog.Log("IsCheating - offhand enchant = " + giantEnemyBird.OffHandItem.EnchantementLevel);
					currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.FallbackOpponentReason = "CHEAT_ENCHANTMENT";
					return true;
				}
			}
			int pvPTeamPowerLevel = DIContainerInfrastructure.GetPowerLevelCalculator().GetPvPTeamPowerLevel(DIContainerInfrastructure.GetCurrentPlayer().PublicPlayer, null);
			int num = DIContainerInfrastructure.GetPowerLevelCalculator().GetPvPTeamPowerLevel(opponent, null) * 2;
			float num2 = Mathf.Abs(num - pvPTeamPowerLevel);
			if (num2 / (float)pvPTeamPowerLevel > (float)DIContainerBalancing.Service.GetBalancingData<WorldBalancingData>("piggy_island").PvpMaxPowerlevelDiff / 100f)
			{
				global::DebugLog.Log("IsCheating - plDiff = " + num2 + " - that's more than " + DIContainerBalancing.Service.GetBalancingData<WorldBalancingData>("piggy_island").PvpMaxPowerlevelDiff + "%");
				currentPlayer.CurrentPvPSeasonGameData.CurrentSeasonTurn.FallbackOpponentReason = "POWERLEVEL_DIFFERENCE";
				return true;
			}
			return false;
		}

		private void ResetIndices(PlayerGameData player)
		{
			List<int> list = new List<int>();
			player.CurrentPvPSeasonGameData.CurrentSeasonTurn.CurrentBirdIndices = new List<int>();
			for (int i = 0; i < player.CurrentPvPSeasonGameData.CurrentSeasonTurn.CurrentPvPOpponent.Birds.Count; i++)
			{
				list.Add(i);
			}
			list.Shuffle();
			player.CurrentPvPSeasonGameData.CurrentSeasonTurn.CurrentBirdIndices = list.GetRange(0, Mathf.Min(3, player.CurrentPvPSeasonGameData.CurrentSeasonTurn.CurrentPvPOpponent.Birds.Count));
			player.CurrentPvPSeasonGameData.CurrentSeasonTurn.CurrentBirdIndices.Sort();
		}

		public PublicPlayerData GetFallbackPvPOpponent(PlayerGameData player)
		{
			DebugLog("Fallback Opponent selected!");
			SocialEnvironmentBalancingData balancingData = DIContainerBalancing.Service.GetBalancingData<SocialEnvironmentBalancingData>("default");
			Dictionary<string, int> pvPFallbackHard = balancingData.PvPFallbackHard;
			int num = Mathf.RoundToInt((GetAverageEquipmentLevel(player) + (float)player.Data.Level) / 2f);
			int value = 0;
			int val = DIContainerBalancing.Service.GetBalancingDataList<ExperienceLevelBalancingData>().Count + 1;
			pvPFallbackHard.TryGetValue("level", out value);
			num = Math.Max(1, num + value);
			num = Math.Min(val, num);
			int value2 = 0;
			pvPFallbackHard.TryGetValue("mastery", out value2);
			int value3 = 0;
			pvPFallbackHard.TryGetValue("high_average", out value3);
			value2 = ((value3 != 1) ? (value2 + (int)Math.Round(RequirementOperationServiceRealImpl.GetHighAverageMasteryValue(player))) : (value2 + (int)Math.Round(RequirementOperationServiceRealImpl.GetHighAverageMasteryValue(player))));
			value2 = Math.Max(1, value2);
			int value4 = 0;
			pvPFallbackHard.TryGetValue("sets", out value4);
			PublicPlayerData fallbackPlayer = DIContainerLogic.SocialService.GetFallbackPlayer(value2, num, (EquipmentState)value4, player.CurrentPvPSeasonGameData.Data.CurrentLeague);
			fallbackPlayer.PvPIndices = GetRandomBirdIndices();
			return fallbackPlayer;
		}

		private List<int> GetRandomBirdIndices()
		{
			int num = UnityEngine.Random.Range(0, 5);
			int num2 = UnityEngine.Random.Range(0, 5);
			if (num == num2)
			{
				num = ((num != 0) ? (num - 1) : (num + 1));
			}
			List<int> list = new List<int>();
			for (int i = 0; i < 5; i++)
			{
				if (i != num && i != num2)
				{
					list.Add(i);
				}
			}
			return list;
		}

		public void UpdateCurrentPvPOpponent(PlayerGameData player, PvPSeasonManagerGameData pvpManager)
		{
			MatchmakeForSinglePvPOpponent(player, pvpManager);
		}

		public void RemoveCurrentOpponent(IAsyncResult ar)
		{
			if (IsCurrentPvPTurnAvailable(DIContainerInfrastructure.GetCurrentPlayer()))
			{
				PvPTurnManagerGameData currentSeasonTurn = DIContainerInfrastructure.GetCurrentPlayer().CurrentPvPSeasonGameData.CurrentSeasonTurn;
				currentSeasonTurn.LastOpponentUpdateTime = DIContainerLogic.GetDeviceTimingService().GetPresentTime();
				currentSeasonTurn.CurrentPvPOpponent = null;
				currentSeasonTurn.FallbackOpponentReason = string.Empty;
				currentSeasonTurn.CurrentBirdIndices = null;
			}
		}

		public TimeSpan GetDailyPvpRefreshTimeLeft(PlayerGameData player, PvPSeasonManagerGameData seasonManager)
		{
			if (seasonManager == null || seasonManager.CurrentSeasonTurn == null)
			{
				return new TimeSpan(0L);
			}
			DateTime trustedTime;
			if (!DIContainerLogic.GetServerOnlyTimingService().TryGetTrustedTime(out trustedTime))
			{
				trustedTime = DIContainerLogic.GetServerOnlyTimingService().GetPresentTime();
			}
			DateTime dateTimeFromTimestamp = DIContainerLogic.GetServerOnlyTimingService().GetDateTimeFromTimestamp(seasonManager.CurrentSeasonTurn.Data.LastUsedPvpEnergy);
			DateTime todayWithOffset = GetTodayWithOffset(trustedTime, new TimeSpan(0, seasonManager.Balancing.HourOfDayToRefreshEnergyAndObjectives, 0, 0));
			if (IsBefore(dateTimeFromTimestamp, todayWithOffset))
			{
				return DIContainerLogic.GetServerOnlyTimingService().TimeLeftUntil(todayWithOffset);
			}
			DateTime targetServerTime = todayWithOffset.AddDays(1.0);
			return DIContainerLogic.GetServerOnlyTimingService().TimeLeftUntil(targetServerTime);
		}

		public bool IsDailyPvpRefreshed(PlayerGameData player, PvPSeasonManagerGameData seasonManager)
		{
			if (seasonManager == null || seasonManager.CurrentSeasonTurn == null)
			{
				return false;
			}
			DateTime trustedTime;
			if (!m_timingService.TryGetTrustedTime(out trustedTime))
			{
				return false;
			}
			DateTime dateTimeFromTimestamp = DIContainerLogic.GetServerOnlyTimingService().GetDateTimeFromTimestamp(seasonManager.CurrentSeasonTurn.Data.LastUsedPvpEnergy);
			DateTime todayWithOffset = GetTodayWithOffset(trustedTime, new TimeSpan(0, seasonManager.Balancing.HourOfDayToRefreshEnergyAndObjectives, 0, 0));
			DateTime dateTime = GetTodayWithOffset(dateTimeFromTimestamp, new TimeSpan(0, seasonManager.Balancing.HourOfDayToRefreshEnergyAndObjectives, 0, 0));
			if (DateTime.Compare(dateTime, dateTimeFromTimestamp) < 0)
			{
				dateTime = GetTomorrowWithOffset(dateTimeFromTimestamp, new TimeSpan(0, seasonManager.Balancing.HourOfDayToRefreshEnergyAndObjectives, 0, 0));
				LogDebug("LAST TIME FIX: It should now be past 12 o'clock!");
			}
			LogDebug(string.Concat("Target Time Today: ", todayWithOffset, " Time left ", todayWithOffset - trustedTime, " Last Time: ", dateTimeFromTimestamp, " LastTimeTargetTime: ", dateTime, " Current Time: ", trustedTime));
			return IsBefore(dateTimeFromTimestamp, todayWithOffset) && DIContainerLogic.GetServerOnlyTimingService().IsAfter(dateTime);
		}

		public DateTime GetTodayWithOffset(DateTime d, TimeSpan addedSpan)
		{
			return new DateTime(d.Year, d.Month, d.Day, 0, 0, 0) + addedSpan;
		}

		public DateTime GetTomorrowWithOffset(DateTime d, TimeSpan addedSpan)
		{
			return GetTodayWithOffset(d, addedSpan).AddDays(1.0);
		}

		public bool IsAfter(DateTime d1, DateTime d2)
		{
			return DateTime.Compare(d1, d2) > 0;
		}

		public bool IsBefore(DateTime d1, DateTime d2)
		{
			return DateTime.Compare(d1, d2) < 0;
		}

		public void ChangeMatchmakingDifficulty(PlayerGameData playerGameData, int change)
		{
			if (!IsCurrentPvPTurnAvailable(playerGameData))
			{
				LogDebug("No turn available to change difficulties return!");
			}
			playerGameData.CurrentPvPSeasonGameData.CurrentSeasonTurn.Data.CurrentMatchingDifficulty = Mathf.Clamp(playerGameData.CurrentPvPSeasonGameData.CurrentSeasonTurn.Data.CurrentMatchingDifficulty + change, 0, playerGameData.CurrentPvPSeasonGameData.Balancing.MaxMatchmakingDifficulty);
		}

		public bool IsSeasonOver(PvPSeasonManagerBalancingData pvPSeasonManagerBalancingData)
		{
			return m_timingService.IsAfter(m_timingService.GetDateTimeFromTimestamp(pvPSeasonManagerBalancingData.SeasonEndTimeStamp));
		}
	}
}
