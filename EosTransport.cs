using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Epic.OnlineServices.P2P;
using Epic.OnlineServices;
using Mirror;
using Epic.OnlineServices.Metrics;
using System.Collections;

namespace EpicTransport {

    /// <summary>
    /// EOS Transport following the Mirror transport standard
    /// </summary>
    public class EosTransport : Transport {
        private const string EPIC_SCHEME = "epic";

        private Client client;
        private Server server;

        private Common activeNode;

        [SerializeField]
        public PacketReliability[] Channels = new PacketReliability[2] { PacketReliability.ReliableOrdered, PacketReliability.UnreliableUnordered };
        
        [Tooltip("Timeout for connecting in seconds.")]
        public int timeout = 25;

        public float ignoreCachedMessagesAtStartUpInSeconds = 2.0f;
        private float ignoreCachedMessagesTimer = 0.0f;

        public RelayControl relayControl = RelayControl.AllowRelays;

        [Header("Info")]
        [Tooltip("This will display your Epic Account ID when you start or connect to a server.")]
        public ProductUserId productUserId;

        private void Awake() {
            Debug.Assert(Channels != null && Channels.Length > 0, "No channel configured for EOS Transport.");

            StartCoroutine("FetchEpicAccountId");
            StartCoroutine("ChangeRelayStatus");
        }

        private void LateUpdate() {
            if (activeNode != null) {
                ignoreCachedMessagesTimer += Time.deltaTime;

                if (ignoreCachedMessagesTimer <= ignoreCachedMessagesAtStartUpInSeconds) {
                    activeNode.ignoreAllMessages = true;
                } else {
                    activeNode.ignoreAllMessages = false;

                    if(client != null && !client.isConnecting) {
                        if (EOSSDKComponent.Initialized) {
                            client.Connect(client.hostAddress);
                        } else {
                            Debug.LogError("EOS not initialized");
                            client.EosNotInitialized();
                        }
                        client.isConnecting = true;
                    }
                }
            }

            if (enabled) {
                activeNode?.ReceiveData();
            }
        }

        public override bool ClientConnected() => ClientActive() && client.Connected;
        public override void ClientConnect(string address) {
            if (!EOSSDKComponent.Initialized) {
                Debug.LogError("EOS not initialized. Client could not be started.");
                OnClientDisconnected.Invoke();
                return;
            }

            StartCoroutine("FetchEpicAccountId");

            if (ServerActive()) {
                Debug.LogError("Transport already running as server!");
                return;
            }

            if (!ClientActive() || client.Error) {
                Debug.Log($"Starting client, target address {address}.");

                client = Client.CreateClient(this, address);
                activeNode = client;

                if (EOSSDKComponent.CollectPlayerMetrics) {
                    // Start Metrics colletion session
                    BeginPlayerSessionOptions sessionOptions = new BeginPlayerSessionOptions();
                    sessionOptions.AccountId = EOSSDKComponent.LocalUserAccountId;
                    sessionOptions.ControllerType = UserControllerType.Unknown;
                    sessionOptions.DisplayName = EOSSDKComponent.DisplayName;
                    sessionOptions.GameSessionId = null;
                    sessionOptions.ServerIp = null;
                    Result result = EOSSDKComponent.GetMetricsInterface().BeginPlayerSession(sessionOptions);

                    if(result == Result.Success) {
                        Debug.Log("Started Metric Session");
                    }
                }
            } else {
                Debug.LogError("Client already running!");
            }
        }

        public override void ClientConnect(Uri uri) {
            if (uri.Scheme != EPIC_SCHEME)
                throw new ArgumentException($"Invalid url {uri}, use {EPIC_SCHEME}://EpicAccountId instead", nameof(uri));

            ClientConnect(uri.Host);
        }

        public override void ClientSend(int channelId, ArraySegment<byte> segment) {
            byte[] data = new byte[segment.Count];
            Array.Copy(segment.Array, segment.Offset, data, 0, segment.Count);
            client.Send(data, channelId);
        }

