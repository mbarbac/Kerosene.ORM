using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Kerosene.ORM.Maps.Concrete
{
	// ====================================================
	internal static partial class Uber
	{
		/// <summary>
		/// Whether the entities' collector is enabled or not by default.
		/// </summary>
		internal const bool DEFAULT_ENABLE_COLLECTOR = true;

		/// <summary>
		/// Whether the entities' GC collector is enabled or not by default.
		/// </summary>
		internal const bool DEFAULT_ENABLE_COLLECTOR_CG = true;

		/// <summary>
		/// Whether the entities' collector is enabled or not.
		/// </summary>
		public static bool EnableCollector
		{
			get
			{
				while (!_EnableCollectorCaptured)
				{
					var info = Configuration.ORMConfiguration.GetInfo(); if (info == null) break;
					if (info.DataMap == null) break;
					if (info.DataMap.EnableCollector == null) break;

					_EnableCollector = (bool)info.DataMap.EnableCollector;
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
		static bool _EnableCollector = DEFAULT_ENABLE_COLLECTOR;
		static bool _EnableCollectorCaptured = false;

		/// <summary>
		/// Whether the entities' GC collector is enabled or not.
		/// </summary>
		public static bool EnableCollectorGC
		{
			get
			{
				while (!_EnableCollectorGCCaptured)
				{
					var info = Configuration.ORMConfiguration.GetInfo(); if (info == null) break;
					if (info.DataMap == null) break;
					if (info.DataMap.EnableCollectorGC == null) break;

					_EnableCollectorGC = (bool)info.DataMap.EnableCollectorGC;
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
		static bool _EnableCollectorGC = DEFAULT_ENABLE_COLLECTOR_CG;
		static bool _EnableCollectorGCCaptured = false;

		/// <summary>
		/// The default minimum interval at which the internal collector is fired.
		/// </summary>
		internal const int DEFAULT_COLLECTOR_MIN_INTERVAL = 1000;

		/// <summary>
		/// The default interval at which the internal collector is fired.
		/// </summary>
		internal const int DEFAULT_COLLECTOR_INTERVAL =
#if DEBUG
				5 * (1000); // seconds
#else
				5 * (60 * 1000); // minutes
#endif

		/// <summary>
		/// The default interval at which the internal collector is fired.
		/// </summary>
		public static int CollectorInterval
		{
			get
			{
				while (!_CollectorIntervalCaptured)
				{
					var info = Configuration.ORMConfiguration.GetInfo(); if (info == null) break;
					if (info.DataMap == null) break;
					if (info.DataMap.CollectorInterval == null) break;

					_CollectorInterval = (int)info.DataMap.CollectorInterval;
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
	}

	// ====================================================
	internal static partial class Uber
	{
		/// <summary>
		/// Whether by default weak maps are enabled.
		/// </summary>
		internal const bool DEFAULT_ENABLE_WEAK_MAPS = true;

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
					if (info.DataMap == null) break;
					if (info.DataMap.EnableWeakMaps == null) break;

					_EnableWeakMaps = (bool)info.DataMap.EnableWeakMaps;
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
	}

	// ==================================================== 
	internal static partial class Uber
	{
		/// <summary>
		/// Whether by default maps cache the entities they retrieve or not.
		/// </summary>
		internal const bool DEFAULT_TRACK_ENTITIES = true;

		/// <summary>
		/// Whether maps track the entities they managed or not.
		/// </summary>
		public static bool TrackEntities
		{
			get
			{
				while (!_TrackEntitiesCaptured)
				{
					var info = Configuration.ORMConfiguration.GetInfo(); if (info == null) break;
					if (info.DataMap == null) break;
					if (info.DataMap.TrackEntities == null) break;

					_TrackEntities = (bool)info.DataMap.TrackEntities;
					break;
				}

				_TrackEntitiesCaptured = true;
				return _TrackEntities;
			}
			set
			{
				_TrackEntitiesCaptured = true;
				_TrackEntities = value;
			}
		}
		static bool _TrackEntities = DEFAULT_TRACK_ENTITIES;
		static bool _TrackEntitiesCaptured = false;

		/// <summary>
		/// Whether by default maps cache the entities they retrieve or not.
		/// </summary>
		internal const bool DEFAULT_TRACK_CHILD_ENTITIES = true;

		/// <summary>
		/// Whether to track child entities for dependency properties or not.
		/// </summary>
		public static bool TrackChildEntities
		{
			get
			{
				while (!_TrackChildEntitiesCaptured)
				{
					var info = Configuration.ORMConfiguration.GetInfo(); if (info == null) break;
					if (info.DataMap == null) break;
					if (info.DataMap.TrackChildEntities == null) break;

					_TrackChildEntities = (bool)info.DataMap.TrackChildEntities;
					break;
				}

				_TrackChildEntitiesCaptured = true;
				return _TrackChildEntities;
			}
			set
			{
				_TrackChildEntitiesCaptured = true;
				TrackChildEntities = value;
			}
		}
		static bool _TrackChildEntities = DEFAULT_TRACK_CHILD_ENTITIES;
		static bool _TrackChildEntitiesCaptured = false;
	}
}
