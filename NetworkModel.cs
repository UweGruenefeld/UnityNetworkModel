/**
 * Network Model Script
 *
 * Attach this script to a gameobject in Unity
 * and all children gameobjects will be
 * synchronized over the specified server
 *
 * @file NetworkModel.cs
 * @author Uwe Gruenefeld
 * @version 2017-05-09
 **/
﻿using System;
using System.Collections.Generic;
using WebSocketSharp;

namespace UnityEngine
{
	/// <summary>
	/// Implements MonoBehaviour and connects to server via websocket
	/// </summary>
	public class NetworkModel : MonoBehaviour
	{
		[HeaderAttribute("Connection")]
		public string IP = "127.0.0.1";
		public int PORT = 8080;

		[HeaderAttribute("Update period in seconds")]
		[Range(0.0f, 10.0f)]
		public float PERIOD = 0.1f;

		// Variables for websocket connection
		private WebSocket webSocket;
		private bool init;

		// Variable for update interval
		private float time;

		// Variables for gameobject synchronization
		private Dictionary<string, GameObject> objectDictionary;
		private Queue<_Request> requestQueue;	// FIX ConcurrentQueue to make the queue thread-safe

		// Variables for hierarchy synchronization
		private Queue<Hierarchy> hierarchyQueue;

		/// <summary>
		/// Initialize all default attributes 
		/// </summary>
		void Start ()
		{
			// Default values
			this.init = true;
			this.time = 0f;

			// Initialize object synchronization
			this.objectDictionary = new Dictionary<string, GameObject>();
			this.requestQueue = new Queue<_Request>();

			// Initialize hierarchy synchronization
			this.hierarchyQueue = new Queue<Hierarchy> ();
		}

		/// <summary>
		/// Update is called once per frame by Unity
		/// </summary>
		void Update ()
		{
			// Check update interval
			if (!this.ProcessTime ())
				return;

			// Check connection
			if (!this.ProcessConnection ())
				return;

			// Process
			this.ProcessRequests ();
			this.ProcessHierarchy ();
			this.ProcessChanges ();
			this.ProcessGarbage ();

			// Initial update run is done
			this.init = false;
		}

		/// <summary>
		/// Checks if it is time for a update with respect to specified update interval
		/// </summary>
		/// <returns><c>true</c>, if update is necessary, <c>false</c> otherwise.</returns>
		private bool ProcessTime()
		{
			// Update time since last run
			this.time += Time.deltaTime;

			// If period is bigger than time, than return false
			if (this.PERIOD > this.time)
				return false;

			// Otherwise update is necessary; reset timer
			this.time = 0f;
			return true;
		}

		/// <summary>
		/// Checks the websocket connection
		/// </summary>
		/// <returns><c>true</c>, if connection was established, <c>false</c> otherwise.</returns>
		private bool ProcessConnection()
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
				catch(Exception exception) 
				{
					// Something went wrong
					Debug.Log (exception);
				}
			}

