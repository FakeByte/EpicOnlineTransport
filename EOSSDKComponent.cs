using Epic.OnlineServices;
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
namespace EpicTransport {
    public class EOSSDKComponent : MonoBehaviour {

        // Unity Inspector shown variables

        // Set these values as appropriate. For more information, see the Developer Portal documentation.
        [Header("SDK Keys")]
        public string epicProductName = "MyApplication";
        public string epicProductVersion = "1.0";
        public string epicProductId = "";
        public string epicSandboxId = "";
        public string epicDeploymentId = "";
        public string epicClientId = "";
        public string epicClientSecret = "";

        [Header("User Login")]
        public bool autoLogoutInEditor = false;
        public bool authInterfaceLogin = false;
        public Epic.OnlineServices.Auth.LoginCredentialType authInterfaceCredentialType = Epic.OnlineServices.Auth.LoginCredentialType.AccountPortal;
        public uint devAuthToolPort = 7878;
        public string devAuthToolCredentialName = "";
        public Epic.OnlineServices.Connect.ExternalCredentialType connectInterfaceCredentialType = Epic.OnlineServices.Connect.ExternalCredentialType.DeviceidAccessToken;
        public string deviceModel = "PC Windows 64bit";
        [SerializeField] private string displayName = "User";
        public static string DisplayName {
            get {
                return Instance.displayName;
            }
            set {
                Instance.displayName = value;
            }
        }

        [Header("Misc")]
        public LogLevel epicLoggerLevel = LogLevel.Error;

        [SerializeField] private bool collectPlayerMetrics = true;
        public static bool CollectPlayerMetrics {
            get {
                return Instance.collectPlayerMetrics;
            }
        }

        public bool checkForEpicLauncherAndRestart = false;
        public bool delayedInitialization = false;
        public float platformTickIntervalInSeconds = 0.0f;
        private float platformTickTimer = 0f;
        public uint tickBudgetInMilliseconds = 0;

        // End Unity Inspector shown variables


        private string authInterfaceLoginCredentialId = null;
        public static void SetAuthInterfaceLoginCredentialId(string credentialId) => Instance.authInterfaceLoginCredentialId = credentialId;
        private string authInterfaceCredentialToken = null;
        public static void SetAuthInterfaceCredentialToken(string credentialToken) => Instance.authInterfaceCredentialToken = credentialToken;
        private string connectInterfaceCredentialToken = null;
        public static void SetConnectInterfaceCredentialToken(string credentialToken) => Instance.connectInterfaceCredentialToken = credentialToken;

        private PlatformInterface EOS;

        // Interfaces
        public static Epic.OnlineServices.Achievements.AchievementsInterface GetAchievementsInterface() => Instance.EOS.GetAchievementsInterface();
        public static Epic.OnlineServices.Auth.AuthInterface GetAuthInterface() => Instance.EOS.GetAuthInterface();
        public static Epic.OnlineServices.Connect.ConnectInterface GetConnectInterface() => Instance.EOS.GetConnectInterface();
        public static Epic.OnlineServices.Ecom.EcomInterface GetEcomInterface() => Instance.EOS.GetEcomInterface();
        public static Epic.OnlineServices.Friends.FriendsInterface GetFriendsInterface() => Instance.EOS.GetFriendsInterface();
        public static Epic.OnlineServices.Leaderboards.LeaderboardsInterface GetLeaderboardsInterface() => Instance.EOS.GetLeaderboardsInterface();
        public static Epic.OnlineServices.Lobby.LobbyInterface GetLobbyInterface() => Instance.EOS.GetLobbyInterface();
        public static Epic.OnlineServices.Metrics.MetricsInterface GetMetricsInterface() => Instance.EOS.GetMetricsInterface(); // Handled by the transport automatically, only use this interface if Mirror is not used for singleplayer
        public static Epic.OnlineServices.Mods.ModsInterface GetModsInterface() => Instance.EOS.GetModsInterface();
        public static Epic.OnlineServices.P2P.P2PInterface GetP2PInterface() => Instance.EOS.GetP2PInterface();
        public static Epic.OnlineServices.PlayerDataStorage.PlayerDataStorageInterface GetPlayerDataStorageInterface() => Instance.EOS.GetPlayerDataStorageInterface();
        public static Epic.OnlineServices.Presence.PresenceInterface GetPresenceInterface() => Instance.EOS.GetPresenceInterface();
        public static Epic.OnlineServices.Sessions.SessionsInterface GetSessionsInterface() => Instance.EOS.GetSessionsInterface();
        public static Epic.OnlineServices.TitleStorage.TitleStorageInterface GetTitleStorageInterface() => Instance.EOS.GetTitleStorageInterface();
        public static Epic.OnlineServices.UI.UIInterface GetUIInterface() => Instance.EOS.GetUIInterface();
        public static Epic.OnlineServices.UserInfo.UserInfoInterface GetUserInfoInterface() => Instance.EOS.GetUserInfoInterface();


