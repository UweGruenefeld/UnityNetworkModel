#!/bin/node

/**
 * Unity Network Model
 *
 * @file server.js
 * @author Uwe Gruenefeld
 * @author Tobias Lunte
 * @version 2019-07-22
 **/

var config		= require('./config.json');
var model		= {};
var assets		= {};
var subscribers = {};

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
	function validate(channel, name, timestamp)
	{
		if(!model.hasOwnProperty(channel))
			return true;
		if(!model[channel].hasOwnProperty(name))
			return true;

		return timestamp >= model[channel][name]['timestamp'];
	}

	// Function to validate timestamp
	function validateAsset(channel, name, timestamp)
	{
		if(!assets.hasOwnProperty(channel))
			return true;
		if(!assets[channel].hasOwnProperty(name))
			return true;

		return timestamp >= assets[channel][name]['timestamp'];
	}

	function ensureChannelExists(channel)
	{
		if (!subscribers.hasOwnProperty(channel)) 
		{
			subscribers[channel] = [];
		}
		if (!assets.hasOwnProperty(channel)) 
		{
			assets[channel] = {};
		}
		if (!model.hasOwnProperty(channel)) 
		{
			model[channel] = {};
		}
	}

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

		// Message is change to subscription by Receiver
		if(json.hasOwnProperty('subscriptionChange'))
		{
			ensureChannelExists(json.channel);

			if(json.subscriptionChange == "subscribe")
			{
				console.log("subscribed to "+json.channel);
				subscribers[json.channel].push(connection);

				// Send assets to new client
				Object.keys(assets[json.channel]).forEach(function(key)
				{
					var auMsg = JSON.stringify({
						type: 'au',
						name: key,
						timestamp: assets[json.channel][key]['timestamp'],
						StringParam1: assets[json.channel][key]['assetType'],
						StringParam2: assets[json.channel][key]['asset'],
					})

					if(config.debugSend)
						console.log("Sent: " + auMsg)
					connection.sendText(auMsg);
				});

				// Send model to new client
				Object.keys(model[json.channel]).forEach(function(key)
				{
					var guMsg = JSON.stringify({
						type: 'gu',
						name: key,
						timestamp: model[json.channel][key]['timestamp'],
						StringParam1: model[json.channel][key]['parent'],
					})

					var cuMsg = JSON.stringify({
						type: 'cu',
						name: key,
						timestamp: model[json.channel][key]['timestamp'],
						ListParam1: keys(model[json.channel][key]['components']),
						ListParam2: values(model[json.channel][key]['components'])
					})

					if(config.debugSend)
						console.log("Sent: " + guMsg)
					connection.sendText(guMsg);

					if(config.debugSend)
						console.log("Sent: " + cuMsg)
					connection.sendText(cuMsg);
				});

			}
			else if(json.subscriptionChange == "unsubscribe")
			{
				console.log("unsubscribed from "+json.channel);
				var index = subscribers[json.channel].indexOf(connection);
				if (index != -1)
				{
					subscribers[json.channel].splice(index, 1);
				}
			}
		}
		// Message is update from Sender
		else if(json.hasOwnProperty('name') && json.hasOwnProperty('channel') && json.hasOwnProperty('type'))
		{
			ensureChannelExists(json.channel);

			var valid = false;
			switch (json.type)
			{
				case 'au':
					if(validateAsset(json.name, json.timestamp)){
						if(!assets[json.channel].hasOwnProperty(json.name))
							assets[json.channel][json.name] = {};
						assets[json.channel][json.name]['timestamp'] = json.timestamp;
						assets[json.channel][json.name]['assetType'] = json.StringParam1;
						assets[json.channel][json.name]['asset'] = json.StringParam2;

						valid = true;
					}
					break;
				case 'ad':
					if(assets[json.channel].hasOwnProperty(json.name) && validateAsset(json.name, json.timestamp))
					{
						delete assets[json.channel][json.name];
						
						valid = true;
					}
					break;
				case 'gu':
					if(validate(json.name, json.timestamp))
					{
						if(!model[json.channel].hasOwnProperty(json.name))
							model[json.channel][json.name] = {};
						if(!model[json.channel][json.name].hasOwnProperty('components'))
							model[json.channel][json.name]['components'] = {}
						model[json.channel][json.name]['timestamp'] = json.timestamp;
						model[json.channel][json.name]['parent'] = json.StringParam1;

						valid = true;
					}
					break;
				case 'gd':
					if(model[json.channel].hasOwnProperty(json.name) && validate(json.name, json.timestamp))
					{
						delete model[json.channel][json.name];
						
						valid = true;
					}
					break;
				case 'cu':
					if(model[json.channel].hasOwnProperty(json.name) && validate(json.name, json.timestamp))
					{
						model[json.channel][json.name]['timestamp'] = json.timestamp;
						// Run through all components
						for(var i = 0; i < json.ListParam1.length && i < json.ListParam2.length; i++)
						{
							model[json.channel][json.name]['components'][json.ListParam1[i]] = json.ListParam2[i];
						}

						valid = true;
					}
					break;
				case 'cd':
					if(model[json.channel].hasOwnProperty(json.name) && validate(json.name, json.timestamp))
					{
						model[json.channel][json.name]['timestamp'] = json.timestamp;
						// Run through all components
						for(var i = 0; i < json.ListParam1.length; i++)
						{
							delete model[json.channel][json.name]['components'][json.ListParam1[i]];
						}

						valid = true;
					}
					break;
			}
			if(valid){
				// Forward request to all other sockets
				//var send = JSON.stringify(json);

				if(config.debugSend)
					console.log("Sent on channel "+json.channel+": " + message)

				subscribers[json.channel].forEach(function (socket) {
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
		values(subscribers).forEach((array)=>
			{
				var index = array.indexOf(connection);
				if (index != -1)
				{
					array.splice(index, 1);
				}
			}
		)
		console.log("Socket connection closed");
	});
});

server.listen(config.port);

// Return a status message to console
console.log('Server is running at ws://localhost:' + config.port + '/');
