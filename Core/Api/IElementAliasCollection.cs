// ======================================================== IElementAliasCollection.cs
namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;

	// ==================================================== 
	/// <summary>
	/// Represents the collection of aliases of elements used in a given context.
	/// </summary>
	public interface IElementAliasCollection
		: IDisposableEx, ICloneable, ISerializable, IEquivalent<IElementAliasCollection>
		, IEnumerable<IElementAlias>
	{
		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		new IElementAliasCollection Clone();

		/// <summary>
		/// Whether the names of the members of this collection are case sensitive or not.
		/// </summary>
		bool CaseSensitiveNames { get; }

		/// <summary>
		/// The number of members this instance contains.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Gets the member stored at the given position.
		/// </summary>
		/// <param name="index">The position at which the member to return is stored.</param>
		/// <returns>The member at the given position.</returns>
		IElementAlias this[int index] { get; }

		/// <summary>
		/// Returns the index at which the given member is stored, or -1 if it does not belong
		/// to this collection.
		/// </summary>
		/// <param name="member">The member whose index if to be found.</param>
		/// <returns>The index at which the given member is stored, or -1 if it does not belong
		/// to this collection.</returns>
		int IndexOf(IElementAlias member);

		/// <summary>
		/// Returns whether the given member belongs to this collection of not.
		/// </summary>
		/// <param name="member">The member to verify.</param>
		/// <returns>True if the given member belongs to this collection, false otherwise.</returns>
		bool Contains(IElementAlias member);

		/// <summary>
		/// Returns the first member in this instance that matches the conditions given in the
		/// predicate, or null if not such member can be found.
		/// </summary>
		/// <param name="match">The predicate that defines the conditions of the member to find.</param>
		/// <returns>The member found, or null.</returns>
		IElementAlias Find(Predicate<IElementAlias> match);

		/// <summary>
		/// Returns the collection of members in this instance that match the conditions given in
		/// the predicate. This collection might be empty if there were no members that match that
		/// conditions.
		/// </summary>
		/// <param name="match">The predicate that defines the conditions of the members to find.</param>
		/// <returns>A collection with the members found.</returns>
		IEnumerable<IElementAlias> FindAll(Predicate<IElementAlias> match);

		/// <summary>
		/// Gets the member whose alias is given, or null if not such member can be found.
		/// </summary>
		/// <param name="alias">The alias of the member to find.</param>
		/// <returns>The member found, or null.</returns>
		IElementAlias FindAlias(string alias);

		/// <summary>
		/// Gets an enumeration containing all the members where the given element is referenced.
		/// The enumeration might be empty is there were no members found.
		/// </summary>
		/// <param name="element">The string representation of the element to find.</param>
		/// <returns>The requested enumeration.</returns>
		IEnumerable<IElementAlias> FindElement(string element);

		/// <summary>
		/// Factory method invoked to create a new orphan member but with the right type for
		/// this collection.
		/// </summary>
		/// <returns>A new orphan member.</returns>
		IElementAlias CreateOrphanMember();

		/// <summary>
		/// Adds the given orphan instance into this collection.
		/// </summary>
		/// <param name="member">The orphan instance to add into this collection.</param>
		void Add(IElementAlias member);

		/// <summary>
		/// Creates and add into this collection a new member using the arguments given.
		/// </summary>
		/// <param name="element">The string representation of the element to be aliased, or null
		/// if it is the default one this context.</param>
		/// <param name="alias">The alias.</param>
		IElementAlias AddCreate(string element, string alias);

		/// <summary>
		/// Creates and adds into this collection a new member with a new alias for the default
		/// element in this context.
		/// </summary>
		/// <param name="alias">The alias.</param>
		IElementAlias AddCreate(string alias);

		/// <summary>
		/// Adds the given range of members into this collection, optionally cloning those that
		/// were not orphan ones.
		/// </summary>
		/// <param name="range">The range of members to add into this collection.</param>
		/// <param name="cloneNotOrphans">True to clone those member in the range that were
		/// not orphan ones, or false to throw an exception if such scenario ocurrs.</param>
		void AddRange(IEnumerable<IElementAlias> range, bool cloneNotOrphans = true);

		/// <summary>
		/// Removes the given parameter from this collection. Returns true if it has been removed
		/// succesfully, or false otherwise.
		/// </summary>
		/// <param name="member">The member to remove.</param>
		/// <returns>True if the member has been removed succesfully, or false otherwise.</returns>
		bool Remove(IElementAlias member);

		/// <summary>
		/// Clears this collection by removing all its members and optionally disposing them.
		/// </summary>
		/// <param name="disposeMembers">True to dispose the removed members, false to just
		/// remove them.</param>
		void Clear(bool disposeMembers = true);
	}

	// ==================================================== 
	/// <summary>
	/// Helpers and extensions for working with <see cref="IElementAliasCollection"/> instances.
	/// </summary>
	public static class ElementAliasCollection
	{
		/// <summary>
		/// Whether by default the names of the members in a collection are case sensitive or not.
		/// </summary>
		public const bool DEFAULT_CASE_SENSITIVE_NAMES = false;
	}
}
// ======================================================== 
