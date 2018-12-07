#!/bin/node

/**
 * Unity Network Model
 *
 * @file server.js
 * @author Uwe Gruenefeld
 * @version 2017-05-11
 **/

var config		= require('./config.json');
var model			= {};

var websocket = require("nodejs-websocket");
var server		= websocket.createServer(function(connection)
{
	console.log("New socket connection");

	// Get keys from associative array
	function keys (array)
	{
		var result = new Array();
		for(var key in array)
			result.push(key);
		return result;
	}

	// Get values from associative array
	function values (array)
	{
		var result = new Array();
		for(var key in array)
			result.push(array[key]);
		return result;
	}

	// Function to validate timestamp
	function validate(id, timestamp)
	{
		if(!model.hasOwnProperty(id))
			return true;

		return timestamp >= model[id]['timestamp'];
	}

	// Send model to new client
	Object.keys(model).forEach(function(key)
	{
		var send = JSON.stringify({
			id: key,
			type: 'i',
			parameter: model[key]['parameter'],
			timestamp: model[key]['timestamp'],
			componentNames: keys(model[key]['components']),
			componentValues: values(model[key]['components'])
		});

		if(config.debug)
			console.log("Sent: " + send)

		connection.sendText(send);
	});

	// Handle event for incoming text
	connection.on("text", function(message)
	{
		// Parse incoming json
		var json;
		try
		{
			if(config.debug)
				console.log("Recieved: " + message);
			json = JSON.parse(message);
		}
		catch(exception)
		{
			if(config.debug)
				console.log(exception);
		}

		// Validate request
		if(json.hasOwnProperty('id') &&
			json.hasOwnProperty('type') &&
			json.hasOwnProperty('parameter'))
		{
			switch (json.type)
			{
				// If it is a delete request
				case 'd':

					// Check if timestamp is newer then last update
					if(model.hasOwnProperty(json.id) && validate(json.id, json.timestamp))
					{
						if(json.parameter == "")
							// Delete object
							delete model[json.id];
						else
							// Delete component
							delete model[json.id]['components'][json.parameter];

						// Send delete request to all sockets
						var send = JSON.stringify(json);

						if(config.debug)
							console.log("Sent: " + send)

						server.connections.forEach(function (socket) {
							socket.sendText(send);
						});
					}
				break;

				// If it is a insert request
				case 'i':
					if(model.hasOwnProperty(json.id) && validate(json.id, json.timestamp))
					{
						json.type = 'u';
						json.parameter = model[json.id]['parameter'];
						json.timestamp = model[json.id]['timestamp'];
						json.componentNames = keys(model[json.id]['components']);
						json.componentValues = values(model[json.id]['components']);
						connection.sendText(JSON.stringify(json));
						break;
					}

				// If it is a update request
				case 'u':
					if(validate(json.id, json.timestamp))
					{
						if(!model.hasOwnProperty(json.id))
							model[json.id] = {};

						if(!model[json.id].hasOwnProperty('components'))
							model[json.id]['components'] = {}

						// Run through all components
						for(var i = 0;
							i < json.componentNames.length &&
							i < json.componentValues.length;
							i++)
						{
							model[json.id]['components'][json.componentNames[i]] =
								json.componentValues[i];
						}

						model[json.id] = {
							'parameter': json.parameter,
							'timestamp': json.timestamp,
							'components': model[json.id]['components']};

						// Send value to all connected sockets
						var send = JSON.stringify(json);

						if(config.debug)
							console.log("Sent: " + send)

						server.connections.forEach(function (socket) {
							socket.sendText(send);
						});
					}
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