        public override void ClientDisconnect() {
            if (ClientActive()) {
                Shutdown();
            }
        }
        public bool ClientActive() => client != null;


        public override bool ServerActive() => server != null;
        public override void ServerStart() {
            if (!EOSSDKComponent.Initialized) {
                Debug.LogError("EOS not initialized. Server could not be started.");
                return;
            }

            StartCoroutine("FetchEpicAccountId");

            if (ClientActive()) {
                Debug.LogError("Transport already running as client!");
                return;
            }

            if (!ServerActive()) {
                Debug.Log("Starting server.");

                server = Server.CreateServer(this, NetworkManager.singleton.maxConnections);
                activeNode = server;

                if (EOSSDKComponent.CollectPlayerMetrics) {
                    // Start Metrics colletion session
                    BeginPlayerSessionOptions sessionOptions = new BeginPlayerSessionOptions();
                    sessionOptions.AccountId = EOSSDKComponent.LocalUserAccountId;
                    sessionOptions.ControllerType = UserControllerType.Unknown;
                    sessionOptions.DisplayName = EOSSDKComponent.DisplayName;
                    sessionOptions.GameSessionId = null;
                    sessionOptions.ServerIp = null;
                    Result result = EOSSDKComponent.GetMetricsInterface().BeginPlayerSession(sessionOptions);

                    if (result == Result.Success) {
                        Debug.Log("Started Metric Session");
                    }
                }
            } else {
                Debug.LogError("Server already started!");
            }
        }

        public override Uri ServerUri() {
            UriBuilder epicBuilder = new UriBuilder { 
                Scheme = EPIC_SCHEME,
                Host = EOSSDKComponent.LocalUserProductIdString
            };

            return epicBuilder.Uri;
        }

        public override void ServerSend(int connectionId, int channelId, ArraySegment<byte> segment) {
            if (ServerActive()) {
                byte[] data = new byte[segment.Count];
                Array.Copy(segment.Array, segment.Offset, data, 0, segment.Count);
                server.SendAll(connectionId, data, channelId);
            }
        }
        public override bool ServerDisconnect(int connectionId) => ServerActive() && server.Disconnect(connectionId);
        public override string ServerGetClientAddress(int connectionId) => ServerActive() ? server.ServerGetClientAddress(connectionId) : string.Empty;
        public override void ServerStop() {
            if (ServerActive()) {
                Shutdown();
            }
        }

        public override void Shutdown() {
            if (EOSSDKComponent.CollectPlayerMetrics) {
                // Stop Metrics collection session
                EndPlayerSessionOptions endSessionOptions = new EndPlayerSessionOptions();
                endSessionOptions.AccountId = EOSSDKComponent.LocalUserAccountId;
                Result result = EOSSDKComponent.GetMetricsInterface().EndPlayerSession(endSessionOptions);

                if (result == Result.Success) {
                    Debug.LogError("Stopped Metric Session");
                }
            }

            server?.Shutdown();
            client?.Disconnect();

            server = null;
            client = null;
            activeNode = null;
            Debug.Log("Transport shut down.");
        }

        public override int GetMaxPacketSize(int channelId) {
            return P2PInterface.MaxPacketSize;
        }

        public override bool Available() {
            try {
                return EOSSDKComponent.Initialized;
            } catch {
                return false;
            }
        }

        private IEnumerator FetchEpicAccountId() {
            while (!EOSSDKComponent.Initialized) {
                yield return null;
            }

            productUserId = EOSSDKComponent.LocalUserProductId;
        }

        private IEnumerator ChangeRelayStatus() {
            while (!EOSSDKComponent.Initialized) {
                yield return null;
            }

            SetRelayControlOptions setRelayControlOptions = new SetRelayControlOptions();
            setRelayControlOptions.RelayControl = relayControl;

            EOSSDKComponent.GetP2PInterface().SetRelayControl(setRelayControlOptions);
        }

        public void ResetIgnoreMessagesAtStartUpTimer() {
            ignoreCachedMessagesTimer = 0;
        }

        private void OnDestroy() {
            if (activeNode != null) {
                Shutdown();
            }
        }
    }
}