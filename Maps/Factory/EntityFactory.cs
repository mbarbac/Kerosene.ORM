// ======================================================== EntityFactory.cs
namespace Kerosene.ORM.Maps
{
	using Kerosene.Tools;
	using System;

	// ==================================================== 
	/// <summary>
	/// Factory class to locate <see cref="IMetaEntity"/> instances.
	/// </summary>
	public static class EntityFactory
	{
		/// <summary>
		/// Returns the meta entity that carries the metadata associated with the given entity.
		/// </summary>
		/// <typeparam name="T">The type of the entity.</typeparam>
		/// <param name="entity">The entity to obtain its meta entity from.</param>
		/// <returns>The requested meta entity.</returns>
		public static Concrete.MetaEntity Locate<T>(T entity) where T : class
		{
			return Concrete.MetaEntity.Locate(entity, create: true);
		}
	}
}
// ======================================================== 
