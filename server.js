#!/bin/node

/**
 * Unity Network Model
 *
 * @file server.js
 * @author Uwe Gruenefeld
 * @version 2017-05-09
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
		connection.sendText(JSON.stringify({ id:key, t:'i', p:model[key]['p'], c:model[key]['c']}));
	});

	// Handle event for incoming text
	connection.on("text", function(message)
	{
		var json;
		try
		{
			console.log(message);
			json = JSON.parse(message);

		} catch(exception) {
			console.log("Data format needs to be JSON");
		}

		// Validate request
		if(json.hasOwnProperty('id') && json.hasOwnProperty('t'))
		{
			switch (json.t)
			{
				// If it is a delete request
				case 'd':
					// Delete object
					delete model[json.id];

					// Send delete request to all sockets
					server.connections.forEach(function (socket) {
						socket.sendText(JSON.stringify(json));
					});
				break;

				// If it is a insert request
				case 'i':
					if(model.hasOwnProperty(json.id))
					{
						json.t = 'u';
						json.p = model[json.id]['p'];
						json.c = model[json.id]['c'];
						connection.sendText(JSON.stringify(json));
						break;
					}

				// If it is a update request
				case 'u':
					model[json.id] = {'p':json.p, 'c':json.c};
					json.t = 'u';

					// Send value to all connected sockets
					server.connections.forEach(function (socket) {
						socket.sendText(JSON.stringify(json));
					});

					break;
			}
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
