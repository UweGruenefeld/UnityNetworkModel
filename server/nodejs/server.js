#!/bin/node

/**
 * Unity Network Model - NodeJS Server
 *
 * @file server.js
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-01-15
 **/

const { inspect } = require('util');

// Request types
const CHANNEL_SUBSCRIBE = 'cs';
const CHANNEL_LEAVE		= 'cl';
const RESOURCE_UPDATE	= 'ru';
const RESOURCE_DELETE	= 'rd';
const OBJECT_UPDATE		= 'ou';
const OBJECT_DELETE		= 'od';
const COMPONENT_UPDATE	= 'cu';
const COMPONENT_DELETE	= 'cd';

// Data storage
var model = {};
var ressources = {};
var clients = {};
var subscriptions = {};

//
// SERVER
//

// Server configuration
var config = require('./config.json');
var websocket = require('nodejs-websocket');
var server = websocket.createServer(function(connection)
{
	// Handle event for open connection
	onOpen(connection);

	// Handle event for closing connection
	connection.on('close', function()
	{
		onClose(connection)
	});

	// Handle event for incoming message
	connection.on("text", function(message)
	{
		onText(connection, message);
	});
});

// Server start
server.listen(config.port);
log(null, 'init server','Server is running at ws://localhost:' + config.port);

//
// HANDLE EVENTS
//

// Handle event for open connection
function onOpen(connection)
{
	// Add client to list
	clients[getID(connection)] = connection;

	// Log message
	log(connection, 'new client', 'New client ID=' + getID(connection) + ',' +
		' host=' + connection.headers['host'] + ', ' +
		' user-agent=' + connection.headers['user-agent']);
}

// Handle event for close connection
function onClose(connection)
{
	// Remove client from list
	delete clients[getID(connection)];

	// Remove all subscriptions of client
	Object.keys(subscriptions).forEach(function(key)
	{
		// Look through all subscriptions of each channel
		for (i = 0; i < subscriptions[key].length; i++) 

			// Remove if subscription ID matches the connection ID
			if(getID(subscriptions[key][i]) == getID(connection))
				subscriptions[key].splice(i, 1);  
	})

	// Log message
	log(connection, 'removed client', 'Removed client ID=' + getID(connection));
}

// Handle event for incoming message
function onText(connection, message)
{
	// Parse incoming JSON message
	var json = parseJSON(message);

	// Validate if message contains type and channel information
	if(json.hasOwnProperty('type') && json.hasOwnProperty('channel'))
	{
		// Log incoming message
		log(connection, 'incoming message', 'New message of type (' + json.type + ')', json);

		// Check if channel exists
		if(!model.hasOwnProperty(json.channel))
			model[json.channel] = {};
		if(!ressources.hasOwnProperty(json.channel))
			ressources[json.channel] = {}
		if(!subscriptions.hasOwnProperty(json.channel))
			subscriptions[json.channel] = [];

		// Handle different message types
		valid = false;
		switch(json.type)
		{
			case CHANNEL_SUBSCRIBE:
				valid = handleMessageChannelSubscribe(connection, json);
				break;
			case CHANNEL_LEAVE:
				valid = handleMessageChannelLeave(connection, json);
				break;
			case RESOURCE_UPDATE:
				valid = handleMessageRessourceUpdate(connection, json);
				break;
			case RESOURCE_DELETE:
				valid = handleMessageRessourceDelete(connection, json);
				break;
			case OBJECT_UPDATE:
				valid = handleMessageObjectUpdate(connection, json);
				break;
			case OBJECT_DELETE:
				valid = handleMessageObjectDelete(connection, json);
				break;
			case COMPONENT_UPDATE:
				valid = handleMessageComponentUpdate(connection, json);
				break;
			case COMPONENT_DELETE:
				valid = handleMessageComponentDelete(connection, json);
				break;
			default:
				// Not supported message type
				log(connection, 'warning', 'Message is dropped because it uses an unsupported type');
				return;
		}

		// Check if message was accepted
		if(valid)
		{
			// Check if type of message requires forwarding to other clients
			if(json.type != CHANNEL_SUBSCRIBE && json.type != CHANNEL_LEAVE)
			{
				var isMessageSend = false;
				subscriptions[json.channel].forEach(function(socket) 
				{
					if(socket !== connection && socket.readyState === websocket.OPEN)
					{
						try 
						{
							socket.sendText(message);
							isMessageSend = true;
						}
						catch(exception) 
						{
							// Connection to client is broken
							log(connection, 'expection', exception);
							onClose(socket);
						}
					}
				});
	
				// Log send message
				if(isMessageSend)
					log(connection, 'outgoing message', 'Message sent on channel ' + json.channel, message);
			}
		}
		else
			// Outdated timestamp
			log(connection, 'warning', 'Message is dropped because the timestamp is outdated', message);
	}
	else
		// Missing type or channel information
		log(connection, 'warning', 'Message does not contain the type or channel information');
}

