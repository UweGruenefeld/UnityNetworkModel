/**
 * Network Model Script
 *
 * Attach this script to a gameobject in Unity
 * and all children gameobjects will be
 * synchronized to the specified server
 *
 * @file NetworkModel.cs
 * @author Uwe Gruenefeld
 * @version 2018-12-07
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
		[HeaderAttribute("Connection")]
		public string IP = "127.0.0.1";
		public int PORT = 8080;
        [Tooltip("Time in seconds between reconnect attempts")]
        [Range(0.5f, 5.0f)]
        public float RECONNECT = 1f;

		[HeaderAttribute("Update direction")]
		public bool RECEIVE = true;
		public bool SEND = true;

		[HeaderAttribute("Update period (in seconds)")]
		[Range(0.0f, 10.0f)]
		public float PERIOD = 0.1f;

		[HeaderAttribute("Update with timestamp")]
		public bool TIMESTAMP = false;

		[HeaderAttribute("Update components")]
        public bool TRANSFORM = true;
        public bool CAMERA = true;
		public bool LIGHT = true;
		public bool MESHFILTER = true;
		public bool MESHRENDERER = true;
        public bool BOXCOLLIDER = true;
        public bool SPHERECOLLIDER = true;

        [HeaderAttribute("Update scripts")]
        [Tooltip("Script has to be [Serializable] and in the namespace UnityEngine")]
        public bool SCRIPTS = true;

        [HeaderAttribute("Debugging")]
        public bool DEBUGGING = false;

        // Variables for websocket connection
        private WebSocket webSocket;
        private float lastAttempt;

		// Variable for update interval
		private float time;

		// Variables for gameobject synchronization
		private Dictionary<string, Node> nodeDictionary;
        private Queue<_Request> requestQueue;

		// Variables for hierarchy synchronization
		private Queue<HierarchyUpdate> hierarchyQueue;

		/// <summary>
		/// Initialize all default attributes
		/// </summary>
		void Start ()
		{
            // Default values
            this.lastAttempt = this.RECONNECT;
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
            if (this.webSocket == null)
                this.webSocket = new WebSocket("ws://" + this.IP + ":" + this.PORT);

			// If the websocket is not alive try to connect to the server
			if (!this.webSocket.IsAlive)
			{
                // Check if last attempt to connect is at least one second ago
                if (this.lastAttempt + this.RECONNECT < Time.realtimeSinceStartup)
                {
                    if (this.DEBUGGING)
                        Debug.Log("NetworkModel: Attempt to connect to server");

                    try
                    {
                        // Connect to the server
                        this.webSocket.ConnectAsync();
                        this.lastAttempt = Time.realtimeSinceStartup;

                        // Method for handling incoming messages
                        this.webSocket.OnMessage += (sender, message) => this.Response(message.Data);
                    }
                    catch (Exception)
                    {
                        Debug.LogError("NetworkModel: Unable to connect to server");
                    }
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
							DeleteGameObject (request.name);
						else
							DeleteComponent (request.name, request.parameter);
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
							string hash = Util.ComponentToSerializableComponent(component).GetHash ();
							if (hash != entry.Value) {
								updatedComponents.Add (Util.ComponentToSerializableComponent(component));
							}
						}
					}

					// Update all hashes for components
					node.UpdateHashes ();

					// Update only if components changed
					if (updatedComponents.Count > 0)
					{
						// Create request
						_Request _request = new _Request (transform.gameObject.name, _Request.UPDATE, "", this);
						_request.parameter = transform.parent.name;
						_request.components = updatedComponents;
                        this.Request(_request);
					}
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
		/// Synchronizes the game object from server request
		/// </summary>
		/// <param name="_request">Request from server</param>
		private void SynchronizeGameObject(_Request _request)
		{
			Node node = this.ReferenceToGameObject(_request);

			// Update the components
			foreach (_Component _component in _request.components)
			{
                Type type = Util.SerializableTypeToType(_component.GetType());

                if (_component.GetType() == typeof(_Script))
                {
                    _Script script = (_Script)_component;
                    type = Type.GetType(script.ScriptType());
                }

                Component component = this.ReferenceToComponent(node.gameObject, type);
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
                    Transform transform = hierarchyUpdate.gameObject.transform;

                    // Save transform values
                    Vector3 position = transform.localPosition;
                    Quaternion rotation = transform.localRotation;
                    Vector3 scale = transform.localScale;

                    transform.parent = parent;

                    // Restore transform values
                    transform.localPosition = position;
                    transform.localRotation = rotation;
                    transform.localScale = scale;

					hierarchyUpdate.gameObject.SetActive (true);
					hierarchyUpdate.gameObject.transform.hasChanged = false;
				}
				else
					this.hierarchyQueue.Enqueue (hierarchyUpdate);
			}
		}

        /// <summary>
        /// Delete gameobject
        /// </summary>
        /// <param name="gameObject">Name of gameobject as string</param>
		private void DeleteGameObject(string gameObject)
		{
			if (this.nodeDictionary.ContainsKey (gameObject))
			{
				Destroy (this.nodeDictionary [gameObject].gameObject);
				this.nodeDictionary.Remove (gameObject);
			}
		}

        /// <summary>
        /// Delete component of gameobject
        /// </summary>
        /// <param name="gameObject">Name of gameobject as string</param>
        /// <param name="component">Name of component as string</param>
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

        /// <summary>
        /// Finds or creates a reference to gameobject specified in request
        /// </summary>
        /// <param name="_request">Request that contains the name of the gameobject</param>
        /// <returns>Returns a node (representation of gameobject)</returns>
        private Node ReferenceToGameObject(_Request _request)
		{
			Node node;

            // Name of gameobject is not existing
			if (!this.nodeDictionary.ContainsKey (_request.name))
			{
				GameObject gameObject = new GameObject ();
				gameObject.name = _request.name;
				gameObject.transform.hasChanged = false;
				gameObject.SetActive(false);
				node = new Node (gameObject);
				this.hierarchyQueue.Enqueue (new HierarchyUpdate (gameObject, _request.parameter));
				this.nodeDictionary.Add (_request.name, node);
			}
            // Name of gameobject does exist
			else
			{
				node = this.nodeDictionary [_request.name];
				if (node.gameObject.transform.parent.name != _request.parameter)
				{
					node.gameObject.SetActive (false);
					this.hierarchyQueue.Enqueue (new HierarchyUpdate (node.gameObject, _request.parameter));
				}
			}

			return node;
		}

        /// <summary>
        /// Finds or creates a reference to component specified by gameobject and type
        /// </summary>
        /// <param name="gameObject">Gameobject of component</param>
        /// <param name="type">Type of component</param>
        /// <returns>Returns the component</returns>
        private Component ReferenceToComponent(GameObject gameObject, Type type)
		{
			Component component = gameObject.GetComponent(type);
			if (component != null)
				return component;

			component = gameObject.AddComponent (type);
			return component;
		}

        /// <summary>
        /// Sending a request
        /// </summary>
        /// <param name="_request">Request to send</param>
        private void Request(_Request _request)
		{
			if (this.SEND)
            {
				if (_request.parameter == this.gameObject.name)
					_request.parameter = "root";
				string json = JsonUtility.ToJson (_request);
				if(this.DEBUGGING)
					Debug.Log ("NetworkModel: Sent message " + json);
				this.webSocket.SendAsync (json, null);
			}
		}

        /// <summary>
        /// Handling a response
        /// </summary>
        /// <param name="json">Response as json string</param>
		private void Response(string json)
		{
			if (this.RECEIVE)
            {
				if(this.DEBUGGING)
					Debug.Log ("NetworkModel: Recieved message " + json);
				this.requestQueue.Enqueue (JsonUtility.FromJson<_Request> (json));
			}
		}
	}

	/// <summary>
	/// Utils
	/// </summary>
	static class Util
	{
        /// <summary>
        /// Create a binary timestamp
        /// </summary>
        /// <returns>Returns the timestamp</returns>
		public static long Timestamp()
		{
			return DateTime.Now.ToUniversalTime().ToBinary();
		}

        /// <summary>
        /// Convert a Unity Component type to a NetworkModel _Component type
        /// </summary>
        /// <param name="type">Type of Unity Component</param>
        /// <returns>Type of NetworkModel _Component</returns>
		public static Type TypeToSerializableType(Type type)
		{
            return Type.GetType("UnityEngine._" + type.Name);
		}

        /// <summary>
        /// Convert a NetworkModel _Component type to a Unity Component type
        /// </summary>
        /// <param name="type">Type of NetworkModel _Component</param>
        /// <returns>Type of Unity Component</returns>
		public static Type SerializableTypeToType(Type type)
		{
			return Type.GetType("UnityEngine." + type.Name.Substring(1) + ", UnityEngine");
		}

        /// <summary>
        /// Convert a Unity Component to a NetworkModel Component
        /// </summary>
        /// <param name="component">Unity Component</param>
        /// <returns>NetworkModel _Component</returns>
		public static _Component ComponentToSerializableComponent(Component component)
		{
			Type type = Util.TypeToSerializableType (component.GetType ());

            if (component.GetType().IsSubclassOf(typeof(MonoBehaviour)))
                type = typeof(_Script);

            if (type != null)
				return (_Component)Activator.CreateInstance (type, new Object[]{ component });
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

        /// <summary>
        /// Transforms a GameObject to a Node
        /// </summary>
        /// <param name="gameObject"></param>
		public Node(GameObject gameObject)
		{
			this.gameObject = gameObject;
			this.UpdateHashes ();
		}

        /// <summary>
        /// Updates the hashvalues for a Node
        /// </summary>
		public void UpdateHashes()
		{
			this.componentDictionary = new Dictionary<Type, string>();

			foreach (Component component in this.gameObject.GetComponents(typeof(Component)))
			{
                Type type = Util.TypeToSerializableType(component.GetType());

                if (component.GetType().IsSubclassOf(typeof(MonoBehaviour)))
                    type = typeof(_Script);

				if(type != null)
					this.componentDictionary.Add(component.GetType(), Util.ComponentToSerializableComponent(component).GetHash());
			}
		}

        /// <summary>
        /// Adds missing components of a Node
        /// </summary>
		public void AddMissingComponents()
		{
			foreach (Component component in this.gameObject.GetComponents(typeof(Component)))
			{
				Type type = Util.TypeToSerializableType(component.GetType ());

                if (component.GetType().IsSubclassOf(typeof(MonoBehaviour)))
                    type = typeof(_Script);

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

        /// <summary>
        /// Describes a changed parent for a GameObject
        /// </summary>
        /// <param name="gameObject">GameObject</param>
        /// <param name="parent">The name of the new parent GameObject</param>
		public HierarchyUpdate(GameObject gameObject, string parent)
		{
			this.gameObject = gameObject;
			this.parent = parent;
		}
	}

	/// <summary>
	/// Class for all requests to server
	/// </summary>
	[Serializable]
	class _Request : ISerializationCallbackReceiver
	{
		public const string DELETE = "d";
		public const string INSERT = "i";
		public const string UPDATE = "u";

		public string name;
		public string type;
		public string parameter;

		public long timestamp;

		public List<string> componentNames;
		public List<string> componentValues;

		[NonSerialized]
		public List<_Component> components;

        /// <summary>
        /// Constructor for _Request
        /// </summary>
        /// <param name="name">Name of GameObject</param>
        /// <param name="type">Type of the Component as string</param>
        /// <param name="parameter">Parameter (DELETE, INSERT or UPDATE)</param>
        /// <param name="root">Reference to NetworkModel object</param>
		public _Request(string name, string type, string parameter, NetworkModel root)
		{
			this.name = name;
			this.type = type;
			this.parameter = parameter;

			this.timestamp = 0;
			if(root.TIMESTAMP)
				this.timestamp = Util.Timestamp ();

			this.components = new List<_Component> ();
		}

        /// <summary>
        /// Constructor for _Request
        /// </summary>
        /// <param name="gameObject">GameObject</param>
        /// <param name="type">Type of the Component as string</param>
        /// <param name="root">Reference to NetworkModel object</param>
		public _Request(GameObject gameObject, string type, NetworkModel root)
		{
			this.name = gameObject.name;
			this.type = type;
			this.parameter = gameObject.transform.parent.name;

			this.timestamp = 0;
			if(root.TIMESTAMP)
				this.timestamp = Util.Timestamp ();

			this.components = new List<_Component> ();

			// Save supported and enabled components of gameobject
			foreach (Component component in gameObject.GetComponents(typeof(Component)))
            {
                if (component.GetType().IsSubclassOf(typeof(MonoBehaviour)) && root.SCRIPTS)
                {
                    this.components.Add(new _Script(component));
                }
                else if (Util.TypeToSerializableType (component.GetType ()) != null)
				{
					try
					{
                        // Check if public variable is existing for component
                        if ((bool)typeof(NetworkModel).GetField(component.GetType ().Name.ToUpper()).GetValue(root))
							this.components.Add(Util.ComponentToSerializableComponent(component));
					}
					catch(Exception)
					{
						Debug.LogWarning("NetworkModel: Serializable class for " + component + "exists but no public bool variable to enable/disbale it.");
						this.components.Add(Util.ComponentToSerializableComponent(component));
					}
				}
                else
                    if (root.DEBUGGING)
                        Debug.Log("NetworkModel: Component " + component.GetType() + " is unknown and cannot be synchronized.");
			}
		}

        /// <summary>
        /// Before the request is send to the server
        /// </summary>
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

        /// <summary>
        /// After the response is received from the server
        /// </summary>
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
	/// Super class for all serializable Components
	/// </summary>
	[Serializable]
	abstract class _Component
	{
		public _Component(Component component) {}

		abstract public void Apply(Component component);

		public virtual string GetHash()
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
    /// Serializable Script
    /// </summary>
    [Serializable]
    class _Script : _Component
    {
        public string type;
        public string value;

        public _Script(Component component) : base(component)
        {
            this.type = component.GetType().AssemblyQualifiedName;
            this.value = JsonUtility.ToJson(component);
        }

        public override void Apply(Component component)
        {
            JsonUtility.FromJsonOverwrite(this.value, component);
        }

        public string ScriptType()
        {
            return this.type;
        }
    }

    /// <summary>
    /// Serializable Transform
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
			if (!transform.hasChanged)
            { 
                transform.localPosition = this.p;
                transform.localRotation = this.r;
				transform.localScale = this.s;

				// Avoid triggering update of changes
				transform.hasChanged = false;
            }
		}
	}

	/// <summary>
	/// Serializable Camera
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
	/// Serializable Light
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
	/// Serializable MeshFilter
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
	/// Serializable MeshRenderer
	/// </summary>
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

    /// <summary>
    /// Serializable BoxCollider
    /// </summary>
    class _BoxCollider : _Component
    {
        public Vector3 c, s;

        public _BoxCollider(Component component) : base(component)
        {
            BoxCollider boxcollider = (BoxCollider)component;

            this.c = boxcollider.center;
            this.s = boxcollider.size;
        }

        public override void Apply(Component component)
        {
            BoxCollider boxcollider = (BoxCollider)component;

            boxcollider.center = this.c;
            boxcollider.size = this.s;
        }
    }

    /// <summary>
    /// Serializable SphereCollider
    /// </summary>
    class _SphereCollider : _Component
    {
        public Vector3 c;
        public float r;

        public _SphereCollider(Component component) : base(component)
        {
            SphereCollider spherecollider = (SphereCollider)component;

            this.c = spherecollider.center;
            this.r = spherecollider.radius;
        }

        public override void Apply(Component component)
        {
            SphereCollider spherecollider = (SphereCollider)component;

            spherecollider.center = this.c;
            spherecollider.radius = this.r;
        }
    }

    /*
     * TEMPLATE FOR NEW COMPONENT
     * 
     * [Serializable]
     * class _NAMEOFCOMPONENT : _Component
     * {
     *     CLASS VARIABLES TO SYNCHRONIZE
     *     
     *     // Prepare component for sending to server
     *     public _NAMEOFCOMPONENT(Component component) : base(component)
     *     {
     *          SAVE VARIABLES FROM COMPONENT IN CLASS VARIABLES
     *     }
     *     
     *     // Apply received values to component
     *     public override void Apply (Component component)
     *     {
     *          RESTORE CLASS VARIABLES INTO VARIABLES FROM COMPONENT
     *     }
     * }
     *
     */
}
