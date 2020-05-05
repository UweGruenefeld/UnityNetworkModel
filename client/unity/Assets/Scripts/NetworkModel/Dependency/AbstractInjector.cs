/**
 * 
 * Abstract Injector for Dependencies of Unity Network Model
 *
 * @file AbstractInjector.cs
 * @author Uwe Gruenefeld
 * @version 2020-05-03
 **/
namespace UnityNetworkModel
{
    /// <summary>
    /// Abstract Injector to handle Dependencies for Network Model Configuration
    /// </summary>
    internal abstract class AbstractInjector
    {
        // Reference to Injector
        protected Injector injector;

        /// <summary>
        /// Creates Injektor-based class with injector reference
        /// </summary>
        /// <param name="injector"></param>
        internal AbstractInjector(Injector injector)
        {
            // Asign the Injector Reference
            this.injector = injector;
        }
    }
}