using Kerosene.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace Kerosene.ORM.Configuration
{
	// ==================================================== 
	/// <summary>
	/// The configuration handler for the Kerosene ORM framework.
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

	// ==================================================== 
	/// <summary>
	/// The entry that defines common options for data links.
	/// </summary>
	public class DataLinkElement : ConfigurationElement
	{
		/// <summary></summary>
		public const string ELEMENT_NAME = "dataLink";
		/// <summary></summary>
		public const string PROPERTY_CONNECTION_STRING = "connectionString";
		/// <summary></summary>
		public const string PROPERTY_RETRIES = "retries";
		/// <summary></summary>
		public const string PROPERTY_RETRY_INTERVAL = "retryInterval";

		/// <summary>
		/// The default connection string to use, if any.
		/// </summary>
		[ConfigurationProperty(PROPERTY_CONNECTION_STRING, IsRequired = false, DefaultValue = null)]
		public string ConnectionString
		{
			get { return ((string)this[PROPERTY_CONNECTION_STRING]).NullIfTrimmedIsEmpty(); }
			set { this[PROPERTY_CONNECTION_STRING] = value; }
		}

		/// <summary>
		/// The number of retries to open a link connection.
		/// </summary>
		[ConfigurationProperty(PROPERTY_RETRIES, IsRequired = false, DefaultValue = null)]
		public int? Retries
		{
			get { return (int?)this[PROPERTY_RETRIES]; }
			set { this[PROPERTY_RETRIES] = value; }
		}

		/// <summary>
		/// The milliseconds to wait between retries to open a link connection.
		/// </summary>
		[ConfigurationProperty(PROPERTY_RETRY_INTERVAL, IsRequired = false, DefaultValue = null)]
		public int? RetryInterval
		{
			get { return (int?)this[PROPERTY_RETRY_INTERVAL]; }
			set { this[PROPERTY_RETRY_INTERVAL] = value; }
		}
	}

	public partial class ORMConfiguration
	{
		/// <summary>
		/// Common options for data links.
		/// </summary>
		[ConfigurationProperty(DataLinkElement.ELEMENT_NAME, IsRequired = false, DefaultValue = null)]
		public DataLinkElement DataLink
		{
			get { return (DataLinkElement)base[DataLinkElement.ELEMENT_NAME]; }
		}
	}
}
