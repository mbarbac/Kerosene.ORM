// ======================================================== IMapColumnCollection.cs
namespace Kerosene.ORM.Maps
{
	using Kerosene.Tools;
	using System;
	using System.Collections.Generic;

	// ==================================================== 
	/// <summary>
	/// Represents the collection of columns in the primary table of the database the associated
	/// map is taken into consideration for its operations.
	/// </summary>
	public interface IMapColumnCollection : IEnumerable<IMapColumn>
	{
		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		IDataMap Map { get; }

		/// <summary>
		/// The number of entries in this collection.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Adds into this collection a new entry for the member whose name is specified.
		/// </summary>
		/// <param name="name">A dynamic lambda expression that resolves into the name of the
		/// column in the master table of the map.</param>
		/// <returns>The new entry added into this collection.</returns>
		IMapColumn Add(Func<dynamic, object> name);

		/// <summary>
		/// Removes the given entry from this collection. Returns true if the member has been
		/// removed, or false otherwise.
		/// </summary>
		/// <param name="entry">The entry to remove.</param>
		/// <returns>True if the entry has been removed, or false otherwise.</returns>
		bool Remove(IMapColumn entry);
	}

	// ==================================================== 
	/// <summary>
	/// Represents the collection of columns in the primary table of the database the associated
	/// map is taken into consideration for its operations.
	/// </summary>
	public interface IMapColumnCollection<T> : IEnumerable<IMapColumn<T>>, IMapColumnCollection where T : class
	{
		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		new IDataMap<T> Map { get; }

		/// <summary>
		/// Adds into this collection a new entry for the member whose name is specified.
		/// </summary>
		/// <param name="name">A dynamic lambda expression that resolves into the name of the
		/// column in the master table of the map.</param>
		/// <returns>The new entry added into this collection.</returns>
		new IMapColumn<T> Add(Func<dynamic, object> name);

		/// <summary>
		/// Removes the given entry from this collection. Returns true if the member has been
		/// removed, or false otherwise.
		/// </summary>
		/// <param name="entry">The entry to remove.</param>
		/// <returns>True if the entry has been removed, or false otherwise.</returns>
		bool Remove(IMapColumn<T> entry);
	}
}
// ======================================================== 
