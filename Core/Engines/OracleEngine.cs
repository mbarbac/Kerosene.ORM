// ======================================================== OracleEngine.cs
namespace Kerosene.ORM.Core.Concrete
{
	using Kerosene.Tools;
	using System;
	using System.Collections.Generic;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents a generic ORACLE database engine.
	/// </summary>
	public class OracleEngine : DataEngine, IDataEngine
	{
		public const string ORACLE_INVARIANT_NAME = "System.Data.OracleClient";
		public const bool ORACLE_CASE_SENSITIVENAMES = true;
		public const string ORACLE_PARAMETER_PREFIX = ":";
		public const bool ORACLE_POSITIONAL_PARAMETERS = false;
		public const bool ORACLE_SUPPORT_NATIVE_SKIPTAKE = false;

		/// <summary>
		/// Initializes a new instance using default values.
		/// </summary>
		public OracleEngine()
			: base()
		{
			InvariantName = ORACLE_INVARIANT_NAME;
			CaseSensitiveNames = ORACLE_CASE_SENSITIVENAMES;
			ParameterPrefix = ORACLE_PARAMETER_PREFIX;
			PositionalParameters = ORACLE_POSITIONAL_PARAMETERS;
			SupportsNativeSkipTake = ORACLE_SUPPORT_NATIVE_SKIPTAKE;
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
		public OracleEngine(
			string invariantName,
			string serverVersion = null,
			bool caseSensitiveNames = ORACLE_CASE_SENSITIVENAMES,
			string parameterPrefix = ORACLE_PARAMETER_PREFIX,
			bool positionalParameters = ORACLE_POSITIONAL_PARAMETERS,
			bool supportsSkipTake = ORACLE_SUPPORT_NATIVE_SKIPTAKE)
			: base(invariantName, serverVersion, caseSensitiveNames, parameterPrefix, positionalParameters, supportsSkipTake)
		{
		}

		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		public new OracleEngine Clone()
		{
			var cloned = new OracleEngine();
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
		public new OracleEngine Clone(IDictionary<string, object> settings)
		{
			var cloned = new OracleEngine();
			OnClone(cloned, settings); return cloned;
		}
		IDataEngine IDataEngine.Clone(IDictionary<string, object> settings)
		{
			return this.Clone(settings);
		}
	}
}
// ======================================================== 
