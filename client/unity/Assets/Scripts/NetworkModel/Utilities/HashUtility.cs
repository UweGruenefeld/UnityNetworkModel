/**
 * 
 * Hashes Utility of Unity Network Model
 *
 * @file HashUtility.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-05-04
 *
 **/
namespace UnityNetworkModel
{
    /// <summary>
    /// Utility class with functions related to Hashes
    /// </summary>
    internal static class HashUtility
    {
        /// <summary>
        /// Utility function to create order-dependent hash from two hashes. Collisions unlikely if original hashes are already well-distibuted.
        /// </summary>
        /// <param name="h1"></param>
        /// <param name="h2"></param>
        /// <returns></returns>
        internal static long Combine2Hashes(long h1, long h2)
        {
            return h1 * 31 + h2;
        }

        /// <summary>
        /// Utility function to create order-dependent hash from multiple hashes. Collisions unlikely if original hashes are already well-distibuted.
        /// </summary>
        /// <param name="hashes"></param>
        /// <returns></returns>
        internal static long CombineHashes(params long[] hashes)
        {
            long hash = 17;
            
            foreach (long h in hashes)
            {
                hash = HashUtility.Combine2Hashes(hash, h);
            }
            
            return hash;
        }
    }
}