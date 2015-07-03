using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;
using System.Collections.Generic;

namespace Kerosene.ORM.Maps
{
	// ====================================================
	/// <summary>
	/// Represents the collection of columns in the database associated with a map.
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
		/// Adds into this collection a new entry whose name is specified or, optionally, returns
		/// the existing column for that name.
		/// </summary>
		/// <param name="name">A dynamic lambda expression that resolves into the name of the
		/// entry to add.</param>
		/// <param name="raise">If true and the name matches with the name of an existing column
		/// then throw an exception. False to return the existing column.</param>
		/// <returns>The entry added into or found in this collection.</returns>
		IMapColumn Add(Func<dynamic, object> name, bool raise = true);

		/// <summary>
		/// Removes the given entry from this collection. Returns true if the member has been
		/// removed, or false otherwise.
		/// </summary>
		/// <param name="entry">The entry to remove.</param>
		/// <returns>True if the entry has been removed, or false otherwise.</returns>
		bool Remove(IMapColumn entry);

		/// <summary>
		/// Clears and disposes all the entries in this collection.
		/// </summary>
		void Clear();
	}

	// ====================================================
	/// <summary>
	/// Represents the collection of columns in the database associated with a map.
	/// </summary>
	public interface IMapColumnCollection<T> : IMapColumnCollection, IEnumerable<IMapColumn<T>> where T : class
	{
		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		new IDataMap<T> Map { get; }

		/// <summary>
		/// Adds into this collection a new entry whose name is specified or, optionally, returns
		/// the existing column for that name.
		/// </summary>
		/// <param name="name">A dynamic lambda expression that resolves into the name of the
		/// entry to add.</param>
		/// <param name="raise">If true and the name matches with the name of an existing column
		/// then throw an exception. False to return the existing column.</param>
		/// <returns>The entry added into or found in this collection.</returns>
		new IMapColumn<T> Add(Func<dynamic, object> name, bool raise = true);

		/// <summary>
		/// Removes the given entry from this collection. Returns true if the member has been
		/// removed, or false otherwise.
		/// </summary>
		/// <param name="entry">The entry to remove.</param>
		/// <returns>True if the entry has been removed, or false otherwise.</returns>
		bool Remove(IMapColumn<T> entry);
	}
}
