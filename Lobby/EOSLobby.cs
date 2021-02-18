using EpicTransport;
using Epic.OnlineServices.Lobby;
using UnityEngine;
using Epic.OnlineServices;
using System.Collections.Generic;

public class EOSLobby : MonoBehaviour
{
    [HideInInspector] public bool ConnectedToLobby { get; private set; }
    public LobbyDetails ConnectedLobbyDetails = new LobbyDetails();

    [SerializeField] public string[] AttributeKeys = new string[0];

    public const string DefaultAttributeKey = "default";
    private const string hostAddressKey = "host_address";

    private string currentLobbyId = string.Empty;
    private bool isLobbyOwner = false;
    private List<LobbyDetails> foundLobbies = new List<LobbyDetails>();
    private List<Attribute> lobbyData = new List<Attribute>();

    //create lobby events
    public delegate void CreateLobbySuccess(List<Attribute> attributes);
    public event CreateLobbySuccess CreateLobbySucceeded;

    public delegate void CreateLobbyFailure(string errorMessage);
    public event CreateLobbyFailure CreateLobbyFailed;

    //join lobby events
    public delegate void JoinLobbySuccess(List<Attribute> attributes);
    public event JoinLobbySuccess JoinLobbySucceeded;

    public delegate void JoinLobbyFailure(string errorMessage);
    public event JoinLobbyFailure JoinLobbyFailed;

    //find lobby events
    public delegate void FindLobbiesSuccess(List<LobbyDetails> foundLobbies);
    public event FindLobbiesSuccess FindLobbiesSucceeded;

    public delegate void FindLobbiesFailure(string errorMessage);
    public event FindLobbiesFailure FindLobbiesFailed;

    //leave lobby events
    public delegate void LeaveLobbySuccess();
    public event LeaveLobbySuccess LeaveLobbySucceeded;

    public delegate void LeaveLobbyFailure(string errorMessage);
    public event LeaveLobbyFailure LeaveLobbyFailed;

    //lobby update events
    private ulong lobbyMemberStatusNotifyId = 0;
    private ulong lobbyAttributeUpdateNotifyId = 0;

    public delegate void LobbyMemberStatusUpdate(LobbyMemberStatusReceivedCallbackInfo callback);
    public event LobbyMemberStatusUpdate LobbyMemberStatusUpdated;

    public delegate void LobbyAttributeUpdate(LobbyUpdateReceivedCallbackInfo callback);
    public event LobbyAttributeUpdate LobbyAttributeUpdated;

    public virtual void Start()
    {
        lobbyMemberStatusNotifyId = EOSSDKComponent.GetLobbyInterface().AddNotifyLobbyMemberStatusReceived(new AddNotifyLobbyMemberStatusReceivedOptions { }, null,
        (LobbyMemberStatusReceivedCallbackInfo callback) =>
        {
            if (LobbyMemberStatusUpdated != null)
            {
                LobbyMemberStatusUpdated.Invoke(callback);
            }

            if (callback.CurrentStatus == LobbyMemberStatus.Closed)
            {
                LeaveLobby();
            }
        });

        lobbyAttributeUpdateNotifyId = EOSSDKComponent.GetLobbyInterface().AddNotifyLobbyUpdateReceived(new AddNotifyLobbyUpdateReceivedOptions { }, null,
        (LobbyUpdateReceivedCallbackInfo callback) =>
        {
            if (LobbyAttributeUpdated != null)
            {
                LobbyAttributeUpdated.Invoke(callback);
            }
        });
    }

    public virtual void OnDisable()
    {
        EOSSDKComponent.GetLobbyInterface().RemoveNotifyLobbyMemberStatusReceived(lobbyMemberStatusNotifyId);
        EOSSDKComponent.GetLobbyInterface().RemoveNotifyLobbyUpdateReceived(lobbyAttributeUpdateNotifyId);
    }

