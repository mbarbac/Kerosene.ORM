// ======================================================== IUpdateCommand.cs
namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents an update operation against the underlying database.
	/// </summary>
	public interface IUpdateCommand : ICommand, IEnumerableCommand, IScalarCommand, ITableNameProvider
	{
		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		new IUpdateCommand Clone();

		/// <summary>
		/// Defines the contents of the WHERE clause, or appends the new ones to any previous
		/// specification.
		/// </summary>
		/// <param name="where">The dynamic lambda expression that resolves into the contents of
		/// this clause.
		/// <para>- By default, if any previous contents exist the new ones are appended using an
		/// AND logical operator. However, the virtual extension methods 'x => x.And(...)' and
		/// 'x => x.Or(...)' can be used to specify the concrete logical operator to use for
		/// concatenation purposes.</para>
		/// </param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IUpdateCommand Where(Func<dynamic, object> where);

		/// <summary>
		/// Defines, or adds to a previous specification, the columns affected by this command
		/// along with its values.
		/// </summary>
		/// <param name="columns">A collection of dynamic lambda expressions resolving into the
		/// column and values affected by this command using a 'x => x.Column = Value' syntax,
		/// where the value part can be any valid SQL sentence.</param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IUpdateCommand Columns(params Func<dynamic, object>[] columns);
	}
}
// ======================================================== 
