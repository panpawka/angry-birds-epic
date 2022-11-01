using System.Collections;
using UnityEngine;

public class PopupEnterName : MonoBehaviour
{
	[SerializeField]
	private UIInputTrigger m_OkButton;

	[SerializeField]
	private UIInputTrigger m_CancelButton;

	[SerializeField]
	private UILabel m_OpponentNameLabel;

	private string m_enteredNickname;

	private void Awake()
	{
		base.gameObject.SetActive(false);
		base.transform.parent = DIContainerInfrastructure.GetCoreStateMgr().m_GenericInterfaceRoot;
		DIContainerInfrastructure.GetCoreStateMgr().m_EnterNamePopup = this;
	}

	public void ShowEnterNamePopup()
	{
		base.gameObject.SetActive(true);
		StartCoroutine("EnterCoroutine");
	}

	private IEnumerator EnterCoroutine()
	{
		DIContainerInfrastructure.GetCoreStateMgr().RegisterPopupEntered(true);
		DIContainerInfrastructure.BackButtonMgr.RegisterBlockReason("popup_enter_name_enter");
		DIContainerInfrastructure.GetCoreStateMgr().m_GenericUI.RegisterBar(new BarRegistry
		{
			Depth = 6u,
			showSnoutlings = false
		}, true);
		yield return new WaitForSeconds(base.gameObject.PlayAnimationOrAnimatorState("Popup_UseVoucherCode_Enter"));
		RegisterEventHandlers();
		DIContainerInfrastructure.BackButtonMgr.DeRegisterBlockReason("popup_enter_name_enter");
	}

	private void RegisterEventHandlers()
	{
		DeRegisterEventHandlers();
		DIContainerInfrastructure.BackButtonMgr.RegisterAction(5, CancelButtonClicked);
		m_OkButton.Clicked += OkButtonClicked;
		m_CancelButton.Clicked += CancelButtonClicked;
	}

	private void DeRegisterEventHandlers()
	{
		DIContainerInfrastructure.BackButtonMgr.DeRegisterAction(5);
		m_OkButton.Clicked -= OkButtonClicked;
		m_CancelButton.Clicked -= CancelButtonClicked;
	}

	public void OnSubmit(string value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			m_enteredNickname = value;
			DIContainerInfrastructure.IdentityService.ValidateNickname(value, CheckBlackListSuccess, CheckBlackListFailed);
		}
	}

	private void CheckBlackListSuccess(bool success, string message)
	{
		if (success)
		{
			m_OpponentNameLabel.text = DIContainerInfrastructure.GetLocaService().ReplaceUnmappableCharacters(m_enteredNickname);
		}
		else
		{
			CheckBlackListFailed(message);
		}
	}

	private void CheckBlackListFailed(string message)
	{
		DIContainerInfrastructure.GetAsynchStatusService().ShowInfo(DIContainerInfrastructure.GetLocaService().Tr("toast_playername_blacklist"), "blacklistfail", DispatchMessage.Status.Error);
		DebugLog.Warn("Invalid nickname entered: " + message);
		StartCoroutine(DelayedResetName());
	}

	private IEnumerator DelayedResetName()
	{
		yield return new WaitForEndOfFrame();
		string realName = DIContainerInfrastructure.GetCurrentPlayer().SocialEnvironmentGameData.Data.EventPlayerName;
		m_enteredNickname = DIContainerInfrastructure.GetLocaService().ReplaceUnmappableCharacters(realName);
		m_OpponentNameLabel.text = m_enteredNickname;
	}

	private void OkButtonClicked()
	{
		SaveNewlyEnteredName();
		StartCoroutine(LeaveCoroutine());
	}

	private void SaveNewlyEnteredName()
	{
		DIContainerInfrastructure.GetCurrentPlayer().SocialEnvironmentGameData.Data.EventPlayerName = m_OpponentNameLabel.text;
		DebugLog.Log(GetType(), "saving the name entered: " + m_OpponentNameLabel.text);
		DIContainerInfrastructure.GetCurrentPlayer().SavePlayerData();
	}

	private void CancelButtonClicked()
	{
		StartCoroutine(LeaveCoroutine());
	}

	private IEnumerator LeaveCoroutine()
	{
		DIContainerInfrastructure.GetCoreStateMgr().RegisterPopupEntered(false);
		DeRegisterEventHandlers();
		DIContainerInfrastructure.BackButtonMgr.RegisterBlockReason("popup_enter_name_enter");
		DIContainerInfrastructure.GetCoreStateMgr().m_GenericUI.DeRegisterBar(6u);
		DIContainerInfrastructure.GetCoreStateMgr().m_GenericUI.UpdateAllBars();
		yield return new WaitForSeconds(base.gameObject.PlayAnimationOrAnimatorState("Popup_UseVoucherCode_Leave"));
		DIContainerInfrastructure.BackButtonMgr.DeRegisterBlockReason("popup_enter_name_enter");
		base.gameObject.SetActive(false);
	}
}
