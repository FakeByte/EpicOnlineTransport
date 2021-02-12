using Epic.OnlineServices.Lobby;
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
    
    private void Start()
    {
        EOSSDKComponent.Initialize();
    }

    private void OnEnable()
    {
        //subscribe to events
        JoinLobbyComplete += DrawLobbyMenu;
        FindLobbiesComplete += DrawLobbyList;
    }

    private void OnDisable()
    {
        //unsubscribe from events
        JoinLobbyComplete -= DrawLobbyMenu;
        FindLobbiesComplete -= DrawLobbyList;
    }

    private void OnGUI()
    {
        if (!EOSSDKComponent.Initialized)
        {
            return;
        }

        GUILayout.BeginHorizontal();

        DrawMenuButtons();

        //player list and lobby list drawing is handled by events

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

        lobbyName = GUILayout.TextField(lobbyName, 40, new GUILayoutOption[] { GUILayout.Width(200)});

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

    private void DrawLobbyList(List<LobbyDetails> lobbiesFound)
    {
        //set bool to allow OnGUI method to draw this?
        GUILayout.Label("Lobby Name    /    Player Count");

        foreach (LobbyDetails lobby in lobbiesFound)
        {
            Attribute lobbyNameAttribute = new Attribute();
            lobby.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = attributeKeys[0] }, out lobbyNameAttribute);

            GUILayout.BeginHorizontal();
            GUILayout.Label(lobbyNameAttribute.Data.Value.AsUtf8);
            GUILayout.Space(75);
            GUILayout.Label(lobby.GetMemberCount(new LobbyDetailsGetMemberCountOptions { }).ToString());
            GUILayout.Space(75);

            if (GUILayout.Button("Join"))
            {
                JoinLobby(lobby, attributeKeys);
            }

            GUILayout.EndHorizontal();
        }
    }

    private void DrawLobbyMenu(List<Attribute> lobbyData)
    {
        GUILayout.BeginScrollView(Vector2.zero, new GUILayoutOption[] { GUILayout.MaxHeight(400) });

        Debug.Log("Drew menu");
        if(lobbyData.Count != 0)
        {
            GUILayout.Label("Name: " + lobbyData[1].Data.Value.AsUtf8);
        }

        for (uint i = 0; i < ConnectedLobbyDetails.GetMemberCount(new LobbyDetailsGetMemberCountOptions { }); i++)
        {
            GUILayout.Label(ConnectedLobbyDetails.GetMemberByIndex(new LobbyDetailsGetMemberByIndexOptions { MemberIndex = i }).ToString());
        }

        GUILayout.EndScrollView();
    }
}
