// ======================================================== IMapVersionColumn.cs
namespace Kerosene.ORM.Maps
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// If this instance is not empty represents the column in the primary table that will be
	/// used for row version control purposes.
	/// </summary>
	public interface IMapVersionColumn
	{
		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		IDataMap Map { get; }

		/// <summary>
		/// The name of the column to be used for row version control, if any.
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// Sets the name of the database column. If this value is null the row version control is
		/// disabled.
		/// </summary>
		/// <param name="name">A dynamic lambda expression that resolves into the name of the
		/// database column. If this argument is null, or resolves into null, row version control
		/// is disable.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		IMapVersionColumn SetName(Func<dynamic, object> name);

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
	/// If this instance is not empty represents the column in the primary table that will be
	/// used for row version control purposes.
	/// </summary>
	public interface IMapVersionColumn<T> : IMapVersionColumn where T : class
	{
		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		new IDataMap<T> Map { get; }

		/// <summary>
		/// Sets the name of the database column. If this value is null the row version control is
		/// disabled.
		/// </summary>
		/// <param name="name">A dynamic lambda expression that resolves into the name of the
		/// database column. If this argument is null, or resolves into null, row version control
		/// is disable.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		new IMapVersionColumn<T> SetName(Func<dynamic, object> name);

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
// ======================================================== 
