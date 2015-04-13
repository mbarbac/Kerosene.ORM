// ======================================================== SqlServerEngine.cs
namespace Kerosene.ORM.Core.Concrete
{
	using Kerosene.Tools;
	using System;
	using System.Collections.Generic;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents a generic SQLSERVER database engine.
	/// </summary>
	public class SqlServerEngine : DataEngine, IDataEngine
	{
		public const string SQLSERVER_INVARIANT_NAME = "System.Data.SqlClient";
		public const bool SQLSERVER_CASE_SENSITIVENAMES = false;
		public const string SQLSERVER_PARAMETER_PREFIX = "@";
		public const bool SQLSERVER_POSITIONAL_PARAMETERS = false;
		public const bool SQLSERVER_SUPPORT_NATIVE_SKIPTAKE = false;

		/// <summary>
		/// Initializes a new instance using default values.
		/// </summary>
		public SqlServerEngine()
			: base()
		{
			InvariantName = SQLSERVER_INVARIANT_NAME;
			CaseSensitiveNames = SQLSERVER_CASE_SENSITIVENAMES;
			ParameterPrefix = SQLSERVER_PARAMETER_PREFIX;
			PositionalParameters = SQLSERVER_POSITIONAL_PARAMETERS;
			SupportsNativeSkipTake = SQLSERVER_SUPPORT_NATIVE_SKIPTAKE;
		}

		/// <summary>
		/// Initializes a new instance using the values given.
		/// </summary>
		/// <param name="invariantName">The invariant name of the engine.</param>
		/// <param name="serverVersion">The server version, or null.</param>
		/// <param name="caseSensitiveNames">Whether names in the database are case sensitive or not.</param>
		/// <param name="parameterPrefix">The default prefix for naming command parameters.</param>
		/// <param name="positionalParameters">Whether the command parameters are positional or not.</param>
		/// <param name="supportsSkipTake">Whether the engine supports a normalized way of implementing a skip/take functionality or not.</param>
		public SqlServerEngine(
			string invariantName,
			string serverVersion = null,
			bool caseSensitiveNames = SQLSERVER_CASE_SENSITIVENAMES,
			string parameterPrefix = SQLSERVER_PARAMETER_PREFIX,
			bool positionalParameters = SQLSERVER_POSITIONAL_PARAMETERS,
			bool supportsSkipTake = SQLSERVER_SUPPORT_NATIVE_SKIPTAKE)
			: base(invariantName, serverVersion, caseSensitiveNames, parameterPrefix, positionalParameters, supportsSkipTake)
		{
		}

		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		public new SqlServerEngine Clone()
		{
			var cloned = new SqlServerEngine();
			OnClone(cloned, null); return cloned;
		}
		IDataEngine IDataEngine.Clone()
		{
			return this.Clone();
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <param name="settings">A dictionary containing the names of the properties whose
		/// values are to be changed with respect to the original instance, or null to not
		/// modify any of those.</param>
		/// <returns>A new instance.</returns>
		public new SqlServerEngine Clone(IDictionary<string, object> settings)
		{
			var cloned = new SqlServerEngine();
			OnClone(cloned, settings); return cloned;
		}
		IDataEngine IDataEngine.Clone(IDictionary<string, object> settings)
		{
			return this.Clone(settings);
		}
	}
}
// ======================================================== 
