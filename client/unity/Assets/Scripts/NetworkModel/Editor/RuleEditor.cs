/**
 * 
 * RuleEditor Script of Unity Network Model
 *
 * @file RuleEditor.cs
 * @author Uwe Gruenefeld
 * @version 2020-05-03
 **/
using UnityEngine;
using UnityEditor;

namespace UnityNetworkModel
{
    /// <summary>
    /// Editor interface for Network Model Rule
    /// </summary>
#if UNITY_EDITOR
    [CustomEditor(typeof(NetworkModelRule))]
    [CanEditMultipleObjects]
    public class RuleEditor : Editor
    {
        // Save toggle state of groups
        bool toggleGroupScope = true;
        bool toggleGroupSettings = true;
        bool toggleGroupQuick = true;
        bool toggleGroupRules = true;

        float currentWidth = 70;

        /// <summary>
        /// Override method to show custom editor
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Data object of Unity component
            serializedObject.Update();
            var networkModelRule = target as NetworkModelRule;

            // Find NetworkModelConfiguration in parent
            Transform pointer = networkModelRule.transform;
            bool isConfigurationExisting = false;
            while(pointer != null)
            {
                if(pointer.GetComponent<NetworkModelConfiguration>() != null)
                    isConfigurationExisting = true;
                pointer = pointer.parent;
            }

            // No NetworkModelConfiugration found, show help message
            if(!isConfigurationExisting)
                EditorGUILayout.HelpBox("No NetworkModelConfiguration found in parent or this GameObject. Therefore, this rule is not considered.", MessageType.Error);

            // Hover text for Help
            string applyObject = "Apply the following rules to this GameObject.";
            string applyChildren = "Apply the following rules to all children of this GameObject.";

            string decimalPlaces = "Specify the number of decimal places. Smaller numbers of decimal places result in less frequent updates.";

            string enableAll = "Enable all components.";
            string enableNone = "Disable all components.";
            string sendAll = "Enable sending for all components.";
            string sendNone = "Disable sending for all components.";
            string receiveAll = "Enable receiving for all components.";
            string receiveNone = "Disable receiving for all components.";

            string enableText = "Enable/Disable Component";
            string sendText = "Enable/Disable Sending Component";
            string receiveText = "Enable/Disable Receiving Component";

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

