/**
 * Network Model Script
 *
 * Attach this script to a gameobject in Unity
 * and all children gameobjects will be
 * synchronized to the specified server
 * 
 * Currently supported components are
 * - Transform
 * - Camera
 * - Light
 * - MeshFilter
 * - MeshRenderer
 * - LineRenderer
 * - MeshCollider
 * - BoxCollider
 * - SphereCollider
 * - Scripts
 * 
 * Currently supported assets are
 * - Mesh
 * - Material
 * - Texture2D
 * 
 * To add new synchronizable components or assets search this file and Serializer.js for
 * TEMPLATE FOR NEW COMPONENT
 * or
 * TEMPLATE FOR NEW ASSET
 *
 * @file NetworkModel.cs
 * @author Uwe Gruenefeld
 * @author Tobias Lunte
 * @version 2019-12-30
 **/
 
 using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine
{
    /// <summary>
    /// Wrapper class to manage different components and run the update loop.
    /// Holds configuration
    /// </summary>
    public class NetworkModel : MonoBehaviour
    {
        // Config variables
        // Connection
        public string IP = "127.0.0.1";
        public int PORT = 8080;
        public float RECONNECT = 1f;
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
        // public bool COMPONENTNAME = true; //TEMPLATE FOR NEW COMPONENT
        // Receiver
        public string RECEIVECHANNELS = "default";
        public bool EXISTINGCOMP = false;
        public bool EXISTINGASSETS = false;
        // Debugging
        public string SENDCHANNEL = "default";
        public bool DEBUGSEND = false;
        public bool DEBUGREC = false;

        // Components
        private Serializer serializer;
        private GOStore goStore;
        private AssetStore assetStore;
        private Connection connection;
        private WorldModel worldModel;

        // Store old receiveChannels to check for changes
        private string OLDRECEIVECHANNELS = "";

        // time since last update loop
        private float time;

        private void Start()
        {
            //Set up components
            this.serializer = new Serializer(this);
            this.goStore = new GOStore(serializer, this);
            this.goStore.Add("root", this.gameObject);
            this.assetStore = new AssetStore(serializer, this);
            this.assetStore.Add("null", null);
            this.serializer.SetAssetStore(assetStore);
            this.connection = new Connection(this);
            this.worldModel = WorldModel.CreateWorldModel(this.gameObject, goStore, assetStore, connection, serializer, this);

            //Set up handler for received messages
            this.connection.OnMessage += (sender, message) => worldModel.ReceiveRequest(message.Data);
        }

        private void Update()
        {
            //check if update period has passed
            this.time += Time.deltaTime;
            if (this.time > this.PERIOD)
            {
                this.time = 0f;
                //try to establish connection, only proceed if successful
                if (connection.EnsureIsAlive())
                {
                    if (this.RECEIVE)
                    {
                        //apply change-requests that were received from the server since the last update
                        worldModel.ApplyChanges();

                        //if the receivechannels were altered, handle subscribing and unsubscribing
                        if (RECEIVECHANNELS != OLDRECEIVECHANNELS)
                        {
                            ISet<string> _rChannels = new HashSet<string>(RECEIVECHANNELS.Split(','));
                            ISet<string> _oldRChannels = new HashSet<string>(OLDRECEIVECHANNELS.Split(','));
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
                                    connection.SendAsync("{\"subscriptionChange\":\"unsubscribe\", \"channel\":\"" + channel + "\"}", null);
                            }
                            foreach (string channel in newRChannels)
                            {
                                if (channel != "")
                                    connection.SendAsync("{\"subscriptionChange\":\"subscribe\", \"channel\":\"" + channel + "\"}", null);
                            }
                            OLDRECEIVECHANNELS = RECEIVECHANNELS;
                        }
                    }

                    if (this.SEND)
                    {
                        //send change-requests for modifications since last update to the server
                        worldModel.TrackChanges();
                    }
                };
            }
        }
    }

    /// <summary>
    /// Editor interface for public variables
    /// </summary>
