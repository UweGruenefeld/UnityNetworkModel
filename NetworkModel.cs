/**
 * Network Model
 *
 * Attach this script to a gameobject in Unity
 * and all children gameobjects will be
 * synchronized over the specified server
 *
 * @file NetworkModel.cs
 * @author Uwe Gruenefeld
 * @version 2017-05-06
 **/
﻿using System;
using System.Collections.Generic;
using WebSocketSharp;

namespace UnityEngine
{
	public class NetworkModel : MonoBehaviour
	{
		[Header("Server properties")]
		public string IP = "127.0.0.1";
		public int PORT = 8080;

		// Private variables
		// TODO ConcurrentStack to make the queue thread-safe
		private Dictionary<string, GameObject> objectMap;
		private Stack<_Request> requestStack;
		private WebSocket webSocket;
		private bool init;

		void Start ()
		{
			// Initialize object map and update list
			this.objectMap = new Dictionary<string, GameObject>();
			this.requestStack = new Stack<_Request>();
			this.init = true;
		}

		void Update ()
		{
			// If the websocket is null
			if(this.webSocket == null)
				this.webSocket = new WebSocket ("ws://" + this.IP + ":" + this.PORT);

			// If the websocket is not alive
			// try to connect to the server
			if (!this.webSocket.IsAlive)
			{
				try
				{
					// Connect to the server
					this.webSocket.Connect ();

					// Method for handling incoming messages
					this.webSocket.OnMessage += (sender, message) => this.Response (message.Data);
				}
				// Could not connect to server
				#pragma warning disable 0168
				catch(Exception exception) {}
				#pragma warning restore 0168
			}

			// If the websocket still is not alive
			if (!this.webSocket.IsAlive)
				return;

			// Get all children gameobjects of the gameobject
			// with this script attached to
			foreach (Transform transform in this.gameObject.transform)
			{
				// Check if there was an update on the gameobject
				if (transform.hasChanged)
				{
					// GameObject has changed
					if (!this.objectMap.ContainsKey (transform.gameObject.name))
						this.objectMap.Add (transform.gameObject.name, transform.gameObject);

					if(this.init)
						this.RequestInsert(transform.gameObject);
					else
						this.RequestUpdate(transform.gameObject);
						
					transform.hasChanged = false;
				}
			}

			// Handle requests from server
			// update or delete gameobjects from stack
			while (this.requestStack.Count > 0)
			{
				_Request request = this.requestStack.Pop ();

				switch (request.t) 
				{
					case _Request.DELETE:
						if (this.objectMap.ContainsKey (request.id))
							Destroy (this.objectMap [request.id]);
						break;
					case _Request.INSERT:
					case _Request.UPDATE:
						GameObject gameObject;
						if (!this.objectMap.ContainsKey (request.id)) {
							gameObject = GameObject.CreatePrimitive (PrimitiveType.Cube).transform.gameObject;
							gameObject.transform.SetParent (this.gameObject.transform);
							gameObject.name = request.id;
							this.objectMap.Add (request.id, gameObject);
						} else
							gameObject = this.objectMap [request.id];

						gameObject.transform.localPosition = request.o.p;
						gameObject.transform.localRotation = request.o.r;
						gameObject.transform.localScale = request.o.s;

							// Avoid triggering update of changes
						gameObject.transform.hasChanged = false;
						break;
				}
			}

			// Remove not existing objects
			List<string> removeList = new List<string>();  
			foreach (KeyValuePair<string, GameObject> entry in this.objectMap) 
			{
				// If object is not existing remove from map and server
				if (entry.Value == null || entry.Key != entry.Value.name)
					removeList.Add (entry.Key);
			}
			foreach (string entry in removeList) 
			{
				this.RequestDelete (entry);
				this.objectMap.Remove (entry);
			}

			this.init = false;
		}

		private void RequestDelete(string entry)
		{
			this.webSocket.Send (JsonUtility.ToJson(new _Request(entry, _Request.DELETE, null)));
		}

		private void RequestInsert(GameObject gameObject)
		{
			this.webSocket.Send (JsonUtility.ToJson(new _Request(gameObject.name, _Request.INSERT, gameObject)));
		}

		private void RequestUpdate(GameObject gameObject)
		{
			this.webSocket.Send (JsonUtility.ToJson(new _Request(gameObject.name, _Request.UPDATE, gameObject)));
		}

		private void Response(string json)
		{
			this.requestStack.Push (JsonUtility.FromJson<_Request>(json));
		}
	}
		
	[Serializable]
	class _Request
	{
		public const string DELETE = "d";
		public const string INSERT = "i";
		public const string UPDATE = "u";

		public string id;
		public string t; 
		public _GameObject o;

		public _Request(string id, string t, GameObject o) 
		{
			this.id = id;
			this.t = t;
			this.o = new _GameObject(o);
		}
	}

	[Serializable]
	class _GameObject
	{
		public Vector3 p, s;
		public Quaternion r;

		public _GameObject(GameObject gameObject)
		{
			if (gameObject == null)
				return;
			
			this.p = gameObject.transform.localPosition;
			this.s = gameObject.transform.localScale;

			this.r = gameObject.transform.localRotation;
		}
	}
}
