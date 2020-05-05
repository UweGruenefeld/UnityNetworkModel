# Unity Network Model
Package for Unity3D, which allows to synchronize GameObjects, Components and Resources with multiple Unity clients over websockets. On the server side, a NodeJS script opens a websocket connection, stores the incoming GameObjects, Components and Resources as JSON objects and sends updates to the Unity clients to keep them sync.

> Ideal for beginners, to synchronize multiple Unity clients without any knowledge
> about network programming. Perfect as well, to rapidly prototype
> multi-user experiences with a large number of supported devices.

## Features
* Bidirectional updates between Unity client and NodeJS server
   - Every Unity client shares the same synchronized hierarchy of GameObjects and their Components and Resources
* Unidirectional updates (only receive updates or only send updates)
   - One Unity client, for example, can be used to show the copy of GameObjects without sending changes to the NodeJS server
* Selected updates (specify which Components and Resources can send or receive updates)
   - Supported components are BoxCollider, Camera, Light, LineRenderer, MeshCollider, MeshFilter, MeshRenderer, Script, SphereCollider, and Transform
   - Supported resources are Material, Mesh, and Texture2D
   - Extendable to support more components and resources
* Supports multiple channel to synchronize different hierarchies on client and server at the same time
* Synchronizes any hierarchy depth of GameObjects
* Update frequency and decimal places adjustable
* Updates only changed GameObjects, Components and Resources

## Install
To install the UnityNetworkModel follow these five simple steps:
1. Clone this repository to your computer
2. Install the NodeJS package inside the nodejs folder
```sh
npm install
```
3. Start node.js server
```sh
node server.js
```
4. Import UnityNetworkModel.unitypackage in Unity3D
5. Add the script NetworkModelConfiguration.cs to a GameObject

## Requirements
* NodeJS version 4.5 or higher *required*
* Unity version 2017.4 or higher *required*
* Tested on Unity version 2019.3 and 2018.4
* If Clients (Unity) are deployed on different machines and compare timestamps is activated, a clock synchronization on all Clients is required

## Platforms
* Windows (including Hololens und VR headsets)
* Linux
* MacOS
* Android (Oculus Go, Quest)
* iOS

## External Libraries
* WebSocket-Sharp (see https://github.com/sta/websocket-sharp)


-----
## Screenshots

![Unity GUI with imported UnityNetworkModel Package](https://github.com/UweGruenefeld/UnityNetworkModel/blob/master/client/unity/screenshots/unity-gui.jpg?raw=true)
*Unity GUI with imported UnityNetworkModel Package*

![UnityNetworkModel Configuration Component](https://github.com/UweGruenefeld/UnityNetworkModel/blob/master/client/unity/screenshots/unity-config.jpg?raw=true)
*UnityNetworkModel Configuration Component*

![UnityNetworkModel Rule Component](https://github.com/UweGruenefeld/UnityNetworkModel/blob/master/client/unity/screenshots/unity-rules.jpg?raw=true)
*UnityNetworkModel Rule Component*

![NodeJS server console output](https://github.com/UweGruenefeld/UnityNetworkModel/blob/master/server/nodejs/screenshots/nodejs-console.jpg?raw=true)
*NodeJS server console output*