        protected EpicAccountId localUserAccountId;
        public static EpicAccountId LocalUserAccountId {
            get {
                return Instance.localUserAccountId;
            }
        }

        protected string localUserAccountIdString;
        public static string LocalUserAccountIdString {
            get {
                return Instance.localUserAccountIdString;
            }
        }

        protected ProductUserId localUserProductId;
        public static ProductUserId LocalUserProductId {
            get {
                return Instance.localUserProductId;
            }
        }

        protected string localUserProductIdString;
        public static string LocalUserProductIdString {
            get {
                return Instance.localUserProductIdString;
            }
        }

        protected bool initialized;
        public static bool Initialized {
            get {
                return Instance.initialized;
            }
        }

        protected bool isConnecting;
        public static bool IsConnecting {
            get {
                return Instance.isConnecting;
            }
        }

        protected static EOSSDKComponent instance;
        protected static EOSSDKComponent Instance {
            get {
                if (instance == null) {
                    return new GameObject("EOSSDKComponent").AddComponent<EOSSDKComponent>();
                } else {
                    return instance;
                }
            }
        }

        void Awake() {
            // Prevent multiple instances
            if (instance != null) {
                Destroy(gameObject);
                return;
            }
            instance = this;

            if (!delayedInitialization) {
                Initialize();
            }
        }

        protected void InitializeImplementation() {
            isConnecting = true;

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
            LoggingInterface.SetLogLevel(LogCategory.AllCategories, epicLoggerLevel);
            LoggingInterface.SetCallback(message => Logger.EpicDebugLog(message));

            var options = new Options() {
                ProductId = epicProductId,
                SandboxId = epicSandboxId,
                DeploymentId = epicDeploymentId,
                ClientCredentials = new ClientCredentials() {
                    ClientId = epicClientId,
                    ClientSecret = epicClientSecret
                },
                TickBudgetInMilliseconds = tickBudgetInMilliseconds
            };

            EOS = PlatformInterface.Create(options);
            if (EOS == null) {
                throw new System.Exception("Failed to create platform");
            }

            if (checkForEpicLauncherAndRestart) {
                Result result = EOS.CheckForLauncherAndRestart();

                // If not started through epic launcher the app will be restarted and we can quit 
                if (result != Result.NoChange) {

                    // Log error if launcher check failed, but still quit to prevent hacking
                    if (result == Result.UnexpectedError) {
                        Debug.LogError("Unexpected Error while checking if app was started through epic launcher");
                    }

                    Application.Quit();
                }
            }

            // If we use the Auth interface then only login into the Connect interface after finishing the auth interface login
            // If we don't use the Auth interface we can directly login to the Connect interface
            if (authInterfaceLogin) {
                if (authInterfaceCredentialType == Epic.OnlineServices.Auth.LoginCredentialType.Developer) {
                    authInterfaceLoginCredentialId = "localhost:" + devAuthToolPort;
                    authInterfaceCredentialToken = devAuthToolCredentialName;
                }

                // Login to Auth Interface
                Epic.OnlineServices.Auth.LoginOptions loginOptions = new Epic.OnlineServices.Auth.LoginOptions() {
                    Credentials = new Epic.OnlineServices.Auth.Credentials() {
                        Type = authInterfaceCredentialType,
                        Id = authInterfaceLoginCredentialId,
                        Token = authInterfaceCredentialToken
                    },
                    ScopeFlags = Epic.OnlineServices.Auth.AuthScopeFlags.BasicProfile | Epic.OnlineServices.Auth.AuthScopeFlags.FriendsList | Epic.OnlineServices.Auth.AuthScopeFlags.Presence
                };

                EOS.GetAuthInterface().Login(loginOptions, null, OnAuthInterfaceLogin);
            } else {
                // Login to Connect Interface
                if (connectInterfaceCredentialType == Epic.OnlineServices.Connect.ExternalCredentialType.DeviceidAccessToken) {
                    Epic.OnlineServices.Connect.CreateDeviceIdOptions createDeviceIdOptions = new Epic.OnlineServices.Connect.CreateDeviceIdOptions();
                    createDeviceIdOptions.DeviceModel = deviceModel;
                    EOS.GetConnectInterface().CreateDeviceId(createDeviceIdOptions, null, OnCreateDeviceId);
                } else {
                    ConnectInterfaceLogin();
                }
            }

        }
        public static void Initialize() {
            if (Instance.initialized || Instance.isConnecting) {
                return;
            }

            Instance.InitializeImplementation();
        }

