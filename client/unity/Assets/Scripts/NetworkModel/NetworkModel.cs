/**
 * 
 * Main Script of Unity Network Model
 * 
 * Attach this script to a GameObject in Unity
 * All children of that GameObject can be synchronized with the server
 * 
 * Currently supported components are
 * - BoxCollider
 * - Camera
 * - Light
 * - LineRenderer
 * - MeshCollider
 * - MeshFilter
 * - MeshRenderer
 * - Script
 * - SphereCollider
 * - Transform
 * 
 * Currently supported ressources are
 * - Material
 * - Mesh
 * - Texture2D
 *
 * @file NetworkModel.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-04-30
 **/
using System.Collections.Generic;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Main class of the UnityNetworkModel component
    /// </summary>
    public class NetworkModel : MonoBehaviour
    {
        // Connection
        public string IP = "127.0.0.1";
        public int PORT = 8080;
        public float RECONNECT = 1;
        public float PERIOD = 0.1f;

        // Update
        public bool RECEIVE = true;
        public bool SEND = true;

        // Sender
        public bool TIMESTAMP = false;
        public bool TRANSFORM = true;
        public bool CAMERA = true;
        public bool LIGHT = true;
        public bool MESHFILTER = true;
        public bool MESHRENDERER = true;
        public bool LINERENDERER = true;
        public bool MESHCOLLIDER = true;
        public bool BOXCOLLIDER = true;
        public bool SPHERECOLLIDER = true;
        public bool SCRIPTS = true;

        // Receiver
        public string RECEIVECHANNELS = "default";
        public bool EXISTINGCOMPENTS = false;
        public bool EXISTINGRESOURCES = false;

        // Debugging
        public string SENDCHANNEL = "default";
        public bool DEBUGSEND = false;
        public bool DEBUGREC = false;

        // Bundle
        private Bundle bundle;

        private void Start()
        {
            this.bundle = new Bundle();

            this.bundle.objectStore = new ObjectStore(this, this.bundle);
            this.bundle.objectStore.Add("root", this.gameObject);
            this.bundle.resourceStore = new ResourceStore(this, this.bundle);
            this.bundle.resourceStore.Add("null", null);

            this.bundle.serializer = new Serializer(this, this.bundle);

            this.bundle.model = Model.CreateModel(this, this.bundle, this.gameObject);

            this.bundle.connection = new Connection(this, this.bundle);
            this.bundle.connection.OnMessage += (sender, message) => this.bundle.connection.ReceiveRequest(message.Data);
        }

        private void Update()
        {
            // Check if update period has passed
            this.bundle.time += Time.deltaTime;
            if (this.bundle.time > this.PERIOD)
            {
                this.bundle.time = 0f;
                // Try to establish connection, only proceed if successful
                if (this.bundle.connection.EnsureIsAlive())
                {
                    if (this.RECEIVE)
                    {
                        // Apply change-requests that were received from the server since the last update
                        this.bundle.model.ApplyChanges();

                        // If the receivechannels were altered, handle subscribing and unsubscribing
                        if (RECEIVECHANNELS != this.bundle.OLDRECEIVECHANNELS)
                        {
                            ISet<string> _rChannels = new HashSet<string>(RECEIVECHANNELS.Split(','));
                            ISet<string> _oldRChannels = new HashSet<string>(this.bundle.OLDRECEIVECHANNELS.Split(','));
                            ISet<string> rChannels = new HashSet<string>();
                            ISet<string> oldRChannels = new HashSet<string>();
                            foreach (string channel in _rChannels)
                            {
                                rChannels.Add(channel.Trim());
                            }
                            foreach (string channel in _oldRChannels)
                            {
                                oldRChannels.Add(channel.Trim());
                            }
                            ISet<string> newRChannels = new HashSet<string>(rChannels);
                            newRChannels.ExceptWith(oldRChannels);
                            oldRChannels.ExceptWith(rChannels);

                            foreach (string channel in oldRChannels)
                            {
                                if (channel != "")
                                    this.bundle.connection.SendAsync("{\"type\":\"cl\", \"channel\":\"" + channel + "\"}", null);
                            }
                            foreach (string channel in newRChannels)
                            {
                                if (channel != "")
                                    this.bundle.connection.SendAsync("{\"type\":\"cs\", \"channel\":\"" + channel + "\"}", null);
                            }
                            this.bundle.OLDRECEIVECHANNELS = RECEIVECHANNELS;
                        }
                    }

                    if (this.SEND)
                    {
                        // Send change-requests for modifications since last update to the server
                        this.bundle.model.TrackChanges();
                    }
                };
            }
        }
    }
}