#if UNITY_EDITOR
    [CustomEditor(typeof(NetworkModel))]
    public class MyScriptEditor : Editor
    {
        override public void OnInspectorGUI()
        {
            var nwm = target as NetworkModel;
            EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);
            nwm.IP = EditorGUILayout.TextField("IP", nwm.IP);
            nwm.PORT = EditorGUILayout.IntField("Port", nwm.PORT);
            nwm.RECONNECT = EditorGUILayout.Slider(new GUIContent("Reconnect Period", "Minimum time in seconds between reconnect attempts. Will never be faster than update period."), nwm.RECONNECT, 0.5f, 5.0f);
            nwm.PERIOD = EditorGUILayout.Slider(new GUIContent("Update Period", "Minimum time in seconds between two updates."), nwm.PERIOD, 0.0f, 10.0f);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Update direction", EditorStyles.boldLabel);
            nwm.SEND = EditorGUILayout.Toggle("Send", nwm.SEND);
            nwm.RECEIVE = EditorGUILayout.Toggle("Receive", nwm.RECEIVE);
            EditorGUILayout.Space();
            if (nwm.SEND)
            {
                EditorGUILayout.LabelField("Sender properties", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                nwm.SENDCHANNEL = EditorGUILayout.TextField(new GUIContent("Send on channel", "Singular channel on which to broadcast changes. May not contain \",\"."), nwm.SENDCHANNEL);
                nwm.TIMESTAMP = EditorGUILayout.Toggle("Update with timestamp", nwm.TIMESTAMP);
                EditorGUILayout.LabelField(new GUIContent("Update components", "For which components should updates be sent"), EditorStyles.boldLabel);
                nwm.TRANSFORM = EditorGUILayout.Toggle(new GUIContent("Transform", "localPosition, localRotation, localScale, tag of gameObject"), nwm.TRANSFORM);
                nwm.CAMERA = EditorGUILayout.Toggle(new GUIContent("Camera", "depth, nearClipPlane, farClipPlane, fieldOfView, backgroundColor, clearFlags"), nwm.CAMERA);
                nwm.LIGHT = EditorGUILayout.Toggle(new GUIContent("Light", "type, color, intensity, bounceIntensity"), nwm.LIGHT);
                nwm.MESHFILTER = EditorGUILayout.Toggle(new GUIContent("MeshFilter", "mesh"), nwm.MESHFILTER);
                nwm.MESHRENDERER = EditorGUILayout.Toggle(new GUIContent("MeshRenderer", "material"), nwm.MESHRENDERER);
                nwm.LINERENDERER = EditorGUILayout.Toggle(new GUIContent("LineRenderer", "material, positions, widthMultiplier, loop, numCapVertices, numCornerVertices, textureMode, startColor, endColor"), nwm.LINERENDERER);
                nwm.MESHCOLLIDER = EditorGUILayout.Toggle(new GUIContent("MeshCollider", "mesh"), nwm.MESHCOLLIDER);
                nwm.BOXCOLLIDER = EditorGUILayout.Toggle(new GUIContent("BoxCollider", "center, size"), nwm.BOXCOLLIDER);
                nwm.SPHERECOLLIDER = EditorGUILayout.Toggle(new GUIContent("SphereCollider", "center, radius"), nwm.SPHERECOLLIDER);
                //nwm.COMPONENTNAME = EditorGUILayout.Toggle(new GUIContent("ComponentName", "supported parameters"), nwm.COMPONENTNAME); //TEMPLATE FOR NEW COMPONENT
                EditorGUILayout.LabelField("Update scripts", EditorStyles.boldLabel);
                nwm.SCRIPTS = EditorGUILayout.Toggle(new GUIContent("Scripts", "Scripts have to be [Serializable] and in the namespace UnityEngine. A matching script Resource must be available in the receiving client."), nwm.SCRIPTS);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
            if (nwm.RECEIVE)
            {
                EditorGUILayout.LabelField("Receiver properties", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                nwm.RECEIVECHANNELS = EditorGUILayout.TextField(new GUIContent("Receive on channels", "List of channels from which to receive changes, separated by \",\"."), nwm.RECEIVECHANNELS);
                nwm.EXISTINGCOMP = EditorGUILayout.Toggle(new GUIContent("Use existing components", "Attempt to find existing GameObjects of the correct name before creating new ones. Names of GameObject descendants of NetworkModel must be unique when using this option."), nwm.EXISTINGCOMP);
                nwm.EXISTINGASSETS = EditorGUILayout.Toggle(new GUIContent("Use existing assets", "Attempt to find existing Assets of the correct name and type in \"/Resources/NetworkModel\" before creating new ones. Names of Assets in the folder must be unique when using this option."), nwm.EXISTINGASSETS);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
            EditorGUILayout.LabelField("Debugging", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            if (nwm.SEND)
            {
                nwm.DEBUGSEND = EditorGUILayout.Toggle("Debug Sending", nwm.DEBUGSEND);
            }
            if (nwm.RECEIVE)
            {
                nwm.DEBUGREC = EditorGUILayout.Toggle("Debug Receiving", nwm.DEBUGREC);
            }
            EditorGUI.indentLevel--;
        }
    }
#endif
}
