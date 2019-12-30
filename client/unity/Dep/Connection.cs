using System;
using WebSocketSharp;

namespace UnityEngine
{
    /// <summary>
    /// Manages connection to webserver. Provides Sender and Receiver capabilities.
    /// </summary>
    class Connection : WebSocket
    {
        private NetworkModel config;

        // Variables for websocket connection
        private float lastAttempt;

        public Connection(NetworkModel config) : base("ws://" + config.IP + ":" + config.PORT)
        {
            this.config = config;
            lastAttempt = -config.RECONNECT;
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
    }
}