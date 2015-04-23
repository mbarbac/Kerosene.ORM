namespace Kerosene.ORM.Direct
{
	using Kerosene.Tools;
	using System;
	using System.Collections.Generic;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Factory class to register and locate <see cref="IDataEngine"/> instances adapted to
	/// direct connection scenarios.
	/// </summary>
	public static partial class DataEngine
	{
		/// <summary>
		/// Initializes the list of registered engines with the default ones and those that are
		/// specified in the configuration files.
		/// </summary>
		public static void InitializeEngines()
		{
			Core.DataEngine.InitializeEngines();
		}

		/// <summary>
		/// Clears the list of registered engines.
		/// </summary>
		public static void ClearEngines()
		{
			Core.DataEngine.ClearEngines();
		}

		/// <summary>
		/// Removes the given direct engine from the list of registered ones.
		/// </summary>
		/// <param name="engine">The engine to remove.</param>
		/// <returns>True if the engine was removed, false otherwise.</returns>
		public static bool RemoveEngine(IDataEngine engine)
		{
			return Core.DataEngine.RemoveEngine(engine);
		}

		/// <summary>
		/// Registers the given direct engine if it has not been registered before.
		/// </summary>
		/// <param name="engine">The engine to register.</param>
		public static void RegisterEngine(IDataEngine engine)
		{
			Core.DataEngine.RegisterEngine(engine);
		}
		/// <summary>
		/// The collection of registered direct engines.
		/// </summary>
		public static IEnumerable<IDataEngine> Engines
		{
			get { return Core.DataEngine.Engines.OfType<IDataEngine>(); }
		}

		/// <summary>
		/// Locates and returns the latest registered direct engine that matches the conditions
		/// given, or null if no one is found.
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

			var engine = Core.DataEngine.Locate(name, minVersion, maxVersion, func, settings);
			return (IDataEngine)engine;
		}
	}
}
