using System;
using System.Runtime.CompilerServices;
using Interfaces.Identity;
using Rcs;

public class IdentityServiceNullImpl : IIdentityService
{
	public IdentityCredentials UserCredentials
	{
		get
		{
			IdentityCredentials identityCredentials = new IdentityCredentials();
			identityCredentials.Password = "123456";
			identityCredentials.UserName = "null-user";
			identityCredentials.Email = "none";
			return identityCredentials;
		}
	}

	public string AccountId
	{
		get
		{
			return "0";
		}
	}

	public string SharedId
	{
		get
		{
			return "current";
		}
	}

	[method: MethodImpl(32)]
	public event Action OnLoggedIn;

	[method: MethodImpl(32)]
	public event Action<int, string> OnLoginError;

	public void Initialize()
	{
		DebugLog.Log(GetType().Name + " IdentityServiceNullImpl");
	}

	public void LoginAuto()
	{
	}

	public void Logout()
	{
		DebugLog.Log(GetType().Name + " Logout");
	}

	public bool IsGuest()
	{
		return true;
	}

	public void Login(Identity.LoginView view)
	{
		DebugLog.Log(GetType().Name + " Login");
	}

	public void LoginGuest()
	{
	}

	public void LoginFacebook()
	{
	}

	public void ValidateNickname(string nickname, Identity.ValidateNicknameSuccessCallback OnSuccess, Identity.ValidateNicknameErrorCallback OnError)
	{
		if (nickname.Contains("ass") || nickname.Contains("fuck") || nickname.Contains("hitler"))
		{
			if (OnError != null)
			{
				OnError("Invalid nickname");
			}
		}
		else if (OnSuccess != null)
		{
			OnSuccess(true, "Valid nickname");
		}
	}
}
