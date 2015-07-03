using Kerosene.Tools;
using System;
using System.Configuration;

namespace Kerosene.ORM.Configuration
{
	// ====================================================
	/// <summary>
	/// The configuration handler for the Kerosene framework.
	/// </summary>
	public partial class ORMConfiguration : ConfigurationSection
	{
		/// <summary></summary>
		public const string SECTION_NAME = "keroseneORM";

		/// <summary>
		/// Returns an instance containing the information of the Kerosene ORM configuration
		/// section, or null if such section does not exist.
		/// </summary>
		/// <returns>An instance with the Kerosene ORM configuration section information, or null.</returns>
		public static ORMConfiguration GetInfo()
		{
			var info = ConfigurationManager.GetSection(SECTION_NAME) as ORMConfiguration;
			return info;
		}
	}
}
