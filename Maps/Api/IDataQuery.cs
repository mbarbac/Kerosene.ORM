using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Kerosene.ORM.Maps
{
	// ====================================================
	/// <summary>
	/// Represents a query command for the entities of the associated map.
	/// </summary>
	public interface IDataQuery : IMetaCommand, IEnumerable
	{
		/// <summary>
		/// Creates a new object able to execute this command.
		/// </summary>
		/// <returns>A new enumerator.</returns>
		new IDataQueryEnumerator GetEnumerator();

		/// <summary>
		/// Defines the contents of the TOP clause. Any previous ones are removed.
		/// </summary>
		/// <param name="top">An integer with the value to set for the TOP clause. A value of cero
		/// or negative merely removes this clause.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		IDataQuery Top(int top);

		/// <summary>
		/// Defines the contents of the WHERE clause or append the new ones to any previous
		/// specification.
		/// <para>By default if any previous contents exist the new ones are appended using an AND
		/// operator. However, the virtual extension methods 'x => x.And(...)' and 'x => x.Or(...)'
		/// can be used to specify what logical operator to use.</para>
		/// </summary>
		/// <param name="where">The dynamic lambda expression that resolves into the contents of
		/// this clause.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		IDataQuery Where(Func<dynamic, object> where);

		/// <summary>
		/// Defines the contents of the ORDER BY clause or append the new ones to any previous
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
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		IDataQuery OrderBy(params Func<dynamic, object>[] orderbys);

		/// <summary>
		/// Defines the contents of the SKIP clause. Any previous ones are removed.
		/// </summary>
		/// <param name="skip">An integer with the value to set for the SKIP clause. A value of cero
		/// or negative merely removes this clause.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		IDataQuery Skip(int skip);

		/// <summary>
		/// Defines the contents of the TAKE clause. Any previous ones are removed.
		/// </summary>
		/// <param name="take">An integer with the value to set for the TAKE clause. A value of cero
		/// or negative merely removes this clause.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		IDataQuery Take(int take);

		/// <summary>
		/// Executes this command and returns a list with the results.
		/// </summary>
		/// <returns>A list with the results of the execution.</returns>
		IList ToList();

		/// <summary>
		/// Executes this command and returns an array with the results.
		/// </summary>
		/// <returns>An array with the results of the execution.</returns>
		object[] ToArray();

		/// <summary>
		/// Executes this command and returns the first result produced from the database,
		/// or null if it produced no results.
		/// </summary>
		/// <returns>The first result produced, or null.</returns>
		object First();

		/// <summary>
		/// Executes this command and returns the last result produced from the database,
		/// or null if it produced no results.
		/// <para>
		/// - Note that the concrete implementation of this method may emulate this capability
		/// by retrieving all possible records and discarding them until the last one is found.
		/// Client applications may want to modify the logic of the command to avoid using it.
		/// </para>
		/// </summary>
		/// <returns>The first result produced, or null.</returns>
		object Last();

		/// <summary>
		/// Defines the alias to use for the primary table associated with this command, in
		/// case extended logic is used that require an alias to be assigned to it.
		/// </summary>
		/// <param name="alias">If not null a dynamic lambda expression that resolves into
		/// the alias to use for the primary table of this command.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		IDataQuery MasterAlias(Func<dynamic, object> alias);

		/// <summary>
		/// Defines additional contents of the FROM clause to be appended to the standard ones,
		/// to accomodate extended logic for this command if required.
		/// </summary>
		/// <param name="froms">The collection of lambda expressions that resolve into the
		/// elements to include in this clause:
		/// <para>- A string, as in 'x => "name AS alias"', where the alias part is optional.</para>
		/// <para>- A table specification, as in 'x => x.Table.As(alias)', where both the alias part
		/// is optional.</para>
		/// <para>- Any expression that can be parsed into a valid SQL sentence for this clause.</para>
		/// </param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		IDataQuery From(params Func<dynamic, object>[] froms);

		/// <summary>
		/// Defines the contents of the additional JOIN clauses, or append the new ones to any
		/// previous specifications, to accomodate extended logic for this command if required. 
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
		/// must be the first one in the chain, and whose parameter is a string containing the
		/// join clause to use.</para>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		IDataQuery Join(Func<dynamic, object> join);

		/// <summary>
		/// Defines the contents of the additional GROUP BY clauses, or append the new ones to any
		/// previous specification, to accomodate extended logic for this command if required.
		/// </summary>
		/// <param name="groupbys">The collection of dynamic lambda expressions that resolve into
		/// the contents of this clause:
		/// <para>- A string as in 'x => "Table.Column"', where the table part is optional.</para>
		/// </param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		IDataQuery GroupBy(params Func<dynamic, object>[] groupbys);

		/// <summary>
		/// Defines the contents of the additional HAVING clause that follows the GROUP BY one, or
		/// append the new ones to any previous specification, to accomodate extended logic for
		/// this command if required.
		/// <para>By default if any previous contents exist the new ones are appended using an AND
		/// operator. However, the virtual extension methods 'x => x.And(...)' and 'x => x.Or(...)'
		/// can be used to specify what logical operator to use.</para>
		/// </summary>
		/// <param name="having">The dynamic lambda expression that resolves into the contents of
		/// this clause.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		IDataQuery Having(Func<dynamic, object> having);
	}

	// ==================================================== 
	/// <summary>
	/// Represents a query command for the entities of the associated map.
	/// </summary>
	public interface IDataQuery<T> : IDataQuery, IMetaCommand<T>, IEnumerable<T> where T : class
	{
		/// <summary>
		/// Creates a new object able to execute this command.
		/// </summary>
		/// <returns>A new enumerator.</returns>
		new IDataQueryEnumerator<T> GetEnumerator();

		/// <summary>
		/// Defines the contents of the TOP clause. Any previous ones are removed.
		/// </summary>
		/// <param name="top">An integer with the value to set for the TOP clause. A value of cero
		/// or negative merely removes this clause.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		new IDataQuery<T> Top(int top);

		/// <summary>
		/// Defines the contents of the WHERE clause or append the new ones to any previous
		/// specification.
		/// <para>By default if any previous contents exist the new ones are appended using an AND
		/// operator. However, the virtual extension methods 'x => x.And(...)' and 'x => x.Or(...)'
		/// can be used to specify what logical operator to use.</para>
		/// </summary>
		/// <param name="where">The dynamic lambda expression that resolves into the contents of
		/// this clause.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		new IDataQuery<T> Where(Func<dynamic, object> where);

		/// <summary>
		/// Defines the contents of the ORDER BY clause or append the new ones to any previous
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
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		new IDataQuery<T> OrderBy(params Func<dynamic, object>[] orderbys);

		/// <summary>
		/// Defines the contents of the SKIP clause. Any previous ones are removed.
		/// </summary>
		/// <param name="skip">An integer with the value to set for the SKIP clause. A value of cero
		/// or negative merely removes this clause.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		new IDataQuery<T> Skip(int skip);

		/// <summary>
		/// Defines the contents of the TAKE clause. Any previous ones are removed.
		/// </summary>
		/// <param name="take">An integer with the value to set for the TAKE clause. A value of cero
		/// or negative merely removes this clause.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		new IDataQuery<T> Take(int take);

		/// <summary>
		/// Executes this command and returns a list with the results.
		/// </summary>
		/// <returns>A list with the results of the execution.</returns>
		new List<T> ToList();

		/// <summary>
		/// Executes this command and returns an array with the results.
		/// </summary>
		/// <returns>An array with the results of the execution.</returns>
		new T[] ToArray();

		/// <summary>
		/// Executes this command and returns the first result produced from the database,
		/// or null if it produced no results.
		/// </summary>
		/// <returns>The first result produced, or null.</returns>
		new T First();

		/// <summary>
		/// Executes this command and returns the last result produced from the database,
		/// or null if it produced no results.
		/// <para>
		/// - Note that the concrete implementation of this method may emulate this capability
		/// by retrieving all possible records and discarding them until the last one is found.
		/// Client applications may want to modify the logic of the command to avoid using it.
		/// </para>
		/// </summary>
		/// <returns>The first result produced, or null.</returns>
		new T Last();

		/// <summary>
		/// Defines the alias to use for the primary table associated with this command, in
		/// case extended logic is used that require an alias to be assigned to it.
		/// </summary>
		/// <param name="alias">If not null a dynamic lambda expression that resolves into
		/// the alias to use for the primary table of this command.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		new IDataQuery<T> MasterAlias(Func<dynamic, object> alias);

		/// <summary>
		/// Defines additional contents of the FROM clause to be appended to the standard ones,
		/// to accomodate extended logic for this command if required.
		/// </summary>
		/// <param name="froms">The collection of lambda expressions that resolve into the
		/// elements to include in this clause:
		/// <para>- A string, as in 'x => "name AS alias"', where the alias part is optional.</para>
		/// <para>- A table specification, as in 'x => x.Table.As(alias)', where both the alias part
		/// is optional.</para>
		/// <para>- Any expression that can be parsed into a valid SQL sentence for this clause.</para>
		/// </param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		new IDataQuery<T> From(params Func<dynamic, object>[] froms);

		/// <summary>
		/// Defines the contents of the additional JOIN clauses, or append the new ones to any
		/// previous specifications, to accomodate extended logic for this command if required. 
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
		/// must be the first one in the chain, and whose parameter is a string containing the
		/// join clause to use.</para>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		new IDataQuery<T> Join(Func<dynamic, object> join);

		/// <summary>
		/// Defines the contents of the additional GROUP BY clauses, or append the new ones to any
		/// previous specification, to accomodate extended logic for this command if required.
		/// </summary>
		/// <param name="groupbys">The collection of dynamic lambda expressions that resolve into
		/// the contents of this clause:
		/// <para>- A string as in 'x => "Table.Column"', where the table part is optional.</para>
		/// </param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		new IDataQuery<T> GroupBy(params Func<dynamic, object>[] groupbys);

		/// <summary>
		/// Defines the contents of the additional HAVING clause that follows the GROUP BY one, or
		/// append the new ones to any previous specification, to accomodate extended logic for
		/// this command if required.
		/// <para>By default if any previous contents exist the new ones are appended using an AND
		/// operator. However, the virtual extension methods 'x => x.And(...)' and 'x => x.Or(...)'
		/// can be used to specify what logical operator to use.</para>
		/// </summary>
		/// <param name="having">The dynamic lambda expression that resolves into the contents of
		/// this clause.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		new IDataQuery<T> Having(Func<dynamic, object> having);
	}
}
