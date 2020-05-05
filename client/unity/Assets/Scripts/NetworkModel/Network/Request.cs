/**
 * 
 * Request Script of Unity Network Model
 *
 * @file Request.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-05-05
 *
 **/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Request for changes to Objects, Components and Resources to be sent between Client and Server
    /// </summary>
    [Serializable]
    internal class Request : ISerializationCallbackReceiver
    {
        private Injector injector;

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
        /// <param name="injector"></param>
        /// <param name="messageType"></param>
        /// <param name="updateType"></param>
        /// <param name="name"></param>
        private Request(Injector injector, string messageType, string updateType, string name)
        {
            this.injector = injector;
            this.messageType = messageType;
            this.updateType = updateType;
            this.name = name;

            this.channel = "";
            this.iteration = 0;

            this.RefreshTimestamp();
        }

        /// <summary>
        /// Refreshes current timestamp
        /// </summary>
        internal void RefreshTimestamp()
        {
            if (injector.configuration.TIME)
                this.timestamp = DateTime.Now.ToUniversalTime().ToBinary();
            else
                this.timestamp = 0;
        }

        /// <summary>
        /// Create Request to update specific GameObject
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="gameObject"></param>
        /// <returns>Request</returns>
        internal static Request UpdateObject(Injector injector, GameObject gameObject)
        {
            Request request = new Request(injector, Request.OBJECT, Request.UPDATE, gameObject.name);
            request.parent = gameObject.transform.parent.name;

            // If the Parent GameObject is the GameObject with the NetworkModel Configuration attached, then assign the Alias Name of the root GameObject
            if (request.parent == injector.configuration.gameObject.name)
                request.parent = Model.ROOT_NAME;
            
            return request;
        }

        /// <summary>
        /// Create Request to delete specific GameObject
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="name"></param>
        /// <returns>Request</returns>
        internal static Request DeleteObject(Injector injector, string name)
        {
            return new Request(injector, Request.OBJECT, Request.DELETE, name);
        }


        /// <summary>
        /// Create Request to update a specific Resource
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="name"></param>
        /// <param name="resourceType"></param>
        /// <param name="resource"></param>
        /// <returns>Request</returns>
        internal static Request UpdateResource(Injector injector, string name, Type resourceType, AbstractResource resource)
        {
            Request request = new Request(injector, Request.RESOURCE, Request.UPDATE, name);
            request.resource = resource;
            request.resourceType = resourceType;
            return request;
        }

        /// <summary>
        /// Create Request to delete a specific Resource
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="name"></param>
        /// <returns>Request</returns>
        internal static Request DeleteResource(Injector injector, string name)
        {
            return new Request(injector, Request.RESOURCE, Request.DELETE, name);
        }

        /// <summary>
        /// Create Request to update a List of Components belonging to a specific GameObject
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="name"></param>
        /// <param name="components"></param>
        /// <returns>Request</returns>
        internal static Request UpdateComponents(Injector injector, string name, List<AbstractComponent> components)
        {
            Request request = new Request(injector, Request.COMPONENT, Request.UPDATE, name);
            request.components = components;
            return request;
        }

        /// <summary>
        /// Create Request to delete a List of Components belonging to a specific GameObject
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="name"></param>
        /// <param name="types"></param>
        /// <returns>Request</returns>
        internal static Request DeleteComponents(Injector injector, string name, List<Type> types)
        {
            Request request = new Request(injector, Request.COMPONENT, Request.DELETE, name);
            request.componentTypes = types;
            return request;
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

        /// <summary>
        /// Encodes a String representing a NetworkModel class into a string representing a UnityEngine class
        /// </summary>
        /// <param name="name"></param>
        private String EncodeClass(String name)
        {
            return name.Replace("UnityNetworkModel.", "").Replace("Serializable", "");
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

        /// <summary>
        /// Encodes a String representing a UnityEngine class into a string representing a NetworkModel class
        /// </summary>
        /// <param name="name"></param>
        private String DecodeClass(String name)
        {
            return "UnityNetworkModel." + name + "Serializable";
        }
    }
}