//
// HANDLE MESSAGES
//

// Handle message for channel subscribe
function handleMessageChannelSubscribe(connection, json)
{
	// Subscribe to channel
	subscriptions[json.channel].push(connection);

	// Log subscription
	log(connection, 'channel subscribe', 'Subscribed to ' + json.channel);

	// Send all ressources of the subscribed channel to the new subscriber
	Object.keys(ressources[json.channel]).forEach(function(key)
	{
		// Create ressource update message
		var messageRessourceUpdate = JSON.stringify({
			type: 'ru',
			name: key,
			channel: json.channel,
			timestamp: ressources[json.channel][key]['timestamp'],
			string1: ressources[json.channel][key]['ressourceType'],
			string2: ressources[json.channel][key]['ressource'],
		})

		// Send message to subscriber
		connection.sendText(messageRessourceUpdate);

		// Log the outgoing message
		log(connection, 'outgoing message', 'Sent resource update message', messageRessourceUpdate);
	});

	// Send all model of the subscribed to the new subscriber
	Object.keys(model[json.channel]).forEach(function(key)
	{
		var messageObjectUpdate = JSON.stringify({
			type: 'ou',
			name: key,
			channel: json.channel,
			timestamp: model[json.channel][key]['timestamp'],
			string1: model[json.channel][key]['parent'],
			string2: ''
		})

		// Send message to subscriber
		connection.sendText(messageObjectUpdate);

		// Log the outgoing message
		log(connection, 'outgoing message', 'Sent object update message', messageObjectUpdate);

		var messageComponentUpdate = JSON.stringify({
			type: 'cu',
			name: key,
			channel: json.channel,
			timestamp: model[json.channel][key]['timestamp'],
			string1: keys(model[json.channel][key]['components']),
			string2: values(model[json.channel][key]['components'])
		})

		// Send message to subscriber
		connection.sendText(messageComponentUpdate);

		// Log the outgoing message
		log(connection, 'outgoing message', 'Sent component update message', messageComponentUpdate);
	});

	// Successful
	return true;
}

// Handle message for channel unsubscribe
function handleMessageChannelLeave(connection, json)
{
	// Get subscription from array
	var index = subscriptions[json.channel].indexOf(connection);

	// Remove subscription from array
	if (index != -1)
		subscriptions[json.channel].splice(index, 1);

	// Log unsubscribe
	log(connection, 'channel leave', 'Unsubscribed from ' + json.channel);

	// Successful
	return true;
}

// Handle message for ressource update
function handleMessageRessourceUpdate(connection, json)
{
	// Check if request is valid
	if(json.hasOwnProperty('string1') && json.hasOwnProperty('string2') &&
		validateRessourceTimestamp(json.channel, json.name, json.timestamp))
	{
		// Does the ressource already exist
		if(!ressources[json.channel].hasOwnProperty(json.name))
			ressources[json.channel][json.name] = {};

		// Update the ressource
		ressources[json.channel][json.name]['timestamp'] = json.timestamp;
		ressources[json.channel][json.name]['ressourceType'] = json.string1;
		ressources[json.channel][json.name]['ressource'] = [JSON.stringify(json.string2[0])];

		// Log ressource update
		log(connection, 'ressource update', 'Updated ressource ' + json.name);

		// Successful
		return true;
	}
	return false;
}

// Handle message for resource delete
function handleMessageRessourceDelete(connection, json)
{
	// Check if request is valid
	if(ressources[json.channel].hasOwnProperty(json.name) && 
		validateRessourceTimestamp(json.channel, json.name, json.timestamp))
	{
		// Remove the assest
		delete ressources[json.channel][json.name];
		
		// Log ressource delete
		log(connection, 'ressource delete', 'Removed ressource ' + json.name);

		// Successful
		return true;
	}
	return false;
}

// Handle message for object update
function handleMessageObjectUpdate(connection, json)
{
	// Check if request is valid
	if(validateObjectTimestamp(json.channel, json.name, json.timestamp))
	{
		// Does the object already exist
		if(!model[json.channel].hasOwnProperty(json.name))
			model[json.channel][json.name] = {};

		// Does the object already have components attached
		if(!model[json.channel][json.name].hasOwnProperty('components'))
			model[json.channel][json.name]['components'] = {}

		// Update the object
		model[json.channel][json.name]['timestamp'] = json.timestamp;
		model[json.channel][json.name]['parent'] = json.string1;

		// Log object update
		log(connection, 'object update', 'Updated object ' + json.name);

		// Successful
		return true;
	}
	return false;
}

