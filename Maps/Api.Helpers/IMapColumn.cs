// ======================================================== IMapColumn.cs
namespace Kerosene.ORM.Maps
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents a column in the primary table that has been associated with a map.
	/// </summary>
	public interface IMapColumn
	{
		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		IDataMap Map { get; }

		/// <summary>
		/// The name of the database column.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Whether this column has been automatically discovered and included in the map as
		/// part of the process of map validation.
		/// </summary>
		bool AutoDiscovered { get; }

		/// <summary>
		/// Whether this database column is explicitly excluded from the map so that it will not
		/// be taken into consideration for any map operations.
		/// </summary>
		bool Excluded { get; }

		/// <summary>
		/// Sets whether this database column is explicitly excluded from the map so that it will
		/// not be taken into consideration for any map operations.
		/// </summary>
		/// <param name="excluded">True to exclude the database column from the map.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		IMapColumn SetExcluded(bool excluded);

		/// <summary>
		/// Whether writing into the database record for this column is enabled or not.
		/// </summary>
		bool WriteEnabled { get; set; }

		/// <summary>
		/// Sets whether writing into the database record for this column is enabled or not.
		/// </summary>
		/// <param name="enabled">True or false.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		IMapColumn OnWriteRecord(bool enabled);

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
		IMapColumn OnWriteRecord(Func<object, object> onWrite);

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
		IMapColumn OnLoadEntity(bool enabled);

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
		IMapColumn OnLoadEntity(Action<object, object> onLoad);

		/// <summary>
		/// Identifies the member into which this database column will be mapped. Members can
		/// be both properties and fields and either public, protected or private ones.
		/// </summary>
		/// <param name="element">A dynamic lambda expression that resolves into the name of the
		/// member of the type into which this database column will be mapped.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		IMapColumn OnElement(Func<dynamic, object> element);
	}

	// ==================================================== 
	/// <summary>
	/// Represents a column in the primary table that has been associated with a map.
	/// </summary>
	public interface IMapColumn<T> : IMapColumn where T : class
	{
		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		new IDataMap<T> Map { get; }

		/// <summary>
		/// Sets whether this database column is explicitly excluded from the map so that it will
		/// not be taken into consideration for any map operations.
		/// </summary>
		/// <param name="excluded">True to exclude the database column from the map.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		new IMapColumn<T> SetExcluded(bool excluded);

		/// <summary>
		/// Sets whether writing into the database record for this column is enabled or not.
		/// </summary>
		/// <param name="enabled">True or false.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		new IMapColumn<T> OnWriteRecord(bool enabled);

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
		IMapColumn<T> OnWriteRecord(Func<T, object> onWrite);

		/// <summary>
		/// Sets loading the value from the database record into the entity is enabled or not
		/// for this column.
		/// </summary>
		/// <param name="enabled">True or false.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		new IMapColumn<T> OnLoadEntity(bool enabled);

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
		IMapColumn<T> OnLoadEntity(Action<object, T> onLoad);

		/// <summary>
		/// Identifies the member into which this database column will be mapped. Members can
		/// be both properties and fields and either public, protected or private ones.
		/// </summary>
		/// <param name="element">A dynamic lambda expression that resolves into the name of the
		/// member of the type into which this database column will be mapped.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		new IMapColumn<T> OnElement(Func<dynamic, object> element);
	}
}
// ======================================================== 