			// Return if websocket is alive
			return this.webSocket.IsAlive;
		}

		/// <summary>
		/// Process the request from server
		/// </summary>
		private void ProcessRequests()
		{
			// Handle requests from server in specific order
			string[] order = new string[]{_Request.DELETE_OBJECT, _Request.INSERT_OBJECT, _Request.UPDATE_OBJECT};

			// Run through request once per type of request
			for (int i = 0; i < order.Length; i++)
				// Run through all requests
				for (int j = 0; j < this.requestQueue.Count; j++) 
				{
					// Get next Request
					_Request request = this.requestQueue.Dequeue ();
					// If request do not fit order put it back in queue
					if (request.t != order[i]) {
						this.requestQueue.Enqueue (request);
						continue;
					}

					// Process request
					switch (request.t) 
					{
					case _Request.DELETE_OBJECT:
						if (this.objectDictionary.ContainsKey (request.id)) 
						{
							Destroy (this.objectDictionary [request.id]);
							this.objectDictionary.Remove (request.id);
						}
						break;
					case _Request.INSERT_OBJECT:
					case _Request.UPDATE_OBJECT:
						SynchronizeGameObject (request);
						break;
					}
				}
		}

		/// <summary>
		/// Process the hierarchy changes
		/// </summary>
		private void ProcessHierarchy()
		{
			// List of gameobjects that need an hierarchy update 
			for (int i = 0; i < this.hierarchyQueue.Count; i++) 
			{
				// Get first request
				Hierarchy hierarchy = this.hierarchyQueue.Dequeue ();

				// Check for parent
				Transform parent = null;
				if (hierarchy.parent == "root")
					parent = this.transform;
				else if (this.objectDictionary.ContainsKey (hierarchy.parent))
					parent = this.objectDictionary [hierarchy.parent].transform;

				// If parent is not null update otherwise put request back in queue
				if (parent != null) 
				{
					hierarchy.gameObject.transform.parent = parent;
					hierarchy.gameObject.SetActive (true);
					hierarchy.gameObject.transform.hasChanged = false;
				}
				else
					this.hierarchyQueue.Enqueue (hierarchy);
			}
		}

		/// <summary>
		/// Process the gameobject changes
		/// </summary>
		private void ProcessChanges()
		{
			// TODO Syncronize if a single component is changed

			// Process changes for the game object itself
			foreach (Transform transform in this.gameObject.GetComponentsInChildren<Transform>())
			{
				// Check if there was an update on the gameobject
				if (transform.hasChanged && transform != this.gameObject.transform)
				{
					// GameObject has changed
					if (!this.objectDictionary.ContainsKey (transform.gameObject.name))
						this.objectDictionary.Add (transform.gameObject.name, transform.gameObject);
					else 
					{
						// Is name already used?
						if (this.objectDictionary [transform.gameObject.name].GetInstanceID() != transform.gameObject.GetInstanceID ()) 
						{
							transform.gameObject.name = "RENAMED_" + transform.gameObject.GetInstanceID ().ToString();
							this.objectDictionary.Add (transform.gameObject.name, transform.gameObject);
						}
					}

					if(this.init)
						this.Request(new _Request(transform.gameObject, _Request.INSERT_OBJECT, this.gameObject.name));
					else
						this.Request(new _Request(transform.gameObject, _Request.UPDATE_OBJECT, this.gameObject.name));

					transform.hasChanged = false;
				}
			}
		}

		/// <summary>
		/// Remove the gameobjects that are not necessary anymore
		/// </summary>
		private void ProcessGarbage()
		{
			// Remove not existing objects
			List<string> removeList = new List<string>();  
			foreach (KeyValuePair<string, GameObject> entry in this.objectDictionary) 
			{
				// If object is not existing remove from map and server
				if (entry.Value == null || entry.Key != entry.Value.name) 
					removeList.Add (entry.Key);
			}
			foreach (string entry in removeList) 
			{
				this.Request (new _Request(entry, _Request.DELETE_OBJECT, ""));
				this.objectDictionary.Remove (entry);
			}
		}

		/// <summary>
		/// Synchronizes the game objectfrom server request
		/// Extend this for supporting more components
		/// </summary>
		/// <param name="_request">Request from server</param>
		private void SynchronizeGameObject(_Request _request)
		{
			GameObject gameObject = this.ReferenceToGameObject(_request);

			// Update the components
			foreach (_Component component in _request.components) 
			{
				if (component.GetType () == typeof(_Transform)) 
				{
					_Transform _transform = (_Transform)component;
					Transform transform = this.ReferenceToComponent<Transform> (gameObject);

					// If there was no change on client side than use server values
					if (!transform.hasChanged) {
						transform.localPosition = _transform.p;
						transform.localRotation = _transform.r;
						transform.localScale = _transform.s;

						// Avoid triggering update of changes
						transform.hasChanged = false;
					}
				} 
				else if (component.GetType () == typeof(_Camera)) 
				{
					_Camera _camera = (_Camera)component;
					Camera camera = this.ReferenceToComponent<Camera> (gameObject);

					camera.depth = _camera.d;
					camera.nearClipPlane = _camera.n;
					camera.farClipPlane = _camera.f;
					camera.fieldOfView = _camera.v;
				} 
				else if (component.GetType () == typeof(_Light)) 
				{
					_Light _light = (_Light)component;
					Light light = this.ReferenceToComponent<Light> (gameObject);

					light.type = _light.t;
					light.color = _light.c;
					light.intensity = _light.i;
					light.bounceIntensity = _light.b;
				} 
				else if (component.GetType () == typeof(_MeshFilter)) 
				{
					_MeshFilter _meshFilter = (_MeshFilter)component;
					MeshFilter meshFilter = this.ReferenceToComponent<MeshFilter> (gameObject);

					meshFilter.mesh = new Mesh ();
					meshFilter.mesh.vertices = _meshFilter.v;
					meshFilter.mesh.uv = _meshFilter.u;
					meshFilter.mesh.triangles = _meshFilter.t;
				} 
				else if (component.GetType () == typeof(_MeshRenderer)) 
				{
					#pragma warning disable CS0219
					_MeshRenderer _meshRenderer = (_MeshRenderer)component;
					MeshRenderer meshRenderer = this.ReferenceToComponent<MeshRenderer> (gameObject);
					#pragma warning restore CS0219
				}
			}
		}

		// Finds or creates a reference to gameobject specified in request
		private GameObject ReferenceToGameObject(_Request _request)
		{
			GameObject gameObject;

			if (!this.objectDictionary.ContainsKey (_request.id)) 
			{
				gameObject = new GameObject ();
				gameObject.name = _request.id;
				gameObject.transform.hasChanged = false;
				gameObject.SetActive (false);
				this.hierarchyQueue.Enqueue (new Hierarchy (gameObject, _request.p));
				this.objectDictionary.Add (_request.id, gameObject);
			} 
			else 
			{
				gameObject = this.objectDictionary [_request.id];
				if (gameObject.transform.parent.name != _request.p) 
				{
					gameObject.SetActive (false);
					this.hierarchyQueue.Enqueue (new Hierarchy (gameObject, _request.p));
				}
			}
				
			return gameObject;
		}

		// Finds or creates a reference to component specified by T and gameobject
		private T ReferenceToComponent<T>(GameObject gameObject) where T : Component 
		{
			T component = gameObject.GetComponent<T>();
			if (component != null)
				return component;

			component = (T)gameObject.AddComponent (typeof(T));
			return component;
		}

		// Sending a request
		private void Request(_Request _request)
		{			
			this.webSocket.Send (JsonUtility.ToJson (_request));
		}

		// Handling a response
		private void Response(string json)
		{
			this.requestQueue.Enqueue (JsonUtility.FromJson<_Request> (json));
		}
	}


	/// <summary>
	/// Serialization Utils
	/// </summary>
	/// <param name="obj">Object.</param>
	/// <typeparam name="T">The 1st type parameter.</typeparam>
	static class Util
	{
		public static string Serialize<T>(T obj)
		{
			return obj.GetType () + ":" + JsonUtility.ToJson (obj);
		}

		public static T Deserialize<T>(string json)
		{
			int delimiter = json.IndexOf (":");
			Type type = Type.GetType (json.Substring (0, delimiter));
			return (T)JsonUtility.FromJson (json.Substring (delimiter + 1), type);
		}
	}

	/// <summary>
	/// Struct for a hierarchy change 
	/// </summary>
	struct Hierarchy
	{
		public GameObject gameObject;
		public string parent;

		public Hierarchy(GameObject gameObject, string parent)
		{
			this.gameObject = gameObject;
			this.parent = parent;
		}
	}

	/// <summary>
	/// Class for every kind of request
	/// </summary>
	[Serializable]
	class _Request : ISerializationCallbackReceiver
	{
		public const string DELETE_OBJECT = "d";
		public const string INSERT_OBJECT = "i";
		public const string UPDATE_OBJECT = "u";

		public string id;
		public string t; 
		public string p;

		public List<string> c;

		[NonSerialized]
		public List<_Component> components;

		public _Request(string id, string t, string p) 
		{
			this.id = id;
			this.t = t;
			this.p = p;

			this.components = new List<_Component> ();
		}

		public _Request(GameObject gameObject, string t, string root) 
		{
			this.id = gameObject.name;
			this.t = t;
			this.p = gameObject.transform.parent.name;

			if (this.p == root)
				this.p = "root";

			this.components = new List<_Component> ();

			// Save supported components of gameobject
			Component[] list = gameObject.GetComponents(typeof(Component));
			foreach (Component component in list) 
			{
				Type type = Type.GetType("UnityEngine._" + component.GetType ().Name);

				if (type != null) 
					this.components.Add((_Component)Activator.CreateInstance(type, new Object[]{component}));
			}
		}

		public void OnBeforeSerialize()
		{
			this.c = new List<string> ();

			foreach (_Component component in this.components) 
				this.c.Add (Util.Serialize<_Component>(component));
		}

		public void OnAfterDeserialize()
		{
			this.components = new List<_Component> ();

			foreach (string s in this.c) 
				this.components.Add(Util.Deserialize<_Component>(s));
		}
	}

	/// <summary>
	/// Super class for all components
	/// </summary>
	/// <param name="component">Component.</param>
	[Serializable]
	class _Component 
	{
		public _Component(Component component) {}
	}

	/// <summary>
	/// Serializable transform
	/// </summary>
	[Serializable]
	class _Transform : _Component
	{
		public Vector3 p, s;
		public Quaternion r;

		public _Transform(Component component) : base(component)
		{
			Transform transform = (Transform)component;

			this.p = transform.localPosition;
			this.r = transform.localRotation;
			this.s = transform.localScale;
		}
	}

	/// <summary>
	/// Serializable camera
	/// </summary>
	[Serializable]
	class _Camera : _Component
	{
		public float d, n, f, v;

		public _Camera(Component component) : base(component) 
		{
			Camera camera = (Camera)component;

			this.d = camera.depth;
			this.n = camera.nearClipPlane;
			this.f = camera.farClipPlane;
			this.v = camera.fieldOfView;
		}
	}

	/// <summary>
	/// Serializable light
	/// </summary>
	[Serializable]
	class _Light : _Component
	{
		public LightType t;
		public Color c;
		public float i, b;

		public _Light(Component component) : base(component) 
		{
			Light light = (Light)component;

			this.t = light.type;
			this.c = light.color;
			this.i = light.intensity;
			this.b = light.bounceIntensity;
		}
	}

	/// <summary>
	/// Serializable meshfilter
	/// </summary>
	[Serializable]
	class _MeshFilter : _Component
	{
		public Vector3[] v;
		public Vector2[] u;
		public int[] t;

		public _MeshFilter(Component component) : base(component) 
		{
			MeshFilter meshFilter = (MeshFilter)component;

			Mesh mesh = meshFilter.sharedMesh;

			this.v = mesh.vertices;
			this.u = mesh.uv;
			this.t = mesh.triangles;
		}
	}

	/// <summary>
	/// Serializable meshrenderer
	/// </summary>
	/// <param name="component">Component.</param>
	[Serializable]
	class _MeshRenderer : _Component
	{
		public _MeshRenderer(Component component) : base(component) {}
	}
}