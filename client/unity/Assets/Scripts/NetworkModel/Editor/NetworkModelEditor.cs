/**
 * 
 * Editor Script of Unity Network Model
 *
 * @file NetworkModelEditor.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-04-30
 **/
using UnityEngine;
using UnityEditor;

namespace UnityNetworkModel
{
    /// <summary>
    /// Editor interface for public variables
    /// </summary>
    #if UNITY_EDITOR
    [CustomEditor(typeof(NetworkModel))]
    public class NetworkModelEditor : Editor
    {
        override public void OnInspectorGUI()
        {
            var nwm = target as NetworkModel;
            EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);
            nwm.IP = EditorGUILayout.TextField("IP", nwm.IP);
            nwm.PORT = EditorGUILayout.IntField("Port", nwm.PORT);
            nwm.RECONNECT = EditorGUILayout.Slider(
                new GUIContent("Reconnect Period", "Minimum time in seconds between reconnect attempts. Will never be faster than update period."), nwm.RECONNECT, 0.5f, 5.0f);
            nwm.PERIOD = EditorGUILayout.Slider(
                new GUIContent("Update Period", "Minimum time in seconds between two updates."), nwm.PERIOD, 0.0f, 10.0f);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Update direction", EditorStyles.boldLabel);
            nwm.SEND = EditorGUILayout.Toggle("Send", nwm.SEND);
            nwm.RECEIVE = EditorGUILayout.Toggle("Receive", nwm.RECEIVE);

            EditorGUILayout.Space();

            if (nwm.SEND)
            {
                EditorGUILayout.LabelField("Sender properties", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                nwm.SENDCHANNEL = EditorGUILayout.TextField(
                    new GUIContent("Send on channel", "Singular channel on which to broadcast changes. May not contain \",\"."), nwm.SENDCHANNEL);
                nwm.TIMESTAMP = EditorGUILayout.Toggle("Update with timestamp", nwm.TIMESTAMP);
                EditorGUILayout.LabelField(
                    new GUIContent("Update components", "For which components should updates be sent"), EditorStyles.boldLabel);
                nwm.TRANSFORM = EditorGUILayout.Toggle(
                    new GUIContent("Transform", "localPosition, localRotation, localScale, tag of gameObject"), nwm.TRANSFORM);
                nwm.CAMERA = EditorGUILayout.Toggle(
                    new GUIContent("Camera", "depth, nearClipPlane, farClipPlane, fieldOfView, backgroundColor, clearFlags"), nwm.CAMERA);
                nwm.LIGHT = EditorGUILayout.Toggle(
                    new GUIContent("Light", "type, color, intensity, bounceIntensity"), nwm.LIGHT);
                nwm.MESHFILTER = EditorGUILayout.Toggle(
                    new GUIContent("MeshFilter", "mesh"), nwm.MESHFILTER);
                nwm.MESHRENDERER = EditorGUILayout.Toggle(
                    new GUIContent("MeshRenderer", "material"), nwm.MESHRENDERER);
                nwm.LINERENDERER = EditorGUILayout.Toggle(
                    new GUIContent("LineRenderer", "material, positions, widthMultiplier, loop, numCapVertices, numCornerVertices, textureMode, startColor, endColor"), nwm.LINERENDERER);
                nwm.MESHCOLLIDER = EditorGUILayout.Toggle(
                    new GUIContent("MeshCollider", "mesh"), nwm.MESHCOLLIDER);
                nwm.BOXCOLLIDER = EditorGUILayout.Toggle(
                    new GUIContent("BoxCollider", "center, size"), nwm.BOXCOLLIDER);
                nwm.SPHERECOLLIDER = EditorGUILayout.Toggle(
                    new GUIContent("SphereCollider", "center, radius"), nwm.SPHERECOLLIDER);
                //nwm.COMPONENTNAME = EditorGUILayout.Toggle(new GUIContent("ComponentName", "supported parameters"), nwm.COMPONENTNAME); //TEMPLATE FOR NEW COMPONENT
                EditorGUILayout.LabelField("Update scripts", EditorStyles.boldLabel);
                nwm.SCRIPTS = EditorGUILayout.Toggle(
                    new GUIContent("Scripts", "Scripts have to be [Serializable] and in the namespace UnityEngine. A matching script Resource must be available in the receiving client."), nwm.SCRIPTS);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            if (nwm.RECEIVE)
            {
                EditorGUILayout.LabelField("Receiver properties", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                nwm.RECEIVECHANNELS = EditorGUILayout.TextField(
                    new GUIContent("Receive on channels", "List of channels from which to receive changes, separated by \",\"."), nwm.RECEIVECHANNELS);
                nwm.EXISTINGCOMPENTS = EditorGUILayout.Toggle(
                    new GUIContent("Use existing components", "Attempt to find existing GameObjects of the correct name before creating new ones. Names of GameObject descendants of NetworkModel must be unique when using this option."), nwm.EXISTINGCOMPENTS);
                nwm.EXISTINGRESOURCES = EditorGUILayout.Toggle(
                    new GUIContent("Use existing resources", "Attempt to find existing Resources of the correct name and type in \"/Resources/NetworkModel\" before creating new ones. Names of Resources in the folder must be unique when using this option."), nwm.EXISTINGRESOURCES);
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