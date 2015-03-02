// ======================================================== UberHelper.Defaults.cs
namespace Kerosene.ORM.Maps.Concrete
{
	using System;

	// ==================================================== 
	/// <summary>
	/// Internal helpers and extensions.
	/// </summary>
	internal static partial class UberHelper
	{
		/// <summary>
		/// Whether by default weak maps are enabled.
		/// </summary>
		internal const bool DEFAULT_ENABLE_WEAK_MAPS = true;

#if DEBUG
		/// <summary>
		/// The default interval at which the internal collector is fired.
		/// </summary>
		internal const int DEFAULT_COLLECTOR_INTERVAL = 1000 * (10); // seconds

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
