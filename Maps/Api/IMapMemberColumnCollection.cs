// ======================================================== IMapMemberColumnCollection.cs
namespace Kerosene.ORM.Maps
{
	using Kerosene.Tools;
	using System;
	using System.Collections.Generic;

	// ==================================================== 
	/// <summary>
	/// The collection of columns defined to support the mapping of its associated member.
	/// </summary>
	public interface IMapMemberColumnCollection : IEnumerable<IMapMemberColumn>
	{
		/// <summary>
		/// The member this instance is associated with.
		/// </summary>
		IMapMember Member { get; }

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
		IMapMemberColumn Add(Func<dynamic, object> name);

		/// <summary>
		/// Removes the given entry from this collection. Returns true if the member has been
		/// removed, or false otherwise.
		/// </summary>
		/// <param name="entry">The entry to remove.</param>
		/// <returns>True if the entry has been removed, or false otherwise.</returns>
		bool Remove(IMapMemberColumn entry);
	}

	// ==================================================== 
	/// <summary>
	/// The collection of columns defined to support the mapping of its associated member.
	/// </summary>
	public interface IMapMemberColumnCollection<T> : IMapMemberColumnCollection, IEnumerable<IMapMemberColumn<T>> where T : class
	{
		/// <summary>
		/// The member this instance is associated with.
		/// </summary>
		new IMapMember<T> Member { get; }

		/// <summary>
		/// Adds into this collection a new entry for the member whose name is specified.
		/// </summary>
		/// <param name="name">A dynamic lambda expression that resolves into the name of the
		/// column in the master table of the map.</param>
		/// <returns>The new entry added into this collection.</returns>
		new IMapMemberColumn<T> Add(Func<dynamic, object> name);

		/// <summary>
		/// Removes the given entry from this collection. Returns true if the member has been
		/// removed, or false otherwise.
		/// </summary>
		/// <param name="entry">The entry to remove.</param>
		/// <returns>True if the entry has been removed, or false otherwise.</returns>
		bool Remove(IMapMemberColumn<T> entry);
	}
}
// ======================================================== 
