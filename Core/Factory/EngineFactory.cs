// ======================================================== EngineFactory.cs
namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Data.Common;
	using System.Linq;
	using System.Reflection;

	// ==================================================== 
	/// <summary>
	/// Factory class to register and locate <see cref="IDataEngine"/> instances.
	/// </summary>
	public static class EngineFactory
	{
		static List<IDataEngine> _Engines = null;

		/// <summary>
		/// Initializes the list of registered engines with the default ones and those that are
		/// specified in the configuration files.
		/// </summary>
		public static void InitializeEngines()
		{
			if (_Engines == null) _Engines = new List<IDataEngine>();
			_Engines.Clear();

			lock (_Engines)
			{
				var info = Configuration.ORMConfiguration.GetInfo();
				if (info != null && info.CustomEngines.Count != 0)
				{
					string currentDLL = Assembly.GetExecutingAssembly().GetName().Name + ".dll";

					foreach (Configuration.CustomEngineElement entry in info.CustomEngines)
					{
						IDataEngine engine = null;
						Type type = null;
						Assembly asm = null;

						if (string.Compare(entry.AssemblyName, currentDLL, ignoreCase: true) == 0)
						{
							if ((type = Type.GetType(entry.TypeName)) == null)
								throw new NotFoundException("Type '{0}' not found".FormatWith(entry.TypeName));
						}
						else
						{
							if ((asm = Assembly.LoadFrom(entry.AssemblyName)) == null)
								throw new NotFoundException("Assembly '{0}' not found.".FormatWith(entry.AssemblyName));

							if ((type = asm.GetType(entry.TypeName)) == null)
								throw new NotFoundException("Type '{0}' not found in assembly '{1}'.".FormatWith(entry.TypeName, entry.AssemblyName));
						}

						try { engine = (IDataEngine)Activator.CreateInstance(type); }
						catch { }
						if (engine == null) continue;

						var settings = new Dictionary<string, object>();
						if (entry.InvarianteName != null) settings.Add(ElementInfo.ParseName<IDataEngine>(x => x.InvariantName), entry.InvarianteName);
						if (entry.ServerVersion != null) settings.Add(ElementInfo.ParseName<IDataEngine>(x => x.ServerVersion), entry.ServerVersion);
						if (entry.CaseSensitiveNames != null) settings.Add(ElementInfo.ParseName<IDataEngine>(x => x.CaseSensitiveNames), entry.CaseSensitiveNames);
						if (entry.ParameterPrefix != null) settings.Add(ElementInfo.ParseName<IDataEngine>(x => x.ParameterPrefix), entry.ParameterPrefix);
						if (entry.PositionalParameters != null) settings.Add(ElementInfo.ParseName<IDataEngine>(x => x.PositionalParameters), entry.PositionalParameters);
						if (entry.SupportsNativeSkipTake != null) settings.Add(ElementInfo.ParseName<IDataEngine>(x => x.SupportsNativeSkipTake), entry.SupportsNativeSkipTake);

						if (settings.Count != 0) engine = engine.Clone(settings);
						_Engines.Add(engine);
					}
				}

				_Engines.Add(new Core.Concrete.DataEngine());
				_Engines.Add(new Core.Concrete.OdbcEngine());
				_Engines.Add(new Core.Concrete.OleDbEngine());
				_Engines.Add(new Core.Concrete.OracleEngine());
				_Engines.Add(new Core.Concrete.SqlServerEngine());

				_Engines.Add(new Direct.Concrete.OdbcEngine());
				_Engines.Add(new Direct.Concrete.OleDbEngine());
				_Engines.Add(new Direct.Concrete.OracleEngine());
				_Engines.Add(new Direct.Concrete.SqlServerEngine());
			}
		}

		/// <summary>
		/// Clears the list of registered engines.
		/// </summary>
		public static void ClearEngines()
		{
			if (_Engines == null) _Engines = new List<IDataEngine>();
			_Engines.Clear();
		}

		/// <summary>
		/// Removes the given engine from the list of registered ones.
		/// </summary>
		/// <param name="engine">The engine to remove.</param>
		/// <returns>True if the engine was removed, false otherwise.</returns>
		public static bool RemoveEngine(IDataEngine engine)
		{
			if (engine == null) throw new ArgumentNullException("engine", "Engine cannot be null.");

			if (_Engines == null) _Engines = new List<IDataEngine>();

			bool r = false; lock (_Engines) { r = _Engines.Remove(engine); }
			return r;
		}

		/// <summary>
		/// Registers the given engine if it has not been registered before.
		/// </summary>
		/// <param name="engine">The engine to register.</param>
		public static void RegisterEngine(IDataEngine engine)
		{
			if (engine == null) throw new ArgumentNullException("engine", "Engine cannot be null.");

			if (_Engines == null) _Engines = new List<IDataEngine>();
			lock (_Engines)
			{
				if (!_Engines.Contains(engine)) _Engines.Add(engine);
			}
		}

		/// <summary>
		/// The collection of registered engines.
		/// </summary>
		public static IEnumerable<IDataEngine> Engines
		{
			get
			{
				if (_Engines == null) InitializeEngines();
				return _Engines;
			}
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
			if (_Engines == null) InitializeEngines();

			name = name.NullIfTrimmedIsEmpty();
			minVersion = minVersion.NullIfTrimmedIsEmpty(); var minEasyVersion = new EasyVersion(minVersion);
			maxVersion = maxVersion.NullIfTrimmedIsEmpty(); var maxEasyVersion = new EasyVersion(maxVersion);

			string cnVersion = null;
			var cnEasyVersion = EasyVersion.Empty;
			var cn = Configuration.ConnectionStringEx.Find(name); if (cn == null)
			{
				if (name == null) throw new ArgumentNullException(
					"Engine name is null and no default entry was found.");
			}
			else
			{
				if (cn.ProviderName == null) throw new InvalidOperationException(
					"Connection string entry '{0}' contains no provider name property."
					.FormatWith(cn.Name));

				name = cn.ProviderName; try
				{
					var factory = DbProviderFactories.GetFactory(cn.ProviderName);
					var dbconn = factory.CreateConnection();
					dbconn.ConnectionString = cn.ConnectionString;
					dbconn.Open();

					cnVersion = dbconn.ServerVersion.NullIfTrimmedIsEmpty();
					cnEasyVersion = new EasyVersion(cnVersion);

					dbconn.Close();
					dbconn.Dispose();
				}
				catch { }
			}

			IDataEngine engine = null; lock (_Engines)
			{
				foreach (var item in _Engines)
				{
					bool match = string.Compare(name, item.InvariantName, ignoreCase: true) == 0;
					if (!match)
					{
						int i = item.InvariantName.LastIndexOf('.'); if (i >= 0)
						{
							var str = item.InvariantName.Substring(i + 1);
							match = string.Compare(name, str, ignoreCase: true) == 0;
						}
					}
					if (!match) continue;

					var entry = settings == null ? item : item.Clone(settings);
					if (validator != null && !validator(entry)) continue;

					var entryVersion = new EasyVersion(entry.ServerVersion.NullIfTrimmedIsEmpty());
					if (minEasyVersion != EasyVersion.Empty && entryVersion < minEasyVersion) continue;
					if (maxEasyVersion != EasyVersion.Empty && entryVersion > maxEasyVersion) continue;

					// TODO: figure out what to do in this scenario...
					//if (cnEasyVersion != EasyVersion.Empty && 
					//	entryVersion != EasyVersion.Empty &&
					//	entryVersion < cnEasyVersion) continue;

					if (engine == null) engine = entry;
					else
					{
						// Bigger or last one has precedence...
						var engineVersion = new EasyVersion(engine.ServerVersion);
						if (entryVersion >= engineVersion) engine = entry;
					}
				}
			}

			return engine;
		}
	}
}
// ======================================================== 
