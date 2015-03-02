// ======================================================== EngineFactory.cs
namespace Kerosene.ORM.Direct
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Factory class to register and locate <see cref="IDataEngine"/> instances.
	/// </summary>
	public static class EngineFactory
	{
		/// <summary>
		/// Initializes the list of registered engines with the default ones and those that are
		/// specified in the configuration files.
		/// </summary>
		public static void InitializeEngines()
		{
			Core.EngineFactory.InitializeEngines();
		}

		/// <summary>
		/// Clears the list of registered engines.
		/// </summary>
		public static void ClearEngines()
		{
			Core.EngineFactory.ClearEngines();
		}

		/// <summary>
		/// Removes the given engine from the list of registered ones.
		/// </summary>
		/// <param name="engine">The engine to remove.</param>
		/// <returns>True if the engine was removed, false otherwise.</returns>
		public static bool RemoveEngine(IDataEngine engine)
		{
			return Core.EngineFactory.RemoveEngine(engine);
		}

		/// <summary>
		/// Registers the given engine if it has not been registered before.
		/// </summary>
		/// <param name="engine">The engine to register.</param>
		public static void RegisterEngine(IDataEngine engine)
		{
			Core.EngineFactory.RegisterEngine(engine);
		}

		/// <summary>
		/// The collection of registered engines.
		/// </summary>
		public static IEnumerable<IDataEngine> Engines
		{
			get { return Core.EngineFactory.Engines.OfType<IDataEngine>(); }
		}

		/// <summary>
		/// Locates and returns the latest registered engine that matches the conditions given,
		/// or null if no one is found.
		/// </summary>
		/// <param name="name">A string containing either the invariant name of the engine, or
		/// its tail part, or the name of a connection string entry in the configuration files,
		/// or null. In the later case, the name of the default connection string entry is used.</param>
		/// <param name="minVersion">If not null the minimum acceptable version.</param>
		/// <param name="maxVersion">If not null the maximum acceptable version.</param>
		/// <param name="validator">If not null the delegate to invoke for custom validation of
		/// a candidate engine, which is considered valid if the delegate returns true, or invalid
		/// if it returns false.</param>
		/// <param name="settings">If not null a dictionary containing the names and values of
		/// the properties to modify on the found engine, generating a clone with the new values.</param>
		/// <returns>An engine found, or null.</returns>
		public static IDataEngine Locate(
			string name = null,
			string minVersion = null,
			string maxVersion = null,
			Func<IDataEngine, bool> validator = null,
			IDictionary<string, object> settings = null)
		{
			Func<Core.IDataEngine, bool> func = item =>
			{
				if (!(item is IDataEngine)) return false;
				return validator == null ? true : validator((IDataEngine)item);
			};

			var engine = Core.EngineFactory.Locate(name, minVersion, maxVersion, func, settings);
			return (IDataEngine)engine;
		}
	}
}
// ======================================================== 
