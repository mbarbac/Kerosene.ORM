namespace Kerosene.ORM.Maps
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents a column in the primary table that has been explicitly associated with
	/// a member of the map.
	/// </summary>
	public interface IMapMemberColumn
	{
		/// <summary>
		/// The member this instance is associated with.
		/// </summary>
		IMapMember Member { get; }

		/// <summary>
		/// The name of the database column.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Whether writing into the database record for this column is enabled or not.
		/// </summary>
		bool WriteEnabled { get; set; }

		/// <summary>
		/// Sets whether writing into the database record for this column is enabled or not.
		/// </summary>
		/// <param name="enabled">True or false.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		IMapMemberColumn OnWriteRecord(bool enabled);

		/// <summary>
		/// If not null the delegate to invoke with an (entity) argument to obtain the value
		/// to write into the database record for this column.
		/// </summary>
		Func<object, object> WriteRecord { get; set; }

		/// <summary>
		/// Sets the delegate to invoke with an (entity) argument to obtain the value to write
		/// into the database record for this column.
		/// </summary>
		/// <param name="onWrite">The delegate to invoke, or null.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		IMapMemberColumn OnWriteRecord(Func<object, object> onWrite);

		/// <summary>
		/// Whether loading the value from the database record into the entity is enabled or not
		/// for this column.
		/// </summary>
		bool LoadEnabled { get; set; }

		/// <summary>
		/// Sets loading the value from the database record into the entity is enabled or not
		/// for this column.
		/// </summary>
		/// <param name="enabled">True or false.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		IMapMemberColumn OnLoadEntity(bool enabled);

		/// <summary>
		/// If not null the delegate to invoke with (value, entity) arguments to load into the
		/// entity the value from the database record for this column.
		/// </summary>
		Action<object, object> LoadEntity { get; set; }

		/// <summary>
		/// Sets the delegate to invoke with (value, entity) arguments to load into the
		/// entity the value from the database record for this column.
		/// </summary>
		/// <param name="onWrite">The delegate to invoke, or null.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		IMapMemberColumn OnLoadEntity(Action<object, object> onLoad);

		/// <summary>
		/// Identifies the member into which this database column will be mapped. Members can
		/// be both properties and fields and either public, protected or private ones.
		/// <para>
		/// Note that this member can be a different one than the one this instance depends on,
		/// in case such scenario is needed.
		/// </para>
		/// </summary>
		/// <param name="element">A dynamic lambda expression that resolves into the name of the
		/// member of the type into which this database column will be mapped.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		IMapMemberColumn OnElement(Func<dynamic, object> element);
	}

	// ==================================================== 
	/// <summary>
	/// Represents a column in the primary table that has been explicitly associated with
	/// a member of the map.
	/// </summary>
	public interface IMapMemberColumn<T> : IMapMemberColumn where T : class
	{
		/// <summary>
		/// The member this instance is associated with.
		/// </summary>
		new IMapMember<T> Member { get; }

		/// <summary>
		/// Sets whether writing into the database record for this column is enabled or not.
		/// </summary>
		/// <param name="enabled">True or false.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		new IMapMemberColumn<T> OnWriteRecord(bool enabled);

		/// <summary>
		/// If not null the delegate to invoke with an (entity) argument to obtain the value
		/// to write into the database record for this column.
		/// </summary>
		new Func<T, object> WriteRecord { get; set; }

		/// <summary>
		/// Sets the delegate to invoke with an (entity) argument to obtain the value to write
		/// into the database record for this column.
		/// </summary>
		/// <param name="onWrite">The delegate to invoke, or null.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		IMapMemberColumn<T> OnWriteRecord(Func<T, object> onWrite);

		/// <summary>
		/// Sets loading the value from the database record into the entity is enabled or not
		/// for this column.
		/// </summary>
		/// <param name="enabled">True or false.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		new IMapMemberColumn<T> OnLoadEntity(bool enabled);

		/// <summary>
		/// If not null the delegate to invoke with (value, entity) arguments to load into the
		/// entity the value from the database record for this column.
		/// </summary>
		new Action<object, T> LoadEntity { get; set; }

		/// <summary>
		/// Sets the delegate to invoke with (value, entity) arguments to load into the
		/// entity the value from the database record for this column.
		/// </summary>
		/// <param name="onWrite">The delegate to invoke, or null.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		IMapMemberColumn<T> OnLoadEntity(Action<object, T> onLoad);

		/// <summary>
		/// Identifies the member into which this database column will be mapped. Members can
		/// be both properties and fields and either public, protected or private ones.
		/// <para>
		/// Note that this member can be a different one than the one this instance depends on,
		/// in case such scenario is needed.
		/// </para>
		/// </summary>
		/// <param name="element">A dynamic lambda expression that resolves into the name of the
		/// member of the type into which this database column will be mapped.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		new IMapMemberColumn<T> OnElement(Func<dynamic, object> element);
	}
}
