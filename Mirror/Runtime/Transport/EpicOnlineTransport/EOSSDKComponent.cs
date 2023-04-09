using System;
using System.Collections;
using System.Collections.Generic;
using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;


public class EOSSDKComponent : MonoBehaviour {
    public static EOSSDKComponent Instance => mInstance;
    private static EOSSDKComponent mInstance;

    private static EOSManager.EOSSingleton mEosManagerInstance {
        get {
            if (EOSManager.Instance == null) {
                Debug.LogError("No instance of EOS Manager was found. Please ensure that you have added it to a GameObject in the scene.");
                return null;
            }

            return EOSManager.Instance;
        }
    }

    private void Awake() {
        if (mInstance != null && mInstance != this) {
            Destroy(this);
        } else {
            mInstance = this;
        }
    }

    private void Update() {
        mEosManagerInstance.Tick();
    }
    
    public void LoginToEos(ExternalCredentialType externalCredentialType, string token = null, string displayName = "", Action<LoginCallbackInfo> onLoginSuccess = null, Action<CreateUserCallbackInfo> onCreateUser = null,
        Action<LoginCallbackInfo> onLoginFail = null) {
        if (externalCredentialType == ExternalCredentialType.DeviceidAccessToken) {
            if (String.IsNullOrEmpty(displayName)) {
                displayName = SystemInfo.deviceName;
            }
            
            mEosManagerInstance.StartConnectLoginWithDeviceToken(displayName, loginCallback => {
                HandleLoginCallbacks(loginCallback, onLoginSuccess, onCreateUser, onLoginFail);
            });
        } else {
            if (String.IsNullOrEmpty(token)) {
                Debug.LogError("The external login type you've selected requires a token. Please refer to the login provider's documentations on how to retrieve the tokens.");
                onLoginFail?.Invoke(new LoginCallbackInfo() {
                    ClientData = null,
                    ContinuanceToken = null,
                    LocalUserId = null,
                    ResultCode = Result.InvalidParameters
                });
                return;
            }
            
            mEosManagerInstance.StartConnectLoginWithOptions(externalCredentialType, token, displayName, loginCallbackInfo => {
                HandleLoginCallbacks(loginCallbackInfo, onLoginSuccess, onCreateUser, onLoginFail);
            });
        }
    }

    private void HandleLoginCallbacks(LoginCallbackInfo callbackInfo, Action<LoginCallbackInfo> onLoginSuccess, Action<CreateUserCallbackInfo> onCreateUser = null, Action<LoginCallbackInfo> onLoginFail = null) {
        if (callbackInfo.ResultCode == Result.Success) {
            onLoginSuccess?.Invoke(callbackInfo);
        } else if (callbackInfo.ResultCode == Result.InvalidUser) {
            mEosManagerInstance.CreateConnectUserWithContinuanceToken(callbackInfo.ContinuanceToken, (createUserCallbackInfo => {
                if (createUserCallbackInfo.ResultCode == Result.Success) {
                    onCreateUser?.Invoke(createUserCallbackInfo);
                } else {
                    Debug.LogError("Login failed with code: " + callbackInfo.ResultCode);
                    onLoginFail?.Invoke(callbackInfo);
                }
            }));
        } else {
          onLoginFail?.Invoke(callbackInfo);  
        }
    }
}
