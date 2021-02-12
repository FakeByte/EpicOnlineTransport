using EpicTransport;
using Epic.OnlineServices.Lobby;
using UnityEngine;
using Epic.OnlineServices;
using System.Collections.Generic;

public class EOSLobby : MonoBehaviour
{
    [HideInInspector]
    public bool ConnectedToLobby = false;
    public LobbyDetails ConnectedLobbyDetails = new LobbyDetails();

    public static string DefaultAttributeKey = "default";
    private static string hostAddressKey = "host_address";

    private string currentLobbyId = string.Empty;
    private bool isLobbyOwner = false;

    //events
    public delegate void JoinLobbyHandler(List<Attribute> attributes);
    public event JoinLobbyHandler JoinLobbyComplete;

    public delegate void FindLobbiesHandler(List<LobbyDetails> foundLobbies);
    public event FindLobbiesHandler FindLobbiesComplete;

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

            //if the result of CreateLobby is not successful, log and error and return
            if(callback.ResultCode != Result.Success)
            {
                Debug.LogError("There was an error while creating a lobby. Error: " + callback.ResultCode);
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
            EOSSDKComponent.GetLobbyInterface().UpdateLobby(new UpdateLobbyOptions { LobbyModificationHandle = modHandle }, null, (UpdateLobbyCallbackInfo _) => { });

            isLobbyOwner = true;
            ConnectedToLobby = true;
            EOSSDKComponent.GetLobbyInterface().CopyLobbyDetailsHandle(new CopyLobbyDetailsHandleOptions { LobbyId = callback.LobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId }, out ConnectedLobbyDetails);
            currentLobbyId = callback.LobbyId;

            //invoke event
            JoinLobbyComplete.Invoke(lobbyReturnData);
        });
    }

    //find lobbies
    public virtual void FindLobbies(uint maxResults, LobbySearchSetParameterOptions[] lobbySearchSetParameterOptions)
    {
        //create search handle and list of lobby details
        LobbySearch search = new LobbySearch();

        //set the search handle
        EOSSDKComponent.GetLobbyInterface().CreateLobbySearch(new CreateLobbySearchOptions { MaxResults =  maxResults}, out search);

        //set search parameters
        foreach (LobbySearchSetParameterOptions searchOption in lobbySearchSetParameterOptions)
        {
            search.SetParameter(searchOption);
        }
        
        //find lobbies
        search.Find(new LobbySearchFindOptions { LocalUserId = EOSSDKComponent.LocalUserProductId }, null, (LobbySearchFindCallbackInfo callback) => 
        {
            List<LobbyDetails> foundLobbies = new List<LobbyDetails>();

            //if the search was unsuccessful, log an error and return
            if (callback.ResultCode != Result.Success)
            {
                Debug.LogError("There was an error while finding lobbies. Error: " + callback.ResultCode);
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
            FindLobbiesComplete.Invoke(foundLobbies);
        });
    }

    //join lobby
    public virtual void JoinLobby(LobbyDetails lobbyToJoin, string[] attributeKeys, bool presenceEnabled = false)
    {
        //join lobby
        EOSSDKComponent.GetLobbyInterface().JoinLobby(new JoinLobbyOptions { LobbyDetailsHandle = lobbyToJoin, LocalUserId = EOSSDKComponent.LocalUserProductId, PresenceEnabled = presenceEnabled }, null, (JoinLobbyCallbackInfo callback) => 
        {
            List<Attribute> lobbyData = new List<Attribute>();

            //if the result was not a success, log error and return
            if(callback.ResultCode != Result.Success)
            {
                Debug.LogError("There was an error while joining a lobby. Error: " + callback.ResultCode);
                return;
            }

            Attribute hostAddress = new Attribute();
            lobbyToJoin.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = hostAddressKey }, out hostAddress);
            lobbyData.Add(hostAddress);

            foreach (string key in attributeKeys)
            {
                Attribute attribute = new Attribute();
                lobbyToJoin.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = key }, out attribute);
                lobbyData.Add(attribute);
            }

            isLobbyOwner = false;
            ConnectedToLobby = true;
            EOSSDKComponent.GetLobbyInterface().CopyLobbyDetailsHandle(new CopyLobbyDetailsHandleOptions { LobbyId = callback.LobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId }, out ConnectedLobbyDetails);
            currentLobbyId = callback.LobbyId;

            //invoke event
            JoinLobbyComplete.Invoke(lobbyData);
        });
    }

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
                    Debug.LogError("There was an error while destroying the lobby. Error: " + callback.ResultCode);
                    return;
                }

                ConnectedToLobby = false;
            });
        }
        //if we are a member of the lobby
        else
        {
            EOSSDKComponent.GetLobbyInterface().LeaveLobby(new LeaveLobbyOptions { LobbyId = currentLobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId }, null, (LeaveLobbyCallbackInfo callback) => 
            {
                //if the result was not a success, log error and return
                if (callback.ResultCode != Result.Success)
                {
                    Debug.LogError("There was an error while leaving the lobby. Error: " + callback.ResultCode);
                    return;
                }

                ConnectedToLobby = false;
            });
        }
    }
}
