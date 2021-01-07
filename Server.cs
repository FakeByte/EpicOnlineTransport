using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror.Eos {
    public class Server : Common {
        private event Action<int> OnConnected;
        private event Action<int, byte[], int> OnReceivedData;
        private event Action<int> OnDisconnected;
        private event Action<int, Exception> OnReceivedError;

        private BidirectionalDictionary<ProductUserId, int> epicToMirrorIds;
        private int maxConnections;
        private int nextConnectionID;

        public static Server CreateServer(EosTransport transport, int maxConnections) {
            Server s = new Server(transport, maxConnections);

            s.OnConnected += (id) => transport.OnServerConnected.Invoke(id);
            s.OnDisconnected += (id) => transport.OnServerDisconnected.Invoke(id);
            s.OnReceivedData += (id, data, channel) => transport.OnServerDataReceived.Invoke(id, new ArraySegment<byte>(data), channel);
            s.OnReceivedError += (id, exception) => transport.OnServerError.Invoke(id, exception);

            if (!EOSSDKComponent.Initialized) {
                Debug.LogError("EOS not initialized.");
            }

            return s;
        }

        private Server(EosTransport transport, int maxConnections) : base(transport) {
            this.maxConnections = maxConnections;
            epicToMirrorIds = new BidirectionalDictionary<ProductUserId, int>();
            nextConnectionID = 1;
        }

        protected override void OnNewConnection(OnIncomingConnectionRequestInfo result) => EOSSDKComponent.EOS.GetP2PInterface().AcceptConnection(
            new AcceptConnectionOptions() {
                LocalUserId = EOSSDKComponent.localUserProductId,
                RemoteUserId = result.RemoteUserId,
                SocketId = new SocketId() { SocketName = SOCKET_ID }
            });

        protected override void OnReceiveInternalData(InternalMessages type, ProductUserId clientUserId) {
            switch (type) {
                case InternalMessages.CONNECT:
                    if (epicToMirrorIds.Count >= maxConnections) {
                        SendInternal(clientUserId, InternalMessages.DISCONNECT);
                        return;
                    }

                    SendInternal(clientUserId, InternalMessages.ACCEPT_CONNECT);

                    int connectionId = nextConnectionID++;
                    epicToMirrorIds.Add(clientUserId, connectionId);
                    OnConnected.Invoke(connectionId);
                    Debug.Log($"Client with Product User ID {clientUserId} connected. Assigning connection id {connectionId}");
                    break;
                case InternalMessages.DISCONNECT:
                    if (epicToMirrorIds.TryGetValue(clientUserId, out int connId)) {
                        OnDisconnected.Invoke(connId);
                        CloseP2PSessionWithUser(clientUserId);
                        epicToMirrorIds.Remove(clientUserId);
                        Debug.Log($"Client with Product User ID {clientUserId} disconnected.");
                    } else {
                        OnReceivedError.Invoke(-1, new Exception("ERROR Unknown Product User ID"));
                    }

                    break;
                default:
                    Debug.Log("Received unknown message type");
                    break;
            }
        }

        protected override void OnReceiveData(byte[] data, ProductUserId clientUserId, int channel) {
            if (epicToMirrorIds.TryGetValue(clientUserId, out int connectionId)) {
                OnReceivedData.Invoke(connectionId, data, channel);
            } else {
                CloseP2PSessionWithUser(clientUserId);
                Debug.LogError("Data received from steam client thats not known " + clientUserId);
                OnReceivedError.Invoke(-1, new Exception("ERROR Unknown SteamID"));
            }
        }

        public bool Disconnect(int connectionId) {
            if (epicToMirrorIds.TryGetValue(connectionId, out ProductUserId userId)) {
                SendInternal(userId, InternalMessages.DISCONNECT);
                return true;
            } else {
                Debug.LogWarning("Trying to disconnect unknown connection id: " + connectionId);
                return false;
            }
        }

        public void Shutdown() {
            foreach (KeyValuePair<ProductUserId, int> client in epicToMirrorIds) {
                Disconnect(client.Value);
                WaitForClose(client.Key);
            }

            Dispose();
        }

        public void SendAll(int connectionId, byte[] data, int channelId) {
            if (epicToMirrorIds.TryGetValue(connectionId, out ProductUserId userId)) {
                Send(userId, data, (byte)channelId);
            } else {
                Debug.LogError("Trying to send on unknown connection: " + connectionId);
                OnReceivedError.Invoke(connectionId, new Exception("ERROR Unknown Connection"));
            }

        }

        public string ServerGetClientAddress(int connectionId) {
            if (epicToMirrorIds.TryGetValue(connectionId, out ProductUserId userId)) {
                return userId.ToString();
            } else {
                Debug.LogError("Trying to get info on unknown connection: " + connectionId);
                OnReceivedError.Invoke(connectionId, new Exception("ERROR Unknown Connection"));
                return string.Empty;
            }
        }

        protected override void OnConnectionFailed(ProductUserId remoteId) {
            int connectionId = epicToMirrorIds.TryGetValue(remoteId, out int connId) ? connId : nextConnectionID++;
            OnDisconnected.Invoke(connectionId);

            epicToMirrorIds.Remove(remoteId);
        }
    }
}