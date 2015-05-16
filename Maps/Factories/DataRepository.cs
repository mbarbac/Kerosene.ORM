using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;

namespace Kerosene.ORM.Maps
{
	// ====================================================
	/// <summary>
	/// Helpers and extensions for working with Repository instances.
	/// </summary>
	public static class DataRepository
	{
		/// <summary>
		/// Creates a new repository instance associated with the given link.
		/// </summary>
		/// <param name="link">The link reference.</param>
		/// <returns>The newly created repository.</returns>
		public static IDataRepository Create(IDataLink link)
		{
			return new Concrete.DataRepository(link);
		}
	}
}
