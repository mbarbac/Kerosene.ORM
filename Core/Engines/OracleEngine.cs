// ======================================================== OracleEngine.cs
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
	/// Represents a generic ORACLE database.
	/// </summary>
	public class OracleEngine : DataEngine, IDataEngine
	{
		const string ORACLE_INVARIANT_NAME = "System.Data.OracleClient";
		const bool ORACLE_CASE_SENSITIVENAMES = true;
		const string ORACLE_PARAMETER_PREFIX = ":";
		const bool ORACLE_POSITIONAL_PARAMETERS = false;
		const bool ORACLE_SUPPORT_NATIVE_SKIPTAKE = false;

		/// <summary>
		/// Initializes a new engine.
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