    //creates a lobby
    public virtual void CreateLobby(uint maxConnections, LobbyPermissionLevel permissionLevel, bool presenceEnabled, AttributeData[] lobbyData = null)
    {
        EOSSDKComponent.GetLobbyInterface().CreateLobby(new CreateLobbyOptions 
        { 
            //lobby options
            LocalUserId = EOSSDKComponent.LocalUserProductId, 
            MaxLobbyMembers = maxConnections,
            PermissionLevel = permissionLevel,
            PresenceEnabled = presenceEnabled,
        }, null, (CreateLobbyCallbackInfo callback) => 
        {
            List<Attribute> lobbyReturnData = new List<Attribute>();

            //if the result of CreateLobby is not successful, invoke an error event and return
            if (callback.ResultCode != Result.Success)
            {
                if(CreateLobbyFailed != null)
                {
                    CreateLobbyFailed.Invoke("There was an error while creating a lobby. Error: " + callback.ResultCode);
                }
                return;
            }

            //create mod handle and lobby data
            LobbyModification modHandle = new LobbyModification();
            AttributeData defaultData = new AttributeData { Key = DefaultAttributeKey, Value = DefaultAttributeKey };
            AttributeData hostAddressData = new AttributeData { Key = hostAddressKey, Value = EOSSDKComponent.LocalUserProductIdString };

            //set the mod handle
            EOSSDKComponent.GetLobbyInterface().UpdateLobbyModification(new UpdateLobbyModificationOptions 
            { LobbyId = callback.LobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId }, out modHandle);

            //add attributes
            modHandle.AddAttribute(new LobbyModificationAddAttributeOptions { Attribute = defaultData, Visibility = LobbyAttributeVisibility.Public });
            modHandle.AddAttribute(new LobbyModificationAddAttributeOptions { Attribute = hostAddressData, Visibility = LobbyAttributeVisibility.Public });

            //add user attributes
            if (lobbyData != null)
            {
                foreach (AttributeData data in lobbyData)
                {
                    modHandle.AddAttribute(new LobbyModificationAddAttributeOptions { Attribute = data, Visibility = LobbyAttributeVisibility.Public });
                    lobbyReturnData.Add(new Attribute { Data = data, Visibility = LobbyAttributeVisibility.Public });
                }
            }

            //update the lobby
            EOSSDKComponent.GetLobbyInterface().UpdateLobby(new UpdateLobbyOptions { LobbyModificationHandle = modHandle }, null, (UpdateLobbyCallbackInfo updateCallback) => 
            {
                //if there was an error while updating the lobby, invoke an error event and return
                if (updateCallback.ResultCode != Result.Success)
                {
                    if(CreateLobbyFailed != null)
                    {
                        CreateLobbyFailed.Invoke("There was an error while updating the lobby. Error: " + updateCallback.ResultCode);
                    }
                    return;
                }

                isLobbyOwner = true;
                ConnectedToLobby = true;
                EOSSDKComponent.GetLobbyInterface().CopyLobbyDetailsHandle(new CopyLobbyDetailsHandleOptions { LobbyId = callback.LobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId }, out ConnectedLobbyDetails);
                currentLobbyId = callback.LobbyId;

                //invoke event
                if(CreateLobbySucceeded != null)
                {
                    CreateLobbySucceeded.Invoke(lobbyReturnData);
                }
            });
        });
    }

    //find lobbies
    public virtual void FindLobbies(uint maxResults = 100, LobbySearchSetParameterOptions[] lobbySearchSetParameterOptions = null)
    {
        //create search handle and list of lobby details
        LobbySearch search = new LobbySearch();

        //set the search handle
        EOSSDKComponent.GetLobbyInterface().CreateLobbySearch(new CreateLobbySearchOptions { MaxResults =  maxResults}, out search);

        //set search parameters
        if(lobbySearchSetParameterOptions != null)
        {
            foreach (LobbySearchSetParameterOptions searchOption in lobbySearchSetParameterOptions)
            {
                search.SetParameter(searchOption);
            }
        }
        else
        {
            search.SetParameter(new LobbySearchSetParameterOptions
            {
                ComparisonOp = ComparisonOp.Equal,
                Parameter = new AttributeData { Key = DefaultAttributeKey, Value = DefaultAttributeKey }
            });
        }
        
        //find lobbies
        search.Find(new LobbySearchFindOptions { LocalUserId = EOSSDKComponent.LocalUserProductId }, null, (LobbySearchFindCallbackInfo callback) => 
        {
            //if the search was unsuccessful, invoke an error event and return
            if (callback.ResultCode != Result.Success)
            {
                if (FindLobbiesFailed != null)
                {
                    FindLobbiesFailed.Invoke("There was an error while finding lobbies. Error: " + callback.ResultCode);
                }
                return;
            }

            foundLobbies.Clear();

            //for each lobby found, add data to details
            for (int i = 0; i < search.GetSearchResultCount(new LobbySearchGetSearchResultCountOptions { }); i++)
            {
                LobbyDetails lobbyInformation;
                search.CopySearchResultByIndex(new LobbySearchCopySearchResultByIndexOptions { LobbyIndex = (uint)i }, out lobbyInformation);
                foundLobbies.Add(lobbyInformation);
            }

            //invoke event
            if(FindLobbiesSucceeded != null)
            {
                FindLobbiesSucceeded.Invoke(foundLobbies);
            }
        });
    }

