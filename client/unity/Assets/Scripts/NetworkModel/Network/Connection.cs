/**
 * 
 * Connection Script of Unity Network Model
 *
 * @file Connection.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-04-30
 *
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
        private Injector injector;

        // Variables for websocket connection
        private float lastAttempt;
        private bool connectingAttempt;

        /// <summary>
        /// Constructor used to create a new Connection
        /// </summary>
        /// <param name="injector"></param>
        internal Connection(Injector injector) : base("ws://" + injector.configuration.IP + ":" + injector.configuration.PORT)
        {
            this.injector = injector;

            this.lastAttempt = -this.injector.configuration.RECONNECT;
            this.connectingAttempt = false;
        }

        /// <summary>
        /// Checks whether connection is alive and tries to connect if it isn't. Then returns whether the connection is alive
        /// </summary>
        /// <returns></returns>
        internal bool EnsureIsAlive()
        {
            // If the websocket is not alive try to connect to the server
            if (!this.IsAlive)
            {
                this.connectingAttempt = true;

                // Check if last attempt to connect is at least one Reconnect Period ago
                if (Time.realtimeSinceStartup > this.lastAttempt + this.injector.configuration.RECONNECT)
                {
                    LogUtility.Log(this.injector, LogType.INFORMATION, "Attempt to connect to server");

                    try
                    {
                        // Connect to the server
                        this.ConnectAsync();
                        this.lastAttempt = Time.realtimeSinceStartup;
                    }
                    catch (Exception)
                    {
                        LogUtility.Log(this.injector, LogType.ERROR, "Unable to connect to server");
                    }
                }
            }
            else
            {
                // If there was a recent connecting attempt, it was successful
                if (this.connectingAttempt)
                {
                    this.connectingAttempt = false;
                    LogUtility.Log(this.injector, LogType.INFORMATION, "Successful connected to server");
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
            // Send request to every channel
            foreach(string channel in this.injector.subscriptions.sendChannelList)
            {
                // Set channel of request
                request.channel = channel;

                // Refresh timestamp
                request.RefreshTimestamp();

                // Send request
                string json = JsonUtility.ToJson(request);
                if(this.injector.configuration.DEBUGSEND)
                    LogUtility.Log("Sent message " + json);
                this.SendAsync(json, null);
            }
        }

        /// <summary>
        /// Handler for new Requests from the server. Simply enqueues them to be applied during the next ApplyChanges().
        /// </summary>
        /// <param name="json"></param>
        internal void ReceiveRequest(string json)
        {
            if(this.injector.configuration.DEBUGRECEIVE)
                LogUtility.Log("Received message " + json);

            // Remove empty parameter
            json = json.Replace(",\"string1\":\"\"", "");
            json = json.Replace(",\"string2\":\"\"", "");

            this.injector.model.AddRequest(JsonUtility.FromJson<Request>(json));
        }
    }
}