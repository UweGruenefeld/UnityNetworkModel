#!/bin/node

/**
 * Unity Network Model
 *
 * @file server.js
 * @author Uwe Gruenefeld
 * @version 2017-05-11
 **/

var config		= require('./config.json');
var model		= {};
var assets		= {};

var websocket 	= require("nodejs-websocket");
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
	function validate(name, timestamp)
	{
		if(!model.hasOwnProperty(name))
			return true;

		return timestamp >= model[name]['timestamp'];
	}

	// Function to validate timestamp
	function validateAsset(name, timestamp)
	{
		if(!assets.hasOwnProperty(name))
			return true;

		return timestamp >= assets[name]['timestamp'];
	}

	// Send assets to new client
	Object.keys(assets).forEach(function(key)
	{
		var auMsg = JSON.stringify({
			type: 'au',
			name: key,
			timestamp: assets[key]['timestamp'],
			StringParam1: assets[key]['assetType'],
			StringParam2: assets[key]['asset'],
		})

		if(config.debugSend)
			console.log("Sent: " + auMsg)
		connection.sendText(auMsg);
	});

	// Send model to new client
	Object.keys(model).forEach(function(key)
	{
		var guMsg = JSON.stringify({
			type: 'gu',
			name: key,
			timestamp: model[key]['timestamp'],
			StringParam1: model[key]['parent'],
		})

		var cuMsg = JSON.stringify({
			type: 'cu',
			name: key,
			timestamp: model[key]['timestamp'],
			ListParam1: keys(model[key]['components']),
			ListParam2: values(model[key]['components'])
		})

		if(config.debugSend)
			console.log("Sent: " + guMsg)
		connection.sendText(guMsg);

		if(config.debugSend)
			console.log("Sent: " + cuMsg)
		connection.sendText(cuMsg);
	});

	// Handle event for incoming text
	connection.on("text", function(message)
	{
		// Parse incoming json
		var json;
		try
		{
			if(config.debugRec)
				console.log("Received: " + message);
			json = JSON.parse(message);
		}
		catch(exception)
		{
			if(config.debugRec)
				console.log(exception);
			return;
		}

		// Validate request
		if(json.hasOwnProperty('name') && json.hasOwnProperty('type'))
		{
			var valid = false;
			switch (json.type)
			{
				case 'au':
					if(validateAsset(json.name, json.timestamp)){
						if(!assets.hasOwnProperty(json.name))
							assets[json.name] = {};
						assets[json.name]['timestamp'] = json.timestamp;
						assets[json.name]['assetType'] = json.StringParam1;
						assets[json.name]['asset'] = json.StringParam2;

						valid = true;
					}
					break;
				case 'ad':
					if(assets.hasOwnProperty(json.name) && validateAsset(json.name, json.timestamp))
					{
						delete assets[json.name];
						
						valid = true;
					}
					break;
				case 'gu':
					if(validate(json.name, json.timestamp))
					{
						if(!model.hasOwnProperty(json.name))
							model[json.name] = {};
						if(!model[json.name].hasOwnProperty('components'))
							model[json.name]['components'] = {}
						model[json.name]['timestamp'] = json.timestamp;
						model[json.name]['parent'] = json.StringParam1;

						valid = true;
					}
					break;
				case 'gd':
					if(model.hasOwnProperty(json.name) && validate(json.name, json.timestamp))
					{
						delete model[json.name];
						
						valid = true;
					}
					break;
				case 'cu':
					if(model.hasOwnProperty(json.name) && validate(json.name, json.timestamp))
					{
						model[json.name]['timestamp'] = json.timestamp;
						// Run through all components
						for(var i = 0; i < json.ListParam1.length && i < json.ListParam2.length; i++)
						{
							model[json.name]['components'][json.ListParam1[i]] = json.ListParam2[i];
						}

						valid = true;
					}
					break;
				case 'cd':
					if(model.hasOwnProperty(json.name) && validate(json.name, json.timestamp))
					{
						model[json.name]['timestamp'] = json.timestamp;
						// Run through all components
						for(var i = 0; i < json.ListParam1.length; i++)
						{
							delete model[json.name]['components'][json.ListParam1[i]];
						}

						valid = true;
					}
					break;
			}
			if(valid){
				// Forward request to all other sockets
				//var send = JSON.stringify(json);

				if(config.debugSend)
					console.log("Sent: " + message)

				server.connections.forEach(function (socket) {
					if(socket != connection){
						socket.sendText(message);
					}
				});
			}else{
				if(config.debugRec)
					console.log("Invalid: "+message);
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
