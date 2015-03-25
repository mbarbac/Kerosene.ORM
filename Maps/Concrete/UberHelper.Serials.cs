// ======================================================== UberHelper.Serials.cs
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
		/// The last serial number assigned to a repository.
		/// </summary>
		internal static ulong RepositoryLastSerial = 0;

		/// <summary>
		/// The last serial number assigned to a meta entity.
		/// </summary>
		internal static ulong MetaEntityLastSerial = 0;

		/// <summary>
		/// The last serial number assigned to a map.
		/// </summary>
		internal static ulong DataMapLastSerial = 0;
	}
}
// ======================================================== 
