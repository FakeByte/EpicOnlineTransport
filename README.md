# EpicOnlineTransport for Mirror

Hi! 
This is our [Epic Online Services](https://dev.epicgames.com/en-US/services) (EOS) transport for [Mirror](https://github.com/vis2k/Mirror). We developed it for our game **[Nimoyd](https://www.nimoyd.com/)**, it is still under development, but already working without any problems.

[Video Tutorials](https://youtube.com/playlist?list=PLMS9RDQ9ar-dQuAjG8KOBpwhBZa1V7y2_)

## Dependencies
- Mirror ([Documentation](https://mirror-networking.gitbook.io/docs/))
- Epic Online Services C# SDK ([Documentation](https://dev.epicgames.com/docs/services/en-US/index.html))

## Setting Up Epic Online Services and Obtaining API Keys
1. Login to the [Epic Games Dev Portal](https://dev.epicgames.com/portal) with an Epic Games Account
2. Create a new Product
3. Go to Product Settings -> Clients and add a new Client Policy
4. Name your Client Policy and set the Client Policy Type to Peer2Peer, then Save & Exit
5. Add a new Client
6. Name your Client and set the Client Policy to the Client Policy you just created, then Save & Exit
7. Go to Epic Account Services -> Configure Application
8. Set Application Name to what you want your users to see when signing in with the Account Portal, then Save Draft
9. Go to Permissions and set all permissions to Required, then Save
10. Go to Clients and set the Client to the Client you just created, then Save
11. Go back, then click Back to Organization
12. Go to SDK -> SDK Credentials -> Get Credentials
13. Copy and paste all of your credentials into the EOS API Key Asset once your project is set up

## Installation
1. Visit the [Mirror Asset Store Page](https://assetstore.unity.com/packages/tools/network/mirror-129321) and add Mirror to My Assets
2. Import Mirror with Package Manager (Window -> Package Manager -> Packages: My Assets -> Mirror -> Download)
3. Import the EpicOnlineTransport.unitypackage file found under releases (Assets -> Import Package -> Custom Package...)
4. Attach an EOSSDKComponent to a GameObject in your Scene
5. Right click in the Project View and create an EOS API Key Asset (Create -> EOS -> API Key)
6. Fill out all the SDK keys on the EOS API Key Asset, you can find them in the Epic Online Services Dev Portal (SDK -> SDK Credentials -> Get Credentials)
7. Move the EOS API Key Asset into the 'Api Keys' slot on the EOSSDKComponent
8. Attach a NetworkManager to a GameObject in your Scene
9. Attach an EosTransport component to the same GameObject as the NetworkManager
10. Move the EosTransport component into the 'Transport' slot on the NetworkManager
11. (Optional) Add an EOSLobbyUI component, a NetworkManagerHUD component, or a NetworkDiscoveryHUD component to test that the transport is working properly
12. Change the [Auth Interface Credential Type](https://dev.epicgames.com/docs/services/en-US/API/Members/Enums/Auth/EOS_ELoginCredentialType/index.html) and [Connect Interface Credential Types](https://dev.epicgames.com/docs/services/en-US/API/Members/Enums/NoInterface/EOS_EExternalCredentialType/index.html) on the EOSSDKComponent to suit the needs of your project

## Building for Android
0. Install the Android Module through Unity Hub
1. Switch platform to Android in Build Settings
2. In Unity -> Edit -> Project Settings... -> Player -> Android -> Other Settings -> Identification set Minimum API Level to Android 6.0 'Marshmallow' (API Level 23) as required by the EOS Android SDK
3. (Optional) Install Android Logcat through Package Manager to see logs when running on Android device over USB (Window -> Package Manager -> Packages: Unity Registry -> Android Logcat -> Install) then open with Alt+6 or Window -> Analysis -> Android Logcat

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
