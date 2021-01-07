
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using System;
using System.Collections;
using UnityEngine;

namespace Mirror.Eos {
    public abstract class Common {

        public const string SOCKET_ID = "Test";

        private PacketReliability[] channels;
        private int internal_ch => channels.Length;

        protected enum InternalMessages : byte {
            CONNECT,
            ACCEPT_CONNECT,
            DISCONNECT
        }

        private OnIncomingConnectionRequestCallback OnIncomingConnectionRequest;
        private OnRemoteConnectionClosedCallback OnRemoteConnectionClosed;

        protected readonly EosTransport transport;

        protected Common(EosTransport transport) {
            channels = transport.Channels;
            
            AddNotifyPeerConnectionRequestOptions addNotifyPeerConnectionRequestOptions = new AddNotifyPeerConnectionRequestOptions();
            addNotifyPeerConnectionRequestOptions.LocalUserId = EOSSDKComponent.localUserProductId;
            SocketId socketId = new SocketId();
            socketId.SocketName = SOCKET_ID;
            addNotifyPeerConnectionRequestOptions.SocketId = socketId;

            OnIncomingConnectionRequest += OnNewConnection;
            OnRemoteConnectionClosed += OnConnectFail;

            EOSSDKComponent.EOS.GetP2PInterface().AddNotifyPeerConnectionRequest(addNotifyPeerConnectionRequestOptions,
                null, OnIncomingConnectionRequest);

            AddNotifyPeerConnectionClosedOptions addNotifyPeerConnectionClosedOptions = new AddNotifyPeerConnectionClosedOptions();
            addNotifyPeerConnectionClosedOptions.LocalUserId = EOSSDKComponent.localUserProductId;
            addNotifyPeerConnectionClosedOptions.SocketId = socketId;

            EOSSDKComponent.EOS.GetP2PInterface().AddNotifyPeerConnectionClosed(addNotifyPeerConnectionClosedOptions,
                null, OnRemoteConnectionClosed);

            this.transport = transport;
        }

        protected void Dispose() {
            
        }

        protected abstract void OnNewConnection(OnIncomingConnectionRequestInfo result);

        private void OnConnectFail(OnRemoteConnectionClosedInfo result) {
            OnConnectionFailed(result.RemoteUserId);
            CloseP2PSessionWithUser(result.RemoteUserId);

            switch (result.Reason) {
                case ConnectionClosedReason.ClosedByLocalUser:
                    throw new Exception("Connection cLosed: The Connection was gracecfully closed by the local user.");
                case ConnectionClosedReason.ClosedByPeer:
                    throw new Exception("Connection closed: The connection was gracefully closed by remote user.");
                case ConnectionClosedReason.ConnectionClosed:
                    throw new Exception("Connection closed: The connection was unexpectedly closed.");
                case ConnectionClosedReason.ConnectionFailed:
                    throw new Exception("Connection failed: Failled to establish connection.");
                case ConnectionClosedReason.InvalidData:
                    throw new Exception("Connection failed: The remote user sent us invalid data..");
                case ConnectionClosedReason.InvalidMessage:
                    throw new Exception("Connection failed: The remote user sent us an invalid message.");
                case ConnectionClosedReason.NegotiationFailed:
                    throw new Exception("Connection failed: Negotiation failed.");
                case ConnectionClosedReason.TimedOut:
                    throw new Exception("Connection failed: Timeout.");
                case ConnectionClosedReason.TooManyConnections:
                    throw new Exception("Connection failed: Too many connections.");
                case ConnectionClosedReason.UnexpectedError:
                    throw new Exception("Unexpected Error, connection will be closed");
                case ConnectionClosedReason.Unknown:
                default:
                    throw new Exception("Unknown Error, connection has been closed.");
            }
        }

        protected void SendInternal(ProductUserId target, InternalMessages type) =>
            EOSSDKComponent.EOS.GetP2PInterface().SendPacket(new SendPacketOptions() {
                AllowDelayedDelivery = true,
                Channel = (byte)internal_ch,
                Data = new byte[] { (byte) type },
                LocalUserId = EOSSDKComponent.localUserProductId,
                Reliability = PacketReliability.ReliableOrdered,
                RemoteUserId = target,
                SocketId = new SocketId() { SocketName = SOCKET_ID }
            });


        protected void Send(ProductUserId host, byte[] msgBuffer, byte channel) =>
            EOSSDKComponent.EOS.GetP2PInterface().SendPacket(new SendPacketOptions() {
                AllowDelayedDelivery = true,
                Channel = channel,
                Data = msgBuffer,
                LocalUserId = EOSSDKComponent.localUserProductId,
                Reliability = channels[channel],
                RemoteUserId = host,
                SocketId = new SocketId() { SocketName = SOCKET_ID }
            });


        private bool Receive(out ProductUserId clientProductUserId, out byte[] receiveBuffer, byte channel) {

            SocketId socketId = new SocketId();

            Result result = EOSSDKComponent.EOS.GetP2PInterface().ReceivePacket(new ReceivePacketOptions() {
                LocalUserId = EOSSDKComponent.localUserProductId,
                MaxDataSizeBytes = P2PInterface.MaxPacketSize,
                RequestedChannel = channel
            }, out clientProductUserId, out socketId, out channel, out receiveBuffer);

            if(result == Result.Success) {
                return true;
            }

            receiveBuffer = null;
            clientProductUserId = null;
            return false;
        }

        protected void CloseP2PSessionWithUser(ProductUserId clientUserID) => EOSSDKComponent.EOS.GetP2PInterface().CloseConnection(
            new CloseConnectionOptions() {
                LocalUserId = EOSSDKComponent.localUserProductId,
                RemoteUserId = clientUserID,
                SocketId = new SocketId() { SocketName = SOCKET_ID}
            });


        protected void WaitForClose(ProductUserId clientUserID) => transport.StartCoroutine(DelayedClose(clientUserID));
        private IEnumerator DelayedClose(ProductUserId clientUserID) {
            yield return null;
            CloseP2PSessionWithUser(clientUserID);
        }

        public void ReceiveData() {
            try {
                while (transport.enabled && Receive(out ProductUserId clientUserID, out byte[] internalMessage, (byte)internal_ch)) {
                    if (internalMessage.Length == 1) {
                        OnReceiveInternalData((InternalMessages) internalMessage[0], clientUserID);
                        return; // Wait one frame
                    } else {
                        Debug.Log("Incorrect package length on internal channel.");
                    }
                }

                for (int chNum = 0; chNum < channels.Length; chNum++) {
                    while (transport.enabled && Receive(out ProductUserId clientSteamID, out byte[] receiveBuffer, (byte)chNum)) {
                        OnReceiveData(receiveBuffer, clientSteamID, chNum);
                    }
                }

            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        protected abstract void OnReceiveInternalData(InternalMessages type, ProductUserId clientUserID);
        protected abstract void OnReceiveData(byte[] data, ProductUserId clientUserID, int channel);
        protected abstract void OnConnectionFailed(ProductUserId remoteId);
    }
}