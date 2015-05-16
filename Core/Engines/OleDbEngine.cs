using Kerosene.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kerosene.ORM.Core.Concrete
{
	// ==================================================== 
	/// <summary>
	/// Represents a generic OLEDB database engine.
	/// </summary>
	public class OleDbEngine : DataEngine, IDataEngine
	{
		/// <summary></summary>
		public const string OLEDB_INVARIANT_NAME = "System.Data.OleDb";
		/// <summary></summary>
		public const bool OLEDB_CASE_SENSITIVENAMES = false;
		/// <summary></summary>
		public const string OLEDB_PARAMETER_PREFIX = "?";
		/// <summary></summary>
		public const bool OLEDB_POSITIONAL_PARAMETERS = true;
		/// <summary></summary>
		public const bool OLEDB_SUPPORT_NATIVE_SKIPTAKE = false;

		/// <summary>
		/// Initializes a new instance using default values.
		/// </summary>
		public OleDbEngine()
			: base()
		{
			InvariantName = OLEDB_INVARIANT_NAME;
			CaseSensitiveNames = OLEDB_CASE_SENSITIVENAMES;
			ParameterPrefix = OLEDB_PARAMETER_PREFIX;
			PositionalParameters = OLEDB_POSITIONAL_PARAMETERS;
			SupportsNativeSkipTake = OLEDB_SUPPORT_NATIVE_SKIPTAKE;
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
		public OleDbEngine(
			string invariantName,
			string serverVersion = null,
			bool caseSensitiveNames = OLEDB_CASE_SENSITIVENAMES,
			string parameterPrefix = OLEDB_PARAMETER_PREFIX,
			bool positionalParameters = OLEDB_POSITIONAL_PARAMETERS,
			bool supportsSkipTake = OLEDB_SUPPORT_NATIVE_SKIPTAKE)
			: base(invariantName, serverVersion, caseSensitiveNames, parameterPrefix, positionalParameters, supportsSkipTake)
		{
		}

		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		public new OleDbEngine Clone()
		{
			var cloned = new OleDbEngine();
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
		public new OleDbEngine Clone(IDictionary<string, object> settings)
		{
			var cloned = new OleDbEngine();
			OnClone(cloned, settings); return cloned;
		}
		IDataEngine IDataEngine.Clone(IDictionary<string, object> settings)
		{
			return this.Clone(settings);
		}
	}
}
