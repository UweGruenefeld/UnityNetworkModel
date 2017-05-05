#!/bin/node

/**
 * Unity Network Model
 *
 * @file server.js
 * @author Uwe Gruenefeld
 * @version 2017-05-06
 **/

var config		= require('./config.json');
var model			= {};

var websocket = require("nodejs-websocket");
var server		= websocket.createServer(function(connection)
{
	console.log("New socket connection");

	// Send model to new client
	Object.keys(model).forEach(function(key)
	{
		connection.sendText(JSON.stringify({ id: key, t: 'i', o: model[key]}));
	});

	// Handle event for incoming text
	connection.on("text", function(message)
	{
		try
		{
			console.log(message);
			json = JSON.parse(message);

			// Validate request
			if(json.hasOwnProperty('id') && json.hasOwnProperty('t'))
			{
				switch (json.t)
				{
					// If it is a delete request
					case 'd':
						// Send delete to all connected sockets
						server.connections.forEach(function (socket) {
							socket.sendText(JSON.stringify(json));
						});
						delete model[json.id];
						break;

					// If it is a insert request
					case 'i':
						if(model.hasOwnProperty(json.id))
						{
							json.t = 'u';
							json.o = model[json.id];
							connection.sendText(JSON.stringify(json));
							break;
						}

					// If it is a update request
					case 'u':
						model[json.id] = json.o;
						json.t = 'u';

						// Send value to all connected sockets
						server.connections.forEach(function (socket) {
							socket.sendText(JSON.stringify(json));
						});
				}
			}
		} catch(exception) {
			console.log("Data format needs to be JSON")
		}
	});

	// Handle event for close connection
	connection.on("close", function()
	{
		console.log("Socket connection closed");
	});
});

server.listen(config.port);

// Return a status message to console
console.log('Server is running at ws://localhost:' + config.port + '/');
