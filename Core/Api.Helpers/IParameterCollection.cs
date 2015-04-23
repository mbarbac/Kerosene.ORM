namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	// ==================================================== 
	/// <summary>
	/// Represents the collection of parameters of a command.
	/// </summary>
	public interface IParameterCollection
		: IDisposableEx, ICloneable, ISerializable, IEquivalent<IParameterCollection>
		, IEnumerable<IParameter>
	{
		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		new IParameterCollection Clone();

		/// <summary>
		/// Whether the names of the members of this collection are case sensitive or not.
		/// </summary>
		bool CaseSensitiveNames { get; }

		/// <summary>
		/// The default prefix to use to automatically create the name of a new parameter if it
		/// was added into this collection using only its value.
		/// </summary>
		string Prefix { get; set; }

		/// <summary>
		/// The number of members this instance contains.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Gets the member stored at the given position.
		/// </summary>
		/// <param name="index">The position at which the member to return is stored.</param>
		/// <returns>The member at the given position.</returns>
		IParameter this[int index] { get; }

		/// <summary>
		/// Returns the index at which the given member is stored, or -1 if it does not belong
		/// to this collection.
		/// </summary>
		/// <param name="member">The member whose index if to be found.</param>
		/// <returns>The index at which the given member is stored, or -1 if it does not belong
		/// to this collection.</returns>
		int IndexOf(IParameter member);

		/// <summary>
		/// Returns whether the given member is in this collection.
		/// </summary>
		/// <param name="member">The member to validate.</param>
		/// <returns>True if the given member is part of this collection, or false otherwise.</returns>
		bool Contains(IParameter member);

		/// <summary>
		/// Gets the member whose name is given, or null if not such member can be found.
		/// </summary>
		/// <param name="name">The name of the member to find.</param>
		/// <returns>The member found, or null.</returns>
		IParameter FindName(string name);

		/// <summary>
		/// Adds the given orphan instance into this collection.
		/// </summary>
		/// <param name="member">The orphan instance to add into this collection.</param>
		void Add(IParameter member);

		/// <summary>
		/// Creates and add into this collection a new member using the arguments given.
		/// </summary>
		/// <param name="name">The name of the new member to add.</param>
		/// <param name="value">The value the new member will hold.</param>
		IParameter AddCreate(string name, object value);

		/// <summary>
		/// Creates and adds into this collection a new member to hold the given value, whose
		/// name is automatically built using the default prefix of this collection plus an
		/// ordinal number.
		/// </summary>
		/// <param name="value">The value the new member will hold.</param>
		IParameter AddCreate(object value);

		/// <summary>
		/// Adds the given range of members into this collection, optionally cloning those that
		/// were not orphan ones.
		/// </summary>
		/// <param name="range">The range of members to add into this collection.</param>
		/// <param name="cloneNotOrphans">True to clone those member in the range that were
		/// not orphan ones, or false to throw an exception if such scenario ocurrs.</param>
		void AddRange(IEnumerable<IParameter> range, bool cloneNotOrphans = true);

		/// <summary>
		/// Removes the given parameter from this collection. Returns true if it has been removed
		/// succesfully, or false otherwise.
		/// </summary>
		/// <param name="member">The member to remove.</param>
		/// <returns>True if the member has been removed succesfully, or false otherwise.</returns>
		bool Remove(IParameter member);

		/// <summary>
		/// Clears this collection by removing all its members and optionally disposing them.
		/// </summary>
		/// <param name="disposeMembers">True to dispose the removed members, false to just
		/// remove them.</param>
		void Clear(bool disposeMembers = true);
	}

	// ==================================================== 
	/// <summary>
	/// Helpers and extensions for working with <see cref="IParameterCollection"/> instances.
	/// </summary>
	public static class ParameterCollection
	{
		/// <summary>
		/// Whether by default the names of the members in a collection are case sensitive or not.
		/// </summary>
		public const bool DEFAULT_CASE_SENSITIVE_NAMES = DataEngine.DEFAULT_CASESENSITIVE_NAMES;

		/// <summary>
		/// The default prefix to use o automatically create the name of a new parameter if it was
		/// added into a parameter collection using only its value.
		/// </summary>
		public const string DEFAULT_PREFIX = "#";

		/// <summary>
		/// Returns a validated prefix.
		/// </summary>
		/// <param name="prefix">The prefix to validate.</param>
		/// <returns>The validated prefix.</returns>
		public static string ValidatePrefix(string prefix)
		{
			return prefix.Validated("Prefix");
		}
	}
}
