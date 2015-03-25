// ======================================================== IMetaEntity.cs
namespace Kerosene.ORM.Maps
{
	using Kerosene.Tools;
	using System;

	// ==================================================== 
	/// <summary>
	/// Indicates the current state of the underlying entity associated with the metadata
	/// package.
	/// </summary>
	public enum MetaState
	{
		/// <summary>
		/// The underlying entity has been collected.
		/// </summary>
		Collected,

		/// <summary>
		/// The underlying entity is not associated with any map.
		/// </summary>
		Detached,

		/// <summary>
		/// The underlying entity is marked to be inserted into the database.
		/// </summary>
		ToInsert,

		/// <summary>
		/// The underlying entity is marked to be updated into the database.
		/// </summary>
		ToUpdate,

		/// <summary>
		/// The underlying entity is marked to be deleted from the database.
		/// </summary>
		ToDelete,

		/// <summary>
		/// The underlying entity is considered ready, as if it has been retrieved from, or
		/// persisted into the database.
		/// </summary>
		Ready
	}

	// ==================================================== 
	/// <summary>
	/// Represents the metadata that once associated with a given entity permits it to be
	/// managed by the maps framework. Only instances of class types are considered valid
	/// entities, any others are not supported.
	/// </summary>
	public interface IMetaEntity
	{
		/// <summary>
		/// The actual entity this metadata is associated with, or null if it is not available
		/// (for instance, it is not any longer in use and has been collected by the GC).
		/// </summary>
		object Entity { get; }

		/// <summary>
		/// The state of the underlying entity.
		/// </summary>
		MetaState State { get; }

		/// <summary>
		/// The map that is managing this instance, or null if it is a detached one.
		/// </summary>
		IDataMap Map { get; }
	}
}
// ======================================================== 
