# UnityNetworkModel
Unity script and NodeJS server for synchronizing GameObjects with multiple Unity clients over network.

## Features
* Bidirectional updates between server and any number of Unity clients
* Unidirectional updates (only receive updates or only send updates)
* Synchronizes any hierarchy depth of GameObjects
* Updates only changed components of GameObjects
* Update time interval is adjustable
* Specify which components to sync (inlcuding Scripts, Transform, Camera, Light, MeshRenderer, MeshFilter)
* Extendable to support more components or to synchronize only specifc parts of components

## Install
To install the UnityNetworkModel follow these five simple steps:
1. Clone this repository to your computer
2. Install the NodeJS package with *npm install* inside the nodejs folder
3. Start node.js server with node *server.js*
4. Copy NetworkModel.cs and websocket-sharp.dll into Unity
5. Add the script NetworkModel.cs as Component to a GameObject

## Structure

### Server
Node.js server with JSON-storage (in folder nodejs)
* server.js
* package.json
* config.json

### Client
C# script client for Unity (in folder unity)
* NetworkModel.cs
* websocket-sharp.dll
(Client uses WebSocket-Sharp see https://github.com/sta/websocket-sharp)

## Requirements
* NodeJS version 4.5 or higher *required*
* Tested with Unity version 2017.4 or higher
