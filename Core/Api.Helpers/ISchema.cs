// ======================================================== ISchema.cs
namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	// ==================================================== 
	/// <summary>
	/// Represents the schema that describes the metadata and structure of the records returned
	/// from the execution of an enumerable command.
	/// </summary>
	public interface ISchema
		: IDisposableEx, ICloneable, ISerializable, IEquivalent<ISchema>, IElementAliasProvider
		, IEnumerable<ISchemaEntry>
	{
		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		new ISchema Clone();

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
		ISchemaEntry this[int index] { get; }

		/// <summary>
		/// Returns the index at which the given member is stored, or -1 if it does not belong
		/// to this collection.
		/// </summary>
		/// <param name="member">The member whose index if to be found.</param>
		/// <returns>The index at which the given member is stored, or -1 if it does not belong
		/// to this collection.</returns>
		int IndexOf(ISchemaEntry member);

		/// <summary>
		/// Returns whether the given member is in this collection.
		/// </summary>
		/// <param name="member">The member to validate.</param>
		/// <returns>True if the given member is part of this collection, or false otherwise.</returns>
		bool Contains(ISchemaEntry member);

		/// <summary>
		/// Returns the first member in this instance that matches the conditions given in the
		/// predicate, or null if not such member can be found.
		/// </summary>
		/// <param name="match">The predicate that defines the conditions of the member to find.</param>
		/// <returns>The member found, or null.</returns>
		ISchemaEntry Find(Predicate<ISchemaEntry> match);

		/// <summary>
		/// Returns the collection of members in this instance that match the conditions given in
		/// the predicate. This collection might be empty if there were no members that match that
		/// conditions.
		/// </summary>
		/// <param name="match">The predicate that defines the conditions of the members to find.</param>
		/// <returns>A collection with the members found.</returns>
		IEnumerable<ISchemaEntry> FindAll(Predicate<ISchemaEntry> match);

		/// <summary>
		/// Gets the member whose table and column name are given, or null if not such member can
		/// be found.
		/// </summary>
		/// <param name="table">The table name of the member to find, or null to refer to the
		/// default one in this context.</param>
		/// <param name="column">The column name.</param>
		/// <returns>The member found, or null.</returns>
		ISchemaEntry FindEntry(string table, string column);

		/// <summary>
		/// Gets the unique member whose column name is given, or null if not such member can be
		/// found. If several members are found sharing the same column name then an exception is
		/// thrown.
		/// </summary>
		/// <param name="column">The column name.</param>
		/// <returns>The member found, or null.</returns>
		ISchemaEntry FindEntry(string column);

		/// <summary>
		/// Gets the member whose table and colum name are obtained parsing the given dynamic
		/// lambda expression, using either the 'x => x.Table.Column' or 'x => x.Column' forms,
		/// or null if no such member can be found. In the later case, if several members are
		/// found sharing the same column name for different tables then an exception is thrown.
		/// </summary>
		/// <param name="spec">A dynamic lambda expressin that resolves into the specification
		/// of the entry to find.</param>
		/// <returns></returns>
		ISchemaEntry FindEntry(Func<dynamic, object> spec);

		/// <summary>
		/// Gets the collection of entries where the given table name is used.
		/// </summary>
		/// <param name="table">The table name of the member to find, or null to refer to the
		/// default one in this context.</param>
		/// <returns>The requested collection of entries.</returns>
		IEnumerable<ISchemaEntry> FindTable(string table);

		/// <summary>
		/// Gets the collection of entries where the given column name is used.
		/// </summary>
		///	<param name="column">The column name.</param>
		/// <returns>The requested collection of entries.</returns>
		IEnumerable<ISchemaEntry> FindColumn(string column);

		/// <summary>
		/// Gets the collection of entries that refer to primary key columns.
		/// </summary>
		/// <returns>The requested collection.</returns>
		IEnumerable<ISchemaEntry> PrimaryKeyColumns();

		/// <summary>
		/// Gets the collection of entries that refer to unique valued columns.
		/// </summary>
		/// <returns>The requested collection.</returns>
		IEnumerable<ISchemaEntry> UniqueValuedColumns();

		/// <summary>
		/// Factory method invoked to create a new orphan member but with the right type for
		/// this collection.
		/// </summary>
		/// <returns>A new orphan member.</returns>
		ISchemaEntry CreateOrphanMember();

		/// <summary>
		/// Adds the given orphan instance into this collection.
		/// </summary>
		/// <param name="member">The orphan instance to add into this collection.</param>
		void Add(ISchemaEntry member);

		/// <summary>
		/// Creates and add into this collection a new member using the arguments given.
		/// </summary>
		/// <param name="table">The table name of the member to find, or null to refer to the
		/// default one in this context.</param>
		/// <param name="column">The column name.</param>
		ISchemaEntry AddCreate(string table, string column);

		/// <summary>
		/// Creates and add into this collection a new member with the given column name for the
		/// default table.
		/// </summary>
		/// <param name="column">The column name.</param>
		ISchemaEntry AddCreate(string column);

		/// <summary>
		/// Adds the given range of members into this collection, optionally cloning those that
		/// were not orphan ones.
		/// </summary>
		/// <param name="range">The range of members to add into this collection.</param>
		/// <param name="cloneNotOrphans">True to clone those member in the range that were
		/// not orphan ones, or false to throw an exception if such scenario ocurrs.</param>
		void AddRange(IEnumerable<ISchemaEntry> range, bool cloneNotOrphans = true);

		/// <summary>
		/// Removes the given parameter from this collection. Returns true if it has been removed
		/// succesfully, or false otherwise.
		/// </summary>
		/// <param name="member">The member to remove.</param>
		/// <returns>True if the member has been removed succesfully, or false otherwise.</returns>
		bool Remove(ISchemaEntry member);

		/// <summary>
		/// Clears this collection by removing all its members and optionally disposing them.
		/// </summary>
		/// <param name="disposeMembers">True to dispose the removed members, false to just
		/// remove them.</param>
		void Clear(bool disposeMembers = true);
	}

	// ==================================================== 
	/// <summary>
	/// Helpers and extensions for working with <see cref="ISchema"/> instances.
	/// </summary>
	public static class Schema
	{
		/// <summary>
		/// Whether by default the table and column names of the members in a schema are case
		/// sensitive or not.
		/// </summary>
		public const bool DEFAULT_CASE_SENSITIVE_NAMES = false;
	}
}
// ======================================================== 
