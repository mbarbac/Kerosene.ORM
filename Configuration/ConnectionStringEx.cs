// ======================================================== ConnectionStringEx.cs
namespace Kerosene.ORM.Configuration
{
	using Kerosene.Tools;
	using System;
	using System.Configuration;

	// ==================================================== 
	/// <summary>
	/// Helpers and extensions for working with connection string entries in the configuration
	/// files.
	/// </summary>
	public static class ConnectionStringEx
	{
		/// <summary>
		/// Returns the connection string entry whose name is given, or null if such entry cannot
		/// be found.
		/// </summary>
		/// <param name="entry">The name of the connection string entry, or null to find the one
		/// given by the 'connectionString' element on the 'keroseneORM' configuration section.</param>
		/// <returns>The requested connection string entry, or null.</returns>
		public static ConnectionStringSettings Find(string entry = null)
		{
			entry = entry.Validated("Entry Name", canbeNull: true, emptyAsNull: true);

			if (entry == null)
			{
				var info = ORMConfiguration.GetInfo(); if (info == null) return null;
				if (info.ConnectionString == null) return null;
				if (info.ConnectionString.Name == null) return null;

				entry = info.ConnectionString.Name;
			}

			ConnectionStringSettings cn = null;
			try { cn = ConfigurationManager.ConnectionStrings[entry]; }
			catch { }

			return cn;
		}
	}
}
// ======================================================== 
