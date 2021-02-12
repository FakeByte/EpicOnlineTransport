using EpicTransport;
using Epic.OnlineServices.Lobby;
using System.Collections;
using UnityEngine;
using Epic.OnlineServices;
using System.Collections.Generic;

public class EOSLobby : MonoBehaviour
{
    private static string defaultAttributeKey = "default";
    private static string hostAddressKey = string.Empty;

    private string currentLobbyId = string.Empty;
    private bool isOwner = false;

    //creates a lobby
    private void StartLobby(uint maxConnections, LobbyPermissionLevel permissionLevel, bool presenceEnabled, AttributeData[] lobbyData = null)
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
            //if the result of CreateLobby is not successful, log and error and return
            if(callback.ResultCode != Result.Success)
            {
                Debug.LogError("There was an error while creating a lobby. Error: " + callback.ResultCode);
                return;
            }

            //create mod handle and lobby data
            LobbyModification modHandle = new LobbyModification();
            AttributeData defaultData = new AttributeData { Key = defaultAttributeKey, Value = defaultAttributeKey };
            AttributeData hostAddressData = new AttributeData { Key = hostAddressKey, Value = EOSSDKComponent.LocalUserProductIdString };

            //set the mod handle
            EOSSDKComponent.GetLobbyInterface().UpdateLobbyModification(new UpdateLobbyModificationOptions 
            { LobbyId = callback.LobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId }, out modHandle);

            //add attributes
            modHandle.AddAttribute(new LobbyModificationAddAttributeOptions { Attribute = defaultData, Visibility = LobbyAttributeVisibility.Public });
            modHandle.AddAttribute(new LobbyModificationAddAttributeOptions { Attribute = hostAddressData, Visibility = LobbyAttributeVisibility.Public });

            //add user attributes
            if(lobbyData.Length != 0)
            {
                foreach (AttributeData data in lobbyData)
                {
                    modHandle.AddAttribute(new LobbyModificationAddAttributeOptions { Attribute = data, Visibility = LobbyAttributeVisibility.Public });
                }
            }

            //update the lobby
            EOSSDKComponent.GetLobbyInterface().UpdateLobby(new UpdateLobbyOptions { LobbyModificationHandle = modHandle }, null, (UpdateLobbyCallbackInfo _) => { });

            currentLobbyId = callback.LobbyId;
            isOwner = true;
        });
    }

    //find lobbies
    private List<LobbyDetails> FindLobbies(uint maxResults, LobbySearchSetParameterOptions[] lobbySearchSetParameterOptions)
    {
        //create search handle and list of lobby details
        LobbySearch search = new LobbySearch();
        List<LobbyDetails> details = new List<LobbyDetails>();

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
            //if the search was unsuccessful, log an error and return
            if(callback.ResultCode != Result.Success)
            {
                Debug.LogError("There was an error while finding lobbies. Error: " + callback.ResultCode);
                return;
            }

            //for each lobby found, add data to details
            for (int i = 0; i < details.Count; i++)
            {
                LobbyDetails lobbyInformation;
                search.CopySearchResultByIndex(new LobbySearchCopySearchResultByIndexOptions { }, out lobbyInformation);
                details.Add(lobbyInformation);
            }
        });
        
        return details;
    }

    //join lobby
    private List<Attribute> JoinLobby(LobbyDetails lobbyToJoin, bool presenceEnabled = false)
    {
        List<Attribute> dataToReturn = new List<Attribute>();

        //join lobby
        EOSSDKComponent.GetLobbyInterface().JoinLobby(new JoinLobbyOptions { LobbyDetailsHandle = lobbyToJoin, LocalUserId = EOSSDKComponent.LocalUserProductId, PresenceEnabled = presenceEnabled }, null, (JoinLobbyCallbackInfo callback) => 
        {
            //if the result was not a success, log error and return
            if(callback.ResultCode != Result.Success)
            {
                Debug.LogError("There was an error while joining a lobby. Error: " + callback.ResultCode);
                return;
            }

            //TODO
            //add other data that host may have added in StartLobby()
            Attribute hostAddress = new Attribute();
            lobbyToJoin.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { }, out hostAddress);
            dataToReturn.Add(hostAddress);

            currentLobbyId = callback.LobbyId;
        });

        isOwner = false;
        return dataToReturn;
    }

    //leave lobby
    private void LeaveLobby()
    {
        //if we are the owner of the lobby
        if(isOwner)
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
            });
        }
    }
}
