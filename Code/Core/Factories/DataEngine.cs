using Kerosene.Tools;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Kerosene.ORM.Core
{
	// ==================================================== 
	/// <summary>
	/// Factory class to register and locate <see cref="IDataEngine"/> instances.
	/// </summary>
	public static partial class DataEngine
	{
		static List<IDataEngine> _Engines = new List<IDataEngine>();
		static object _SyncRoot = new object();
		static bool _Initialized = false;

		/// <summary>
		/// Initializes the list of registered engines only if it was not initialized yet.
		/// </summary>
		static void InitializeIfNeeded()
		{
			if (!_Initialized) InitializeEngines();
		}

		/// <summary>
		/// Initializes the list of registered engines with the default ones and with the
		/// ones read from the configuration files.
		/// </summary>
		public static void InitializeEngines()
		{
			_Initialized = true; lock (_SyncRoot)
			{
				// Let's start from a clean state...
				_Engines.Clear();

				// Registering the standard ones...
				_Engines.Add(new Core.Concrete.DataEngine());
				_Engines.Add(new Core.Concrete.OdbcEngine());
				_Engines.Add(new Core.Concrete.OleDbEngine());
				_Engines.Add(new Core.Concrete.OracleEngine());
				_Engines.Add(new Core.Concrete.SqlServerEngine());

				_Engines.Add(new Direct.Concrete.OdbcEngine());
				_Engines.Add(new Direct.Concrete.OleDbEngine());
				_Engines.Add(new Direct.Concrete.OracleEngine());
				_Engines.Add(new Direct.Concrete.SqlServerEngine());

				// Registering the ones from the configuration files...
				var info = Configuration.ORMConfiguration.GetInfo();
				if (info != null && info.CustomEngines != null && info.CustomEngines.Count != 0)
				{
					var codeBase = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
					var head = "file:\\";
					if (codeBase.StartsWith(head, StringComparison.OrdinalIgnoreCase))
					{
						codeBase = codeBase.Substring(head.Length);
					}

					var currentDLL = Assembly.GetExecutingAssembly().GetName().Name + ".dll";
					foreach (var entry in info.CustomEngines.Items)
					{
						IDataEngine engine = null;
						Type type = null;
						Assembly asm = null;

						var str = entry.AssemblyName;
						if (!str.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) str += ".dll";
						if (string.Compare(str, currentDLL, ignoreCase: true) == 0)
						{
							if ((type = Type.GetType(entry.TypeName)) == null)
								throw new NotFoundException(
									"Type '{0}' not found.".FormatWith(entry.TypeName));
						}
						else
						{
							try
							{
								if ((asm = Assembly.LoadFrom(str)) == null)
									throw new FileNotFoundException("Cannot load '{0}'.".FormatWith(str));
							}
							catch (FileNotFoundException)
							{
								str = codeBase + "\\" + str;

								if ((asm = Assembly.LoadFrom(str)) == null)
									throw new NotFoundException(
										"Assembly '{0}' not found".FormatWith(entry.AssemblyName));
							}

							if ((type = asm.GetType(entry.TypeName)) == null)
								throw new NotFoundException(
									"Type '{0}' not found in assembly '{1}'."
									.FormatWith(entry.TypeName, str));
						}

						try { engine = (IDataEngine)Activator.CreateInstance(type); }
						catch { }
						if (engine == null) continue;

						var settings = new Dictionary<string, object>();
						if (entry.InvariantName != null) settings.Add(ElementInfo.ParseName<IDataEngine>(x => x.InvariantName), entry.InvariantName);
						if (entry.ServerVersion != null) settings.Add(ElementInfo.ParseName<IDataEngine>(x => x.ServerVersion), entry.ServerVersion);
						if (entry.CaseSensitiveNames != null) settings.Add(ElementInfo.ParseName<IDataEngine>(x => x.CaseSensitiveNames), entry.CaseSensitiveNames);
						if (entry.ParameterPrefix != null) settings.Add(ElementInfo.ParseName<IDataEngine>(x => x.ParameterPrefix), entry.ParameterPrefix);
						if (entry.PositionalParameters != null) settings.Add(ElementInfo.ParseName<IDataEngine>(x => x.PositionalParameters), entry.PositionalParameters);
						if (entry.SupportsNativeSkipTake != null) settings.Add(ElementInfo.ParseName<IDataEngine>(x => x.SupportsNativeSkipTake), entry.SupportsNativeSkipTake);

						if (settings.Count != 0) engine = engine.Clone(settings);
						_Engines.Add(engine);
					}
				}
			}
		}

		/// <summary>
		/// Clears the list of registered engines.
		/// </summary>
		public static void ClearEngines()
		{
			lock (_SyncRoot) { _Initialized = true; _Engines.Clear(); }
		}

		/// <summary>
		/// Removes the given engine from the list of registered ones.
		/// </summary>
		/// <param name="engine">The engine to remove.</param>
		/// <returns>True if the engine was removed, false otherwise.</returns>
		public static bool RemoveEngine(IDataEngine engine)
		{
			if (engine == null) throw new ArgumentNullException("engine", "Engine cannot be null.");
			lock (_SyncRoot) { InitializeIfNeeded(); return _Engines.Remove(engine); }
		}

		/// <summary>
		/// Registers the given engine if it has not been registered before.
		/// </summary>
		/// <param name="engine">The engine to register.</param>
		public static void RegisterEngine(IDataEngine engine)
		{
			if (engine == null) throw new ArgumentNullException("engine", "Engine cannot be null.");
			lock (_SyncRoot) { InitializeIfNeeded(); if (!_Engines.Contains(engine)) _Engines.Add(engine); }
		}

		/// <summary>
		/// The collection of registered engines.
		/// </summary>
		public static IEnumerable<IDataEngine> Engines
		{
			get { InitializeIfNeeded(); return _Engines; }
		}

		/// <summary>
		/// Returns the latest registered engine that matches the conditions given, or null
		/// if no one is found.
		/// </summary>
		/// <param name="name">The invariant name of the engine, or its tail part, or the
		/// name of a connection string entry, or null to use the default 'connectionString'
		/// entry in the 'keroseneORM' section.</param>
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
				catch
				{
					DebugEx.IndentWriteLine("\n- Engine's provider '{0}' not found.".FormatWith(cn.ProviderName));
					DebugEx.Unindent();
				}
			}

			IDataEngine engine = null; lock (_SyncRoot)
			{
				InitializeIfNeeded(); foreach (var item in _Engines)
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
