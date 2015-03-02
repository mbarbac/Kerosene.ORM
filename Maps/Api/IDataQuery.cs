// ======================================================== IDataQuery.cs
namespace Kerosene.ORM.Maps
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;

	// ==================================================== 
	/// <summary>
	/// Represents a query operation to retrieve entities of the the type managed by its
	/// associated map.
	/// </summary>
	public interface IDataQuery : IMetaCommand, IEnumerable
	{
		/// <summary>
		/// Gets a new object able to execute this command and to enumerate through the entities
		/// produced by its execution.
		/// </summary>
		/// <returns>A new enumerator.</returns>
		new IDataQueryEnumerator GetEnumerator();

		/// <summary>
		/// Defines the contents of the TOP clause.
		/// </summary>
		/// <param name="top">An integer with the value to set for the TOP clause. A value of cero
		/// or negative merely removes this clause.</param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IDataQuery Top(int top);

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
		IDataQuery Where(Func<dynamic, object> where);

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
		IDataQuery OrderBy(params Func<dynamic, object>[] orderbys);

		/// <summary>
		/// Defines the contents of the SKIP clause.
		/// </summary>
		/// <param name="skip">An integer with the value to set for the SKIP clause. A value of
		/// cero or negative merely removes this clause.</param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IDataQuery Skip(int skip);

		/// <summary>
		/// Defines the contents of the TAKE clause.
		/// </summary>
		/// <param name="take">An integer with the value to set for the SKIP clause. A value of
		/// cero or negative value merely removes this clause.</param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IDataQuery Take(int take);

		/// <summary>
		/// Defines the alias to be used with the master table in case extended logic for
		/// this command is used.
		/// </summary>
		/// <param name="alias">If not null a dynamic lambda expression that resolves into the
		/// alias of the master table to support extended logic for this command.</param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IDataQuery MasterAlias(Func<dynamic, object> alias);

		/// <summary>
		/// Defines additional contents for the FROM clause to support extended logic for
		/// this command.
		/// </summary>
		/// <param name="froms">The collection of lambda expressions that resolve into the
		/// elements to include in this clause:
		/// <para>- A string, as in 'x => "name AS alias"', where the alias part is optional.</para>
		/// <para>- A table specification, as in 'x => x.Table.As(alias)', where both the alias part
		/// is optional.</para>
		/// <para>- Any expression that can be parsed into a valid SQL sentence for this clause.</para>
		/// </param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IDataQuery From(params Func<dynamic, object>[] froms);

		/// <summary>
		/// Defines additional contents for the JOIN clause to support extended logic for
		/// this command.
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
		IDataQuery Join(Func<dynamic, object> join);

		/// <summary>
		/// Defines the contents of the GROUP BY clause to support extended logic for this
		/// command.
		/// </summary>
		/// <param name="groupbys">The collection of dynamic lambda expressions that resolve into
		/// the contents of this clause:
		/// <para>- A string as in 'x => "Table.Column"', where the table part is optional.</para>
		/// </param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IDataQuery GroupBy(params Func<dynamic, object>[] groupbys);

		/// <summary>
		/// Defines the contents of the HAVING clause of the GROUP BY one to support extended
		/// logic for this command.
		/// </summary>
		/// <param name="having">The dynamic lambda expression that resolves into the contents of
		/// this clause.
		/// <para>- By default, if any previous contents exist the new ones are appended using an
		/// AND logical operator. However, the virtual extension methods 'x => x.And(...)' and
		/// 'x => x.Or(...)' can be used to specify the concrete logical operator to use for
		/// concatenation purposes.</para>
		/// </param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IDataQuery Having(Func<dynamic, object> having);

		/// <summary>
		/// Executes this command and returns a list containing the results.
		/// </summary>
		/// <returns>A list containing the results requested.</returns>
		IList ToList();

		/// <summary>
		/// Executes this command and returns aa arrays containing the results.
		/// </summary>
		/// <returns>An array containing the results requested.</returns>
		object[] ToArray();

		/// <summary>
		/// Executes this command and returns the first instance produced, or null if no one
		/// was produced.
		/// </summary>
		/// <returns>The first instance produced, or null.</returns>
		object First();

		/// <summary>
		/// Executes this command and returns the first instance produced, or null if no one
		/// was produced.
		/// <para>
		/// This method is provided as a fall-back mechanism because it retrieves all possible
		/// results discarding them until the last one is found. Client applications have to
		/// reconsider the logic of their command to avoid using this method if possible.
		/// </para>
		/// </summary>
		/// <returns>The first instance produced, or null.</returns>
		object Last();
	}

	// ==================================================== 
	/// <summary>
	/// Represents a query operation to retrieve entities of the the type managed by its
	/// associated map.
	/// </summary>
	public interface IDataQuery<T> : IDataQuery, IMetaCommand<T>, IEnumerable<T> where T : class
	{
		/// <summary>
		/// Gets a new object able to execute this command and to enumerate through the entities
		/// produced by its execution.
		/// </summary>
		/// <returns>A new enumerator.</returns>
		new IDataQueryEnumerator<T> GetEnumerator();

		/// <summary>
		/// Defines the contents of the TOP clause.
		/// </summary>
		/// <param name="top">An integer with the value to set for the TOP clause. A value of cero
		/// or negative merely removes this clause.</param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		new IDataQuery<T> Top(int top);

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
		new IDataQuery<T> Where(Func<dynamic, object> where);

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
		new IDataQuery<T> OrderBy(params Func<dynamic, object>[] orderbys);

		/// <summary>
		/// Defines the contents of the SKIP clause.
		/// </summary>
		/// <param name="skip">An integer with the value to set for the SKIP clause. A value of
		/// cero or negative merely removes this clause.</param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		new IDataQuery<T> Skip(int skip);

		/// <summary>
		/// Defines the contents of the TAKE clause.
		/// </summary>
		/// <param name="take">An integer with the value to set for the SKIP clause. A value of
		/// cero or negative value merely removes this clause.</param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		new IDataQuery<T> Take(int take);

		/// <summary>
		/// Defines the alias to be used with the master table in case extended logic for
		/// this command is used.
		/// </summary>
		/// <param name="alias">If not null a dynamic lambda expression that resolves into the
		/// alias of the master table to support extended logic for this command.</param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		new IDataQuery<T> MasterAlias(Func<dynamic, object> alias);

		/// <summary>
		/// Defines additional contents for the FROM clause to support extended logic for
		/// this command.
		/// </summary>
		/// <param name="froms">The collection of lambda expressions that resolve into the
		/// elements to include in this clause:
		/// <para>- A string, as in 'x => "name AS alias"', where the alias part is optional.</para>
		/// <para>- A table specification, as in 'x => x.Table.As(alias)', where both the alias part
		/// is optional.</para>
		/// <para>- Any expression that can be parsed into a valid SQL sentence for this clause.</para>
		/// </param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		new IDataQuery<T> From(params Func<dynamic, object>[] froms);

		/// <summary>
		/// Defines additional contents for the JOIN clause to support extended logic for
		/// this command.
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
		new IDataQuery<T> Join(Func<dynamic, object> join);

		/// <summary>
		/// Defines the contents of the GROUP BY clause to support extended logic for this
		/// command.
		/// </summary>
		/// <param name="groupbys">The collection of dynamic lambda expressions that resolve into
		/// the contents of this clause:
		/// <para>- A string as in 'x => "Table.Column"', where the table part is optional.</para>
		/// </param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		new IDataQuery<T> GroupBy(params Func<dynamic, object>[] groupbys);

		/// <summary>
		/// Defines the contents of the HAVING clause of the GROUP BY one to support extended
		/// logic for this command.
		/// </summary>
		/// <param name="having">The dynamic lambda expression that resolves into the contents of
		/// this clause.
		/// <para>- By default, if any previous contents exist the new ones are appended using an
		/// AND logical operator. However, the virtual extension methods 'x => x.And(...)' and
		/// 'x => x.Or(...)' can be used to specify the concrete logical operator to use for
		/// concatenation purposes.</para>
		/// </param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		new IDataQuery<T> Having(Func<dynamic, object> having);

		/// <summary>
		/// Executes this command and returns a list containing the results.
		/// </summary>
		/// <returns>A list containing the results requested.</returns>
		new List<T> ToList();

		/// <summary>
		/// Executes this command and returns aa arrays containing the results.
		/// </summary>
		/// <returns>An array containing the results requested.</returns>
		new T[] ToArray();

		/// <summary>
		/// Executes this command and returns the first instance produced, or null if no one
		/// was produced.
		/// </summary>
		/// <returns>The first instance produced, or null.</returns>
		new T First();

		/// <summary>
		/// Executes this command and returns the first instance produced, or null if no one
		/// was produced.
		/// <para>
		/// This method is provided as a fall-back mechanism because it retrieves all possible
		/// results discarding them until the last one is found. Client applications have to
		/// reconsider the logic of their command to avoid using this method if possible.
		/// </para>
		/// </summary>
		/// <returns>The first instance produced, or null.</returns>
		new T Last();
	}
}
// ======================================================== 
