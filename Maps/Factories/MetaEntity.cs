using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;

namespace Kerosene.ORM.Maps
{
	// ====================================================
	public static class MetaEntity
	{
		/// <summary>
		/// Returns the meta entity associated with the given object.
		/// </summary>
		/// <param name="entity">The meta entity to locate its meta entity.</param>
		/// <returns>The meta entity associated with the given entity.</returns>
		public static IMetaEntity Locate(object entity)
		{
			return Concrete.MetaEntity.Locate(entity);
		}
	}
}
