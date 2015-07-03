using Kerosene.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kerosene.ORM.Core.Concrete
{
	// ==================================================== 
	/// <summary>
	/// Represents a generic ODBC database engine.
	/// </summary>
	public class OdbcEngine : DataEngine, IDataEngine
	{
		/// <summary></summary>
		public const string ODBC_INVARIANT_NAME = "System.Data.Odbc";
		/// <summary></summary>
		public const bool ODBC_CASE_SENSITIVENAMES = false;
		/// <summary></summary>
		public const string ODBC_PARAMETER_PREFIX = "?";
		/// <summary></summary>
		public const bool ODBC_POSITIONAL_PARAMETERS = true;
		/// <summary></summary>
		public const bool ODBC_SUPPORT_NATIVE_SKIPTAKE = false;

		/// <summary>
		/// Initializes a new instance using default values.
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
		/// Initializes a new instance using the values given.
		/// </summary>
		/// <param name="invariantName">The invariant name of the engine.</param>
		/// <param name="serverVersion">The server version, or null.</param>
		/// <param name="caseSensitiveNames">Whether names in the database are case sensitive or not.</param>
		/// <param name="parameterPrefix">The default prefix for naming command parameters.</param>
		/// <param name="positionalParameters">Whether the command parameters are positional or not.</param>
		/// <param name="supportsSkipTake">Whether the engine supports a normalized way of implementing a skip/take functionality or not.</param>
		public OdbcEngine(
			string invariantName,
			string serverVersion = null,
			bool caseSensitiveNames = ODBC_CASE_SENSITIVENAMES,
			string parameterPrefix = ODBC_PARAMETER_PREFIX,
			bool positionalParameters = ODBC_POSITIONAL_PARAMETERS,
			bool supportsSkipTake = ODBC_SUPPORT_NATIVE_SKIPTAKE)
			: base(invariantName, serverVersion, caseSensitiveNames, parameterPrefix, positionalParameters, supportsSkipTake)
		{
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
