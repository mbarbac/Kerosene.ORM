// ======================================================== IQueryCommand.cs
namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents a query operation against the underlying database.
	/// </summary>
	public interface IQueryCommand : ICommand, IEnumerableCommand, IElementAliasProvider
	{
		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		new IQueryCommand Clone();

		/// <summary>
		/// Defines the contents of the SELECT clause, or appends the new ones to any previous
		/// specification.
		/// </summary>
		/// <param name="selects">The collection of lambda expressions that resolve into the
		/// elements to include into this clause:
		/// <para>- A string, as in 'x => "name AS alias"', where the alias part is optional.</para>
		/// <para>- A table and column specification, as in 'x => x.Table.Column.As(alias)', where
		/// both the table and alias parts are optional.</para>
		/// <para>- A specification for all columns of a table using the 'x => x.Table.All()' syntax.</para>
		/// <para>- Any expression that can be parsed into a valid SQL sentence for this clause.</para>
		/// </param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IQueryCommand Select(params Func<dynamic, object>[] selects);

		/// <summary>
		/// Adds a DISTINCT clause to the SELECT one.
		/// </summary>
		/// <param name="distinct">Whether to activate or de-activate this clause.</param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IQueryCommand Distinct(bool distinct = true);

		/// <summary>
		/// Defines the contents of the TOP clause.
		/// </summary>
		/// <param name="top">An integer with the value to set for the TOP clause. A value of cero
		/// or negative merely removes this clause.</param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IQueryCommand Top(int top);

		/// <summary>
		/// Gets the current 'Top' value.
		/// </summary>
		int GetTopValue();

		/// <summary>
		/// Defines the contents of the FROM clause, or appends the new ones to any previous
		/// specification.
		/// </summary>
		/// <param name="froms">The collection of lambda expressions that resolve into the
		/// elements to include in this clause:
		/// <para>- A string, as in 'x => "name AS alias"', where the alias part is optional.</para>
		/// <para>- A table specification, as in 'x => x.Table.As(alias)', where both the alias part
		/// is optional.</para>
		/// <para>- Any expression that can be parsed into a valid SQL sentence for this clause.</para>
		/// </param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IQueryCommand From(params Func<dynamic, object>[] froms);

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
		IQueryCommand Where(Func<dynamic, object> where);

		/// <summary>
		/// Defines the contents of the JOIN clause, or appends the new ones to any previous
		/// specification.
		/// </summary>
		/// <param name="join">The dynamic lambda expression that resolves into the contents of
		/// this clause:
		/// <para>- A string, as in 'x => "jointype table AS alias ON condition"', where both the
		/// jointype and the alias parts are optional. If no jointype is used then a default JOIN
		/// one is used.</para>
		/// <para>- A dynamic specification as in 'x => x.Table.As(Alias).On(condition)' where
		/// the alias part is optional.</para>
		/// <para>- A dynamic specification containing a non-default join operation can be
		/// specified using the 'x => x(jointype).Table...' syntax, where the orphan invocation
		/// must be the first one in the chain.</para>
		/// </param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IQueryCommand Join(Func<dynamic, object> join);

		/// <summary>
		/// Defines the contents of the GROUP BY clause, or appends the new ones to any previous
		/// specification.
		/// </summary>
		/// <param name="groupbys">The collection of dynamic lambda expressions that resolve into
		/// the contents of this clause:
		/// <para>- A string as in 'x => "Table.Column"', where the table part is optional.</para>
		/// </param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IQueryCommand GroupBy(params Func<dynamic, object>[] groupbys);

		/// <summary>
		/// Defines the contents of the HAVING clause of the GROUP BY one, or appends the new ones
		/// to any previous specification.
		/// </summary>
		/// <param name="having">The dynamic lambda expression that resolves into the contents of
		/// this clause.
		/// <para>- By default, if any previous contents exist the new ones are appended using an
		/// AND logical operator. However, the virtual extension methods 'x => x.And(...)' and
		/// 'x => x.Or(...)' can be used to specify the concrete logical operator to use for
		/// concatenation purposes.</para>
		/// </param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IQueryCommand Having(Func<dynamic, object> having);

		/// <summary>
		/// Defines the contents of the ORDER BY one, or appends the new ones to any previous
		/// specification.
		/// </summary>
		/// <param name="orderbys">The collection of dynamic lambda expressions that resolve into
		/// the contents of this clause:
		/// <para>- A string as in 'x => x.Table.Column ORDER' where both the table and order
		/// parts are optional. If no order part is present then a default ASCENDING one is used.</para>
		/// <para>- A string as in 'x => "Table.Column.Order()"', where both the table and order
		/// parts are optional. The order part can be any among the 'Asc()', 'Ascending()',
		/// 'Desc()' or 'Descending()' ones.</para>
		/// </param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IQueryCommand OrderBy(params Func<dynamic, object>[] orderbys);

		/// <summary>
		/// Defines the contents of the SKIP clause.
		/// </summary>
		/// <param name="skip">An integer with the value to set for the SKIP clause. A value of
		/// cero or negative merely removes this clause.</param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IQueryCommand Skip(int skip);

		/// <summary>
		/// Defines the contents of the TAKE clause.
		/// </summary>
		/// <param name="take">An integer with the value to set for the SKIP clause. A value of
		/// cero or negative value merely removes this clause.</param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IQueryCommand Take(int take);

		/// <summary>
		/// Gets the current 'Skip' value.
		/// </summary>
		int GetSkipValue();

		/// <summary>
		/// Gets the current 'Take' value.
		/// </summary>
		int GetTakeValue();

		/// <summary>
		/// Gets whether the current state of the command is valid for a native Skip/Take
		/// implementation. If not it will be emulated by software.
		/// </summary>
		bool IsValidForNativeSkipTake();
	}
}
// ======================================================== 
