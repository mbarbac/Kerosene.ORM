// ======================================================== OleDbEngine.cs
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
	/// Represents a generic OLE DB database.
	/// </summary>
	public class OleDbEngine : DataEngine, IDataEngine
	{
		const string OLEDB_INVARIANT_NAME = "System.Data.OleDb";
		const bool OLEDB_CASE_SENSITIVENAMES = false;
		const string OLEDB_PARAMETER_PREFIX = "?";
		const bool OLEDB_POSITIONAL_PARAMETERS = true;
		const bool OLEDB_SUPPORT_NATIVE_SKIPTAKE = false;

		/// <summary>
		/// Initializes a new engine.
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
// ======================================================== 
