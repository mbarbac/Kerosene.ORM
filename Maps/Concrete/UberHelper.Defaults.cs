// ======================================================== UberHelper.Defaults.cs
namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Internal helpers and extensions.
	/// </summary>
	internal static partial class UberHelper
	{
		/// <summary>
		/// Whether to track child entities.
		/// </summary>
		public static bool TrackChildEntities
		{
			get
			{
				while (!_TrackChildEntitiesCaptured)
				{
					var info = Configuration.ORMConfiguration.GetInfo(); if (info == null) break;
					if (info.Maps == null) break;
					if (info.Maps.TrackChildEntities == null) break;

					_TrackChildEntities = (bool)info.Maps.TrackChildEntities;
					break;
				}

				_TrackChildEntitiesCaptured = true;
				return _TrackChildEntities;
			}
			set
			{
				_TrackChildEntitiesCaptured = true;
				_TrackChildEntities = value;
			}
		}
		static bool _TrackChildEntities = DEFAULT_TRACK_CHILD_ENTITIES;
		static bool _TrackChildEntitiesCaptured = false;

		/// <summary>
		/// Whether to weak maps are enabled or not.
		/// </summary>
		public static bool EnableWeakMaps
		{
			get
			{
				while (!_EnableWeakMapsCaptured)
				{
					var info = Configuration.ORMConfiguration.GetInfo(); if (info == null) break;
					if (info.Maps == null) break;
					if (info.Maps.EnableWeakMaps == null) break;

					_EnableWeakMaps = (bool)info.Maps.EnableWeakMaps;
					break;
				}

				_EnableWeakMapsCaptured = true;
				return _EnableWeakMaps;
			}
			set
			{
				_EnableWeakMapsCaptured = true;
				_EnableWeakMaps = value;
			}
		}
		static bool _EnableWeakMaps = DEFAULT_ENABLE_WEAK_MAPS;
		static bool _EnableWeakMapsCaptured = false;

		/// <summary>
		/// Whether to enable the internal entities' collector.
		/// </summary>
		public static bool EnableCollector
		{
			get
			{
				while (!_EnableCollectorCaptured)
				{
					var info = Configuration.ORMConfiguration.GetInfo(); if (info == null) break;
					if (info.Maps == null) break;
					if (info.Maps.EnableCollector == null) break;

					_EnableCollector = (bool)info.Maps.EnableCollector;
					break;
				}

				_EnableCollectorCaptured = true;
				return _EnableCollector;
			}
			set
			{
				_EnableCollectorCaptured = true;
				_EnableCollector = value;
			}
		}
		static bool _EnableCollector = true;
		static bool _EnableCollectorCaptured = false;

		/// <summary>
		/// Whether to enable the GC procedure each time the internal entities' collector is fired.
		/// </summary>
		public static bool EnableCollectorGC
		{
			get
			{
				while (!_EnableCollectorGCCaptured)
				{
					var info = Configuration.ORMConfiguration.GetInfo(); if (info == null) break;
					if (info.Maps == null) break;
					if (info.Maps.EnableCollectorGC == null) break;

					_EnableCollectorGC = (bool)info.Maps.EnableCollectorGC;
					break;
				}

				_EnableCollectorGCCaptured = true;
				return _EnableCollectorGC;
			}
			set
			{
				_EnableCollectorGCCaptured = true;
				_EnableCollectorGC = value;
			}
		}
		static bool _EnableCollectorGC = true;
		static bool _EnableCollectorGCCaptured = false;

		/// <summary>
		/// The interval at which the internal collector is fired.
		/// </summary>
		public static int CollectorInterval
		{
			get
			{
				while (!_CollectorIntervalCaptured)
				{
					var info = Configuration.ORMConfiguration.GetInfo(); if (info == null) break;
					if (info.Maps == null) break;
					if (info.Maps.CollectorInterval == null) break;

					_CollectorInterval = (int)info.Maps.CollectorInterval;
					break;
				}

				_CollectorIntervalCaptured = true;
				return _CollectorInterval;
			}
			set
			{
				_CollectorIntervalCaptured = true;
				_CollectorInterval = value;
			}
		}
		static int _CollectorInterval = DEFAULT_COLLECTOR_INTERVAL;
		static bool _CollectorIntervalCaptured = false;

		/// <summary>
		/// The minimum interval at which the internal collector is fired.
		/// </summary>
		public static int CollectorMinInterval
		{
			get
			{
				while (!_CollectorMinIntervalCaptured)
				{
					var info = Configuration.ORMConfiguration.GetInfo(); if (info == null) break;
					if (info.Maps == null) break;
					if (info.Maps.CollectorMinInterval == null) break;

					CollectorMinInterval = (int)info.Maps.CollectorMinInterval;
					break;
				}

				_CollectorMinIntervalCaptured = true;
				return _CollectorMinInterval;
			}
			set
			{
				_CollectorMinIntervalCaptured = true;
				_CollectorMinInterval = value;
			}
		}
		static int _CollectorMinInterval = DEFAULT_COLLECTOR_MIN_INTERVAL;
		static bool _CollectorMinIntervalCaptured = false;
	}


	// ==================================================== 
	/// <summary>
	/// Internal helpers and extensions.
	/// </summary>
	internal static partial class UberHelper
	{
		/// <summary>
		/// Whether by default child entities are tracked.
		/// </summary>
		internal const bool DEFAULT_TRACK_CHILD_ENTITIES = true;

		/// <summary>
		/// Whether by default weak maps are enabled.
		/// </summary>
		internal const bool DEFAULT_ENABLE_WEAK_MAPS = true;

#if DEBUG
		/// <summary>
		/// The default interval at which the internal collector is fired.
		/// </summary>
		internal const int DEFAULT_COLLECTOR_INTERVAL = 1000 * (5); // seconds

		/// <summary>
		/// The minimum interval at which the internal collector can be fired.
		/// </summary>
		internal const int DEFAULT_COLLECTOR_MIN_INTERVAL = 500;

		/// <summary>
		/// Whether a CLR garbage collector is requested when the internal collector is fired.
		/// </summary>
		internal const bool DEFAULT_COLLECTOR_GC_ENABLED = true;
#else
		/// <summary>
		/// The default interval at which the internal collector is fired.
		/// </summary>
		internal const int DEFAULT_COLLECTOR_INTERVAL = 1000 * (60 * 5); // minutes

		/// <summary>
		/// The minimum interval at which the internal collector can be fired.
		/// </summary>
		internal const int DEFAULT_COLLECTOR_MIN_INTERVAL = 500;

		/// <summary>
		/// Whether a CLR garbage collector is requested when the internal collector is fired.
		/// </summary>
		internal const bool DEFAULT_COLLECTOR_GC_ENABLED = false;
#endif
	}
}
// ======================================================== 
