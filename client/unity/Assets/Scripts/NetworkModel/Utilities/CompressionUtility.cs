/**
 * 
 * Compression Utility of Unity Network Model
 *
 * @file CompressionUtility.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-05-04
 *
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Utility class with functions related to Compression
    /// </summary>
    internal static class CompressionUtility
    {
        /// <summary>
        /// Compress an array of Color
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Color[] Compress(GameObject gameObject, Color[] value)
        {
            int places = RuleUtility.FindDecimalPlaces(gameObject);

            Color[] result = new Color[value.Length];

            for(int i=0; i < value.Length && i < result.Length; i++)
                result[i] = Compress(places, result[i]);

            return result;
        }

        /// <summary>
        /// Compress a Color
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Color Compress(GameObject gameObject, Color value)
        {
            int places = RuleUtility.FindDecimalPlaces(gameObject);
            return Compress(places, value);
        }

        /// <summary>
        /// Compress a Color based on given decimal places
        /// </summary>
        /// <param name="places"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static Color Compress(int places, Color value)
        {
            value.r = Compress(places, value.b);
            value.g = Compress(places, value.g);
            value.b = Compress(places, value.b);
            value.a = Compress(places, value.a);

            return value;
        }

        /// <summary>
        /// Compress an array of Vector3
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Vector3[] Compress(GameObject gameObject, Vector3[] value)
        {
            int places = RuleUtility.FindDecimalPlaces(gameObject);

            Vector3[] result = new Vector3[value.Length];

            for(int i=0; i < value.Length && i < result.Length; i++)
                result[i] = Compress(places, result[i]);

            return result;
        }

        /// <summary>
        /// Compress a Vector3
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Vector3 Compress(GameObject gameObject, Vector3 value)
        {
            int places = RuleUtility.FindDecimalPlaces(gameObject);
            return Compress(places, value);
        }

        /// <summary>
        /// Compress a Vector3 based on given decimal places
        /// </summary>
        /// <param name="places"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static Vector3 Compress(int places, Vector3 value)
        {
            value.x = Compress(places, value.x);
            value.y = Compress(places, value.y);
            value.z = Compress(places, value.z);

            return value;
        }

        /// <summary>
        /// Compress an array of Vector2
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Vector2[] Compress(GameObject gameObject, Vector2[] value)
        {
            int places = RuleUtility.FindDecimalPlaces(gameObject);

            Vector2[] result = new Vector2[value.Length];

            for(int i=0; i < value.Length && i < result.Length; i++)
                result[i] = Compress(places, result[i]);

            return result;
        }

        /// <summary>
        /// Compress a Vector2
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Vector2 Compress(GameObject gameObject, Vector2 value)
        {
            int places = RuleUtility.FindDecimalPlaces(gameObject);
            return Compress(places, value);
        }

        /// <summary>
        /// Compress a Vector2 based on given decimal places
        /// </summary>
        /// <param name="places"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static Vector2 Compress(int places, Vector2 value)
        {
            value.x = Compress(places, value.x);
            value.y = Compress(places, value.y);

            return value;
        }

        /// <summary>
        /// Compress a float value
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static float Compress(GameObject gameObject, float value)
        {
            int places = RuleUtility.FindDecimalPlaces(gameObject);
            return Compress(places, value);
        }

        /// <summary>
        /// Compress a float value based on given decimal places
        /// </summary>
        /// <param name="places"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static float Compress(int places, float value)
        {
            // Maximum compression allows one decimal place
            if(places <= 0)
                places = 1;

            // Compress value according to decimal places
            int factor = (int)Math.Pow(10, places);
            return (float)Math.Truncate(value * factor) / factor;
        }
    }
}