using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;

namespace Kerosene.ORM.Maps
{
	// ====================================================
	/// <summary>
	/// Represents the type of dependency defined for a member.
	/// </summary>
	public enum MemberDependencyMode
	{
		/// <summary>
		/// There are no dependencies defined for the member.
		/// </summary>
		None,

		/// <summary>
		/// The member shall be considered as a parent of the host instance where it is defined.
		/// </summary>
		Parent,

		/// <summary>
		/// The member shall be considered as a child of the host instance where it is defined.
		/// </summary>
		Child
	}

	// ====================================================
	/// <summary>
	/// Represents a member of the type for which either a dependency and/or a completion method
	/// has been defined.
	/// </summary>
	public interface IMapMember
	{
		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		IDataMap Map { get; }

		/// <summary>
		/// The name of the member this instance refers to.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// The type of the dependency defined for this member, if any.
		/// </summary>
		MemberDependencyMode DependencyMode { get; set; }

		/// <summary>
		/// Sets the type of the dependency defined for this member, if any.
		/// </summary>
		/// <param name="mode">The dependency mode.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		IMapMember SetDependencyMode(MemberDependencyMode mode);

		/// <summary>
		/// If not null maintains the delegate to invoke with (record, entity) arguments to
		/// complete the value of the member this instance refers to.
		/// </summary>
		Action<IRecord, object> CompleteMember { get; set; }

		/// <summary>
		/// Sets the delegate to invoke with (record, entity) arguments to complete the value
		/// of the member this instance refers to.
		/// </summary>
		/// <param name="onComplete">The delegate to invoke, or null.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		IMapMember OnComplete(Action<IRecord, object> onComplete);

		/// <summary>
		/// Identifies a column needed to support this member and, if the column did not exist
		/// previously in the map, it is created.
		/// </summary>
		/// <param name="name">A dynamic lambda expression that resolves into the name of the
		/// column.</param>
		/// <param name="customize">If not null the delegate to invoke with the column as its
		/// argument in order to permit its customization, if needed.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		IMapMember WithColumn(Func<dynamic, object> name, Action<IMapColumn> customize = null);
	}

	// ====================================================
	/// <summary>
	/// Represents a member of the type for which either a dependency and/or a completion method
	/// has been defined.
	/// </summary>
	public interface IMapMember<T> : IMapMember where T : class
	{
		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		new IDataMap<T> Map { get; }

		/// <summary>
		/// If not null maintains the delegate to invoke with (record, entity) arguments to
		/// complete the value of the member this instance refers to.
		/// </summary>
		new Action<IRecord, T> CompleteMember { get; set; }

		/// <summary>
		/// Sets the delegate to invoke with (record, entity) arguments to complete the value
		/// of the member this instance refers to.
		/// </summary>
		/// <param name="onComplete">The delegate to invoke, or null.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		IMapMember<T> OnComplete(Action<IRecord, T> onComplete);

		/// <summary>
		/// Identifies a column needed to support this member and, if the column did not exist
		/// previously in the map, it is created.
		/// </summary>
		/// <param name="name">A dynamic lambda expression that resolves into the name of the
		/// column.</param>
		/// <param name="customize">If not null the delegate to invoke with the column as its
		/// argument in order to permit its customization, if needed.</param>
		/// <returns></returns>
		IMapMember<T> WithColumn(Func<dynamic, object> name, Action<IMapColumn<T>> customize = null);
	}
}
