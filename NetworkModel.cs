/**
 * Network Model Script
 *
 * Attach this script to a gameobject in Unity
 * and all children gameobjects will be
 * synchronized over the specified server
 *
 * @file NetworkModel.cs
 * @author Uwe Gruenefeld
 * @version 2017-05-11
 **/
﻿using System;
using System.Collections.Generic;
using System.Reflection;
using WebSocketSharp;

namespace UnityEngine
{
	/// <summary>
	/// Implements MonoBehaviour and connects to server via websocket
	/// </summary>
	public class NetworkModel : MonoBehaviour
	{
		[HeaderAttribute("Debugging")]
		public bool DEBUGGING = false;

		[HeaderAttribute("Connection")]
		public string IP = "127.0.0.1";
		public int PORT = 8080;

		[HeaderAttribute("Update")]
		public bool CLIENT = true;
		public bool SERVER = true;

		[HeaderAttribute("Update period in seconds")]
		[Range(0.0f, 10.0f)]
		public float PERIOD = 0f;

		[HeaderAttribute("Update with timestamp")]
		public bool TIMESTAMP = false;

		[HeaderAttribute("Update components")]
		public bool TRANSFORM = true;
		public bool CAMERA = true;
		public bool LIGHT = true;
		public bool MESHFILTER = true;
		public bool MESHRENDERER = true;

		// Variables for websocket connection
		private WebSocket webSocket;

		// Variable for update interval
		private float time;

		// Variables for gameobject synchronization
		private Dictionary<string, Node> nodeDictionary;
		private Queue<_Request> requestQueue;	// FIX ConcurrentQueue to make the queue thread-safe

		// Variables for hierarchy synchronization
		private Queue<HierarchyUpdate> hierarchyQueue;

