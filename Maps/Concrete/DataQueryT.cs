// ======================================================== DataQueryT.cs
namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Represents a query command for the entities of the associated map.
	/// </summary>
	public class DataQuery<T> : MetaCommand<T>, IDataQuery<T> where T : class
	{
		IQueryCommand _Template = null;
		string _MasterAlias = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="map">The map this command will be associated with.</param>
		public DataQuery(DataMap<T> map)
			: base(map)
		{
			_Template = map.Link.Query(); if (_Template == null)
				throw new CannotCreateException(
					"Cannot create a template query for this '{0}'.".FormatWith(this));
		}

		/// <summary>
		/// Disposes this instance.
		/// </summary>
		/// <remarks>Gives back public visibility to the Dispose() method.</remarks>
		public new void Dispose()
		{
			base.Dispose();
		}

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		protected override void OnDispose(bool disposing)
		{
			if (disposing)
			{
				if (_Template != null && !_Template.IsDisposed) _Template.Dispose();
			}
			_Template = null;

			base.OnDispose(disposing);
		}

		/// <summary>
		/// Generates a new command that when executed implements the operation this instance
		/// refers to, or null if the state of this instance impedes such core command to be
		/// create
		/// </summary>
		/// <returns>A new command, or null.</returns>
		internal IQueryCommand GenerateCoreCommand()
		{
			var cmd = _Template == null ? null : _Template.Clone(); if (cmd != null)
			{
				var name = Map.Table;
				if (_MasterAlias != null) name = string.Format("{0} AS {1}", name, _MasterAlias);
				cmd.From(x => name);

				foreach (var entry in Map.Schema)
				{
					name = entry.ColumnName;
					if (_MasterAlias != null) name = string.Format("{0}.{1}", _MasterAlias, name);
					cmd.Select(x => name);
				}

				if (Map.Discriminator != null)
				{
					if (_MasterAlias == null) cmd.Where(Map.Discriminator);
					else
					{
						var parser = DynamicParser.Parse(Map.Discriminator);
						var result = parser.Result;

						var host = new DynamicNode.GetMember(
							new DynamicNode.Argument(parser.DynamicArguments.First().Name),
							_MasterAlias);
						MasterVisitor(host, (DynamicNode)result);
						cmd.Where(x => parser.Result);
					}
				}
			}
			return cmd;
		}
		ICommand ICoreCommandProvider.GenerateCoreCommand()
		{
			return this.GenerateCoreCommand();
		}

		/// <summary>
		/// Implements the visitor pattern to modify the discriminator when a master alias is
		/// used.
		/// </summary>
		private void MasterVisitor(DynamicNode host, DynamicNode node)
		{
			if (node is DynamicNode.GetMember)
			{
				if (node.Host is DynamicNode.Argument) node.ChangeHost(host);
				else MasterVisitor(host, node.Host);
				return;
			}
			if (node is DynamicNode.SetMember)
			{
				var temp = (DynamicNode.SetMember)node;

				if (temp.Value is DynamicNode) MasterVisitor(host, (DynamicNode)temp.Value);

				if (node.Host is DynamicNode.Argument) node.ChangeHost(host);
				else MasterVisitor(host, node.Host);
				return;
			}
			if (node is DynamicNode.GetIndexed)
			{
				var temp = (DynamicNode.GetIndexed)node;

				if (temp.Indexes != null)
					foreach (var obj in temp.Indexes)
						if (obj is DynamicNode) MasterVisitor(host, (DynamicNode)obj);

				if (node.Host is DynamicNode.Argument) node.ChangeHost(host);
				else MasterVisitor(host, node.Host);
				return;
			}
			if (node is DynamicNode.SetIndexed)
			{
				var temp = (DynamicNode.SetIndexed)node;

				if (temp.Value is DynamicNode) MasterVisitor(host, (DynamicNode)temp.Value);

				if (temp.Indexes != null)
					foreach (var obj in temp.Indexes)
						if (obj is DynamicNode) MasterVisitor(host, (DynamicNode)obj);

				if (node.Host is DynamicNode.Argument) node.ChangeHost(host);
				else MasterVisitor(host, node.Host);
				return;
			}
			if (node is DynamicNode.Method)
			{
				var temp = (DynamicNode.Method)node;

				if (temp.Arguments != null)
					foreach (var obj in temp.Arguments)
						if (obj is DynamicNode) MasterVisitor(host, (DynamicNode)obj);

				if (node.Host is DynamicNode.Argument) node.ChangeHost(host);
				else MasterVisitor(host, node.Host);
				return;
			}
			if (node is DynamicNode.Invoke)
			{
				var temp = (DynamicNode.Invoke)node;

				if (temp.Arguments != null)
					foreach (var obj in temp.Arguments)
						if (obj is DynamicNode) MasterVisitor(host, (DynamicNode)obj);

				if (node.Host is DynamicNode.Argument) node.ChangeHost(host);
				else MasterVisitor(host, node.Host);
				return;
			}
			if (node is DynamicNode.Binary)
			{
				var temp = (DynamicNode.Binary)node;

				MasterVisitor(host, temp.Left);
				if (temp.Right is DynamicNode) MasterVisitor(host, (DynamicNode)temp.Right);

				return;
			}
			if (node is DynamicNode.Unary)
			{
				var temp = (DynamicNode.Unary)node;

				MasterVisitor(host, temp.Target);
				return;
			}
			if (node is DynamicNode.Convert)
			{
				var temp = (DynamicNode.Convert)node;

				MasterVisitor(host, temp.Target);
				return;
			}
		}

		/// <summary>
		/// Creates a new object able to execute this command.
		/// </summary>
		/// <returns>A new enumerator.</returns>
		public DataQueryEnumerator<T> GetEnumerator()
		{
			return new DataQueryEnumerator<T>(this);
		}
		IDataQueryEnumerator<T> IDataQuery<T>.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		IDataQueryEnumerator IDataQuery.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		/// Defines the contents of the TOP clause. Any previous ones are removed.
		/// </summary>
		/// <param name="top">An integer with the value to set for the TOP clause. A value of cero
		/// or negative merely removes this clause.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		public DataQuery<T> Top(int top)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			_Template.Top(top);
			return this;
		}
		IDataQuery<T> IDataQuery<T>.Top(int top)
		{
			return this.Top(top);
		}
		IDataQuery IDataQuery.Top(int top)
		{
			return this.Top(top);
		}

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
		public DataQuery<T> Where(Func<dynamic, object> where)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			_Template.Where(where);
			return this;
		}
		IDataQuery<T> IDataQuery<T>.Where(Func<dynamic, object> where)
		{
			return this.Where(where);
		}
		IDataQuery IDataQuery.Where(Func<dynamic, object> where)
		{
			return this.Where(where);
		}

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
		public DataQuery<T> OrderBy(params Func<dynamic, object>[] orderbys)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			_Template.OrderBy(orderbys);
			return this;
		}
		IDataQuery<T> IDataQuery<T>.OrderBy(params Func<dynamic, object>[] orderbys)
		{
			return this.OrderBy(orderbys);
		}
		IDataQuery IDataQuery.OrderBy(params Func<dynamic, object>[] orderbys)
		{
			return this.OrderBy(orderbys);
		}

		/// <summary>
		/// Defines the contents of the SKIP clause. Any previous ones are removed.
		/// </summary>
		/// <param name="skip">An integer with the value to set for the SKIP clause. A value of cero
		/// or negative merely removes this clause.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		public DataQuery<T> Skip(int skip)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			_Template.Skip(skip);
			return this;
		}
		IDataQuery<T> IDataQuery<T>.Skip(int skip)
		{
			return this.Skip(skip);
		}
		IDataQuery IDataQuery.Skip(int skip)
		{
			return this.Skip(skip);
		}

		/// <summary>
		/// Defines the contents of the TAKE clause. Any previous ones are removed.
		/// </summary>
		/// <param name="take">An integer with the value to set for the TAKE clause. A value of cero
		/// or negative merely removes this clause.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		public DataQuery<T> Take(int take)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			_Template.Take(take);
			return this;
		}
		IDataQuery<T> IDataQuery<T>.Take(int take)
		{
			return this.Take(take);
		}
		IDataQuery IDataQuery.Take(int take)
		{
			return this.Take(take);
		}

		/// <summary>
		/// Executes this command and returns a list with the results.
		/// </summary>
		/// <returns>A list with the results of the execution.</returns>
		public List<T> ToList()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			var iter = GetEnumerator();
			var list = iter.ToList();

			iter.Dispose();
			return list;
		}
		IList IDataQuery.ToList()
		{
			return this.ToList();
		}

		/// <summary>
		/// Executes this command and returns an array with the results.
		/// </summary>
		/// <returns>An array with the results of the execution.</returns>
		public T[] ToArray()
		{
			return this.ToList().ToArray();
		}
		object[] IDataQuery.ToArray()
		{
			return this.ToArray();
		}

		/// <summary>
		/// Executes this command and returns the first result produced from the database,
		/// or null if it produced no results.
		/// </summary>
		/// <returns>The first result produced, or null.</returns>
		public T First()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			var iter = GetEnumerator();
			var obj = iter.First();

			iter.Dispose();
			return obj;
		}
		object IDataQuery.First()
		{
			return this.First();
		}

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
		public T Last()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			var iter = GetEnumerator();
			var obj = iter.Last();

			iter.Dispose();
			return obj;
		}
		object IDataQuery.Last()
		{
			return this.Last();
		}

		/// <summary>
		/// Defines the alias to use for the primary table associated with this command, in
		/// case extended logic is used that require an alias to be assigned to it.
		/// </summary>
		/// <param name="alias">If not null a dynamic lambda expression that resolves into
		/// the alias to use for the primary table of this command.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		public DataQuery<T> MasterAlias(Func<dynamic, object> alias)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			if (alias == null) _MasterAlias = null;
			else
			{
				_MasterAlias = Map.Link.Parser.Parse(alias);
				_MasterAlias = _MasterAlias.Validated("Master Alias");
			}

			return this;
		}
		IDataQuery<T> IDataQuery<T>.MasterAlias(Func<dynamic, object> alias)
		{
			return this.MasterAlias(alias);
		}
		IDataQuery IDataQuery.MasterAlias(Func<dynamic, object> alias)
		{
			return this.MasterAlias(alias);
		}

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
		public DataQuery<T> From(params Func<dynamic, object>[] froms)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			_Template.From(froms);
			return this;
		}
		IDataQuery<T> IDataQuery<T>.From(params Func<dynamic, object>[] froms)
		{
			return this.From(froms);
		}
		IDataQuery IDataQuery.From(params Func<dynamic, object>[] froms)
		{
			return this.From(froms);
		}

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
		public DataQuery<T> Join(Func<dynamic, object> join)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			_Template.Join(join);
			return this;
		}
		IDataQuery<T> IDataQuery<T>.Join(Func<dynamic, object> join)
		{
			return this.Join(join);
		}
		IDataQuery IDataQuery.Join(Func<dynamic, object> join)
		{
			return this.Join(join);
		}

		/// <summary>
		/// Defines the contents of the additional GROUP BY clauses, or append the new ones to any
		/// previous specification, to accomodate extended logic for this command if required.
		/// </summary>
		/// <param name="groupbys">The collection of dynamic lambda expressions that resolve into
		/// the contents of this clause:
		/// <para>- A string as in 'x => "Table.Column"', where the table part is optional.</para>
		/// </param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		public DataQuery<T> GroupBy(params Func<dynamic, object>[] groupbys)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			_Template.GroupBy(groupbys);
			return this;
		}
		IDataQuery<T> IDataQuery<T>.GroupBy(params Func<dynamic, object>[] groupbys)
		{
			return this.GroupBy(groupbys);
		}
		IDataQuery IDataQuery.GroupBy(params Func<dynamic, object>[] groupbys)
		{
			return this.GroupBy(groupbys);
		}

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
		public DataQuery<T> Having(Func<dynamic, object> having)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			_Template.Having(having);
			return this;
		}
		IDataQuery<T> IDataQuery<T>.Having(Func<dynamic, object> having)
		{
			return this.Having(having);
		}
		IDataQuery IDataQuery.Having(Func<dynamic, object> having)
		{
			return this.Having(having);
		}
	}
}
// ======================================================== 
