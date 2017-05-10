# UnityNetworkModel
Lightweight application to synchronize GameObjects with multiple Unity clients over network. The GameObjects are synchronized with every Unity client that attaches the NetworkModel script as a component to any GameObject.

## Features
* Bidirectional updates between server and any number of clients
* Unidirectional updates (only to server or only to client)
* Synchronizes any hierarchy depth of GameObjects
* Updates only changed components of GameObjects
* Update time interval is adjustable
* Specify which components to sync
* Easy extendable to support more components

## Install
To install the UnityNetworkModel follow these five simple steps:
1. Clone this repository to your computer.
2. Install node.js package with *npm install*
3. Start node.js server with node *server.js*
4. Copy NetworkModel.cs and websocket-sharp.dll into Unity
5. Add the script NetworkModel.cs as Component to a GameObject

## Structure

### Server
Node.js server with JSON-storage
* server.js
* package.json
* config.json

### Client
C# script for Unity
* NetworkModel.cs
* websocket-sharp.dll
(Client uses WebSocket-Sharp see https://github.com/sta/websocket-sharp)
