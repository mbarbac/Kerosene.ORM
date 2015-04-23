namespace Kerosene.ORM.Configuration
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// The configuration handler for the Kerosene ORM framework.
	/// </summary>
	public partial class ORMConfiguration : ConfigurationSection
	{
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
	/// The entry that defines common options for data engines.
	/// </summary>
	public class DataEngineElement : ConfigurationElement
	{
		public const string ELEMENT_NAME = "dataEngine";
		public const string PROPERTY_RELAX_TRANSFORMERS = "relaxTransformers";
		public const string PROPERTY_COMPLEX_TAGS = "complexTags";

		/// <summary>
		/// Whether to relax data link transformers.
		/// </summary>
		[ConfigurationProperty(PROPERTY_RELAX_TRANSFORMERS, IsRequired = false, DefaultValue = null)]
		public bool? RelaxTransformers
		{
			get { return (bool?)this[PROPERTY_RELAX_TRANSFORMERS]; }
			set { this[PROPERTY_RELAX_TRANSFORMERS] = value; }
		}

		/// <summary>
		/// Whether to keep complex tags or not.
		/// </summary>
		[ConfigurationProperty(PROPERTY_COMPLEX_TAGS, IsRequired = false, DefaultValue = null)]
		public bool? ComplexTags
		{
			get { return (bool?)this[PROPERTY_COMPLEX_TAGS]; }
			set { this[PROPERTY_COMPLEX_TAGS] = value; }
		}
	}

	public partial class ORMConfiguration
	{
		/// <summary>
		/// Common options for data engines.
		/// </summary>
		[ConfigurationProperty(DataEngineElement.ELEMENT_NAME, IsRequired = false, DefaultValue = null)]
		public DataEngineElement DataEngine
		{
			get { return (DataEngineElement)base[DataEngineElement.ELEMENT_NAME]; }
		}
	}

	// ==================================================== 
	/// <summary>
	/// The entry that defines common options for data links.
	/// </summary>
	public class DataLinkElement : ConfigurationElement
	{
		public const string ELEMENT_NAME = "dataLink";
		public const string PROPERTY_CONNECTION_STRING = "connectionString";
		public const string PROPERTY_RETRIES = "retries";
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

	// ==================================================== 
	/// <summary>
	/// The collection of custome engines defined.
	/// </summary>
	public class CustomEngineCollection : ConfigurationElementCollection
	{
		public const string COLLECTION_NAME = "customEngines";

		protected override ConfigurationElement CreateNewElement()
		{
			return new CustomEngineElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((CustomEngineElement)element).Id;
		}

		/// <summary>
		/// The collection of custom engines read from the configuration files.
		/// </summary>
		public IEnumerable<CustomEngineElement> Items
		{
			get
			{
				foreach (CustomEngineElement entry in this) yield return entry;
			}
		}
	}

	public class CustomEngineElement : ConfigurationElement
	{
		public const string PROPERTY_ID = "id";
		public const string PROPERTY_TYPENAME = "type";
		public const string PROPERTY_ASSEMBLYNAME = "assembly";
		public const string PROPERTY_INVARIANTNAME = "invariantName";
		public const string PROPERTY_SERVERVERSION = "serverVersion";
		public const string PROPERTY_CASESENSITIVENAMES = "caseSensitiveNames";
		public const string PROPERTY_PARAMETERPREFIX = "parameterPrefix";
		public const string PROPERTY_POSITIONALPARAMETERS = "positionalParameters";
		public const string PROPERTY_SUPPORTSNATIVESKIPTAKE = "supportsNativeSkipTake";

		/// <summary>
		/// The unique id of this engine in the configuration section.
		/// </summary>
		[ConfigurationProperty(PROPERTY_ID, IsRequired = true)]
		public string Id
		{
			get { return ((string)this[PROPERTY_ID]).NullIfTrimmedIsEmpty(); }
			set { this[PROPERTY_ID] = value; }
		}

		/// <summary>
		/// The class type of the engine.
		/// </summary>
		[ConfigurationProperty(PROPERTY_TYPENAME, IsRequired = true)]
		public string TypeName
		{
			get { return ((string)this[PROPERTY_TYPENAME]).NullIfTrimmedIsEmpty(); }
			set { this[PROPERTY_TYPENAME] = value; }
		}

		/// <summary>
		/// The assembly name where to find the type of the engine.
		/// </summary>
		[ConfigurationProperty(PROPERTY_ASSEMBLYNAME, IsRequired = true)]
		public string AssemblyName
		{
			get { return ((string)this[PROPERTY_ASSEMBLYNAME]).NullIfTrimmedIsEmpty(); }
			set { this[PROPERTY_ASSEMBLYNAME] = value; }
		}

		/// <summary>
		/// If not null, overrides the invariant name by which the engine is to be registered.
		/// </summary>
		[ConfigurationProperty(PROPERTY_INVARIANTNAME, IsRequired = false, DefaultValue = null)]
		public string InvariantName
		{
			get { return ((string)this[PROPERTY_INVARIANTNAME]).NullIfTrimmedIsEmpty(); }
			set { this[PROPERTY_INVARIANTNAME] = value; }
		}

		/// <summary>
		/// If not null, overrides the server version this engine supports, using the semantic
		/// versioning format.
		/// </summary>
		/// <summary>
		/// If not null, overrides the invariant name by which the engine is to be registered.
		/// </summary>
		[ConfigurationProperty(PROPERTY_SERVERVERSION, IsRequired = false, DefaultValue = null)]
		public string ServerVersion
		{
			get { return ((string)this[PROPERTY_SERVERVERSION]).NullIfTrimmedIsEmpty(); }
			set { this[PROPERTY_SERVERVERSION] = value; }
		}

		/// <summary>
		/// If not null, overrides whether the identifiers in the database, as table and column
		/// names, are case sensitive or not.
		/// </summary>
		[ConfigurationProperty(PROPERTY_CASESENSITIVENAMES, IsRequired = false, DefaultValue = null)]
		public bool? CaseSensitiveNames
		{
			get { return (bool?)this[PROPERTY_CASESENSITIVENAMES]; }
			set { this[PROPERTY_CASESENSITIVENAMES] = value; }
		}

		/// <summary>
		/// If not null, overrides the default prefix the database engine uses for the names of the
		/// parameters of a command, or null if no default value is used.
		/// </summary>
		[ConfigurationProperty(PROPERTY_PARAMETERPREFIX, IsRequired = false, DefaultValue = null)]
		public string ParameterPrefix
		{
			get { return ((string)this[PROPERTY_PARAMETERPREFIX]).NullIfTrimmedIsEmpty(); }
			set { this[PROPERTY_PARAMETERPREFIX] = value; }
		}

		/// <summary>
		/// If not null, overrides whether the database engine treats the parameters of a command
		/// by position instead of by name.
		/// </summary>
		[ConfigurationProperty(PROPERTY_POSITIONALPARAMETERS, IsRequired = false, DefaultValue = null)]
		public bool? PositionalParameters
		{
			get { return (bool?)this[PROPERTY_POSITIONALPARAMETERS]; }
			set { this[PROPERTY_POSITIONALPARAMETERS] = value; }
		}

		/// <summary>
		/// If not null, overrides whether the database engine provides a normalized syntax to
		/// implement the skip/take functionality, or rather it has to be emulated by software.
		/// </summary>
		[ConfigurationProperty(PROPERTY_SUPPORTSNATIVESKIPTAKE, IsRequired = false, DefaultValue = null)]
		public bool? SupportsNativeSkipTake
		{
			get { return (bool?)this[PROPERTY_SUPPORTSNATIVESKIPTAKE]; }
			set { this[PROPERTY_SUPPORTSNATIVESKIPTAKE] = value; }
		}
	}

	public partial class ORMConfiguration
	{
		/// <summary>
		/// The collection of custom engines that appears in the Kerosene ORM configuration
		/// section, or null.
		/// </summary>
		[ConfigurationProperty(CustomEngineCollection.COLLECTION_NAME, IsRequired = false, DefaultValue = null)]
		[ConfigurationCollection(typeof(CustomEngineCollection), AddItemName = "add")]
		public CustomEngineCollection CustomEngines
		{
			get { return (CustomEngineCollection)base[CustomEngineCollection.COLLECTION_NAME]; }
		}
	}

	// ==================================================== 
	/// <summary>
	/// The entry that defines common options for data engines.
	/// </summary>
	public class DataMapElement : ConfigurationElement
	{
		public const string ELEMENT_NAME = "dataMap";
		public const string PROPERTY_ENABLE_WEAKMAPS = "enableWeakMaps";
		public const string PROPERTY_TRACK_ENTITIES = "trackEntities";
		public const string PROPERTY_TRACK_CHILD_ENTITIES = "trackChildEntities";
		public const string PROPERTY_ENABLE_COLLECTOR = "enableCollector";
		public const string PROPERTY_ENABLE_COLLECTOR_GC = "enableCollectorGC";
		public const string PROPERTY_COLLECTOR_INTERVAL = "collectorInterval";

		/// <summary>
		/// Whether to relax data link transformers.
		/// </summary>
		[ConfigurationProperty(PROPERTY_ENABLE_WEAKMAPS, IsRequired = false, DefaultValue = null)]
		public bool? EnableWeakMaps
		{
			get { return (bool?)this[PROPERTY_ENABLE_WEAKMAPS]; }
			set { this[PROPERTY_ENABLE_WEAKMAPS] = value; }
		}

		/// <summary>
		/// Whether maps track the entities they managed or not.
		/// </summary>
		[ConfigurationProperty(PROPERTY_TRACK_ENTITIES, IsRequired = false, DefaultValue = null)]
		public bool? TrackEntities
		{
			get { return (bool?)this[PROPERTY_TRACK_ENTITIES]; }
			set { this[PROPERTY_TRACK_ENTITIES] = value; }
		}

		/// <summary>
		/// Whether entities keep track of their own child ones in order to cascade change
		/// operations, or not.
		/// </summary>
		[ConfigurationProperty(PROPERTY_TRACK_CHILD_ENTITIES, IsRequired = false, DefaultValue = null)]
		public bool? TrackChildEntities
		{
			get { return (bool?)this[PROPERTY_TRACK_CHILD_ENTITIES]; }
			set { this[PROPERTY_TRACK_CHILD_ENTITIES] = value; }
		}

		/// <summary>
		/// Whether to enable the internal entities' collector.
		/// </summary>
		[ConfigurationProperty(PROPERTY_ENABLE_COLLECTOR, IsRequired = false, DefaultValue = true)]
		public bool? EnableCollector
		{
			get { return (bool?)this[PROPERTY_ENABLE_COLLECTOR]; }
			set { this[PROPERTY_ENABLE_COLLECTOR] = value; }
		}

		/// <summary>
		/// Whether to enable the GC procedure each time the internal entities' collector is fired.
		/// </summary>
		[ConfigurationProperty(PROPERTY_ENABLE_COLLECTOR_GC, IsRequired = false, DefaultValue = true)]
		public bool? EnableCollectorGC
		{
			get { return (bool?)this[PROPERTY_ENABLE_COLLECTOR_GC]; }
			set { this[PROPERTY_ENABLE_COLLECTOR_GC] = value; }
		}

		/// <summary>
		/// The interval at which the internal collector is fired.
		/// </summary>
		[ConfigurationProperty(PROPERTY_COLLECTOR_INTERVAL, IsRequired = false, DefaultValue = null)]
		public int? CollectorInterval
		{
			get { return (int?)this[PROPERTY_COLLECTOR_INTERVAL]; }
			set { this[PROPERTY_COLLECTOR_INTERVAL] = value; }
		}
	}

	public partial class ORMConfiguration
	{
		/// <summary>
		/// Common options for data engines.
		/// </summary>
		[ConfigurationProperty(DataMapElement.ELEMENT_NAME, IsRequired = false, DefaultValue = null)]
		public DataMapElement DataMap
		{
			get { return (DataMapElement)base[DataMapElement.ELEMENT_NAME]; }
		}
	}
}
