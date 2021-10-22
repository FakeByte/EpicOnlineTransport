using Epic.OnlineServices.Lobby;
using UnityEngine;
using System.Collections.Generic;
using EpicTransport;
using Mirror;

public class EOSLobbyUI : EOSLobby {
    private string lobbyName = "My Lobby";
    private bool showLobbyList = false;
    private bool showPlayerList = false;

    private List<LobbyDetails> foundLobbies = new List<LobbyDetails>();
    private List<Attribute> lobbyData = new List<Attribute>();

    //register events
    private void OnEnable() {
        //subscribe to events
        CreateLobbySucceeded += OnCreateLobbySuccess;
        JoinLobbySucceeded += OnJoinLobbySuccess;
        FindLobbiesSucceeded += OnFindLobbiesSuccess;
        LeaveLobbySucceeded += OnLeaveLobbySuccess;
    }

    //deregister events
    private void OnDisable() {
        //unsubscribe from events
        CreateLobbySucceeded -= OnCreateLobbySuccess;
        JoinLobbySucceeded -= OnJoinLobbySuccess;
        FindLobbiesSucceeded -= OnFindLobbiesSuccess;
        LeaveLobbySucceeded -= OnLeaveLobbySuccess;
    }

    //when the lobby is successfully created, start the host
    private void OnCreateLobbySuccess(List<Attribute> attributes) {
        lobbyData = attributes;
        showPlayerList = true;
        showLobbyList = false;

        GetComponent<NetworkManager>().StartHost();
    }

    //when the user joined the lobby successfully, set network address and connect
    private void OnJoinLobbySuccess(List<Attribute> attributes) {
        lobbyData = attributes;
        showPlayerList = true;
        showLobbyList = false;

        NetworkManager netManager = GetComponent<NetworkManager>();
        netManager.networkAddress = attributes.Find((x) => x.Data.Key == hostAddressKey).Data.Value.AsUtf8;
        netManager.StartClient();
    }

    //callback for FindLobbiesSucceeded
    private void OnFindLobbiesSuccess(List<LobbyDetails> lobbiesFound) {
        foundLobbies = lobbiesFound;
        showPlayerList = false;
        showLobbyList = true;
    }

    //when the lobby was left successfully, stop the host/client
    private void OnLeaveLobbySuccess() {
        NetworkManager netManager = GetComponent<NetworkManager>();
        netManager.StopHost();
        netManager.StopClient();
    }

    private void OnGUI() {
        //if the component is not initialized then dont continue
        if (!EOSSDKComponent.Initialized) {
            return;
        }

        //start UI
        GUILayout.BeginHorizontal();

        //draw side buttons
        DrawMenuButtons();

        //draw scroll view
        GUILayout.BeginScrollView(Vector2.zero, GUILayout.MaxHeight(400));

        //runs when we want to show the lobby list
        if (showLobbyList && !showPlayerList) {
            DrawLobbyList();
        }
        //runs when we want to show the player list and we are connected to a lobby
        else if (!showLobbyList && showPlayerList && ConnectedToLobby) {
            DrawLobbyMenu();
        }

        GUILayout.EndScrollView();

        GUILayout.EndHorizontal();
    }

    private void DrawMenuButtons() {
        //start button column
        GUILayout.BeginVertical();

        //decide if we should enable the create and find lobby buttons
        //prevents user from creating or searching for lobbies when in a lobby
        GUI.enabled = !ConnectedToLobby;

        #region Draw Create Lobby Button

        GUILayout.BeginHorizontal();

        //create lobby button
        if (GUILayout.Button("Create Lobby")) {
            CreateLobby(4, LobbyPermissionLevel.Publicadvertised, false, new AttributeData[] { new AttributeData { Key = AttributeKeys[0], Value = lobbyName }, });
        }

        lobbyName = GUILayout.TextField(lobbyName, 40, GUILayout.Width(200));

        GUILayout.EndHorizontal();

        #endregion

        //find lobby button
        if (GUILayout.Button("Find Lobbies")) {
            FindLobbies();
        }

        //decide if we should enable the leave lobby button
        //only enabled when the user is connected to a lobby
        GUI.enabled = ConnectedToLobby;

        if (GUILayout.Button("Leave Lobby")) {
            LeaveLobby();
        }

        GUI.enabled = true;

        GUILayout.EndVertical();
    }

    private void DrawLobbyList() {
        //draw labels
        GUILayout.BeginHorizontal();
        GUILayout.Label("Lobby Name", GUILayout.Width(220));
        GUILayout.Label("Player Count");
        GUILayout.EndHorizontal();

        //draw lobbies
        foreach (LobbyDetails lobby in foundLobbies) {
            //get lobby name
            Attribute lobbyNameAttribute = new Attribute();
            lobby.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = AttributeKeys[0] }, out lobbyNameAttribute);

            //draw the lobby result
            GUILayout.BeginHorizontal(GUILayout.Width(400), GUILayout.MaxWidth(400));
            //draw lobby name
            GUILayout.Label(lobbyNameAttribute.Data.Value.AsUtf8.Length > 30 ? lobbyNameAttribute.Data.Value.AsUtf8.Substring(0, 27).Trim() + "..." : lobbyNameAttribute.Data.Value.AsUtf8, GUILayout.Width(175));
            GUILayout.Space(75);
            //draw player count
            GUILayout.Label(lobby.GetMemberCount(new LobbyDetailsGetMemberCountOptions { }).ToString());
            GUILayout.Space(75);

            //draw join button
            if (GUILayout.Button("Join", GUILayout.ExpandWidth(false))) {
                JoinLobby(lobby, AttributeKeys);
            }

            GUILayout.EndHorizontal();
        }
    }

    private void DrawLobbyMenu() {
        //draws the lobby name
        GUILayout.Label("Name: " + lobbyData.Find((x) => x.Data.Key == AttributeKeys[0]).Data.Value.AsUtf8);

        //draws players
        for (int i = 0; i < ConnectedLobbyDetails.GetMemberCount(new LobbyDetailsGetMemberCountOptions { }); i++) {
            GUILayout.Label("Player " + i);
        }
    }
}