// Handle message for object delete
function handleMessageObjectDelete(connection, json)
{
	// Check if request is valid
	if(model[json.channel].hasOwnProperty(json.name) && 
		validateObjectTimestamp(json.channel, json.name, json.timestamp))
	{
		// Remove the object
		delete model[json.channel][json.name];
		
		// Log object delete
		log(connection, 'object remove', 'Removed object ' + json.name);

		// Successful
		return true;
	}
	return false;
}

// Handle message for component update
function handleMessageComponentUpdate(connection, json)
{
	// Check if request is valid
	if(model[json.channel].hasOwnProperty(json.name) && 
		json.hasOwnProperty('string1') && json.hasOwnProperty('string2') &&
		validateObjectTimestamp(json.channel, json.name, json.timestamp))
	{
		// Refresh timestamp for object
		model[json.channel][json.name]['timestamp'] = json.timestamp;

		// Update the requested components
		for(var i = 0; i < json.string1.length && i < json.string2.length; i++)
			model[json.channel][json.name]['components'][json.string1[i]] = JSON.stringify(json.string2[i]);

		// Log component update
		log(connection, 'component update', 'Updated components from object ' + json.name);

		// Successful
		return true;
	}
	return false;
}

// Handle message for component delete
function handleMessageComponentDelete(connection, json)
{
	// Check if request is valid
	if(model[json.channel].hasOwnProperty(json.name) && 
		json.hasOwnProperty('string1') &&
		validateObjectTimestamp(json.channel, json.name, json.timestamp))
	{
		// Refresh timestamp for object
		model[json.channel][json.name]['timestamp'] = json.timestamp;

		// Remove the requested components
		for(var i = 0; i < json.string1.length; i++)
			delete model[json.channel][json.name]['components'][json.string1[i]];

		// Log object delete
		log(connection, 'component remove', 'Removed components from object ' + json.name);

		// Successful
		return true;
	}
	return false;
}

//
// BASIC FUNCTIONS
//

// Function to log messages to the console
function log(connection, type, message, json)
{
	if(!config.debug)
		return;

	// Create date information for log
	var today = new Date();
	var time = '[' + ("0" + today.getHours()).slice(-2) + ':' + 
		("0" + today.getMinutes()).slice(-2) + ':' + 
		("0" + today.getSeconds()).slice(-2) + ']';
	
	// Create client information for log
	var client = 'SERVER';
	if(connection)
		client = getID(connection).substring(0,6);

	// Write main log message
	console.log(time + ' ' + (new Array(20).join(' ') + '(' + type + ')').slice(-20) + ' [' + client + '] ' + message);

	// If JSON is attached to the log message
	if(json && config.debugJSON)
	{
		// Check if json is a string and if yes, convert it to json
		if (typeof json === 'string' || json instanceof String)
			json = parseJSON(json);

		// Parse json in string1
		if(json.hasOwnProperty('string1') && Array.isArray(json.string1))
			for(var i = 0; i < json.string1.length; i++) 
				if(typeof json.string1[i] === 'string' || json.string1[i] instanceof String)
					if(json.string1[i].startsWith('{', 0) && json.string1[i].endsWith('}', json.string1[i].length))
						json.string1[i] = parseJSON(json.string1[i]);

		// Parse json in string2
		if(json.hasOwnProperty('string2') && Array.isArray(json.string2))
			for(var i = 0; i < json.string2.length; i++) 
				if(typeof json.string2[i] === 'string' || json.string2[i] instanceof String)
					if(json.string2[i].startsWith('{', 0) && json.string2[i].endsWith('}', json.string2[i].length))
						json.string2[i] = parseJSON(json.string2[i]);

		console.log(inspect(json, { showHidden: true, depth: null }));
	}	
}

// Function to validate timestamp for objects
function validateObjectTimestamp(channel, objectName, timestamp)
{
	if(!model.hasOwnProperty(channel))
		return true;
	if(!model[channel].hasOwnProperty(objectName))
		return true;

	// Check if timestamp is empty
	if(timestamp == 0)
		return true;

	// Compare timestamp
	return timestamp >= model[channel][objectName]['timestamp'];
}

// Function to validate timestamp for ressources
function validateRessourceTimestamp(channel, ressourceName, timestamp)
{
	if(!ressources.hasOwnProperty(channel))
		return true;
	if(!ressources[channel].hasOwnProperty(ressourceName))
		return true;

	// Check if timestamp is empty
	if(timestamp == 0)
		return true;

	// Compare timestamp
	return timestamp >= ressources[channel][ressourceName]['timestamp'];
}

//
// UTILITIES
//

// Get the ID of a given socket connection
function getID(connection)
{
	return connection.headers['sec-websocket-key'];
}

// Parse a message to a JSON object
function parseJSON(message)
{
	try
	{
		var json = JSON.parse(message);
		return json;
	}
	catch(exception)
	{
		log(null, 'expection', 'Exception "' + exception + '" parsing ' + message);
	}

	return {};
}

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