    //join lobby
    public virtual void JoinLobby(LobbyDetails lobbyToJoin, string[] attributeKeys = null, bool presenceEnabled = false)
    {
        //join lobby
        EOSSDKComponent.GetLobbyInterface().JoinLobby(new JoinLobbyOptions { LobbyDetailsHandle = lobbyToJoin, LocalUserId = EOSSDKComponent.LocalUserProductId, PresenceEnabled = presenceEnabled }, null, (JoinLobbyCallbackInfo callback) => 
        {
            //if the result was not a success, invoke an error event and return
            if (callback.ResultCode != Result.Success)
            {
                if(JoinLobbyFailed != null)
                {
                    JoinLobbyFailed.Invoke("There was an error while joining a lobby. Error: " + callback.ResultCode);
                }
                return;
            }

            lobbyData.Clear();

            Attribute hostAddress = new Attribute();
            lobbyToJoin.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = hostAddressKey }, out hostAddress);
            lobbyData.Add(hostAddress);

            if(attributeKeys != null)
            {
                foreach (string key in attributeKeys)
                {
                    Attribute attribute = new Attribute();
                    lobbyToJoin.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = key }, out attribute);
                    lobbyData.Add(attribute);
                }
            }

            isLobbyOwner = false;
            ConnectedToLobby = true;
            EOSSDKComponent.GetLobbyInterface().CopyLobbyDetailsHandle(new CopyLobbyDetailsHandleOptions { LobbyId = callback.LobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId }, out ConnectedLobbyDetails);
            currentLobbyId = callback.LobbyId;

            //invoke event
            if (JoinLobbySucceeded != null)
            {
                JoinLobbySucceeded.Invoke(lobbyData);
            }
        });
    }

    //TODO: Needs to be implemented
    //public virtual void JoinLobby(string lobbyId, string[] attributeKeys = null, bool presenceEnabled = false)
    //{
    //    //join lobby
    //    EOSSDKComponent.GetLobbyInterface().JoinLobby(new JoinLobbyOptions { LobbyDetailsHandle = lobbyToJoin, LocalUserId = EOSSDKComponent.LocalUserProductId, PresenceEnabled = presenceEnabled }, null, (JoinLobbyCallbackInfo callback) =>
    //    {
    //        List<Attribute> lobbyData = new List<Attribute>();

    //        //if the result was not a success, invoke an error event and return
    //        if (callback.ResultCode != Result.Success)
    //        {
    //            JoinLobbyFailed.Invoke("There was an error while joining a lobby. Error: " + callback.ResultCode);
    //            return;
    //        }

    //        Attribute hostAddress = new Attribute();
    //        lobbyToJoin.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = hostAddressKey }, out hostAddress);
    //        lobbyData.Add(hostAddress);

    //        if (attributeKeys != null)
    //        {
    //            foreach (string key in attributeKeys)
    //            {
    //                Attribute attribute = new Attribute();
    //                lobbyToJoin.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = key }, out attribute);
    //                lobbyData.Add(attribute);
    //            }
    //        }

    //        isLobbyOwner = false;
    //        ConnectedToLobby = true;
    //        EOSSDKComponent.GetLobbyInterface().CopyLobbyDetailsHandle(new CopyLobbyDetailsHandleOptions { LobbyId = callback.LobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId }, out ConnectedLobbyDetails);
    //        currentLobbyId = callback.LobbyId;

    //        //invoke event
    //        JoinLobbySucceeded.Invoke(lobbyData);
    //    });
    //}

    //leave lobby
    
    public virtual void LeaveLobby()
    {
        //if we are the owner of the lobby
        if(isLobbyOwner)
        {
            //Destroy lobby
            EOSSDKComponent.GetLobbyInterface().DestroyLobby(new DestroyLobbyOptions { LobbyId = currentLobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId }, null, (DestroyLobbyCallbackInfo callback) => 
            {
                //if the result was not a success, log error and return
                if (callback.ResultCode != Result.Success)
                {
                    if (LeaveLobbyFailed != null)
                    {
                        LeaveLobbyFailed.Invoke("There was an error while destroying the lobby. Error: " + callback.ResultCode);
                    }
                    return;
                }

                ConnectedToLobby = false;

                if(LeaveLobbySucceeded != null)
                {
                    LeaveLobbySucceeded.Invoke();
                }
            });
        }
        //if we are a member of the lobby
        else
        {
            EOSSDKComponent.GetLobbyInterface().LeaveLobby(new LeaveLobbyOptions { LobbyId = currentLobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId }, null, (LeaveLobbyCallbackInfo callback) => 
            {
                //if the result was not a success, log error and return
                if (callback.ResultCode != Result.Success && callback.ResultCode != Result.NotFound)
                {
                    if (LeaveLobbyFailed != null)
                    {
                        LeaveLobbyFailed.Invoke("There was an error while leaving the lobby. Error: " + callback.ResultCode);
                    }
                    return;
                }

                ConnectedToLobby = false;
                if (LeaveLobbySucceeded != null)
                {
                    LeaveLobbySucceeded.Invoke();
                }
            });
        }
    }
}
