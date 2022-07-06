
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EpicTransport {
    public abstract class Common {

        private PacketReliability[] channels;
        private int internal_ch => channels.Length;

        protected enum InternalMessages : byte {
            CONNECT,
            ACCEPT_CONNECT,
            DISCONNECT
        }

        protected struct PacketKey {
            public ProductUserId productUserId;
            public byte channel;
        }

        private OnIncomingConnectionRequestCallback OnIncomingConnectionRequest;
        ulong incomingNotificationId = 0;
        private OnRemoteConnectionClosedCallback OnRemoteConnectionClosed;
        ulong outgoingNotificationId = 0;

        protected readonly EosTransport transport;

        protected List<string> deadSockets;
        public bool ignoreAllMessages = false;

        // Mapping from PacketKey to a List of Packet Lists
        protected Dictionary<PacketKey, List<List<Packet>>> incomingPackets = new Dictionary<PacketKey, List<List<Packet>>>();

        protected Common(EosTransport transport) {
            channels = transport.Channels;

            deadSockets = new List<string>();

            AddNotifyPeerConnectionRequestOptions addNotifyPeerConnectionRequestOptions = new AddNotifyPeerConnectionRequestOptions();
            addNotifyPeerConnectionRequestOptions.LocalUserId = EOSSDKComponent.LocalUserProductId;
            addNotifyPeerConnectionRequestOptions.SocketId = null;

            OnIncomingConnectionRequest += OnNewConnection;
            OnRemoteConnectionClosed += OnConnectFail;

            incomingNotificationId = EOSSDKComponent.GetP2PInterface().AddNotifyPeerConnectionRequest(ref addNotifyPeerConnectionRequestOptions,
                null, OnIncomingConnectionRequest);

            AddNotifyPeerConnectionClosedOptions addNotifyPeerConnectionClosedOptions = new AddNotifyPeerConnectionClosedOptions();
            addNotifyPeerConnectionClosedOptions.LocalUserId = EOSSDKComponent.LocalUserProductId;
            addNotifyPeerConnectionClosedOptions.SocketId = null;

            outgoingNotificationId = EOSSDKComponent.GetP2PInterface().AddNotifyPeerConnectionClosed(ref addNotifyPeerConnectionClosedOptions,
                null, OnRemoteConnectionClosed);

            if (outgoingNotificationId == 0 || incomingNotificationId == 0) {
                Debug.LogError("Couldn't bind notifications with P2P interface");
            }

            incomingPackets = new Dictionary<PacketKey, List<List<Packet>>>();

            this.transport = transport;

        }

        protected void Dispose() {
            EOSSDKComponent.GetP2PInterface().RemoveNotifyPeerConnectionRequest(incomingNotificationId);
            EOSSDKComponent.GetP2PInterface().RemoveNotifyPeerConnectionClosed(outgoingNotificationId);

            transport.ResetIgnoreMessagesAtStartUpTimer();
        }

        protected abstract void OnNewConnection(ref OnIncomingConnectionRequestInfo result);

        private void OnConnectFail(ref OnRemoteConnectionClosedInfo result) {
            if (ignoreAllMessages) {
                return;
            }

            OnConnectionFailed(result.RemoteUserId);

            switch (result.Reason) {
                case ConnectionClosedReason.ClosedByLocalUser:
                    Debug.Log("Connection closed: The Connection was gracefully closed by the local user.");
                    break;
                case ConnectionClosedReason.ClosedByPeer:
                    Debug.Log("Connection closed: The connection was gracefully closed by remote user.");
                    break;
                case ConnectionClosedReason.ConnectionClosed:
                    Debug.LogWarning("Connection closed: The connection was unexpectedly closed.");
                    break;
                case ConnectionClosedReason.ConnectionFailed:
                    Debug.LogError("Connection failed: Failed to establish connection.");
                    break;
                case ConnectionClosedReason.InvalidData:
                    Debug.LogError("Connection failed: The remote user sent us invalid data..");
                    break;
                case ConnectionClosedReason.InvalidMessage:
                    Debug.LogError("Connection failed: The remote user sent us an invalid message.");
                    break;
                case ConnectionClosedReason.NegotiationFailed:
                    Debug.LogError("Connection failed: Negotiation failed.");
                    break;
                case ConnectionClosedReason.TimedOut:
                    Debug.LogError("Connection failed: Timeout.");
                    break;
                case ConnectionClosedReason.TooManyConnections:
                    Debug.LogError("Connection failed: Too many connections.");
                    break;
                case ConnectionClosedReason.UnexpectedError:
                    Debug.LogError("Unexpected Error, connection will be closed");
                    break;
                case ConnectionClosedReason.Unknown:
                default:
                    Debug.LogError("Unknown Error, connection has been closed.");
                    break;
            }
        }

        protected void SendInternal(ProductUserId target, SocketId socketId, InternalMessages type) {
            var sendPacketOptions = new SendPacketOptions() {
                AllowDelayedDelivery = true,
                Channel = (byte)internal_ch,
                Data = new ArraySegment<byte>(new byte[] { (byte)type }),
                LocalUserId = EOSSDKComponent.LocalUserProductId,
                Reliability = PacketReliability.ReliableOrdered,
                RemoteUserId = target,
                SocketId = socketId
            };
            EOSSDKComponent.GetP2PInterface().SendPacket(ref sendPacketOptions);
        }


        protected void Send(ProductUserId host, SocketId socketId, byte[] msgBuffer, byte channel) {
            var sendPacketOptions = new SendPacketOptions() {
                AllowDelayedDelivery = true,
                Channel = channel,
                Data = new ArraySegment<byte>(msgBuffer),
                LocalUserId = EOSSDKComponent.LocalUserProductId,
                Reliability = channels[channel],
                RemoteUserId = host,
                SocketId = socketId
            };
            Result result = EOSSDKComponent.GetP2PInterface().SendPacket(ref sendPacketOptions);

            if(result != Result.Success) {
                Debug.LogError("Send failed " + result);
            }
        }

        private bool Receive(out ProductUserId clientProductUserId, out SocketId socketId, out byte[] receiveBuffer, byte channel) {
            var receivePacketOptions = new ReceivePacketOptions() {
                LocalUserId = EOSSDKComponent.LocalUserProductId,
                MaxDataSizeBytes = P2PInterface.MaxPacketSize,
                RequestedChannel = channel
            };
            
            var getNextReceivedPacketSizeOptions = new GetNextReceivedPacketSizeOptions() {
                LocalUserId = EOSSDKComponent.LocalUserProductId,
                RequestedChannel = channel
            };
            Result getPacketSizeResult = EOSSDKComponent.GetP2PInterface()
                .GetNextReceivedPacketSize(ref getNextReceivedPacketSizeOptions, out var packetSize);

            if (getPacketSizeResult != Result.Success) {
                receiveBuffer = null;
                clientProductUserId = null;
                socketId = default;
                return false;
            }
            
            uint bytesWritten = 0;
            receiveBuffer = new byte[packetSize];
            Result result = EOSSDKComponent.GetP2PInterface().ReceivePacket(
                ref receivePacketOptions, 
                out clientProductUserId, 
                out socketId, 
                out channel, 
                new ArraySegment<byte>(receiveBuffer),
                out bytesWritten);

            if (result == Result.Success) {
                return true;
            }

            receiveBuffer = null;
            clientProductUserId = null;
            return false;
        }

        protected virtual void CloseP2PSessionWithUser(ProductUserId clientUserID, SocketId socketId) {
            if (socketId.Equals(default(SocketId))) {
                Debug.LogWarning("Socket ID == null | " + ignoreAllMessages);
                return;
            }

            if (deadSockets == null) {
                Debug.LogWarning("DeadSockets == null");
                return;
            }

            if (deadSockets.Contains(socketId.SocketName)) {
                return;
            } else {
                deadSockets.Add(socketId.SocketName);
            }
        }


        protected void WaitForClose(ProductUserId clientUserID, SocketId socketId) => transport.StartCoroutine(DelayedClose(clientUserID, socketId));
        private IEnumerator DelayedClose(ProductUserId clientUserID, SocketId socketId) {
            yield return null;
            CloseP2PSessionWithUser(clientUserID, socketId);
        }

        public void ReceiveData() {
            try {
                // Internal Channel, no fragmentation here
                SocketId socketId = new SocketId();
                while (transport.enabled && Receive(out ProductUserId clientUserID, out socketId, out byte[] internalMessage, (byte) internal_ch)) {
                    if (internalMessage.Length == 1) {
                        OnReceiveInternalData((InternalMessages) internalMessage[0], clientUserID, socketId);
                        return; // Wait one frame
                    } else {
                        Debug.Log("Incorrect package length on internal channel.");
                    }
                }

                // Insert new packet at the correct location in the incoming queue
                for (int chNum = 0; chNum < channels.Length; chNum++) {
                    while (transport.enabled && Receive(out ProductUserId clientUserID, out socketId, out byte[] receiveBuffer, (byte) chNum)) {
                        PacketKey incomingPacketKey = new PacketKey();
                        incomingPacketKey.productUserId = clientUserID;
                        incomingPacketKey.channel = (byte)chNum;

                        Packet packet = new Packet();
                        packet.FromBytes(receiveBuffer);

                        if (!incomingPackets.ContainsKey(incomingPacketKey)) {
                            incomingPackets.Add(incomingPacketKey, new List<List<Packet>>());
                        }

                        int packetListIndex = incomingPackets[incomingPacketKey].Count;
                        for(int i = 0; i < incomingPackets[incomingPacketKey].Count; i++) {
                            if(incomingPackets[incomingPacketKey][i][0].id == packet.id) {
                                packetListIndex = i;
                                break;
                            }
                        }
                        
                        if (packetListIndex == incomingPackets[incomingPacketKey].Count) {
                            incomingPackets[incomingPacketKey].Add(new List<Packet>());
                        }

                        int insertionIndex = -1;

                        for (int i = 0; i < incomingPackets[incomingPacketKey][packetListIndex].Count; i++) {
                            if (incomingPackets[incomingPacketKey][packetListIndex][i].fragment > packet.fragment) {
                                insertionIndex = i;
                                break;
                            }
                        }

                        if (insertionIndex >= 0) {
                            incomingPackets[incomingPacketKey][packetListIndex].Insert(insertionIndex, packet);
                        } else {
                            incomingPackets[incomingPacketKey][packetListIndex].Add(packet);
                        }
                    }
                }

                // Find fully received packets
                List<List<Packet>> emptyPacketLists = new List<List<Packet>>();
                foreach(KeyValuePair<PacketKey, List<List<Packet>>> keyValuePair in incomingPackets) {
                    for(int packetList = 0; packetList < keyValuePair.Value.Count; packetList++) {
                        bool packetReady = true;
                        int packetLength = 0;
                        for (int packet = 0; packet < keyValuePair.Value[packetList].Count; packet++) {
                            Packet tempPacket = keyValuePair.Value[packetList][packet];
                            if (tempPacket.fragment != packet || (packet == keyValuePair.Value[packetList].Count - 1 && tempPacket.moreFragments)) {
                                packetReady = false;
                            } else {
                                packetLength += tempPacket.data.Length;
                            }
                        }

                        if (packetReady) {
                            byte[] data = new byte[packetLength];
                            int dataIndex = 0;

                            for (int packet = 0; packet < keyValuePair.Value[packetList].Count; packet++) {
                                Array.Copy(keyValuePair.Value[packetList][packet].data, 0, data, dataIndex, keyValuePair.Value[packetList][packet].data.Length);
                                dataIndex += keyValuePair.Value[packetList][packet].data.Length;
                            }

                            OnReceiveData(data, keyValuePair.Key.productUserId, keyValuePair.Key.channel);

                            if(transport.ServerActive() || transport.ClientActive())
                                emptyPacketLists.Add(keyValuePair.Value[packetList]);
                        }
                    }

                    for (int i = 0; i < emptyPacketLists.Count; i++) {
                        keyValuePair.Value.Remove(emptyPacketLists[i]);
                    }
                    emptyPacketLists.Clear();
                }



            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        protected abstract void OnReceiveInternalData(InternalMessages type, ProductUserId clientUserID, SocketId socketId);
        protected abstract void OnReceiveData(byte[] data, ProductUserId clientUserID, int channel);
        protected abstract void OnConnectionFailed(ProductUserId remoteId);
    }
}