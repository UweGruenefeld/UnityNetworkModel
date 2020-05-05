/**
 * 
 * Hierarchy Utility of Unity Network Model
 *
 * @file HierarchyUtility.cs
 * @author Uwe Gruenefeld
 * @version 2020-05-04
 *
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Utility class with functions related to Hierarchy
    /// </summary>
    internal static class RuleUtility
    {
        internal static int DEFAULT_DECIMAL_PLACES = 5;
        internal static bool DEFAULT_RULE = false;

        /// <summary>
        /// Function to find valid decimal places for GanmeObject
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        internal static int FindDecimalPlaces(GameObject gameObject)
        {
            // Find relevant deviation
            Transform pointer = gameObject.transform;

            do
            {
                // See if current GameObject has a NetworkModel Rule component
                NetworkModelRule networkModelRule = pointer.GetComponent<NetworkModelRule>();

                // If true, then a rule was found
                if (networkModelRule != null)
                {
                    // Check if the Rule  was found in the passed GameObject
                    if (gameObject.transform == pointer)
                    {
                        // Check if the Rule is active for the GameObject it is attached to
                        if (networkModelRule.applyToObject)
                            return networkModelRule.decimalPlaces;
                        else
                        {
                            // Rule does not apply, continue with parent GameObject
                            pointer = pointer.parent;
                            continue;
                        }
                    }
                    // ELse, the Rule was found in a parent GameObject
                    else
                    {
                        // Check if the Rule is active for the GameObject it is attached to
                        if (networkModelRule.applyToChildren)
                            return networkModelRule.decimalPlaces;
                        else
                        {
                            // Rule does not apply, continue with parent GameObject
                            pointer = pointer.parent;
                            continue;
                        }
                    }
                }

                // Go one GameObject up in Hierarchy
                pointer = pointer.parent;

            } while (pointer != null);

            // Default value
            return RuleUtility.DEFAULT_DECIMAL_PLACES;
        }

        /// <summary>
        /// Function to find valid rule for Component from GameObject
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="gameObject"></param>
        /// <param name="type"></param>
        /// <param name="updateType"></param>
        /// <returns></returns>
        internal static bool FindComponentRule(Injector injector, GameObject gameObject, Type type, UpdateType updateType)
        {
            // Find relevant rule
            Transform pointer = gameObject.transform;

            do
            {
                // See if current GameObject has a NetworkModel Rule component
                NetworkModelRule networkModelRule = pointer.GetComponent<NetworkModelRule>();

                // If true, then a rule was found
                if (networkModelRule != null)
                {
                    // Check if the Rule  was found in the passed GameObject
                    if (gameObject.transform == pointer)
                    {
                        // Check if the Rule is active for the GameObject it is attached to
                        if (networkModelRule.applyToObject)
                        {
                            // Get the correct Rule
                            RuleType ruleType = CheckComponentRule(networkModelRule, type, updateType);

                            // Check, if Rule is enabled
                            if (ruleType != RuleType.DISABLED)
                                return ruleType.ToBool();
                        }
                        else
                        {
                            // Rule does not apply, continue with parent GameObject
                            pointer = pointer.parent;
                            continue;
                        }
                    }
                    // ELse, the Rule was found in a parent GameObject
                    else
                    {
                        // Check if the Rule is active for the GameObject it is attached to
                        if (networkModelRule.applyToChildren)
                        {
                            // Get the correct Rule
                            RuleType ruleType = CheckComponentRule(networkModelRule, type, updateType);

                            // Check, if Rule is enabled
                            if (ruleType != RuleType.DISABLED)
                                return ruleType.ToBool();
                        }
                        else
                        {
                            // Rule does not apply, continue with parent GameObject
                            pointer = pointer.parent;
                            continue;
                        }
                    }
                }

                // Go one GameObject up in Hierarchy
                pointer = pointer.parent;

            } while (pointer != null);

            LogUtility.Log(injector, LogType.INFORMATION, "No active rule found for Type " + type);

            // Default value
            return RuleUtility.DEFAULT_RULE;
        }

        /// <summary>
        /// Function to find valid rule for Component type
        /// </summary>
        /// <param name="networkModelRule"></param>
        /// <param name="type"></param>
        /// <param name="updateType"></param>
        /// <returns></returns>
        private static RuleType CheckComponentRule(NetworkModelRule networkModelRule, Type type, UpdateType updateType)
        {
            // If Type is BoxCollider
            if (type == typeof(BoxCollider) || type.IsSubclassOf(typeof(BoxCollider)))
            {
                return CheckEntry(networkModelRule.enableBoxCollider, networkModelRule.sendBoxCollider, networkModelRule.receiveBoxCollider, updateType);
            }
            // Else, if Type is Camera
            else if (type == typeof(Camera) || type.IsSubclassOf(typeof(Camera)))
            {
                return CheckEntry(networkModelRule.enableCamera, networkModelRule.sendCamera, networkModelRule.receiveCamera, updateType);
            }
            // Else, if Type is Light
            else if (type == typeof(Light) || type.IsSubclassOf(typeof(Light)))
            {
                return CheckEntry(networkModelRule.enableLight, networkModelRule.sendLight, networkModelRule.receiveLight, updateType);
            }
            // Else, if Type is LineRenderer
            else if (type == typeof(LineRenderer) || type.IsSubclassOf(typeof(LineRenderer)))
            {
                return CheckEntry(networkModelRule.enableLineRenderer, networkModelRule.sendLineRenderer, networkModelRule.receiveLineRenderer, updateType);
            }
            // Else, if Type is MeshCollider
            else if (type == typeof(MeshCollider) || type.IsSubclassOf(typeof(MeshCollider)))
            {
                return CheckEntry(networkModelRule.enableMeshCollider, networkModelRule.sendMeshCollider, networkModelRule.receiveMeshCollider, updateType);
            }
            // Else, if Type is MeshFilter
            else if (type == typeof(MeshFilter) || type.IsSubclassOf(typeof(MeshFilter)))
            {
                return CheckEntry(networkModelRule.enableMeshFilter, networkModelRule.sendMeshFilter, networkModelRule.receiveMeshFilter, updateType);
            }
            // Else, if Type is MeshRenderer
            else if (type == typeof(MeshRenderer) || type.IsSubclassOf(typeof(MeshRenderer)))
            {
                return CheckEntry(networkModelRule.enableMeshRenderer, networkModelRule.sendMeshRenderer, networkModelRule.receiveMeshRenderer, updateType);
            }
            // Else, if Type is MonoBehaviour(Script)
            else if (type.IsSubclassOf(typeof(MonoBehaviour)))
            {
                return CheckEntry(networkModelRule.enableScript, networkModelRule.sendScript, networkModelRule.receiveScript, updateType);
            }
            // Else, if Type is SphereCollider
            else if (type == typeof(SphereCollider) || type.IsSubclassOf(typeof(SphereCollider)))
            {
                return CheckEntry(networkModelRule.enableSphereCollider, networkModelRule.sendSphereCollider, networkModelRule.receiveSphereCollider, updateType);
            }
            // Else, if Type is Transform
            else if (type == typeof(Transform) || type.IsSubclassOf(typeof(Transform)))
            {
                return CheckEntry(networkModelRule.enableTransform, networkModelRule.sendTransform, networkModelRule.receiveTransform, updateType);
            }

            return RuleType.DISABLED;
        }

        /// <summary>
        /// Function to find valid rule for Resource
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="type"></param>
        /// <param name="updateType"></param>
        /// <returns></returns>
        internal static bool FindResourceRule(Injector injector, Type type, UpdateType updateType)
        {
            RuleType result = RuleType.DISABLED;

            // If Type is Material
            if (type == typeof(Material) || type.IsSubclassOf(typeof(Material)))
            {
                result = CheckEntry(injector.configuration.enableMaterial, injector.configuration.sendMaterial, injector.configuration.receiveMaterial, updateType);
            }

            // Else, if Type is Mesh
            else if (type == typeof(Mesh) || type.IsSubclassOf(typeof(Mesh)))
            {
                result = CheckEntry(injector.configuration.enableMesh, injector.configuration.sendMesh, injector.configuration.receiveMesh, updateType);
            }

            // Else, if Type is Texture2D
            else if (type == typeof(Texture2D) || type.IsSubclassOf(typeof(Texture2D)))
            {
                result = CheckEntry(injector.configuration.enableTexture2D, injector.configuration.sendTexture2D, injector.configuration.receiveTexture2D, updateType);
            }

            // Check, if Rule is enabled
            if (result != RuleType.DISABLED)
                return result.ToBool();

            // Default case
            return false;
        }

        /// <summary>
        /// Function to check boolean values if the underlying rule is valid
        /// </summary>
        /// <param name="enable"></param>
        /// <param name="send"></param>
        /// <param name="receive"></param>
        /// <param name="updateType"></param>
        /// <returns></returns>
        private static RuleType CheckEntry(bool enable, bool send, bool receive, UpdateType updateType)
        {
            // Check if rule for this Type is not enabled
            if (!enable)
                return RuleType.DISABLED;

            // Check the status of the enabled rule
            switch (updateType)
            {
                case UpdateType.SEND:
                    return RuleTypeExtension.FromBool(send);
                case UpdateType.RECEIVE:
                    return RuleTypeExtension.FromBool(receive);
            }

            // Default case
            return RuleType.DISABLED;
        }
    }
}