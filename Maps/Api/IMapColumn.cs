using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;

namespace Kerosene.ORM.Maps
{
	// ====================================================
	/// <summary>
	/// Represents a column in the database associated with a map.
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
		bool Excluded { get; set; }

		/// <summary>
		/// Sets whether this database column is explicitly excluded from the map so that it will
		/// not be taken into consideration for any map operations.
		/// </summary>
		/// <param name="excluded">True to exclude the database column from the map.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		IMapColumn SetExcluded(bool excluded);

		/// <summary>
		/// The name of the element of the type this column shall be mapped to, if any.
		/// </summary>
		string ElementName { get; set; }

		/// <summary>
		/// Sets the name of the element of the type this column shall be mapped to, if any.
		/// </summary>
		/// <param name="name">The name of the element, or null.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		IMapColumn SetElementName(Func<dynamic, object> name);

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
	}

	// ====================================================
	/// <summary>
	/// Represents a column in the database associated with a map.
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
		/// Sets the name of the element of the type this column shall be mapped to, if any.
		/// </summary>
		/// <param name="name">The name of the element, or null.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		new IMapColumn<T> SetElementName(Func<dynamic, object> name);

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
	}
}
