// ======================================================== IMapMember.cs
namespace Kerosene.ORM.Maps
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;

	// ==================================================== 
	/// <summary>
	/// Defines how a member depends on its host instance regarding to a change operation
	/// (insert, delete, update) initiated on that host.
	/// </summary>
	public enum MemberDependencyMode
	{
		/// <summary>
		/// The member is not taken into consideration for change operations.
		/// </summary>
		None,

		/// <summary>
		/// The member is considered as a logical parent of the host instance.
		/// </summary>
		Parent,

		/// <summary>
		/// The member is considered as a logical child of the host instance.
		/// </summary>
		Child
	}

	// ==================================================== 
	/// <summary>
	/// Represents a member of the type that has been explicitly included in the map. They are
	/// typically used to identify dependency members, eager or lazy ones, or to identify
	/// alternate ways to obtain their contents.
	/// </summary>
	public interface IMapMember
	{
		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		IDataMap Map { get; }

		/// <summary>
		/// The name of the member of the type this instance refers to.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// How this member depends on its host instance regarding to a change operation
		/// (insert, delete, update) initiated on that host.
		/// </summary>
		MemberDependencyMode DependencyMode { get; set; }

		/// <summary>
		/// Sets how this member depends on its host instance regarding to a change operation
		/// (insert, delete, update) initiated on that host.
		/// </summary>
		/// <param name="mode">The dependency mode value to set for this instance.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		IMapMember SetDependencyMode(MemberDependencyMode mode);

		/// <summary>
		/// If not null the delegate to invoke with (record, entity) arguments to complete the
		/// value of this member. This delegate can invoke any actions it may need, and then
		/// setting the value of the associated member is its sole responsibility.
		/// </summary>
		Action<IRecord, object> CompleteMember { get; set; }

		/// <summary>
		/// Sets the delegate to invoke, if it is not null, with (record, entity) arguments to
		/// complete the value of this member. This delegate can invoke any actions it may need,
		/// and then setting the value of the associated member is its sole responsibility.
		/// </summary>
		/// <param name="onComplete"></param>
		/// <returns></returns>
		IMapMember OnComplete(Action<IRecord, object> onComplete);

		/// <summary>
		/// The collection of columns that have been explicitly defined to support the mapping
		/// of this member.
		/// </summary>
		IMapMemberColumnCollection Columns { get; }

		/// <summary>
		/// Adds explicitly a new column to support the mapping of this member.
		/// </summary>
		/// <param name="name">A dynamic lambda expression that resolves into the name of the
		/// database column.</param>
		/// <param name="onCreate">If not null the delegate to invoke with the newly created
		/// column to further refine its rules and operations.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		IMapMember WithColumn(Func<dynamic, object> name, Action<IMapMemberColumn> onCreate = null);

		/// <summary>
		/// Removes from this instance the column whose name is given.
		/// </summary>
		/// <param name="name">A dynamic lambda expression that resolves into the name of the
		/// database column.</param>
		/// <returns>True if the column has been removed, false otherwise.</returns>
		bool RemoveColum(Func<dynamic, object> name);
	}

	// ==================================================== 
	/// <summary>
	/// Represents a member of the type that has been explicitly included in the map. They are
	/// typically used to identify dependency members, eager or lazy ones, or to identify
	/// alternate ways to obtain their contents.
	/// </summary>
	public interface IMapMember<T> : IMapMember where T : class
	{
		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		new IDataMap<T> Map { get; }

		/// <summary>
		/// Sets how this member depends on its host instance regarding to a change operation
		/// (insert, delete, update) initiated on that host.
		/// </summary>
		/// <param name="mode">The dependency mode value to set for this instance.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		new IMapMember<T> SetDependencyMode(MemberDependencyMode mode);

		/// <summary>
		/// If not null the delegate to invoke with (record, entity) arguments to complete the
		/// value of this member. This delegate can invoke any actions it may need, and then
		/// setting the value of the associated member is its sole responsibility.
		/// </summary>
		new Action<IRecord, T> CompleteMember { get; set; }

		/// <summary>
		/// Sets the delegate to invoke, if it is not null, with (record, entity) arguments to
		/// complete the value of this member. This delegate can invoke any actions it may need,
		/// and then setting the value of the associated member is its sole responsibility.
		/// </summary>
		/// <param name="onComplete"></param>
		/// <returns></returns>
		IMapMember<T> OnComplete(Action<IRecord, T> onComplete);

		/// <summary>
		/// The collection of columns that have been explicitly defined to support the mapping
		/// of this member.
		/// </summary>
		new IMapMemberColumnCollection<T> Columns { get; }

		/// <summary>
		/// Adds explicitly a new column to support the mapping of this member.
		/// </summary>
		/// <param name="name">A dynamic lambda expression that resolves into the name of the
		/// database column.</param>
		/// <param name="onCreate">If not null the delegate to invoke with the newly created
		/// column to further refine its rules and operations.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		IMapMember<T> WithColumn(Func<dynamic, object> name, Action<IMapMemberColumn<T>> onCreate = null);
	}
}
// ======================================================== 
