// ======================================================== LinkFactory.cs
namespace Kerosene.ORM.Direct
{
	using Kerosene.Tools;
	using System;
	using System.Collections.Generic;
	using System.Data;

	// ==================================================== 
	/// <summary>
	/// Factory class to generate new <see cref="IDataLink"/> instances.
	/// </summary>
	public static class LinkFactory
	{
		/// <summary>
		/// Convenience method to instantiate a direct link associated with the given direct
		/// engine.
		/// </summary>
		/// <param name="engine">The engine the new link will be associated with.</param>
		/// <param name="mode">The default initial mode for the transaction to be created when needed.</param>
		/// <returns>A new direct link.</returns>
		public static IDataLink Create(
			IDataEngine engine,
			Core.NestableTransactionMode mode = Core.NestableTransactionMode.Database)
		{
			return new Concrete.DataLink(engine, mode);
		}

		/// <summary>
		/// Creates a bew link finding the appropriate engine and connection string from the
		/// given arguments.
		/// </summary>
		/// <param name="name">A string containing either the invariant name of the engine, or
		/// its tail part, or the name of a connection string entry in the configuration files,
		/// or null. In the later case, the name of the default connection string entry is used.</param>
		/// <param name="cnstr">If not null, and name did not correspond to a connection string
		/// entry, the actual contents of the connection string to set into the new link.</param>
		/// <param name="minVersion">If not null the minimum acceptable version.</param>
		/// <param name="maxVersion">If not null the maximum acceptable version.</param>
		/// <param name="validator">If not null the delegate to invoke for custom validation of
		/// a candidate engine, which is considered valid if the delegate returns true, or invalid
		/// if it returns false.</param>
		/// <param name="settings">If not null a dictionary containing the names and values of
		/// the properties to modify on the found engine, generating a clone with the new values.</param>
		/// <param name="mode">The default initial mode for the transaction to be created when needed.</param>
		/// <returns>A new direct link.</returns>
		public static IDataLink Create(
			string name = null,
			string cnstr = null,
			string minVersion = null,
			string maxVersion = null,
			Func<IDataEngine, bool> validator = null,
			Dictionary<string, object> settings = null,
			Core.NestableTransactionMode mode = Core.NestableTransactionMode.Database
			)
		{
			var engine = EngineFactory.Locate(name, minVersion, maxVersion, validator, settings);
			if (engine == null)
			{
				if (name == null) throw new NotFoundException("Cannot find a default engine.");
				throw new NotFoundException("Cannot find a '{0}' registered engine.".FormatWith(name));
			}
			var link = new Concrete.DataLink(engine, mode);

			var cn = Configuration.ConnectionStringEx.Find(name); if (cn != null)
			{
				link.ConnectionString = cn.ConnectionString;
			}
			else if (cnstr != null) link.ConnectionString = cnstr;

			return link;
		}
	}
}
// ======================================================== 
