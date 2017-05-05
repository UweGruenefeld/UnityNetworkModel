#!/bin/node

/**
 * Unity Network Model
 *
 * @file server.js
 * @author Uwe Gruenefeld
 * @version 2017-05-05
 **/

var config		= require('./config.json');
var model			= {};

var websocket = require("nodejs-websocket");
var server		= websocket.createServer(function(connection)
{
	console.log("New socket connection");

	// Handle event for incoming data
	connection.on('text', function(text)
	{
		try {
			json = JSON.parse(text);

			// If there is a value attribute in the json
			// update or insert value and name in model
			if(json.hasOwnProperty('v'))
			{
				// Update or insert value and name in model
				model[json.n] = json.v;

				// Send value to all connected sockets
				server.connections.forEach(function (socket) {
		        socket.sendText(JSON.stringify(json));
		    });
			}
			// If there is no value attribute in the json
			// return value for name to the requesting socket
			else
				connection.sendText(JSON.stringify({ n: json.n, v: model[json.n]}));

		} catch(exception) {
			console.log("Only JSON support")
		}
	});
});

server.listen(config.port)

// Return a status message to console
console.log('Server is running at ws://localhost:' + config.port + '/');