        private void OnAuthInterfaceLogin(Epic.OnlineServices.Auth.LoginCallbackInfo loginCallbackInfo) {
            if (loginCallbackInfo.ResultCode == Result.Success) {
                Debug.Log("Auth Interface Login succeeded");

                string accountIdString;
                Result result = loginCallbackInfo.LocalUserId.ToString(out accountIdString);
                if (Result.Success == result) {
                    Debug.Log("EOS User ID:" + accountIdString);

                    localUserAccountIdString = accountIdString;
                    localUserAccountId = loginCallbackInfo.LocalUserId;
                }

                ConnectInterfaceLogin();
            } else {
                Debug.Log("Login returned " + loginCallbackInfo.ResultCode);
            }
        }

        private void OnCreateDeviceId(Epic.OnlineServices.Connect.CreateDeviceIdCallbackInfo createDeviceIdCallbackInfo) {
            if (createDeviceIdCallbackInfo.ResultCode == Result.Success || createDeviceIdCallbackInfo.ResultCode == Result.DuplicateNotAllowed) {
                ConnectInterfaceLogin();
            } else {
                Debug.Log("Device ID creation returned " + createDeviceIdCallbackInfo.ResultCode);
            }
        }

        private void ConnectInterfaceLogin() {
            var loginOptions = new Epic.OnlineServices.Connect.LoginOptions();

            if (connectInterfaceCredentialType == Epic.OnlineServices.Connect.ExternalCredentialType.Epic) {
                Epic.OnlineServices.Auth.Token token;
                Result result = EOS.GetAuthInterface().CopyUserAuthToken(new Epic.OnlineServices.Auth.CopyUserAuthTokenOptions(), localUserAccountId, out token);

                if (result == Result.Success) {
                    connectInterfaceCredentialToken = token.AccessToken;
                } else {
                    Debug.LogError("Failed to retrieve User Auth Token");
                }
            } else if (connectInterfaceCredentialType == Epic.OnlineServices.Connect.ExternalCredentialType.DeviceidAccessToken) {
                loginOptions.UserLoginInfo = new Epic.OnlineServices.Connect.UserLoginInfo();
                loginOptions.UserLoginInfo.DisplayName = displayName;
            }

            loginOptions.Credentials = new Epic.OnlineServices.Connect.Credentials();
            loginOptions.Credentials.Type = connectInterfaceCredentialType;
            loginOptions.Credentials.Token = connectInterfaceCredentialToken;

            EOS.GetConnectInterface().Login(loginOptions, null, OnConnectInterfaceLogin);
        }

        private void OnConnectInterfaceLogin(Epic.OnlineServices.Connect.LoginCallbackInfo loginCallbackInfo) {
            if (loginCallbackInfo.ResultCode == Result.Success) {
                Debug.Log("Connect Interface Login succeeded");

                string productIdString;
                Result result = loginCallbackInfo.LocalUserId.ToString(out productIdString);
                if (Result.Success == result) {
                    Debug.Log("EOS User Product ID:" + productIdString);

                    localUserProductIdString = productIdString;
                    localUserProductId = loginCallbackInfo.LocalUserId;
                }
                
                initialized = true;
                isConnecting = false;
            } else {
                Debug.Log("Login returned " + loginCallbackInfo.ResultCode + "\nRetrying...");
                EOS.GetConnectInterface().CreateUser(new Epic.OnlineServices.Connect.CreateUserOptions() { ContinuanceToken = loginCallbackInfo.ContinuanceToken }, null, (Epic.OnlineServices.Connect.CreateUserCallbackInfo cb) => {
                    if (cb.ResultCode != Result.Success) { Debug.Log(cb.ResultCode); return; }
                    localUserProductId = cb.LocalUserId;
                    ConnectInterfaceLogin();
                });
            }
        }

        private void OnAuthInterfaceLogout(Epic.OnlineServices.Auth.LogoutCallbackInfo logoutCallbackInfo) {

        }

        // Calling tick on a regular interval is required for callbacks to work.
        private void Update() {
            if (EOS != null) {
                platformTickTimer += Time.deltaTime;

                if (platformTickTimer >= platformTickIntervalInSeconds) {
                    platformTickTimer = 0;
                    EOS.Tick();
                }
            }
        }

        // When you release and shutdown the SDK library, you cannot initialize it again.
        // Make sure this is done at a relevant time in your game's lifecycle.
        // If you are working in editor, it is advised you do not release and shutdown the SDK
        // as you would be required to restart Unity to initialize the SDK again.
        private void OnDestroy() {
            if (EOS == null) {
                return;
            }

            if (Application.isEditor) {
                if (autoLogoutInEditor) {
                    Epic.OnlineServices.Auth.LogoutOptions logoutOptions = new Epic.OnlineServices.Auth.LogoutOptions();
                    logoutOptions.LocalUserId = LocalUserAccountId;

                    // Callback might not be called since we call Logout in OnDestroy()
                    EOS.GetAuthInterface().Logout(logoutOptions, null, OnAuthInterfaceLogout);
                }
            } else {
                EOS.Release();
                EOS = null;
                PlatformInterface.Shutdown();
            }
            

        }
    }
}