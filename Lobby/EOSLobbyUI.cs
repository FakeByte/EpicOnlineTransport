﻿using Epic.OnlineServices.Lobby;
using Epic.OnlineServices;
using UnityEngine;
using System.Collections.Generic;
using EpicTransport;

public class EOSLobbyUI : EOSLobby
{
    [SerializeField]
    private string[] attributeKeys = new string[] 
    { 
        "lobby_name"
    };

    private string lobbyName = "My Lobby";
    private bool showLobbyList = false;
    private bool showPlayerList = false;

    private List<LobbyDetails> foundLobbies = new List<LobbyDetails>();
    private List<Attribute> lobbyData = new List<Attribute>();
    
    private void Start()
    {
        if(!EOSSDKComponent.Initialized)
        {
            EOSSDKComponent.Initialize();
        }
    }

    //register events
    private void OnEnable()
    {
        //subscribe to events
        JoinLobbyComplete += OnJoinLobbyComplete;
        FindLobbiesComplete += OnFindLobbiesComplete;
    }

    //deregister events
    private void OnDisable()
    {
        //unsubscribe from events
        JoinLobbyComplete -= OnJoinLobbyComplete;
        FindLobbiesComplete -= OnFindLobbiesComplete;
    }

    //callback for JoinLobbyComplete
    private void OnJoinLobbyComplete(List<Attribute> attributes)
    {
        lobbyData = attributes;
        showPlayerList = true;
        showLobbyList = false;
    }

    //callback for FindLobbiesComplete
    private void OnFindLobbiesComplete(List<LobbyDetails> lobbiesFound)
    {
        foundLobbies = lobbiesFound;
        showPlayerList = false;
        showLobbyList = true;
    }

    private void OnGUI()
    {
        //if the component is not initialized then dont continue
        if (!EOSSDKComponent.Initialized)
        {
            return;
        }

        //start UI
        GUILayout.BeginHorizontal();

        //draw side buttons
        DrawMenuButtons();

        //draw scroll view
        GUILayout.BeginScrollView(Vector2.zero, GUILayout.MaxHeight(400));

        //runs when we want to show the lobby list
        if(showLobbyList && !showPlayerList)
        {
            DrawLobbyList();
        }
        //runs when we want to show the player list and we are connected to a lobby
        else if(!showLobbyList && showPlayerList && ConnectedToLobby)
        {
            DrawLobbyMenu();
        }

        GUILayout.EndScrollView();

        GUILayout.EndHorizontal();
    }

    private void DrawMenuButtons()
    {
        //start button column
        GUILayout.BeginVertical();

        //decide if we should enable the create and find lobby buttons
        //prevents user from creating or searching for lobbies when in a lobby
        GUI.enabled = !ConnectedToLobby;

        #region Draw Create Lobby Button

        GUILayout.BeginHorizontal();

        //create lobby button
        if(GUILayout.Button("Create Lobby"))
        {
            CreateLobby(4, LobbyPermissionLevel.Publicadvertised, false, new AttributeData[] { new AttributeData { Key = attributeKeys[0], Value = lobbyName}, });
        }

        lobbyName = GUILayout.TextField(lobbyName, 40, GUILayout.Width(200));

        GUILayout.EndHorizontal();

        #endregion

        //find lobby button
        if (GUILayout.Button("Find Lobbies"))
        {
            FindLobbies(100, new LobbySearchSetParameterOptions[]
            {
                new LobbySearchSetParameterOptions
                {
                    ComparisonOp = ComparisonOp.Equal,
                    Parameter = new AttributeData { Key = DefaultAttributeKey, Value = DefaultAttributeKey }
                },
            });
        }

        //decide if we should enable the leave lobby button
        //only enabled when the user is connected to a lobby
        GUI.enabled = ConnectedToLobby;

        if(GUILayout.Button("Leave Lobby"))
        {
            LeaveLobby();
        }

        GUI.enabled = true;

        GUILayout.EndVertical();
    }

    private void DrawLobbyList()
    {
        //draw labels
        GUILayout.BeginHorizontal();
        GUILayout.Label("Lobby Name", GUILayout.Width(220));
        GUILayout.Label("Player Count");
        GUILayout.EndHorizontal();

        //draw lobbies
        foreach (LobbyDetails lobby in foundLobbies)
        {
            //get lobby name
            Attribute lobbyNameAttribute = new Attribute();
            lobby.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = attributeKeys[0] }, out lobbyNameAttribute);

            //draw the lobby result
            GUILayout.BeginHorizontal(GUILayout.Width(400), GUILayout.MaxWidth(400));
            //draw lobby name
            GUILayout.Label(lobbyNameAttribute.Data.Value.AsUtf8.Length > 30 ? lobbyNameAttribute.Data.Value.AsUtf8.Substring(0, 27).Trim() + "..." : lobbyNameAttribute.Data.Value.AsUtf8, GUILayout.Width(175));
            GUILayout.Space(75);
            //draw player count
            GUILayout.Label(lobby.GetMemberCount(new LobbyDetailsGetMemberCountOptions { }).ToString());
            GUILayout.Space(75);

            //draw join button
            if (GUILayout.Button("Join", GUILayout.ExpandWidth(false)))
            {
                JoinLobby(lobby, attributeKeys);
            }

            GUILayout.EndHorizontal();
        }
    }

    private void DrawLobbyMenu()
    {
        //draws the lobby name
        GUILayout.Label("Name: " + lobbyData.Find((x) => x.Data.Key == attributeKeys[0]).Data.Value.AsUtf8);

        //draws players
        for (uint i = 0; i < ConnectedLobbyDetails.GetMemberCount(new LobbyDetailsGetMemberCountOptions { }); i++)
        {
            GUILayout.Label(ConnectedLobbyDetails.GetMemberByIndex(new LobbyDetailsGetMemberByIndexOptions { MemberIndex = i }).ToString());
        }
    }
}