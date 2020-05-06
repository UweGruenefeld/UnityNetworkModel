/**
 * 
 * Name Type Pair Struct of Unity Network Model
 *
 * @file NameTypePair.cs
 * @author Uwe Gruenefeld
 * @version 2020-05-06
 *
 **/
using System;

namespace UnityNetworkModel
{
    /// <summary>
    /// Struct to contain a Resource or Component
    /// </summary>
    internal struct NameTypePair : IEquatable<NameTypePair>
    {
        public string name { get; }
        public Type type { get; }

        /// <summary>
        /// Creates a NameTypePair
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        internal NameTypePair(string name, Type type)
        {
            this.name = name;
            this.type = type;
        }

        /// <summary>
        /// Compares NameTypePair to another instance
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(NameTypePair other)
        {
            return this.name.Equals(other.name) && this.type.Equals(other.type);
        }

        /// <summary>
        /// Creates a Hash for NameTypePair
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.name.GetHashCode() ^ this.type.GetHashCode();
        }

        /// <summary>
        /// Transforms NameTypePair into string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Concat("{", this.name, ":", this.type, "}");
        }
    }
}