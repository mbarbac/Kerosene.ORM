using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;

namespace Kerosene.ORM.Maps
{
	// ====================================================
	/// <summary>
	/// Represents the column to be used for row version control, if any.
	/// </summary>
	public interface IMapVersionColumn
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
		/// Sets the name of the column to use for row version control purposes. If the name is
		/// null then row version control is not enforced.
		/// </summary>
		/// <param name="name">A dynamic lambda expression that resolves into the name of the
		/// column.</param>
		/// <param name="customize">If not null the delegate to invoke with the column as its
		/// argument in order to permit its customization, if needed.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		IMapVersionColumn SetName(Func<dynamic, object> name, Action<IMapColumn> customize = null);

		/// <summary>
		/// Whether row version control is enabled for delete and update operations, or not.
		/// The getter returns false despite its internal value if the 'Name' property is null.
		/// </summary>
		bool Enabled { get; set; }

		/// <summary>
		/// The delegate to invoke to convert whatever value the row version control column has
		/// into a string for comparison purposes.
		/// <para>If this property is null then a default comparison delegate will be used.</para>
		/// </summary>
		Func<object, string> ValueToString { get; set; }

		/// <summary>
		/// Sets the delegate to invoke to convert whatever value the row version control column
		/// has into a string for comparison purposes.
		/// <para>If this property is null then a default comparison delegate will be used.</para>
		/// </summary>
		/// <param name="func">The delegate to set, or null.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		IMapVersionColumn OnValueToString(Func<object, string> func);
	}

	// ====================================================
	/// <summary>
	/// Represents the column to be used for row version control, if any.
	/// </summary>
	public interface IMapVersionColumn<T> : IMapVersionColumn where T : class
	{
		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		new IDataMap<T> Map { get; }

		/// <summary>
		/// Sets the name of the column to use for row version control purposes. If the name is
		/// null then row version control is not enforced.
		/// </summary>
		/// <param name="name">A dynamic lambda expression that resolves into the name of the
		/// column.</param>
		/// <param name="customize">If not null the delegate to invoke with the column as its
		/// argument in order to permit its customization, if needed.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		IMapVersionColumn<T> SetName(Func<dynamic, object> name, Action<IMapColumn<T>> customize = null);

		/// <summary>
		/// Sets the delegate to invoke to convert whatever value the row version control column
		/// has into a string for comparison purposes.
		/// <para>If this property is null then a default comparison delegate will be used.</para>
		/// </summary>
		/// <param name="func">The delegate to set, or null.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		new IMapVersionColumn<T> OnValueToString(Func<object, string> func);
	}
}
