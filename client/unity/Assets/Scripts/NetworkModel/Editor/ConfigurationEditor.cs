/**
 * 
 * ConfigEditor Script of Unity Network Model
 *
 * @file ConfigEditor.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-05-03
 **/
using UnityEngine;
using UnityEditor;

namespace UnityNetworkModel
{
    /// <summary>
    /// Editor interface for Network Model Configuration
    /// </summary>
#if UNITY_EDITOR
    [CustomEditor(typeof(NetworkModelConfiguration))]
    [CanEditMultipleObjects]
    public class ConfigurationEditor : Editor
    {
        // Save toggle state of groups
        bool toggleGroupConnection = true;
        bool toggleGroupSettings = true;
        bool toggleGroupChannel = true;
        bool toggleGroupRules = true;
        bool toggleGroupDebugging = true;

        float currentWidth = 70;

        /// <summary>
        /// Override method to show custom editor
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Data object of Unity component
            serializedObject.Update();
            var networkModelConfiguration = target as NetworkModelConfiguration;

            // Ensure no NetworkModelConfiguration in parent
            Transform pointer = networkModelConfiguration.transform;
            while (pointer.parent != null)
            {
                pointer = pointer.parent;
                if (pointer.GetComponent<NetworkModelConfiguration>() != null)
                {
                    EditorGUILayout.HelpBox("Another NetworkModelConfiguration found in parent GameObject. Therefore, this component is disabled.", MessageType.Error);
                    networkModelConfiguration.enabled = false;
                }
            }

            // Ensure a NetworkModel Rule is attached to GameObject as well
            if (networkModelConfiguration.GetComponent<NetworkModelRule>() == null)
                networkModelConfiguration.gameObject.AddComponent<NetworkModelRule>();

            // Hover text for Help
            string serverIP = "Specify the IP address of the server.";
            string serverPort = "Specify the port of the server.";
            string reconnectTime = "Minimum time in seconds before next reconnect attempt. Will never be faster than the time between updates.";
            string updateDelay = "Minimum time in seconds between two updates.";

            string compareTime = "If checked, every update must contain a more recent timestamp than the last update.";
            string allowSend = "If checked, the network model will send updates to the server.";
            string allowReceive = "If checked, the network model will receive updates from the server.";
            string existingObjects = "Attempt to find existing GameObjects of the correct name before creating new ones. Names of GameObject descendants of NetworkModel must be unique when using this option.";
            string existingResources = "Attempt to find existing Resources of the correct name and type in \"/Resources/NetworkModel\" before creating new ones. Names of Resources in the folder must be unique when using this option.";

            string sendChannel = "List of channels on which to broadcast changes, seperated by \",\".";
            string receiveChannel = "List of channels from which to receive changes, separated by \",\".";

            string enableText = "Enable/Disable Resource";
            string sendText = "Enable/Disable Sending Resource";
            string receiveText = "Enable/Disable Receiving Resource";

            string debugLevel = "Show all debug messages with regard to specified log level.";
            string debugSend = "Debug messages sent to the server.";
            string debugReceive = "Debug messages received from server.";

            // Set GUILayout mode
            GUILayout.FlexibleSpace();

            // Textures
            Texture2D textureDark = new Texture2D(1, 1);
            textureDark.SetPixels(new Color[] { new Color(.6f, .6f, .6f) });
            textureDark.Apply();

            Texture2D textureLight = new Texture2D(1, 1);
            textureLight.SetPixels(new Color[] { new Color(.8f, .8f, .8f) });
            textureLight.Apply();

            // GUIStyles
            GUIStyle foldout = EditorStyles.foldoutHeader;
            foldout.fontStyle = FontStyle.Bold;

            GUIStyle header = new GUIStyle();
            header.normal.background = textureDark;

            GUIStyle empty = new GUIStyle();

            GUIStyle rowOdd = new GUIStyle();
            GUIStyle rowEven = new GUIStyle();
            rowEven.normal.background = textureLight;

