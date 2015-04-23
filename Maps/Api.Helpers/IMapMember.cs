namespace Kerosene.ORM.Maps
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Defines how dependencies are treated for a member.
	/// </summary>
	public enum MemberDependencyMode
	{
		/// <summary>
		/// The member is not taken into consideration (cascaded) for change operations that
		/// are initiated on its host.
		/// </summary>
		None,

		/// <summary>
		/// The member is considered as a logical parent of its host instance. Typically parents
		/// are just signalled when a change operation on its childs happens.
		/// </summary>
		Parent,

		/// <summary>
		/// The member is considered as a logical child of its host instance. Typically childs
		/// are affected by cascading change operations initiated on their parents.
		/// </summary>
		Child
	}

	// ==================================================== 
	/// <summary>
	/// Represents a member in the type that has been explicitly associated with the map.
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
		MemberDependencyMode DependencyMode { get; }

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
		/// of this member, if any.
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
	/// Represents a member in the type that has been explicitly associated with the map.
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
		new Action<Core.IRecord, T> CompleteMember { get; set; }

		/// <summary>
		/// Sets the delegate to invoke, if it is not null, with (record, entity) arguments to
		/// complete the value of this member. This delegate can invoke any actions it may need,
		/// and then setting the value of the associated member is its sole responsibility.
		/// </summary>
		/// <param name="onComplete"></param>
		/// <returns></returns>
		IMapMember<T> OnComplete(Action<Core.IRecord, T> onComplete);

		/// <summary>
		/// The collection of columns that have been explicitly defined to support the mapping
		/// of this member, if any.
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
