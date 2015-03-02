// ======================================================== IMetaEntity.cs
namespace Kerosene.ORM.Maps
{
	using Kerosene.Tools;
	using System;

	// ==================================================== 
	/// <summary>
	/// Indicates the current status a meta entity is.
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
		/// The underlying entity is associated with an insert operation.
		/// </summary>
		ToInsert,

		/// <summary>
		/// The underlying entity is associated with an update operation.
		/// </summary>
		ToUpdate,

		/// <summary>
		/// The underlying entity is associated with a delete operation.
		/// </summary>
		ToDelete,

		/// <summary>
		/// The underlying entity is considered ready.
		/// </summary>
		Ready
	}

	// ==================================================== 
	/// <summary>
	/// Represents the metadata associated with a given entity that permits it to be managed
	/// by the maps framework.
	/// </summary>
	public interface IMetaEntity
	{
		/// <summary>
		/// The actual entity this metadata instance is associated with, or null if that entity
		/// has been collected.
		/// </summary>
		object Entity { get; }

		/// <summary>
		/// The state of the underlying entity.
		/// </summary>
		MetaState State { get; }
	}
}
// ======================================================== 
