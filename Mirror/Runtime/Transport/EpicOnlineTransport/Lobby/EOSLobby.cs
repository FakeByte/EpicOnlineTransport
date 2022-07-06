using EpicTransport;
using Epic.OnlineServices.Lobby;
using UnityEngine;
using Epic.OnlineServices;
using System.Collections.Generic;

public class EOSLobby : MonoBehaviour {
    ///<returns>True if the user is connected to a lobby.</returns>
    [HideInInspector] public bool ConnectedToLobby { get; private set; }
    ///<returns>The details of the lobby that the user is currently connected to.</returns>
    public LobbyDetails ConnectedLobbyDetails { get; private set; }

    ///<value>The available keys assigned by the user.</value>
    [SerializeField] public string[] AttributeKeys = new string[] { "lobby_name" };

    private const string DefaultAttributeKey = "default";
    public const string hostAddressKey = "host_address";

    private string currentLobbyId = string.Empty;
    private bool isLobbyOwner = false;
    private List<LobbyDetails> foundLobbies = new List<LobbyDetails>();
    private List<Attribute> lobbyData = new List<Attribute>();

    //create lobby events
    public delegate void CreateLobbySuccess(List<Attribute> attributes);
    /// <summary>When invoked, a message is sent to all subscribers with a list of <see cref="Attribute"/> that were assigned to the lobby. </summary>
    public event CreateLobbySuccess CreateLobbySucceeded;

    public delegate void CreateLobbyFailure(string errorMessage);
    /// <summary>When invoked, a message is sent to all subscribers with an error message. </summary>
    public event CreateLobbyFailure CreateLobbyFailed;

    //join lobby events
    public delegate void JoinLobbySuccess(List<Attribute> attributes);
    /// <summary>When invoked, a message is sent to all subscribers with a list of <see cref="Attribute"/> that were found when joining the lobby. </summary>
    public event JoinLobbySuccess JoinLobbySucceeded;

    public delegate void JoinLobbyFailure(string errorMessage);
    /// <summary>When invoked, a message is sent to all subscribers with an error message. </summary>
    public event JoinLobbyFailure JoinLobbyFailed;

    //find lobby events
    public delegate void FindLobbiesSuccess(List<LobbyDetails> foundLobbies);
    /// <summary>When invoked, a message is sent to all subscribers with a list of <see cref="LobbyDetails"/> that contains the found lobbies.</summary>
    public event FindLobbiesSuccess FindLobbiesSucceeded;

    public delegate void FindLobbiesFailure(string errorMessage);
    /// <summary>When invoked, a message is sent to all subscribers with an error message. </summary>
    public event FindLobbiesFailure FindLobbiesFailed;

    //leave lobby events
    public delegate void LeaveLobbySuccess();
    /// <summary>When invoked, an empty message is sent to all subscribers.</summary>
    public event LeaveLobbySuccess LeaveLobbySucceeded;

    public delegate void LeaveLobbyFailure(string errorMessage);
    /// <summary>When invoked, a message is sent to all subscribers with an error message. </summary>
    public event LeaveLobbyFailure LeaveLobbyFailed;

    //update attribute events
    public delegate void UpdateAttributeSuccess(string key);
    /// <summary>When invoked, a message is sent to all subscribers with the key of the attribute that was updated.</summary>
    public event UpdateAttributeSuccess AttributeUpdateSucceeded;

    public delegate void UpdateAttributeFailure(string key, string errorMessage);
    /// <summary>When invoked, a message is sent to all subscribers with the key of the attribute that wasn't updated and an error message. </summary>
    public event UpdateAttributeFailure AttributeUpdateFailed;

    //lobby update events
    private ulong lobbyMemberStatusNotifyId = 0;
    private ulong lobbyAttributeUpdateNotifyId = 0;

    public delegate void LobbyMemberStatusUpdate(LobbyMemberStatusReceivedCallbackInfo callback);
    /// <summary>When invoked, a message is sent to all subscribers with an update on member status.</summary>
    public event LobbyMemberStatusUpdate LobbyMemberStatusUpdated;

