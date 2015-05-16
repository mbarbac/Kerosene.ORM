using Kerosene.Tools;
using System;
using System.Configuration;

namespace Kerosene.ORM.Configuration
{
	// ==================================================== 
	/// <summary>
	/// Helpers and extensions for working with connection strings.
	/// </summary>
	public static class ConnectionStringEx
	{
		/// <summary>
		/// Returns the connection string settings that correspond to the entry name given,
		/// or null if such entry cannot be found.
		/// </summary>
		/// <param name="entryName">The name of the connection string entry, or null to use
		/// the one that appears in the default 'connectionString' configuration entry, if any.</param>
		/// <returns>The requested settings, or null.</returns>
		public static ConnectionStringSettings Find(string entryName = null)
		{
			entryName = entryName.NullIfTrimmedIsEmpty();

			if (entryName == null)
			{
				var info = ORMConfiguration.GetInfo();
				if (info == null ||
					info.DataLink == null ||
					info.DataLink.ConnectionString == null) return null;

				entryName = info.DataLink.ConnectionString;
			}

			ConnectionStringSettings cn = null;
			try { cn = ConfigurationManager.ConnectionStrings[entryName]; }
			catch { }

			return cn;
		}
	}
}
