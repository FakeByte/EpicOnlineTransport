# EpicOnlineTransport for Mirror

Hi! 
This is our [Epic Online Services](https://dev.epicgames.com/en-US/services) (EOS) transport for [Mirror](https://github.com/vis2k/Mirror). We developed it for our game **[Nimoyd](https://www.nimoyd.com/)** , it is still under development, but already working without any problems.

[Video Tutorials](https://youtube.com/playlist?list=PLMS9RDQ9ar-dQuAjG8KOBpwhBZa1V7y2_)

## Dependencies
-Mirror

-Epic Online Service C# SDK

## Installation
1. Import the unitypackage file found under releases
2. In Unity -> Project Settings -> Player -> Other Settings -> Scripting define symbols set PLATFORM_64BITS for Win64 or PLATFORM_32BITS for Win32. For other platforms see Epic.OnlineServices.Config in the SDK for their defines
3. Attach the EOSSDKComponent to a gameobject in your scene
4. Fill out all the SDK keys on the EOSSDKComponent, you can find them in the Epic Online Services Dev Portal
5. Attach the EosTransport component to  the same gameobject as the NetworkManager
6. Move the EosTransport reference into the 'Transport' slot on the NetworkManager

## Testing multiplayer on one device
Running multiple instances of your game on one device for testing requires you to have multiple epic accounts.
Even if your game doesn't use epic accounts you will need them for testing.

0. Add all epic accounts you want to test with to your organization in the dev portal
1. On the EOSSDKComponent under User Login set Auth Interface Login to true
2. Choose 'Developer' as Auth Interface Credential Type
3. Choose 'Epic' as Connect Interface Credential Type
4. Open the epic transport folder with a file explorer and go into the DevAuthTool folder
5. Create a folder that ends with ~ e.g. Tool~, this makes unity ignore this folder
6. Unzip the dev auth tool for your OS (Mac/Win) into the folder you created in step 5.
7. Run the dev auth tool 
8. Enter a port in the dev auth tool
9. Login to your epic account and give the credential a name
10. Repeat step 9 for as many accounts you want to use
11. On the EOSSDKComponent set the port to the one you used in the dev auth tool
12. On the EOSSDKCOmponent set Dev Auth Tool Credential Name to the named you chose in the tool

Note: In the editor after logging in with the dev auth tool you cant change the credential name as the sdk stays initialized even after finish playing. You either have to restart unity or the dev auth tool. For builds it is useful to set delayed initialization on the EOSSDKComponent to true and then provide a user input field to set the dev tool credential name and then calling EOSSDKComponent.Initialize().

## Connecting to other users

You need the epic online product id to connect to another user, you can get it by calling:

    EOSSDKComponent.LocalUserProductId
    or
    EOSSDKComponent.LocalUserProductIdString
The string variant can be sent to other users to connect.

## Lobbies

You can quickly add lobbies to your game using the pre-built EOSLobbyUI script. The EOSLobbyUI extends the EOSLobby script which has methods for handling lobby creation, joining, finding, and leaving and has many events that you can subscribe to. To make the EOSLobbyUI work, create a GameObject, and add the script. If you don't have an EOSSDKComponent present in the scene, make sure to add it to the GameObject. If you prefer to create your own UI for lobbies, you can reference the EOSLobbyUI script.

**NOTE:** The EOSLobby script creates lobbies with the host address predefined. You can get the host address from the ``JoinLobbySucceeded`` event so you can establish a connection using Mirror.

### EOSLobbyUI Features

The EOSLobbyUI allows for a fast implementation of lobbies in your project and it is also an example of what you can do.
Here are the features:
- Creating a lobby with a name
- Lobby list that displays the name and player count
- Joining
- Leaving (If the owner of the lobby leaves, then the lobby will be destroyed)

## Credits
Big thanks to erikas-taroza aka TypicalEgg for his help in improving and extending this transport!
