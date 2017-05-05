/**
 * Network Model
 *
 * Attach this script to a gameobject in Unity
 * and all children gameobjects will be
 * synchronized over the specified server
 *
 * @file NetworkModel.cs
 * @author Uwe Gruenefeld
 * @version 2017-05-05
 **/
﻿using System;
using System.Collections.Generic;
using WebSocketSharp;

namespace UnityEngine
{
	/*
	 * TODO sync more then only transform
	 * TODO allow to remove objects
	 */
	public class NetworkModel : MonoBehaviour
	{
		[Header("Server properties")]
		public string IP = "127.0.0.1";
		public int PORT = 8080;

		// Private variables
		// TODO ConcurrentStack to make the queue thread-safe
		private Stack<_Wrapper> updateList;
		private WebSocket webSocket;

		void Start ()
		{
			// Initialize update list
			this.updateList = new Stack<_Wrapper>();
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
					this.webSocket.OnMessage += (sender, message) => this.PullRequest (message.Data);
				}
				// Could not connect to server
				catch(Exception exception) {
					//Debug.LogError (exception);
				}
			}

			// If the websocket still is not alive
			if (!this.webSocket.IsAlive)
				return;

			// Get all children gameobjects of the gameobject
			// with this script attached to
			foreach (Transform child in this.gameObject.transform)
			{
				// Check if there was an update on the gameobject
				if (child.transform.hasChanged)
				{
					//Debug.Log ("Child gameobject has changed");
					this.PushRequest(child.gameObject);
					child.transform.hasChanged = false;
				}
			}

			// Update and create all gameobjects in list
			while (this.updateList.Count > 0)
			{
				_Wrapper wrapper = this.updateList.Pop ();

				Transform transform = this.gameObject.transform.Find(wrapper.n);

				// If object is not existing
				if (transform == null)
				{
					//TODO create object
				}
				// If object is existing
				else {
					transform.localPosition = wrapper.v.p;
					transform.localRotation = wrapper.v.r;
					transform.localScale = wrapper.v.s;

					// Avoid triggering update of changes
					transform.hasChanged = false;
				}
			}
		}

		private void PushRequest(GameObject gameObject)
		{
			this.webSocket.Send (JsonUtility.ToJson(new _Wrapper(gameObject)));
		}

		private void PullRequest(string json)
		{
			this.updateList.Push (JsonUtility.FromJson<_Wrapper> (json));
		}
	}

	[Serializable]
	class _Wrapper
	{
		public string n;
		public _GameObject v;

		public _Wrapper(GameObject gameObject)
		{
			this.n = gameObject.ToString ();
			this.n = this.n.Substring (0, this.n.Length - 25);

			this.v = new _GameObject (gameObject);
		}
	}

	[Serializable]
	class _GameObject
	{
		public Vector3 p, s;
		public Quaternion r;

		public _GameObject(GameObject gameObject)
		{
			this.p = gameObject.transform.localPosition;
			this.s = gameObject.transform.localScale;

			this.r = gameObject.transform.localRotation;
		}
	}
}
