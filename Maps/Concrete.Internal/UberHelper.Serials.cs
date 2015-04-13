// ======================================================== UberHelper.Serials.cs
namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
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
