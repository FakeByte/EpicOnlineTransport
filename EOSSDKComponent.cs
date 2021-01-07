using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Lobby;
using Epic.OnlineServices.Logging;
using Epic.OnlineServices.Platform;
using UnityEngine;

/// <summary>
/// Manages the Epic Online Services SDK
/// Do not destroy this component!
/// The Epic Online Services SDK can only be initialized once,
/// after releasing the SDK the game has to be restarted in order to initialize the SDK again.
/// In the unity editor the OnDestroy function will not run so that we dont have to restart the editor after play.
/// </summary>
public class EOSSDKComponent : MonoBehaviour {
    // Set these values as appropriate. For more information, see the Developer Portal documentation.
    public string epicProductName = "MyApplication";
    public string epicProductVersion = "1.0";
    public string epicProductId = "";
    public string epicSandboxId = "";
    public string epicDeploymentId = "";
    public string epicClientId = "";
    public string epicClientSecret = "";

    //Use this static handle to access all epic online services interfaces
    public static PlatformInterface EOS { get; private set; }
    private const float c_PlatformTickInterval = 0.1f;
    private float m_PlatformTickTimer = 0f;

    public static ProductUserId localUserProductId { get; private set; }
    public static string localUserProductIdString { get; private set; }
    public static bool Initialized { get; private set; }
    public static bool IsConnecting { get; private set; }

    void Awake() {
        IsConnecting = true;

        var initializeOptions = new InitializeOptions() {
            ProductName = epicProductName,
            ProductVersion = epicProductVersion
        };

        var initializeResult = PlatformInterface.Initialize(initializeOptions);

        // This code is called each time the game is run in the editor, so we catch the case where the SDK has already been initialized in the editor.
        var isAlreadyConfiguredInEditor = Application.isEditor && initializeResult == Result.AlreadyConfigured;
        if (initializeResult != Result.Success && !isAlreadyConfiguredInEditor) {
            throw new System.Exception("Failed to initialize platform: " + initializeResult);
        }

        // The SDK outputs lots of information that is useful for debugging.
        // Make sure to set up the logging interface as early as possible: after initializing.
        LoggingInterface.SetLogLevel(LogCategory.AllCategories, LogLevel.VeryVerbose);
        LoggingInterface.SetCallback((LogMessage logMessage) => {
            Debug.Log(logMessage.Message);
        });

        var options = new Options() {
            ProductId = epicProductId,
            SandboxId = epicSandboxId,
            DeploymentId = epicDeploymentId,
            ClientCredentials = new ClientCredentials() {
                ClientId = epicClientId,
                ClientSecret = epicClientSecret
            }
            
        };

        EOS = PlatformInterface.Create(options);
        if (EOS == null) {
            throw new System.Exception("Failed to create platform");
        }

        //Login to the Connect Interface
        Epic.OnlineServices.Connect.CreateDeviceIdOptions createDeviceIdOptions = new Epic.OnlineServices.Connect.CreateDeviceIdOptions();
        createDeviceIdOptions.DeviceModel = "PC Windows 64bit";
        EOS.GetConnectInterface().CreateDeviceId(createDeviceIdOptions, null,
            (Epic.OnlineServices.Connect.CreateDeviceIdCallbackInfo createDeviceIdCallbackInfo) => {
                if (createDeviceIdCallbackInfo.ResultCode == Result.Success || createDeviceIdCallbackInfo.ResultCode == Result.DuplicateNotAllowed) {
                    var loginOptions = new Epic.OnlineServices.Connect.LoginOptions();
                    loginOptions.UserLoginInfo = new Epic.OnlineServices.Connect.UserLoginInfo();
                    loginOptions.UserLoginInfo.DisplayName = "Justin";
                    loginOptions.Credentials = new Epic.OnlineServices.Connect.Credentials();
                    loginOptions.Credentials.Type = Epic.OnlineServices.Connect.ExternalCredentialType.DeviceidAccessToken;
                    loginOptions.Credentials.Token = null;

                    EOS.GetConnectInterface().Login(loginOptions, null,
                        (Epic.OnlineServices.Connect.LoginCallbackInfo loginCallbackInfo) => {
                            if (loginCallbackInfo.ResultCode == Result.Success) {
                                Debug.Log("Login succeeded");

                                string productIdString;
                                Result result = loginCallbackInfo.LocalUserId.ToString(out productIdString);
                                if (Result.Success == result) {
                                    Debug.Log("EOS User Product ID:" + productIdString);

                                    localUserProductIdString = productIdString;
                                    localUserProductId = loginCallbackInfo.LocalUserId;
                                }

                                Initialized = true;
                                IsConnecting = false;
                            } else {
                                Debug.Log("Login returned " + loginCallbackInfo.ResultCode);
                            }
                        });
                } else {
                    Debug.Log("Device ID creation returned " + createDeviceIdCallbackInfo.ResultCode);
                }
            });

    }

    // Calling tick on a regular interval is required for callbacks to work.
    private void Update() {
        if (EOS != null) {
            m_PlatformTickTimer += Time.deltaTime;

            if (m_PlatformTickTimer >= c_PlatformTickInterval) {
                m_PlatformTickTimer = 0;
                EOS.Tick();
            }
        }
    }

    // When you release and shutdown the SDK library, you cannot initialize it again.
    // Make sure this is done at a relevant time in your game's lifecycle.
    // If you are working in editor, it is advised you do not release and shutdown the SDK
    // as you would be required to restart Unity to initialize the SDK again.
    private void OnDestroy() {
        if (!Application.isEditor && EOS != null) {
            EOS.Release();
            EOS = null;
            PlatformInterface.Shutdown();
        }
    }
}