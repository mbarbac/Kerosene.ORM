// ======================================================== OdbcEngine.cs
namespace Kerosene.ORM.Core.Concrete
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Represents a generic ODBC database.
	/// </summary>
	public class OdbcEngine : DataEngine, IDataEngine
	{
		const string ODBC_INVARIANT_NAME = "System.Data.Odbc";
		const bool ODBC_CASE_SENSITIVENAMES = false;
		const string ODBC_PARAMETER_PREFIX = "?";
		const bool ODBC_POSITIONAL_PARAMETERS = true;
		const bool ODBC_SUPPORT_NATIVE_SKIPTAKE = false;

		/// <summary>
		/// Initializes a new engine.
		/// </summary>
		public OdbcEngine()
			: base()
		{
			InvariantName = ODBC_INVARIANT_NAME;
			CaseSensitiveNames = ODBC_CASE_SENSITIVENAMES;
			ParameterPrefix = ODBC_PARAMETER_PREFIX;
			PositionalParameters = ODBC_POSITIONAL_PARAMETERS;
			SupportsNativeSkipTake = ODBC_SUPPORT_NATIVE_SKIPTAKE;
		}

		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		public new OdbcEngine Clone()
		{
			var cloned = new OdbcEngine();
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
		public new OdbcEngine Clone(IDictionary<string, object> settings)
		{
			var cloned = new OdbcEngine();
			OnClone(cloned, settings); return cloned;
		}
		IDataEngine IDataEngine.Clone(IDictionary<string, object> settings)
		{
			return this.Clone(settings);
		}
	}
}
// ======================================================== 
