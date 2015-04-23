namespace Kerosene.ORM.Maps
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Linq;

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
	/// Represents the metadata the framework associates with its managed entities.
	/// </summary>
	public interface IMetaEntity
	{
		/// <summary>
		/// The actual entity this metadata is associated with, or null if it has been collected
		/// or it is not available for any reasons.
		/// </summary>
		object Entity { get; }

		/// <summary>
		/// The map that is managing this instance, or null if it is a detached one.
		/// </summary>
		IDataMap Map { get; }

		/// <summary>
		/// The state of the underlying entity.
		/// </summary>
		MetaState State { get; }
	}
}