    public delegate void LobbyAttributeUpdate(LobbyUpdateReceivedCallbackInfo callback);
    /// <summary>When invoked, a message is sent to all subscribers with information on the lobby that was updated.</summary>
    public event LobbyAttributeUpdate LobbyAttributeUpdated;

    public virtual void Start() {
        var addNotifyLobbyMemberStatusReceivedOptions = new AddNotifyLobbyMemberStatusReceivedOptions { };
        lobbyMemberStatusNotifyId = EOSSDKComponent.GetLobbyInterface().AddNotifyLobbyMemberStatusReceived(ref addNotifyLobbyMemberStatusReceivedOptions, null,
        (ref LobbyMemberStatusReceivedCallbackInfo callback) => {
            LobbyMemberStatusUpdated?.Invoke(callback);

            if (callback.CurrentStatus == LobbyMemberStatus.Closed) {
                LeaveLobby();
            }
        });

        var addNotifyLobbyUpdateReceivedOptions = new AddNotifyLobbyUpdateReceivedOptions { };
        lobbyAttributeUpdateNotifyId = EOSSDKComponent.GetLobbyInterface().AddNotifyLobbyUpdateReceived(ref addNotifyLobbyUpdateReceivedOptions, null,
        (ref LobbyUpdateReceivedCallbackInfo callback) => {
            LobbyAttributeUpdated?.Invoke(callback);
        });
    }

    /// <summary>
    /// Creates a lobby based on given parameters using Epic Online Services.
    /// <para>You can get the data that was added to the lobby by subscribing to the <see cref="CreateLobbySucceeded"/> event which gives you a list of <see cref="Attribute"/>.</para>
    /// <para>This process may throw errors. You can get errors by subscribing to the <see cref="CreateLobbyFailed"/> event.</para>
    /// </summary>
    /// <param name="maxConnections">The maximum amount of connections the lobby allows.</param>
    /// <param name="permissionLevel">The restriction on the lobby to prevent unwanted people from joining.</param>
    /// <param name="presenceEnabled">Use Epic's overlay to display information to others.</param>
    /// <param name="lobbyData">Optional data that you can to the lobby. By default, there is an empty attribute for searching and an attribute which holds the host's network address.</param>
    public virtual void CreateLobby(uint maxConnections, LobbyPermissionLevel permissionLevel, bool presenceEnabled, AttributeData[] lobbyData = null) {

        var createLobbyOptions = new CreateLobbyOptions {
            //lobby options
            LocalUserId = EOSSDKComponent.LocalUserProductId,
            MaxLobbyMembers = maxConnections,
            PermissionLevel = permissionLevel,
            PresenceEnabled = presenceEnabled,
            BucketId = DefaultAttributeKey,
        };
        EOSSDKComponent.GetLobbyInterface().CreateLobby(ref createLobbyOptions, null, (ref CreateLobbyCallbackInfo callback) => {
            List<Attribute> lobbyReturnData = new List<Attribute>();

            //if the result of CreateLobby is not successful, invoke an error event and return
            if (callback.ResultCode != Result.Success) {
                CreateLobbyFailed?.Invoke("There was an error while creating a lobby. Error: " + callback.ResultCode);
                return;
            }

            //create mod handle and lobby data
            LobbyModification modHandle = new LobbyModification();
            AttributeData defaultData = new AttributeData { Key = DefaultAttributeKey, Value = DefaultAttributeKey };
            AttributeData hostAddressData = new AttributeData { Key = hostAddressKey, Value = EOSSDKComponent.LocalUserProductIdString };

            var updateLobbyModificationOptions = new UpdateLobbyModificationOptions
                { LobbyId = callback.LobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId };
            //set the mod handle
            EOSSDKComponent.GetLobbyInterface().UpdateLobbyModification(ref updateLobbyModificationOptions, out modHandle);

            //add attributes
            var defaultLobbyModificationAddAttributeOptions = new LobbyModificationAddAttributeOptions
                { Attribute = defaultData, Visibility = LobbyAttributeVisibility.Public };
            var lobbyModificationAddAttributeOptions = new LobbyModificationAddAttributeOptions
                { Attribute = hostAddressData, Visibility = LobbyAttributeVisibility.Public };
            
            modHandle.AddAttribute(ref defaultLobbyModificationAddAttributeOptions);
            modHandle.AddAttribute(ref lobbyModificationAddAttributeOptions);

            //add user attributes
            if (lobbyData != null) {
                foreach (AttributeData data in lobbyData) {
                    var options = new LobbyModificationAddAttributeOptions();
                    options.Attribute = data;
                    options.Visibility = LobbyAttributeVisibility.Public;
                    modHandle.AddAttribute(ref options);
                    lobbyReturnData.Add(new Attribute { Data = data, Visibility = LobbyAttributeVisibility.Public });
                }
            }

            var lobbyId = callback.LobbyId;
            
            //update the lobby
            var updateLobbyOptions = new UpdateLobbyOptions { LobbyModificationHandle = modHandle };
            EOSSDKComponent.GetLobbyInterface().UpdateLobby(ref updateLobbyOptions, null, (ref UpdateLobbyCallbackInfo updateCallback) => {

                //if there was an error while updating the lobby, invoke an error event and return
                if (updateCallback.ResultCode != Result.Success) {
                    CreateLobbyFailed?.Invoke("There was an error while updating the lobby. Error: " + updateCallback.ResultCode);
                    return;
                }

                LobbyDetails details;
                
                var copyLobbyDetailsHandleOptions = new CopyLobbyDetailsHandleOptions
                    { LobbyId = lobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId };
                EOSSDKComponent.GetLobbyInterface().CopyLobbyDetailsHandle(ref copyLobbyDetailsHandleOptions, out details);

                ConnectedLobbyDetails = details;
                isLobbyOwner = true;
                ConnectedToLobby = true;
                currentLobbyId = lobbyId;

                //invoke event
                CreateLobbySucceeded?.Invoke(lobbyReturnData);
            });
        });
    }

