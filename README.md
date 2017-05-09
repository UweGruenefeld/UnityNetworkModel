# UnityNetworkModel
Simple applications to synchronize GameObjects with multiple Unity clients over network. The GameObjects are synchronized with every Unity client that attaches the NetworkModel script as a component to any GameObject.

## Features
* Bi-directional updates over server with any number of clients
* Synchronizes any hierarchy depth of GameObjects
* Easy extendable to support more components
* Update time interval is adjustable
* Enables to specify which attribute to sync

## Install
To install the UnityNetworkModel follow these steps:
1. Clone this repository to your computer.
2. Install node.js package with *npm install*
3. Start node.js server with node *server.js*
4. Copy NetworkModel.cs and websocket-sharp.dll into Unity
5. Add the script NetworkModel.cs as Component to a GameObject

## Repository

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
