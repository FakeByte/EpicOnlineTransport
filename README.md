# EpicOnlineTransport for Mirror

Hi! 
This is our [Epic Online Services](https://dev.epicgames.com/en-US/services) (EOS) transport for [Mirror](https://github.com/vis2k/Mirror). We developed it for our game **[Nimoyd](https://www.nimoyd.com/)** , it is still under development, but already working without any problems.

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


## Connecting to other users

You need the epic online product id to connect to another user, you can get it by calling:

    EOSSDKComponent.localUserProductId
    or
    EOSSDKComponent.localUserProductIdString
The string variant can be sent to other users to connect.
