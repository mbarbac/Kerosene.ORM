using Kerosene.Tools;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Odbc;
using System.Linq;

namespace Kerosene.ORM.Direct.Concrete
{
	// ==================================================== 
	/// <summary>
	/// Represents a generic ODBC database engine for a direct connection scenario.
	/// </summary>
	public class OdbcEngine : Core.Concrete.OdbcEngine, IDataEngine
	{
		/// <summary>
		/// Initializes a new instance using default values.
		/// </summary>
		public OdbcEngine()
			: base()
		{
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
		/// Invoked to obtain the type name for string representation purposes.
		/// </summary>
		protected override string ToStringType()
		{
			return GetType().EasyName();
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
		Core.IDataEngine Core.IDataEngine.Clone()
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
		Core.IDataEngine Core.IDataEngine.Clone(IDictionary<string, object> settings)
		{
			return this.Clone(settings);
		}

		/// <summary>
		/// Gets the provider factory associated with this engine.
		/// </summary>
		public OdbcFactory ProviderFactory
		{
			get { return OdbcFactory.Instance; }
		}
		DbProviderFactory Direct.IDataEngine.ProviderFactory
		{
			get { return this.ProviderFactory; }
		}
	}
}