    /// <summary>
    /// Finds lobbies based on given parameters using Epic Online Services.
    /// <para>You can get the found lobbies by subscribing to the <see cref="FindLobbiesSucceeded"/> event which gives you a list of <see cref="LobbyDetails"/>.</para>
    /// <para>This process may throw errors. You can get errors by subscribing to the <see cref="FindLobbiesFailed"/> event.</para>
    /// </summary>
    /// <param name="maxResults">The maximum amount of results to return.</param>
    /// <param name="lobbySearchSetParameterOptions">The parameters to search by. If left empty, then the search will use the default attribute attached to all the lobbies.</param>
    public virtual void FindLobbies(uint maxResults = 100, LobbySearchSetParameterOptions[] lobbySearchSetParameterOptions = null) {
        //create search handle and list of lobby details
        LobbySearch search = new LobbySearch();

        //set the search handle
        var createLobbySearchOptions = new CreateLobbySearchOptions { MaxResults = maxResults };
        EOSSDKComponent.GetLobbyInterface().CreateLobbySearch(ref createLobbySearchOptions, out search);

        //set search parameters
        if (lobbySearchSetParameterOptions != null) {
            foreach (LobbySearchSetParameterOptions searchOption in lobbySearchSetParameterOptions) {
                var option = searchOption;
                search.SetParameter(ref option);
            }
        } else {
            var options = new LobbySearchSetParameterOptions();
            options.ComparisonOp = ComparisonOp.Equal;
            options.Parameter = new AttributeData { Key = DefaultAttributeKey, Value = DefaultAttributeKey };
            search.SetParameter(ref options);
        }

        //find lobbies
        var findOptions = new LobbySearchFindOptions();
        findOptions.LocalUserId = EOSSDKComponent.LocalUserProductId;
        search.Find(ref findOptions, null, (ref LobbySearchFindCallbackInfo callback) => {
            //if the search was unsuccessful, invoke an error event and return
            if (callback.ResultCode != Result.Success) {
                FindLobbiesFailed?.Invoke("There was an error while finding lobbies. Error: " + callback.ResultCode);
                return;
            }

            foundLobbies.Clear();

            //for each lobby found, add data to details
            var lobbySearchGetSearchResultCountOptions = new LobbySearchGetSearchResultCountOptions { };
            for (int i = 0; i < search.GetSearchResultCount(ref lobbySearchGetSearchResultCountOptions); i++) {
                LobbyDetails lobbyInformation;
                var options = new LobbySearchCopySearchResultByIndexOptions();
                options.LobbyIndex = (uint) i;
                search.CopySearchResultByIndex(ref options, out lobbyInformation);
                foundLobbies.Add(lobbyInformation);
            }

            //invoke event
            FindLobbiesSucceeded?.Invoke(foundLobbies);
        });
    }

