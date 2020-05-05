/**
 * 
 * Extension of Rule Type Enum of Unity Network Model
 *
 * @file RuleTypeExtension.cs
 * @author Uwe Gruenefeld
 * @version 2020-05-04
 *
 **/
namespace UnityNetworkModel
{
    /// <summary>
    /// Extension of Enum RuleType
    /// </summary>
    internal static class RuleTypeExtension
    {
        /// <summary>
        /// Transform Enum RuleType into a boolean
        /// </summary>
        /// <param name="ruleType"></param>
        /// <returns></returns>
        internal static bool ToBool(this RuleType ruleType)
        {
            switch (ruleType)
            {
                case RuleType.TRUE:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Transform boolean into Enum RuleType
        /// </summary>
        /// <param name="boolValue"></param>
        /// <returns></returns>
        internal static RuleType FromBool(bool boolValue)
        {
            if(boolValue)
                return RuleType.TRUE;

            return RuleType.FALSE;
        }
    }
}