            // Connection
            this.toggleGroupConnection = EditorGUILayout.BeginFoldoutHeaderGroup(this.toggleGroupConnection, "Connection", foldout);
            if (this.toggleGroupConnection)
            {
                EditorGUI.indentLevel++;

                if (EditorApplication.isPlaying)
                {
                    EditorGUILayout.TextField(new GUIContent("Server IP", serverIP), networkModelConfiguration.IP, EditorStyles.label);
                    EditorGUILayout.IntField(new GUIContent("Server Port", serverPort), networkModelConfiguration.PORT, EditorStyles.label);
                }
                else
                {
                    networkModelConfiguration.IP = EditorGUILayout.TextField(new GUIContent("Server IP", serverIP), networkModelConfiguration.IP);
                    networkModelConfiguration.PORT = EditorGUILayout.IntField(new GUIContent("Server Port", serverPort), networkModelConfiguration.PORT);
                }
                networkModelConfiguration.RECONNECT = EditorGUILayout.Slider(new GUIContent("Seconds to reconnect", reconnectTime), networkModelConfiguration.RECONNECT, 0.0f, 10.0f);
                networkModelConfiguration.DELAY = EditorGUILayout.Slider(new GUIContent("Seconds bet. updates", updateDelay), networkModelConfiguration.DELAY, 0.0f, 1.0f);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space();

            // Caluclate Editor width
            var windowVisibleRect = GUILayoutUtility.GetLastRect();
            if (windowVisibleRect.width > 1)
                this.currentWidth = windowVisibleRect.width;

            // GUILayoutOptions
            GUILayoutOption[] optionsLeftField = { GUILayout.MinWidth(70), GUILayout.Width((this.currentWidth * 0.45f) - 45) };
            GUILayoutOption[] optionsRightFields = { GUILayout.MaxWidth(65.0f), GUILayout.MinWidth(65.0f), GUILayout.Width(65.0f) };

            // Settings
            this.toggleGroupSettings = EditorGUILayout.BeginFoldoutHeaderGroup(this.toggleGroupConnection, "Settings", foldout);
            if (this.toggleGroupSettings)
            {
                EditorGUI.indentLevel++;
                networkModelConfiguration.TIME = EditorGUILayout.Toggle(new GUIContent("Compare timestamps", compareTime), networkModelConfiguration.TIME);
                networkModelConfiguration.SEND = EditorGUILayout.Toggle(new GUIContent("Allow to send updates", allowSend), networkModelConfiguration.SEND);
                networkModelConfiguration.RECEIVE = EditorGUILayout.Toggle(new GUIContent("Allow to receive updates", allowReceive), networkModelConfiguration.RECEIVE);
                networkModelConfiguration.EXISTINGOBJECTS = EditorGUILayout.Toggle(new GUIContent("Use existing objects", existingObjects), networkModelConfiguration.EXISTINGOBJECTS);
                networkModelConfiguration.EXISTINGRESOURCES = EditorGUILayout.Toggle(new GUIContent("Use existing resources", existingResources), networkModelConfiguration.EXISTINGRESOURCES);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space();

            // Channel
            this.toggleGroupChannel = EditorGUILayout.BeginFoldoutHeaderGroup(this.toggleGroupConnection, "Channel", foldout);
            if (this.toggleGroupChannel)
            {
                EditorGUI.indentLevel++;
                if (EditorApplication.isPlaying)
                {
                    EditorGUILayout.TextField(new GUIContent("Send on channel", sendChannel), networkModelConfiguration.SENDCHANNELS, EditorStyles.label);
                    EditorGUILayout.TextField(new GUIContent("Receive on channels", receiveChannel), networkModelConfiguration.RECEIVECHANNELS, EditorStyles.label);
                }
                else
                {
                    networkModelConfiguration.SENDCHANNELS = EditorGUILayout.TextField(new GUIContent("Send on channel", sendChannel), networkModelConfiguration.SENDCHANNELS);
                    networkModelConfiguration.RECEIVECHANNELS = EditorGUILayout.TextField(new GUIContent("Receive on channels", receiveChannel), networkModelConfiguration.RECEIVECHANNELS);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space();

            // Rules
            this.toggleGroupRules = EditorGUILayout.BeginFoldoutHeaderGroup(this.toggleGroupRules, "Rules", foldout);
            if (this.toggleGroupRules)
            {
                EditorGUI.indentLevel++;

                // Rules Resources
                EditorGUILayout.BeginHorizontal(header);
                EditorGUILayout.LabelField(new GUIContent("Resources", "For which resources should updates be sent"), optionsLeftField);
                EditorGUILayout.LabelField("Send", optionsRightFields);
                EditorGUILayout.LabelField("Receive", optionsRightFields);
                EditorGUILayout.EndHorizontal();

                // Resource Material
                EditorGUILayout.BeginHorizontal(rowOdd);
                networkModelConfiguration.enableMaterial = EditorGUILayout.ToggleLeft(new GUIContent("Material", enableText), 
                networkModelConfiguration.enableMaterial, EditorStyles.label, optionsLeftField);
                EditorGUI.BeginDisabledGroup(networkModelConfiguration.enableMaterial == false);
                networkModelConfiguration.sendMaterial = EditorGUILayout.ToggleLeft(new GUIContent("", sendText), networkModelConfiguration.sendMaterial, optionsRightFields);
                networkModelConfiguration.receiveMaterial = EditorGUILayout.ToggleLeft(new GUIContent("", receiveText), networkModelConfiguration.receiveMaterial, optionsRightFields);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                // Resource Mesh
                EditorGUILayout.BeginHorizontal(rowEven);
                networkModelConfiguration.enableMesh = EditorGUILayout.ToggleLeft(new GUIContent("Mesh", enableText), 
                networkModelConfiguration.enableMesh, EditorStyles.label, optionsLeftField);
                EditorGUI.BeginDisabledGroup(networkModelConfiguration.enableMesh == false);
                networkModelConfiguration.sendMesh = EditorGUILayout.ToggleLeft(new GUIContent("", sendText), networkModelConfiguration.sendMesh, optionsRightFields);
                networkModelConfiguration.receiveMesh = EditorGUILayout.ToggleLeft(new GUIContent("", receiveText), networkModelConfiguration.receiveMesh, optionsRightFields);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                // Resource Texture2D
                EditorGUILayout.BeginHorizontal(rowOdd);
                networkModelConfiguration.enableTexture2D = EditorGUILayout.ToggleLeft(new GUIContent("Texture2D", enableText), 
                networkModelConfiguration.enableTexture2D, EditorStyles.label, optionsLeftField);
                EditorGUI.BeginDisabledGroup(networkModelConfiguration.enableTexture2D == false);
                networkModelConfiguration.sendTexture2D = EditorGUILayout.ToggleLeft(new GUIContent("", sendText), networkModelConfiguration.sendTexture2D, optionsRightFields);
                networkModelConfiguration.receiveTexture2D = EditorGUILayout.ToggleLeft(new GUIContent("", receiveText), networkModelConfiguration.receiveTexture2D, optionsRightFields);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space();

            // Debugging
            this.toggleGroupDebugging = EditorGUILayout.BeginFoldoutHeaderGroup(this.toggleGroupConnection, "Debugging", foldout);
            if (this.toggleGroupDebugging)
            {
                EditorGUI.indentLevel++;
                networkModelConfiguration.DEBUGLEVEL = (LogType)EditorGUILayout.EnumPopup(new GUIContent("Debug Level", debugLevel), networkModelConfiguration.DEBUGLEVEL);

                if (networkModelConfiguration.SEND)
                    networkModelConfiguration.DEBUGSEND = EditorGUILayout.Toggle(new GUIContent("Log outgoing messages", debugSend), networkModelConfiguration.DEBUGSEND);
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.Toggle(new GUIContent("Log outgoing messages", debugSend), networkModelConfiguration.DEBUGSEND);
                    EditorGUI.EndDisabledGroup();
                }

                if (networkModelConfiguration.RECEIVE)
                    networkModelConfiguration.DEBUGRECEIVE = EditorGUILayout.Toggle(new GUIContent("Log incoming messages", debugReceive), networkModelConfiguration.DEBUGRECEIVE);
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.Toggle(new GUIContent("Log incoming messages", debugReceive), networkModelConfiguration.DEBUGRECEIVE);
                    EditorGUI.EndDisabledGroup();
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Apply changes
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}