    /// <summary>
    /// Join the given lobby and get the data attached.
    /// <para>You can get the lobby's data by subscribing to the <see cref="JoinLobbySucceeded"/> event which gives you a list of <see cref="Attribute"/>.</para>
    /// <para>This process may throw errors. You can get errors by subscribing to the <see cref="JoinLobbyFailed"/> event.</para>
    /// </summary>
    /// <param name="lobbyToJoin"><see cref="LobbyDetails"/> of the lobby to join that is retrieved from the <see cref="FindLobbiesSucceeded"/> event.</param>
    /// <param name="attributeKeys">The keys to use to retrieve the data attached to the lobby. If you leave this empty, the host address attribute will still be read.</param>
    /// <param name="presenceEnabled">Use Epic's overlay to display information to others.</param>
    public virtual void JoinLobby(LobbyDetails lobbyToJoin, string[] attributeKeys = null, bool presenceEnabled = false) {
        //join lobby
        var joinLobbyOptions = new JoinLobbyOptions {
            LobbyDetailsHandle = lobbyToJoin, LocalUserId = EOSSDKComponent.LocalUserProductId,
            PresenceEnabled = presenceEnabled
        };
        EOSSDKComponent.GetLobbyInterface().JoinLobby(ref joinLobbyOptions, null, (ref JoinLobbyCallbackInfo callback) => {
            //if the result was not a success, invoke an error event and return
            if (callback.ResultCode != Result.Success) {
                JoinLobbyFailed?.Invoke("There was an error while joining a lobby. Error: " + callback.ResultCode);
                return;
            }

            lobbyData.Clear();

            Attribute? hostAddress = new Attribute();
            var keyOptions = new LobbyDetailsCopyAttributeByKeyOptions();
            keyOptions.AttrKey = hostAddressKey;
            lobbyToJoin.CopyAttributeByKey(ref keyOptions, out hostAddress);
            lobbyData.Add(hostAddress.Value);

            if (attributeKeys != null) {
                foreach (string key in attributeKeys) {
                    Attribute? attribute = new Attribute();
                    var options = new LobbyDetailsCopyAttributeByKeyOptions();
                    options.AttrKey = key;
                    lobbyToJoin.CopyAttributeByKey(ref options, out attribute);
                    lobbyData.Add(attribute.Value);
                }
            }

            LobbyDetails details;
            var copyLobbyDetailsHandleOptions = new CopyLobbyDetailsHandleOptions
                { LobbyId = callback.LobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId };
            EOSSDKComponent.GetLobbyInterface().CopyLobbyDetailsHandle(ref copyLobbyDetailsHandleOptions, out details);

            ConnectedLobbyDetails = details;
            isLobbyOwner = false;
            ConnectedToLobby = true;
            currentLobbyId = callback.LobbyId;

            //invoke event
            JoinLobbySucceeded?.Invoke(lobbyData);
        });
    }

