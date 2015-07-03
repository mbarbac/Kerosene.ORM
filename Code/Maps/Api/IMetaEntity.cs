using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;

namespace Kerosene.ORM.Maps
{
	// ====================================================
	/// <summary>
	/// Represents the metadata associated with an entity that can become a managed one.
	/// </summary>
	public interface IMetaEntity
	{
		/// <summary>
		/// The actual entity this metadata is associated with, or null if it has been collected
		/// or if it is invalid.
		/// </summary>
		object Entity { get; }

		/// <summary>
		/// The map that is currently managing this instance, if any.
		/// </summary>
		IDataMap Map { get; }
	}

	// ====================================================
	/// <summary>
	/// Helpers and extensions for working with IMetaEntity instances.
	/// </summary>
	public static class MetaEntity
	{
		/// <summary>
		/// Returns the meta entity associated with the given object.
		/// </summary>
		/// <param name="entity">The entity to locate its meta entity from.</param>
		/// <returns>The meta entity associated with the given entity.</returns>
		public static IMetaEntity Locate(object entity)
		{
			return Concrete.MetaEntity.Locate(entity);
		}
	}
}
