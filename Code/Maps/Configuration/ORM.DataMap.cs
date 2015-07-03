using Kerosene.Tools;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace Kerosene.ORM.Configuration
{
	// =====================================================
	/// <summary>
	/// The entry related to map options.
	/// </summary>
	public class DataMapElement : ConfigurationElement
	{
		/// <summary></summary>
		public const string ELEMENT_NAME = "dataMap";

		/// <summary></summary>
		public const string PROPERTY_ENABLE_COLLECTOR = "enableCollector";

		/// <summary></summary>
		public const string PROPERTY_ENABLE_COLLECTOR_GC = "enableCollectorGC";
		
		/// <summary></summary>
		public const string PROPERTY_COLLECTOR_INTERVAL = "collectorInterval";

		/// <summary></summary>
		public const string PROPERTY_ENABLE_WEAKMAPS = "enableWeakMaps";

		/// <summary></summary>
		public const string PROPERTY_TRACK_ENTITIES = "trackEntities";

		/// <summary></summary>
		public const string PROPERTY_TRACK_CHILD_ENTITIES = "trackChildEntities";

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
		/// Whether to invoke a CLR's GC each time the internal entities' collector is fired.
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

		/// <summary>
		/// Whether to enable weak maps or not. Weak maps are automatically created when a type
		/// is used in a map operation and there was not a map registered for it.
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
		/// Whether entities keep track or their child dependencies or not.
		/// </summary>
		[ConfigurationProperty(PROPERTY_TRACK_CHILD_ENTITIES, IsRequired = false, DefaultValue = null)]
		public bool? TrackChildEntities
		{
			get { return (bool?)this[PROPERTY_TRACK_CHILD_ENTITIES]; }
			set { this[PROPERTY_TRACK_CHILD_ENTITIES] = value; }
		}
	}

	// =====================================================
	public partial class ORMConfiguration
	{
		/// <summary>
		/// Common options for data maps.
		/// </summary>
		[ConfigurationProperty(DataMapElement.ELEMENT_NAME, IsRequired = false, DefaultValue = null)]
		public DataMapElement DataMap
		{
			get { return (DataMapElement)base[DataMapElement.ELEMENT_NAME]; }
		}
	}
}
