namespace Kerosene.ORM.Maps
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Helpers and extensions for working with <see cref="IMetaEntity"/> instances.
	/// </summary>
	public static partial class MetaEntity
	{
		/// <summary>
		/// Returns the meta entity that carries the metadata associated with the given entity.
		/// </summary>
		/// <typeparam name="T">The type of the entity.</typeparam>
		/// <param name="entity">The entity to obtain its meta entity from.</param>
		/// <returns>The requested meta entity.</returns>
		public static IMetaEntity Locate<T>(T entity) where T : class
		{
			return Concrete.MetaEntity.Locate(entity, create: true);
		}
	}
}
