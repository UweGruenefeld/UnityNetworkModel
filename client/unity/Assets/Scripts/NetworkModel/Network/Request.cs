/**
 * 
 * Request Script of Unity Network Model
 *
 * @file Request.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-04-30
 **/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Request for changes to Ressources, GameObjects and Components to be sent between Client and Server
    /// </summary>
    [Serializable]
    internal class Request : ISerializationCallbackReceiver
    {
        // Types of request
        public const string RESOURCE = "r";
        public const string OBJECT = "o";
        public const string COMPONENT = "c";

        public const string UPDATE = "u";
        public const string DELETE = "d";

        // Parameters used by all requests
        [NonSerialized]
        public string messageType;
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
        public AbstractResource resource;
        [NonSerialized]
        public Type resourceType;
        [NonSerialized]
        public List<AbstractComponent> components;
        [NonSerialized]
        public List<Type> componentTypes;

        // Serialized parameters that human-readable parameters are mapped to while sending over the network
        public List<string> string1;
        public List<string> string2;

        /// <summary>
        /// Common constructor used by static Request-generating methdos
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="updateType"></param>
        /// <param name="name"></param>
        /// <param name="config"></param>
        private Request(string messageType, string updateType, string name, NetworkModel config)
        {
            this.messageType = messageType;
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
        /// Create Request to update a specific Resource
        /// </summary>
        /// <param name="name"></param>
        /// <param name="resourceType"></param>
        /// <param name="resource"></param>
        /// <param name="config"></param>
        /// <returns>Request</returns>
        internal static Request UpdateResource(string name, Type resourceType, AbstractResource resource, NetworkModel config)
        {
            Request req = new Request(Request.RESOURCE, Request.UPDATE, name, config);
            req.resource = resource;
            req.resourceType = resourceType;
            return req;
        }

        /// <summary>
        /// Create Request to delete a specific Resource
        /// </summary>
        /// <param name="name"></param>
        /// <param name="config"></param>
        /// <returns>Request</returns>
        internal static Request DeleteResource(string name, NetworkModel config)
        {
            Request req = new Request(Request.RESOURCE, Request.DELETE, name, config);
            return req;
        }

        /// <summary>
        /// Create Request to update specific GameObject
        /// </summary>
        /// <param name="go"></param>
        /// <param name="config"></param>
        /// <returns>Request</returns>
        internal static Request UpdateObject(GameObject gameObject, NetworkModel config)
        {
            Request req = new Request(Request.OBJECT, Request.UPDATE, gameObject.name, config);
            req.parent = gameObject.transform.parent.name;
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
        public static Request DeleteObject(string name, NetworkModel config)
        {
            Request req = new Request(Request.OBJECT, Request.DELETE, name, config);
            return req;
        }

        /// <summary>
        /// Create Request to update a List of Components belonging to a specific GameObject
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comps"></param>
        /// <param name="config"></param>
        /// <returns>Request</returns>
        public static Request UpdateComponents(string name, List<AbstractComponent> comps, NetworkModel config)
        {
            Request req = new Request(Request.COMPONENT, Request.UPDATE, name, config);
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
            Request req = new Request(Request.COMPONENT, Request.DELETE, name, config);
            req.componentTypes = types;
            return req;
        }

        /// <summary>
        /// Before the request is sent to the server, packs human-readable parameters into serializable parameters
        /// </summary>
        public void OnBeforeSerialize()
        {
            this.type = this.messageType + this.updateType;
            switch (type)
            {
                case Request.RESOURCE + Request.UPDATE:
                    this.string1 = new List<string>() { this.EncodeClass(resourceType.ToString()) };
                    this.string2 = new List<string>() { JsonUtility.ToJson(resource) };
                    break;
                case Request.OBJECT + Request.UPDATE:
                    this.string1 = new List<string>() { this.parent };
                    break;
                case Request.COMPONENT + Request.UPDATE:
                    this.string1 = new List<string>();
                    this.string2 = new List<string>();
                    foreach (AbstractComponent component in this.components)
                    {
                        this.string1.Add(this.EncodeClass(component.GetType().ToString()));
                        this.string2.Add(JsonUtility.ToJson(component));
                    }
                    break;
                case Request.COMPONENT + Request.DELETE:
                    this.string1 = new List<string>();
                    foreach (Type type in this.componentTypes)
                    {
                        this.string1.Add(this.EncodeClass(type.ToString()));
                    }
                    break;
            }
        }

        private String EncodeClass(String name)
        {
            return name.Replace("UnityNetworkModel.", "").Replace("Clone", "");
        }

        /// <summary>
        /// After the response is received from the server, unpacks serialized parameters into human-readable parameters
        /// </summary>
        public void OnAfterDeserialize()
        {
            this.messageType = this.type.Substring(0, 1);
            this.updateType = this.type.Substring(1, 1);
            switch (type)
            {
                case Request.RESOURCE + Request.UPDATE:
                    this.resourceType = Type.GetType(this.DecodeClass(string1[0]));
                    this.resource = (AbstractResource)JsonUtility.FromJson(this.string2[0], this.resourceType);
                    break;
                case Request.OBJECT + Request.UPDATE:
                    this.parent = this.string1[0];
                    break;
                case Request.COMPONENT + Request.UPDATE:
                    this.components = new List<AbstractComponent>();
                    for (int i = 0; i < this.string1.Count && i < this.string2.Count; i++)
                    {
                        Type type = Type.GetType(this.DecodeClass(this.string1[i]));
                        this.components.Add((AbstractComponent)JsonUtility.FromJson(this.string2[i], type));
                    }
                    break;
                case Request.COMPONENT + Request.DELETE:
                    this.componentTypes = new List<Type>();
                    foreach (string typeName in string1)
                    {
                        this.componentTypes.Add(Type.GetType(this.DecodeClass(typeName)));
                    }
                    break;
            }
        }

        private String DecodeClass(String name)
        {
            return "UnityNetworkModel." + name + "Clone";
        }
    }
}