    public virtual void JoinLobbyByID(string lobbyID){
        LobbySearch search = new LobbySearch();
        var createLobbySearchOptions = new CreateLobbySearchOptions { MaxResults = 1 };
        EOSSDKComponent.GetLobbyInterface().CreateLobbySearch(ref createLobbySearchOptions, out search);
        var lobbySearchSetLobbyOptions = new LobbySearchSetLobbyIdOptions { LobbyId = lobbyID };
        search.SetLobbyId(ref lobbySearchSetLobbyOptions);

        var lobbySearchFindOptions = new LobbySearchFindOptions { LocalUserId = EOSSDKComponent.LocalUserProductId };
        search.Find(ref lobbySearchFindOptions, null, (ref LobbySearchFindCallbackInfo callback) => {
            //if the search was unsuccessful, invoke an error event and return
            if (callback.ResultCode != Result.Success) {
                FindLobbiesFailed?.Invoke("There was an error while finding lobbies. Error: " + callback.ResultCode);
                return;
            }

            foundLobbies.Clear();

            //for each lobby found, add data to details
            var lobbySearchGetSearchResultCountOptions = new LobbySearchGetSearchResultCountOptions { };
            for (int i = 0; i < search.GetSearchResultCount(ref lobbySearchGetSearchResultCountOptions); i++) {
                LobbyDetails lobbyInformation;
                var options = new LobbySearchCopySearchResultByIndexOptions();
                options.LobbyIndex = (uint) i;
                search.CopySearchResultByIndex(ref options, out lobbyInformation);
                foundLobbies.Add(lobbyInformation);
            }

            if (foundLobbies.Count > 0) {
                JoinLobby(foundLobbies[0]);
            }
        });     
    }

    /// <summary>
    /// Leave the lobby that the user is connected to. If the creator of the lobby leaves, the lobby will be destroyed, and any client connected to the lobby will leave. If a member leaves, there will be no further action.
    /// <para>If the player was able to destroy or leave the lobby, the <see cref="LeaveLobbySucceeded"/> event will be invoked.</para>
    /// <para>This process may throw errors. You can errors by subscribing to the <see cref="LeaveLobbyFailed"/> event.</para>
    /// </summary>
    public virtual void LeaveLobby() {
        //if we are the owner of the lobby
        if (isLobbyOwner) {
            //Destroy lobby
            var destroyLobbyOptions = new DestroyLobbyOptions
                { LobbyId = currentLobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId };
            EOSSDKComponent.GetLobbyInterface().DestroyLobby(ref destroyLobbyOptions, null, (ref DestroyLobbyCallbackInfo callback) => {
                //if the result was not a success, log error and return
                if (callback.ResultCode != Result.Success) {
                    LeaveLobbyFailed?.Invoke("There was an error while destroying the lobby. Error: " + callback.ResultCode);
                    return;
                }

                ConnectedToLobby = false;
                LeaveLobbySucceeded?.Invoke();
            });
        }
        //if we are a member of the lobby
        else {
            var leaveLobbyOptions = new LeaveLobbyOptions
                { LobbyId = currentLobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId };
            EOSSDKComponent.GetLobbyInterface().LeaveLobby(ref leaveLobbyOptions, null, (ref LeaveLobbyCallbackInfo callback) => {
                //if the result was not a success, log error and return
                if (callback.ResultCode != Result.Success && callback.ResultCode != Result.NotFound) {
                    LeaveLobbyFailed?.Invoke("There was an error while leaving the lobby. Error: " + callback.ResultCode);
                    return;
                }

                ConnectedToLobby = false;
                LeaveLobbySucceeded?.Invoke();
            });
        }

        //when the player leaves the lobby, remove notifications
        //will be useless when not connected to lobby
        EOSSDKComponent.GetLobbyInterface().RemoveNotifyLobbyMemberStatusReceived(lobbyMemberStatusNotifyId);
        EOSSDKComponent.GetLobbyInterface().RemoveNotifyLobbyUpdateReceived(lobbyAttributeUpdateNotifyId);
    }

    
    /// <summary>
    /// Remove an attribute attached to the lobby.
    /// </summary>
    /// <param name="key">The key of the attribute that will be removed.</param>
    public virtual void RemoveAttribute(string key) {
        LobbyModification modHandle = new LobbyModification();

        var updateLobbyModificationOptions = new UpdateLobbyModificationOptions
            { LobbyId = currentLobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId };
        
        EOSSDKComponent.GetLobbyInterface().UpdateLobbyModification(ref updateLobbyModificationOptions, out modHandle);

        var options = new LobbyModificationRemoveAttributeOptions();
        options.Key = key;
        modHandle.RemoveAttribute(ref options);

        var updateLobbyOptions = new UpdateLobbyOptions { LobbyModificationHandle = modHandle };
        EOSSDKComponent.GetLobbyInterface().UpdateLobby(ref updateLobbyOptions, null, (ref UpdateLobbyCallbackInfo callback) => {
            if (callback.ResultCode != Result.Success) {
                AttributeUpdateFailed?.Invoke(key, $"There was an error while removing attribute \"{ key }\". Error: " + callback.ResultCode);
                return;
            }

            AttributeUpdateSucceeded?.Invoke(key);
        });
    }