		/// <summary>
		/// Initialize all default attributes 
		/// </summary>
		void Start ()
		{
			// Default values
			this.time = 0f;

			// Initialize object synchronization
			this.nodeDictionary = new Dictionary<string, Node>();
			this.requestQueue = new Queue<_Request>();

			// Initialize hierarchy synchronization
			this.hierarchyQueue = new Queue<HierarchyUpdate> ();
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

			// Changes
			this.ProcessServerChanges ();
			this.ProcessClientChanges ();

			// Garbage
			this.ProcessGarbage ();
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
		private void ProcessServerChanges()
		{
			// Handle requests from server
			for (int j = 0; j < this.requestQueue.Count; j++) 
			{
				// Get next Request
				_Request request = this.requestQueue.Dequeue ();

				// Process request
				switch (request.type) 
				{
					case _Request.DELETE:
						if (request.parameter == "")
							DeleteGameObject (request.id);
						else
							DeleteComponent (request.id, request.parameter);
						break;
					case _Request.INSERT:
					case _Request.UPDATE:
						this.SynchronizeGameObject (request);
						break;
				}
			}
			this.SynchronizeHierarchy ();
		}

		/// <summary>
		/// Process the gameobject changes
		/// </summary>
		private void ProcessClientChanges()
		{
			// Run through all child gameobjects from root gameobject
			foreach (Transform transform in this.gameObject.GetComponentsInChildren<Transform>()) 
			{
				// Check if given transform is not from root
				if (transform != this.gameObject.transform) 
				{
					// Check if given transform is not in nodeDictionary
					if (!this.nodeDictionary.ContainsKey (transform.gameObject.name)) 
					{
						this.nodeDictionary.Add (transform.gameObject.name, new Node (transform.gameObject));
						this.Request (new _Request (transform.gameObject, _Request.INSERT, this));
						transform.hasChanged = false;
						continue;
					} 
					// Check if given transform is in nodeDictionary as different node
					else if (this.nodeDictionary [transform.gameObject.name].gameObject.GetInstanceID () != transform.gameObject.GetInstanceID ()) 
					{
						transform.gameObject.name = "RENAMED_" + transform.gameObject.GetInstanceID ().ToString ();
						this.nodeDictionary.Add (transform.gameObject.name, new Node (transform.gameObject));
						this.Request (new _Request (transform.gameObject, _Request.INSERT, this));
						transform.hasChanged = false;
						continue;
					}

					// Get node and add new components
					Node node = this.nodeDictionary [transform.gameObject.name];
					node.AddMissingComponents();

					// Save updated components
					List<_Component> updatedComponents = new List<_Component>();

					// Update existing components
					foreach (KeyValuePair<Type, string> entry in node.componentDictionary) 
					{
						Component component = transform.gameObject.GetComponent (entry.Key);
						Type type = Util.TypeToSerializableType (component.GetType ());

						// Component is not existing anymore
						if (component == null) 
						{
							// Send delete to server
							this.Request(new _Request(transform.gameObject.name, _Request.DELETE, type.ToString(), this));
						} 
						else 
						{
							// Update the component
							string hash = Util.Convert(component).GetHash ();
							if (hash != entry.Value) {
								updatedComponents.Add (Util.Convert(component));
							}
						}
					}

					// Update all hashes for components
					node.UpdateHashes ();

					// Update only if components changed
					if (transform.hasChanged || updatedComponents.Count > 0) 
					{
						// Create request
						_Request _request = new _Request (transform.gameObject.name, _Request.UPDATE, "", this);
						_request.parameter = transform.parent.name;
						_request.components = updatedComponents;
						this.Request (_request);
						transform.hasChanged = false;
					}
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
			foreach (KeyValuePair<string, Node> entry in this.nodeDictionary) 
			{
				// If object is not existing remove from map and server
				if (entry.Value == null || entry.Value.gameObject == null || entry.Key != (entry.Value.gameObject.name)) 
					removeList.Add (entry.Key);
			}
			foreach (string entry in removeList) 
			{
				this.Request (new _Request(entry, _Request.DELETE, "", this));
				this.nodeDictionary.Remove (entry);
			}
		}

		/// <summary>
		/// Synchronizes the game objectfrom server request
		/// Extend this for supporting more components
		/// </summary>
		/// <param name="_request">Request from server</param>
		private void SynchronizeGameObject(_Request _request)
		{
			Node node = this.ReferenceToGameObject(_request);

			// Update the components
			foreach (_Component _component in _request.components) 
			{
				Component component = this.ReferenceToComponent (node.gameObject, Util.SerializableTypeToType(_component.GetType()));
				_component.Apply(component);
			}

			// Add hashes for new added components
			node.UpdateHashes ();
		}

		/// <summary>
		/// Synchronizes the hierarchy for gameobjects from hierarchy queue
		/// </summary>
		private void SynchronizeHierarchy()
		{
			// List of gameobjects that need an hierarchy update 
			for (int i = 0; i < this.hierarchyQueue.Count; i++) 
			{
				// Get first request
				HierarchyUpdate hierarchyUpdate = this.hierarchyQueue.Dequeue ();

				// Check for parent
				Transform parent = null;
				if (hierarchyUpdate.parent == "root")
					parent = this.transform;
				else if (this.nodeDictionary.ContainsKey (hierarchyUpdate.parent))
					parent = this.nodeDictionary [hierarchyUpdate.parent].gameObject.transform;

				// If parent is not null update otherwise put request back in queue
				if (parent != null) 
				{
					hierarchyUpdate.gameObject.transform.parent = parent;
					hierarchyUpdate.gameObject.SetActive (true);
					hierarchyUpdate.gameObject.transform.hasChanged = false;
				}
				else
					this.hierarchyQueue.Enqueue (hierarchyUpdate);
			}
		}

		private void DeleteGameObject(string gameObject)
		{
			if (this.nodeDictionary.ContainsKey (gameObject)) 
			{
				Destroy (this.nodeDictionary [gameObject].gameObject);
				this.nodeDictionary.Remove (gameObject);
			}
		}

		private void DeleteComponent(string gameObject, string component)
		{
			if (this.nodeDictionary.ContainsKey (gameObject)) 
			{
				GameObject obj = this.nodeDictionary [gameObject].gameObject;
				Type type = Type.GetType (component + ", UnityEngine");
				if (type != null) 
				{
					Component comp = obj.GetComponent(type);
					Destroy (comp);
				}
			}
		}

		// Finds or creates a reference to gameobject specified in request
		private Node ReferenceToGameObject(_Request _request)
		{
			Node node;	
			if (!this.nodeDictionary.ContainsKey (_request.id)) 
			{
				GameObject gameObject = new GameObject ();
				gameObject.name = _request.id;
				gameObject.transform.hasChanged = false;
				gameObject.SetActive(false);
				node = new Node (gameObject);
				this.hierarchyQueue.Enqueue (new HierarchyUpdate (gameObject, _request.parameter));
				this.nodeDictionary.Add (_request.id, node);
			} 
			else 
			{
				node = this.nodeDictionary [_request.id];
				if (node.gameObject.transform.parent.name != _request.parameter) 
				{
					node.gameObject.SetActive (false);
					this.hierarchyQueue.Enqueue (new HierarchyUpdate (node.gameObject, _request.parameter));
				}
			}
				
			return node;
		}

		// Finds or creates a reference to component specified by T and gameobject
		private Component ReferenceToComponent(GameObject gameObject, Type type) 
		{
			Component component = gameObject.GetComponent(type);
			if (component != null)
				return component;

			component = gameObject.AddComponent (type);
			return component;
		}

		// Sending a request
		private void Request(_Request _request)
		{
			if (this.SERVER) {
				if (_request.parameter == this.gameObject.name)
					_request.parameter = "root";
				string json = JsonUtility.ToJson (_request);
				if(this.DEBUGGING)
					Debug.Log ("Sent: " + json);
				this.webSocket.Send (json);
			}
		}

		// Handling a response
		private void Response(string json)
		{
			if (this.CLIENT) {
				if(this.DEBUGGING)
					Debug.Log ("Recieved: " + json);
				this.requestQueue.Enqueue (JsonUtility.FromJson<_Request> (json));
			}
		}
	}
		
	/// <summary>
	/// Utils
	/// </summary>
	/// <param name="obj">Object.</param>
	/// <typeparam name="T">The 1st type parameter.</typeparam>
	static class Util
	{
		public static long Timestamp()
		{
			return DateTime.Now.ToUniversalTime().ToBinary();
		}

		public static Type TypeToSerializableType(Type type)
		{
			return Type.GetType("UnityEngine._" + type.Name);
		}

		public static Type SerializableTypeToType(Type type)
		{
			return Type.GetType("UnityEngine." + type.Name.Substring(1) + ", UnityEngine");
		}

		public static _Component Convert(Component component)
		{
			Type componentType = Util.TypeToSerializableType (component.GetType ());
			if (componentType != null)
				return (_Component)Activator.CreateInstance (componentType, new Object[]{ component });
			return null;
		}
	}

	/// <summary>
	/// Internal representation of gameobjects
	/// </summary>
	class Node
	{
		public GameObject gameObject;
		public Dictionary<Type, string> componentDictionary;

		public Node(GameObject gameObject)
		{
			this.gameObject = gameObject;
			this.UpdateHashes ();
		}

		public void UpdateHashes()
		{
			this.componentDictionary = new Dictionary<Type, string>();

			foreach (Component component in this.gameObject.GetComponents(typeof(Component))) 
			{
				Type type = Util.TypeToSerializableType(component.GetType ());

				if(type != null)
					this.componentDictionary.Add(component.GetType(), Util.Convert(component).GetHash());
			}
		}

		public void AddMissingComponents()
		{
			foreach (Component component in this.gameObject.GetComponents(typeof(Component))) 
			{
				Type type = Util.TypeToSerializableType(component.GetType ());

				if (type != null && !this.componentDictionary.ContainsKey(component.GetType()))
					this.componentDictionary.Add(component.GetType(), "");
			}
		}
	}

	/// <summary>
	/// Struct for a hierarchy change 
	/// </summary>
	struct HierarchyUpdate
	{
		public GameObject gameObject;
		public string parent;

		public HierarchyUpdate(GameObject gameObject, string parent)
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
		public const string DELETE = "d";
		public const string INSERT = "i";
		public const string UPDATE = "u";

		public string id;
		public string type; 
		public string parameter;

		public long timestamp;

		public List<string> componentNames;
		public List<string> componentValues;

		[NonSerialized]
		public List<_Component> components;

		public _Request(string id, string type, string parameter, NetworkModel root) 
		{
			this.id = id;
			this.type = type;
			this.parameter = parameter;

			this.timestamp = 0;
			if(root.TIMESTAMP)
				this.timestamp = Util.Timestamp ();

			this.components = new List<_Component> ();
		}

		public _Request(GameObject gameObject, string type, NetworkModel root) 
		{
			this.id = gameObject.name;
			this.type = type;
			this.parameter = gameObject.transform.parent.name;

			this.timestamp = 0;
			if(root.TIMESTAMP)
				this.timestamp = Util.Timestamp ();

			this.components = new List<_Component> ();

			// Save supported and enabled components of gameobject
			foreach (Component component in gameObject.GetComponents(typeof(Component))) {
				// Check if public variable is existing for component
				#pragma warning disable CS0168 
				if(Util.TypeToSerializableType (component.GetType ()) != null)
				{
					try
					{
						if ((bool)typeof(NetworkModel).GetField(component.GetType ().Name.ToUpper()).GetValue(root)) 
							this.components.Add(Util.Convert(component));
					}
					catch(Exception exc)
					{
						Debug.LogWarning("Serializable class for " + component + "exists but no public bool variable to enable/disbale it.");
						this.components.Add(Util.Convert(component));
					}
				}
				#pragma warning restore CS0168
			}
		}

		public void OnBeforeSerialize()
		{
			this.componentNames = new List<string> ();
			this.componentValues = new List<string> ();

			foreach (_Component component in this.components) 
			{
				this.componentNames.Add (component.GetType ().ToString());
				this.componentValues.Add (JsonUtility.ToJson(component));
			}
		}

		public void OnAfterDeserialize()
		{
			this.components = new List<_Component> ();

			for (int i = 0; i < this.componentNames.Count && i < this.componentValues.Count; i++) 
			{
				Type type = Type.GetType (this.componentNames[i]);
				this.components.Add((_Component)JsonUtility.FromJson (this.componentValues[i], type));
			}
		}
	}

	/// <summary>
	/// Super class for all components
	/// </summary>
	/// <param name="component">Component.</param>
	[Serializable]
	abstract class _Component 
	{
		public _Component(Component component) {}

		abstract public void Apply(Component component);

		public string GetHash()
		{
			string hash = "";
			FieldInfo[] fields = this.GetType().GetFields();
			foreach(FieldInfo field in fields)
			{
				hash += field.GetValue(this).ToString() + ":";
			}
			return hash;
		}
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

		public override void Apply(Component component)
		{
			Transform transform = (Transform)component;

			// If there was no change on client side than use server values
			if (!transform.hasChanged) {
				transform.localPosition = this.p;
				transform.localRotation = this.r;
				transform.localScale = this.s;

				// Avoid triggering update of changes
				transform.hasChanged = false;
			}
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

		public override void Apply (Component component)
		{
			Camera camera = (Camera)component;

			camera.depth = this.d;
			camera.nearClipPlane = this.n;
			camera.farClipPlane = this.f;
			camera.fieldOfView = this.v;
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

		public override void Apply (Component component)
		{
			Light light = (Light)component;

			light.type = this.t;
			light.color = this.c;
			light.intensity = this.i;
			light.bounceIntensity = this.b;
		}
	}

	/// <summary>
	/// Serializable meshfilter
	/// </summary>
	[Serializable]
	class _MeshFilter : _Component
	{
		public string name;
		public Vector3[] v, n;
		public Vector2[] u;
		public int[] t;

		public _MeshFilter(Component component) : base(component) 
		{
			MeshFilter meshFilter = (MeshFilter)component;
			Mesh mesh = meshFilter.sharedMesh;

			this.name = mesh.name;
			this.v = mesh.vertices;
			this.n = mesh.normals;
			this.u = mesh.uv;
			this.t = mesh.triangles;
		}

		public override void Apply (Component component)
		{
			MeshFilter meshFilter = (MeshFilter)component;
			meshFilter.mesh = new Mesh ();
			meshFilter.mesh.name = this.name;
			meshFilter.mesh.vertices = this.v;
			meshFilter.mesh.normals = this.n;
			meshFilter.mesh.uv = this.u;
			meshFilter.mesh.triangles = this.t;
		}
	}

	/// <summary>
	/// Serializable meshrenderer
	/// </summary>
	/// <param name="component">Component.</param>
	[Serializable]
	class _MeshRenderer : _Component
	{
		public string s;

		public _MeshRenderer(Component component) : base(component) 
		{
			MeshRenderer meshRenderer = (MeshRenderer)component;
			Material material = meshRenderer.material;
			this.s = material.shader.name;
		}

		public override void Apply (Component component) 
		{
			MeshRenderer meshRenderer = (MeshRenderer)component;
			Shader shader = Shader.Find(this.s);

			if(shader != null)
			{
				Material material = new Material (shader);
				meshRenderer.material = material;
			}
		}
	}
}