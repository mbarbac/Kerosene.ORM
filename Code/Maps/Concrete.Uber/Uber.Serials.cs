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
		/// The last serial number assigned to a meta entity.
		/// </summary>
		internal static ulong MetaEntityLastSerial = 0;

		/// <summary>
		/// The last serial number assigned to a map.
		/// </summary>
		internal static ulong DataMapLastSerial = 0;

		/// <summary>
		/// The last serial number assigned to a repository.
		/// </summary>
		internal static ulong RepositoryLastSerial = 0;
	}
}