    /// <summary>
    /// Update an attribute that is attached to the lobby.
    /// </summary>
    /// <param name="attribute">The new data to apply.</param>
    private void UpdateAttribute(AttributeData attribute) {
        LobbyModification modHandle = new LobbyModification();

        var updateLobbyModificationOptions = new UpdateLobbyModificationOptions
            { LobbyId = currentLobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId };
        
        EOSSDKComponent.GetLobbyInterface().UpdateLobbyModification(ref updateLobbyModificationOptions, out modHandle);

        var options = new LobbyModificationAddAttributeOptions();
        options.Attribute = attribute;
        options.Visibility = LobbyAttributeVisibility.Public;
        modHandle.AddAttribute(ref options);

        var updateLobbyOptions = new UpdateLobbyOptions { LobbyModificationHandle = modHandle };
        EOSSDKComponent.GetLobbyInterface().UpdateLobby(ref updateLobbyOptions, null, (ref UpdateLobbyCallbackInfo callback) => {
            if (callback.ResultCode != Result.Success) {
                AttributeUpdateFailed?.Invoke(attribute.Key, $"There was an error while updating attribute \"{ attribute.Key }\". Error: " + callback.ResultCode);
                return;
            }

            AttributeUpdateSucceeded?.Invoke(attribute.Key);
        });
    }

    /// <summary>
    /// Update a boolean attribute.
    /// </summary>
    /// <param name="key">The key of the attribute.</param>
    /// <param name="newValue">The new boolean value.</param>
    public void UpdateLobbyAttribute(string key, bool newValue) {
        AttributeData data = new AttributeData { Key = key, Value = newValue };
        UpdateAttribute(data);
    }

    /// <summary>
    /// Update an integer attribute.
    /// </summary>
    /// <param name="key">The key of the attribute.</param>
    /// <param name="newValue">The new integer value.</param>
    public void UpdateLobbyAttribute(string key, int newValue) {
        AttributeData data = new AttributeData { Key = key, Value = newValue };
        UpdateAttribute(data);
    }

    /// <summary>
    /// Update a double attribute.
    /// </summary>
    /// <param name="key">The key of the attribute.</param>
    /// <param name="newValue">The new double value.</param>
    public void UpdateLobbyAttribute(string key, double newValue) {
        AttributeData data = new AttributeData { Key = key, Value = newValue };
        UpdateAttribute(data);
    }

    /// <summary>
    /// Update a string attribute.
    /// </summary>
    /// <param name="key">The key of the attribute.</param>
    /// <param name="newValue">The new string value.</param>
    public void UpdateLobbyAttribute(string key, string newValue) {
        AttributeData data = new AttributeData { Key = key, Value = newValue };
        UpdateAttribute(data);
    }

    /// <summary>
    /// Returns the current lobby id
    /// </summary>
    /// <returns>current lobby id</returns>
    public string GetCurrentLobbyId() {
        return currentLobbyId;
    }
}