            // Scope
            this.toggleGroupScope = EditorGUILayout.BeginFoldoutHeaderGroup(this.toggleGroupScope, "Scope", foldout);
            if (this.toggleGroupScope)
            {
                EditorGUI.indentLevel++;

                if(networkModelRule.GetComponent<NetworkModelConfiguration>() != null)
                {
                    // If rule is attached at the same GameObject as configuration, then disable control and set predefined values
                    EditorGUI.BeginDisabledGroup(true);
                    networkModelRule.applyToObject = false;
                    EditorGUILayout.Toggle(new GUIContent("Apply to this gameobject", applyObject), networkModelRule.applyToObject);
                    networkModelRule.applyToChildren = true;
                    EditorGUILayout.Toggle(new GUIContent("Apply to all children", applyChildren), networkModelRule.applyToChildren);
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    // Otherwise let the user decide the values
                    networkModelRule.applyToObject = EditorGUILayout.Toggle(new GUIContent("Apply to this gameobject", applyObject), networkModelRule.applyToObject);
                    networkModelRule.applyToChildren = EditorGUILayout.Toggle(new GUIContent("Apply to all children", applyChildren), networkModelRule.applyToChildren);
                }
                
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
            this.toggleGroupSettings = EditorGUILayout.BeginFoldoutHeaderGroup(this.toggleGroupQuick, "Settings", foldout);
            if (this.toggleGroupSettings)
            {
                EditorGUI.indentLevel++;

                // Decimal places
                networkModelRule.decimalPlaces = EditorGUILayout.IntSlider(new GUIContent("Decimal places", decimalPlaces), networkModelRule.decimalPlaces, 1, 10);

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space();

            // Quick
            this.toggleGroupQuick = EditorGUILayout.BeginFoldoutHeaderGroup(this.toggleGroupQuick, "Quick", foldout);
            if (this.toggleGroupQuick)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal(empty);

                // Quick actions for Enable
                EditorGUILayout.LabelField("Enable", optionsLeftField);
                if(GUILayout.Button(new GUIContent("All", enableAll), optionsRightFields))
                {
                    // Components
                    networkModelRule.enableBoxCollider = true;
                    networkModelRule.enableCamera = true;
                    networkModelRule.enableLight = true;
                    networkModelRule.enableLineRenderer = true;
                    networkModelRule.enableMeshCollider = true;
                    networkModelRule.enableMeshFilter = true;
                    networkModelRule.enableMeshRenderer = true;
                    networkModelRule.enableScript = true;
                    networkModelRule.enableSphereCollider = true;
                    networkModelRule.enableTransform = true;
                }
                if(GUILayout.Button(new GUIContent("None", enableNone), optionsRightFields))
                {
                    // Components
                    networkModelRule.enableBoxCollider = false;
                    networkModelRule.enableCamera = false;
                    networkModelRule.enableLight = false;
                    networkModelRule.enableLineRenderer = false;
                    networkModelRule.enableMeshCollider = false;
                    networkModelRule.enableMeshFilter = false;
                    networkModelRule.enableMeshRenderer = false;
                    networkModelRule.enableScript = false;
                    networkModelRule.enableSphereCollider = false;
                    networkModelRule.enableTransform = false;

                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal(empty);

                // Quick actions for Send
                EditorGUILayout.LabelField("Send", optionsLeftField);
                if(GUILayout.Button(new GUIContent("All", sendAll), optionsRightFields))
                {
                    // Components
                    networkModelRule.sendBoxCollider = true;
                    networkModelRule.sendCamera = true;
                    networkModelRule.sendLight = true;
                    networkModelRule.sendLineRenderer = true;
                    networkModelRule.sendMeshCollider = true;
                    networkModelRule.sendMeshFilter = true;
                    networkModelRule.sendMeshRenderer = true;
                    networkModelRule.sendScript = true;
                    networkModelRule.sendSphereCollider = true;
                    networkModelRule.sendTransform = true;
                }
                if(GUILayout.Button(new GUIContent("None", sendNone), optionsRightFields))
                {
                    // Components
                    networkModelRule.sendBoxCollider = false;
                    networkModelRule.sendCamera = false;
                    networkModelRule.sendLight = false;
                    networkModelRule.sendLineRenderer = false;
                    networkModelRule.sendMeshCollider = false;
                    networkModelRule.sendMeshFilter = false;
                    networkModelRule.sendMeshRenderer = false;
                    networkModelRule.sendScript = false;
                    networkModelRule.sendSphereCollider = false;
                    networkModelRule.sendTransform = false;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal(empty);

                // Quick actions for Receive
                EditorGUILayout.LabelField("Receive", optionsLeftField);
                if(GUILayout.Button(new GUIContent("All", receiveAll), optionsRightFields))
                {
                    // Components
                    networkModelRule.receiveBoxCollider = true;
                    networkModelRule.receiveCamera = true;
                    networkModelRule.receiveLight = true;
                    networkModelRule.receiveLineRenderer = true;
                    networkModelRule.receiveMeshCollider = true;
                    networkModelRule.receiveMeshFilter = true;
                    networkModelRule.receiveMeshRenderer = true;
                    networkModelRule.receiveScript = true;
                    networkModelRule.receiveSphereCollider = true;
                    networkModelRule.receiveTransform = true;
                }
                if(GUILayout.Button(new GUIContent("None", receiveNone), optionsRightFields))
                {
                    // Components
                    networkModelRule.receiveBoxCollider = false;
                    networkModelRule.receiveCamera = false;
                    networkModelRule.receiveLight = false;
                    networkModelRule.receiveLineRenderer = false;
                    networkModelRule.receiveMeshCollider = false;
                    networkModelRule.receiveMeshFilter = false;
                    networkModelRule.receiveMeshRenderer = false;
                    networkModelRule.receiveScript = false;
                    networkModelRule.receiveSphereCollider = false;
                    networkModelRule.receiveTransform = false;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space();

            // Rules
            this.toggleGroupRules = EditorGUILayout.BeginFoldoutHeaderGroup(this.toggleGroupRules, "Rules", foldout);
            if (this.toggleGroupRules)
            {
                EditorGUI.indentLevel++;

                // Rules Components
                EditorGUILayout.BeginHorizontal(header);
                EditorGUILayout.LabelField(new GUIContent("Components", "For which components should updates be sent"), optionsLeftField);
                EditorGUILayout.LabelField("Send", optionsRightFields);
                EditorGUILayout.LabelField("Receive", optionsRightFields);
                EditorGUILayout.EndHorizontal();

                // Component BoxCollider
                EditorGUILayout.BeginHorizontal(rowOdd);
                networkModelRule.enableBoxCollider = EditorGUILayout.ToggleLeft(new GUIContent("BoxCollider", enableText), 
                    networkModelRule.enableBoxCollider, EditorStyles.label, optionsLeftField);
                EditorGUI.BeginDisabledGroup(networkModelRule.enableBoxCollider == false);
                networkModelRule.sendBoxCollider = EditorGUILayout.ToggleLeft(new GUIContent("", sendText), networkModelRule.sendBoxCollider, optionsRightFields);
                networkModelRule.receiveBoxCollider = EditorGUILayout.ToggleLeft(new GUIContent("", receiveText), networkModelRule.receiveBoxCollider, optionsRightFields);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                // Component Camera
                EditorGUILayout.BeginHorizontal(rowEven);
                networkModelRule.enableCamera = EditorGUILayout.ToggleLeft(new GUIContent("Camera", enableText), 
                    networkModelRule.enableCamera, EditorStyles.label, optionsLeftField);
                EditorGUI.BeginDisabledGroup(networkModelRule.enableCamera == false);
                networkModelRule.sendCamera = EditorGUILayout.ToggleLeft(new GUIContent("", sendText), networkModelRule.sendCamera, optionsRightFields);
                networkModelRule.receiveCamera = EditorGUILayout.ToggleLeft(new GUIContent("", receiveText), networkModelRule.receiveCamera, optionsRightFields);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                // Component Light
                EditorGUILayout.BeginHorizontal(rowOdd);
                networkModelRule.enableLight = EditorGUILayout.ToggleLeft(new GUIContent("Light", enableText), 
                    networkModelRule.enableLight, EditorStyles.label, optionsLeftField);
                EditorGUI.BeginDisabledGroup(networkModelRule.enableLight == false);
                networkModelRule.sendLight = EditorGUILayout.ToggleLeft(new GUIContent("", sendText), networkModelRule.sendLight, optionsRightFields);
                networkModelRule.receiveLight = EditorGUILayout.ToggleLeft(new GUIContent("", receiveText), networkModelRule.receiveLight, optionsRightFields);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                // Component LineRenderer
                EditorGUILayout.BeginHorizontal(rowEven);
                networkModelRule.enableLineRenderer = EditorGUILayout.ToggleLeft(new GUIContent("LineRenderer", enableText), 
                    networkModelRule.enableLineRenderer, EditorStyles.label, optionsLeftField);
                EditorGUI.BeginDisabledGroup(networkModelRule.enableLineRenderer == false);
                networkModelRule.sendLineRenderer = EditorGUILayout.ToggleLeft(new GUIContent("", sendText), networkModelRule.sendLineRenderer, optionsRightFields);
                networkModelRule.receiveLineRenderer = EditorGUILayout.ToggleLeft(new GUIContent("", receiveText), networkModelRule.receiveLineRenderer, optionsRightFields);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                // Component MeshCollider
                EditorGUILayout.BeginHorizontal(rowOdd);
                networkModelRule.enableMeshCollider = EditorGUILayout.ToggleLeft(new GUIContent("MeshCollider", enableText), 
                    networkModelRule.enableMeshCollider, EditorStyles.label, optionsLeftField);
                EditorGUI.BeginDisabledGroup(networkModelRule.enableMeshCollider == false);
                networkModelRule.sendMeshCollider = EditorGUILayout.ToggleLeft(new GUIContent("", sendText), networkModelRule.sendMeshCollider, optionsRightFields);
                networkModelRule.receiveMeshCollider = EditorGUILayout.ToggleLeft(new GUIContent("", receiveText), networkModelRule.receiveMeshCollider, optionsRightFields);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                // Component MeshFilter
                EditorGUILayout.BeginHorizontal(rowEven);
                networkModelRule.enableMeshFilter = EditorGUILayout.ToggleLeft(new GUIContent("MeshFilter", enableText), 
                    networkModelRule.enableMeshFilter, EditorStyles.label, optionsLeftField);
                EditorGUI.BeginDisabledGroup(networkModelRule.enableMeshFilter == false);
                networkModelRule.sendMeshFilter = EditorGUILayout.ToggleLeft(new GUIContent("", sendText), networkModelRule.sendMeshFilter, optionsRightFields);
                networkModelRule.receiveMeshFilter = EditorGUILayout.ToggleLeft(new GUIContent("", receiveText), networkModelRule.receiveMeshFilter, optionsRightFields);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                // Component MeshRenderer
                EditorGUILayout.BeginHorizontal(rowOdd);
                networkModelRule.enableMeshRenderer = EditorGUILayout.ToggleLeft(new GUIContent("MeshRenderer", enableText), 
                    networkModelRule.enableMeshRenderer, EditorStyles.label, optionsLeftField);
                EditorGUI.BeginDisabledGroup(networkModelRule.enableMeshRenderer == false);
                networkModelRule.sendMeshRenderer = EditorGUILayout.ToggleLeft(new GUIContent("", sendText), networkModelRule.sendMeshRenderer, optionsRightFields);
                networkModelRule.receiveMeshRenderer = EditorGUILayout.ToggleLeft(new GUIContent("", receiveText), networkModelRule.receiveMeshRenderer, optionsRightFields);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                // Component Script
                EditorGUILayout.BeginHorizontal(rowEven);
                networkModelRule.enableScript = EditorGUILayout.ToggleLeft(new GUIContent("Script [beta]", enableText), 
                    networkModelRule.enableScript, EditorStyles.label, optionsLeftField);
                EditorGUI.BeginDisabledGroup(networkModelRule.enableScript == false);
                networkModelRule.sendScript = EditorGUILayout.ToggleLeft(new GUIContent("", sendText), networkModelRule.sendScript, optionsRightFields);
                networkModelRule.receiveScript = EditorGUILayout.ToggleLeft(new GUIContent("", receiveText), networkModelRule.receiveScript, optionsRightFields);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                // Component SphereCollider
                EditorGUILayout.BeginHorizontal(rowOdd);
                networkModelRule.enableSphereCollider = EditorGUILayout.ToggleLeft(new GUIContent("SphereCollider", enableText), 
                    networkModelRule.enableSphereCollider, EditorStyles.label, optionsLeftField);
                EditorGUI.BeginDisabledGroup(networkModelRule.enableSphereCollider == false);
                networkModelRule.sendSphereCollider = EditorGUILayout.ToggleLeft(new GUIContent("", sendText), networkModelRule.sendSphereCollider, optionsRightFields);
                networkModelRule.receiveSphereCollider = EditorGUILayout.ToggleLeft(new GUIContent("", receiveText), networkModelRule.receiveSphereCollider, optionsRightFields);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                // Component Transform
                EditorGUILayout.BeginHorizontal(rowEven);
                networkModelRule.enableTransform = EditorGUILayout.ToggleLeft(new GUIContent("Transform", enableText), 
                    networkModelRule.enableTransform, EditorStyles.label, optionsLeftField);
                EditorGUI.BeginDisabledGroup(networkModelRule.enableTransform == false);
                networkModelRule.sendTransform = EditorGUILayout.ToggleLeft(new GUIContent("", sendText), networkModelRule.sendTransform, optionsRightFields);
                networkModelRule.receiveTransform = EditorGUILayout.ToggleLeft(new GUIContent("", receiveText), networkModelRule.receiveTransform, optionsRightFields);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Apply changes
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}