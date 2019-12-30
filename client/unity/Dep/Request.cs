using System;
using System.Collections.Generic;

namespace UnityEngine
{
    /// <summary>
    /// Request for changes to Assets, GameObjects and Components to be sent between Client and Server
    /// </summary>
    [Serializable]
    class Request : ISerializationCallbackReceiver
    {
        // Types of request
        public const string ASSET = "a";
        public const string GAMEOBJECT = "g";
        public const string COMPONENT = "c";

        public const string UPDATE = "u";
        public const string DELETE = "d";

        // Parameters used by all requests
        [NonSerialized]
        public string objectType;
        [NonSerialized]
        public string updateType;
        public string type;
        public string name;
        public string channel;
        public long timestamp;

        [NonSerialized]
        public int iteration;

        // Human-readable parameters for specific types of request
        [NonSerialized]
        public string parent;
        [NonSerialized]
        public Serializer.Asset asset;
        [NonSerialized]
        public Type assetType;
        [NonSerialized]
        public List<Serializer.Component> components;
        [NonSerialized]
        public List<Type> componentTypes;

        // Serialized parameters that human-readable parameters are mapped to while sending over the network
        public string StringParam1;
        public string StringParam2;
        public List<string> ListParam1;
        public List<string> ListParam2;

        /// <summary>
        /// Common constructor used by static Request-generating methdos
        /// </summary>
        /// <param name="objectType"></param>
        /// <param name="updateType"></param>
        /// <param name="name"></param>
        /// <param name="config"></param>
        private Request(string objectType, string updateType, string name, NetworkModel config)
        {
            this.objectType = objectType;
            this.updateType = updateType;
            this.name = name;
            this.channel = config.SENDCHANNEL.Trim();
            this.iteration = 0;
            if (config.TIMESTAMP)
            {
                this.timestamp = DateTime.Now.ToUniversalTime().ToBinary();
            }
            else
            {
                this.timestamp = 0;
            }
        }

        /// <summary>
        /// Create Request to update a specific Asset
        /// </summary>
        /// <param name="name"></param>
        /// <param name="assetType"></param>
        /// <param name="asset"></param>
        /// <param name="config"></param>
        /// <returns>Request</returns>
        internal static Request UpdateAsset(string name, Type assetType, Serializer.Asset asset, NetworkModel config)
        {
            Request req = new Request(ASSET, UPDATE, name, config);
            req.asset = asset;
            req.assetType = assetType;
            return req;
        }

        /// <summary>
        /// Create Request to delete a specific Asset
        /// </summary>
        /// <param name="name"></param>
        /// <param name="config"></param>
        /// <returns>Request</returns>
        internal static Request DeleteAsset(string name, NetworkModel config)
        {
            Request req = new Request(ASSET, DELETE, name, config);
            return req;
        }

        /// <summary>
        /// Create Request to update specific GameObject
        /// </summary>
        /// <param name="go"></param>
        /// <param name="config"></param>
        /// <returns>Request</returns>
        internal static Request UpdateGO(GameObject go, NetworkModel config)
        {
            Request req = new Request(GAMEOBJECT, UPDATE, go.name, config);
            req.parent = go.transform.parent.name;
            if (req.parent == config.gameObject.name)
            {
                req.parent = "root";
            }
            return req;
        }

        /// <summary>
        /// Create Request to delete specific GameObject
        /// </summary>
        /// <param name="name"></param>
        /// <param name="config"></param>
        /// <returns>Request</returns>
        public static Request DeleteGO(string name, NetworkModel config)
        {
            Request req = new Request(GAMEOBJECT, DELETE, name, config);
            return req;
        }

        /// <summary>
        /// Create Request to update a List of Components belonging to a specific GameObject
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comps"></param>
        /// <param name="config"></param>
        /// <returns>Request</returns>
        public static Request UpdateComponents(string name, List<Serializer.Component> comps, NetworkModel config)
        {
            Request req = new Request(COMPONENT, UPDATE, name, config);
            req.components = comps;
            return req;
        }

        /// <summary>
        /// Create Request to delete a List of Components belonging to a specific GameObject
        /// </summary>
        /// <param name="name"></param>
        /// <param name="types"></param>
        /// <param name="config"></param>
        /// <returns>Request</returns>
        public static Request DeleteComponents(string name, List<Type> types, NetworkModel config)
        {
            Request req = new Request(COMPONENT, DELETE, name, config);
            req.componentTypes = types;
            return req;
        }

        /// <summary>
        /// Before the request is sent to the server, packs human-readable parameters into serializable parameters
        /// </summary>
        public void OnBeforeSerialize()
        {
            this.type = this.objectType + this.updateType;
            switch (type)
            {
                case ASSET + UPDATE:
                    this.StringParam1 = assetType.ToString();
                    this.StringParam2 = JsonUtility.ToJson(asset);
                    break;
                case GAMEOBJECT + UPDATE:
                    this.StringParam1 = this.parent;
                    break;
                case COMPONENT + UPDATE:
                    this.ListParam1 = new List<string>();
                    this.ListParam2 = new List<string>();
                    foreach (Serializer.Component component in this.components)
                    {
                        this.ListParam1.Add(component.GetType().ToString());
                        this.ListParam2.Add(JsonUtility.ToJson(component));
                    }
                    break;
                case COMPONENT + DELETE:
                    this.ListParam1 = new List<string>();
                    foreach (Type type in this.componentTypes)
                    {
                        this.ListParam1.Add(type.ToString());
                    }
                    break;
            }
        }

        /// <summary>
        /// After the response is received from the server, unpacks serialized parameters into human-readable parameters
        /// </summary>
        public void OnAfterDeserialize()
        {
            this.objectType = this.type.Substring(0, 1);
            this.updateType = this.type.Substring(1, 1);
            switch (type)
            {
                case ASSET + UPDATE:
                    this.assetType = Type.GetType(StringParam1);
                    this.asset = (Serializer.Asset)JsonUtility.FromJson(this.StringParam2, assetType);
                    break;
                case GAMEOBJECT + UPDATE:
                    this.parent = this.StringParam1;
                    break;
                case COMPONENT + UPDATE:
                    this.components = new List<Serializer.Component>();
                    for (int i = 0; i < this.ListParam1.Count && i < this.ListParam2.Count; i++)
                    {
                        Type type = Type.GetType(this.ListParam1[i]);
                        this.components.Add((Serializer.Component)JsonUtility.FromJson(this.ListParam2[i], type));
                    }
                    break;
                case COMPONENT + DELETE:
                    this.componentTypes = new List<Type>();
                    foreach (string typeName in ListParam1)
                    {
                        this.componentTypes.Add(Type.GetType(typeName));
                    }
                    break;
            }
        }
    }
}