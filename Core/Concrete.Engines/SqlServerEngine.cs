// ======================================================== SqlServerEngine.cs
namespace Kerosene.ORM.Core.Concrete
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Represents a generic SQL SERVER database.
	/// </summary>
	public class SqlServerEngine : DataEngine, IDataEngine
	{
		const string SQLSERVER_INVARIANT_NAME = "System.Data.SqlClient";
		const bool SQLSERVER_CASE_SENSITIVENAMES = false;
		const string SQLSERVER_PARAMETER_PREFIX = "@";
		const bool SQLSERVER_POSITIONAL_PARAMETERS = false;
		const bool SQLSERVER_SUPPORT_NATIVE_SKIPTAKE = false;

		/// <summary>
		/// Initializes a new engine.
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
