/**
 * 
 * Connection Script of Unity Network Model
 *
 * @file Connection.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-04-30
 **/
using System;
using WebSocketSharp;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Manages connection to webserver. Provides Sender and Receiver capabilities.
    /// </summary>
    internal class Connection : WebSocket
    {
        private NetworkModel config;
        private Bundle bundle;

        // Variables for websocket connection
        private float lastAttempt;

        public Connection(NetworkModel config, Bundle bundle) : base("ws://" + config.IP + ":" + config.PORT)
        {
            this.config = config;
            this.bundle = bundle;

            this.lastAttempt = -config.RECONNECT;
        }

        /// <summary>
        /// Checks whether connection is alive and tries to connect if it isn't. Then returns whether the connection is alive
        /// </summary>
        /// <returns></returns>
        public bool EnsureIsAlive()
        {
            // If the websocket is not alive try to connect to the server
            if (!this.IsAlive)
            {
                // Check if last attempt to connect is at least one Reconnect Period ago
                if (Time.realtimeSinceStartup > this.lastAttempt + config.RECONNECT)
                {
                    if (config.DEBUGSEND || config.DEBUGREC)
                        Debug.Log("NetworkModel: Attempt to connect to server");

                    try
                    {
                        // Connect to the server
                        this.ConnectAsync();
                        this.lastAttempt = Time.realtimeSinceStartup;
                    }
                    catch (Exception)
                    {
                        Debug.LogError("NetworkModel: Unable to connect to server");
                    }
                }
            }
            // Return if websocket is alive
            return this.IsAlive;
        }

        /// <summary>
        /// Asynchronously send Request to the server
        /// </summary>
        /// <param name="request"></param>
        internal void SendRequest(Request request)
        {
            string json = JsonUtility.ToJson(request);
            if (config.DEBUGSEND)
                Debug.Log("NetworkModel: Sent message " + json);
            this.SendAsync(json, null);
        }

        /// <summary>
        /// Handler for new Requests from the server. Simply enqueues them to be applied during the next ApplyChanges().
        /// </summary>
        /// <param name="json"></param>
        internal void ReceiveRequest(string json)
        {
            if (config.DEBUGREC)
                Debug.Log("NetworkModel: Received message " + json);

            // Remove empty parameter
            json = json.Replace(",\"string1\":\"\"", "");
            json = json.Replace(",\"string2\":\"\"", "");

            this.bundle.model.AddRequest(JsonUtility.FromJson<Request>(json));
        }
    }
}