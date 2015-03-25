// ======================================================== QueryCommand.cs
namespace Kerosene.ORM.Core.Concrete
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Represents a query command.
	/// </summary>
	public class QueryCommand : CommandEnum, IQueryCommand
	{
		IElementAliasCollection _Aliases = null;
		protected string TheSelectData = null;
		protected bool TheDistinctData = false;
		protected int TheTopData = 0;
		protected string TheFromData = null;
		protected string TheWhereData = null;
		protected string TheJoinData = null;
		protected string TheGroupByData = null;
		protected string TheHavingData = null;
		protected string TheOrderByData = null;
		protected int TheSkipData = 0;
		protected int TheTakeData = 0;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="link">The link this instance will be associated with.</param>
		public QueryCommand(IDataLink link)
			: base(link)
		{
			if ((_Aliases = Link.Engine.CreateElementAliasCollection()) == null)
				throw new CannotCreateException(
					"Cannot create a collection of aliases for this instance.");
		}

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		protected override void OnDispose(bool disposing)
		{
			if (disposing)
			{
				if (_Aliases != null && !_Aliases.IsDisposed) _Aliases.Dispose();
			}
			_Aliases = null;

			base.OnDispose(disposing);
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			var str = base.ToString();

			if ((TheSkipData > 0 || TheTakeData > 0) && !IsValidForNativeSkipTake())
			{
				StringBuilder sb = new StringBuilder();
				sb.AppendFormat("{0} -- ", str);

				sb.Append("[");
				if (TheSkipData > 0) sb.AppendFormat("Skip:{0}", TheSkipData);
				if (TheSkipData > 0 && TheTakeData > 0) sb.Append(", ");
				if (TheTakeData > 0) sb.AppendFormat("Take:{0}", TheTakeData);
				sb.Append("]");

				str = sb.ToString();
			}

			return str;
		}

		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		public QueryCommand Clone()
		{
			var cloned = new QueryCommand(Link);
			OnClone(cloned); return cloned;
		}
		IQueryCommand IQueryCommand.Clone()
		{
			return this.Clone();
		}
		ICommand ICommand.Clone()
		{
			return this.Clone();
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		/// <summary>
		/// Invoked when cloning this object to set its state at this point of the inheritance
		/// chain.
		/// </summary>
		/// <param name="cloned">The cloned object.</param>
		protected override void OnClone(object cloned)
		{
			base.OnClone(cloned);
			var temp = cloned as QueryCommand;
			if (cloned == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' one."
				.FormatWith(cloned.Sketch(), typeof(QueryCommand).EasyName()));

			temp.Aliases.AddRange(Aliases, cloneNotOrphans: true);
			temp.TheSelectData = TheSelectData;
			temp.TheDistinctData = TheDistinctData;
			temp.TheTopData = TheTopData;
			temp.TheFromData = TheFromData;
			temp.TheWhereData = TheWhereData;
			temp.TheJoinData = TheJoinData;
			temp.TheGroupByData = TheGroupByData;
			temp.TheHavingData = TheHavingData;
			temp.TheOrderByData = TheOrderByData;
			temp.TheSkipData = TheSkipData;
			temp.TheTakeData = TheTakeData;
		}

		/// <summary>
		/// Whether the state and contents of this command permits its execution.
		/// </summary>
		public override bool CanBeExecuted
		{
			get { return (IsDisposed || TheFromData == null) ? false : true; }
		}

		/// <summary>
		/// Generates a string containing the command to be executed on the underlying database.
		/// <para>The text returned might be incomplete and should not be used until the value of
		/// the '<see cref="CanBeExecuted"/>' property is true.</para>
		/// </summary>
		/// <param name="iterable">True to generate the iterable version, false to generate the
		/// scalar one.</param>
		/// <returns>The requested command string.</returns>
		/// <remarks>This method must not throw an exception if this instance is disposed.</remarks>
		public override string GetCommandText(bool iterable)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("SELECT");
			if (TheDistinctData) sb.Append(" DISTINCT");
			if (TheTopData > 0) sb.AppendFormat(" TOP {0}", TheTopData);
			if (TheSelectData != null) sb.AppendFormat(" {0}", TheSelectData); else sb.Append(" *");
			if (TheFromData != null) sb.AppendFormat(" FROM {0}", TheFromData);
			if (TheJoinData != null) sb.AppendFormat(" {0}", TheJoinData);
			if (TheWhereData != null) sb.AppendFormat(" WHERE {0}", TheWhereData);
			if (TheGroupByData != null) sb.AppendFormat(" GROUP BY {0}", TheGroupByData);
			if (TheHavingData != null) sb.AppendFormat(" HAVING {0}", TheHavingData);
			if (TheOrderByData != null) sb.AppendFormat(" ORDER BY {0}", TheOrderByData);

			return sb.ToString();
		}

		/// <summary>
		/// The collection of aliases used in the context of this instance.
		/// </summary>
		public IElementAliasCollection Aliases
		{
			get { return _Aliases; }
		}

		/// <summary>
		/// Defines the contents of the SELECT clause or append the new ones to any previous
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
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		public IQueryCommand Select(params Func<dynamic, object>[] selects)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (selects == null) throw new ArgumentNullException("selects", "Array of specifications cannot be null.");

			for (int i = 0; i < selects.Length; i++)
			{
				string main = null;
				string nick = null; bool capture = true;
				bool all = false;

				var parser = DynamicParser.Parse(selects[i]);
				var result = parser.Result; if (result == null) throw new ArgumentException(
					"Expression #{0} '{1}' cannot resolve to null.".FormatWith(i, parser));

				while (true)
				{
					if (result is string)
					{
						var node = ((string)result).Trim();
						var tuple = Core.Parser.SplitInMainAndAlias(node);
						main = tuple.Item1;
						nick = tuple.Item2;
						break;
					}

					if (result is DynamicNode.Method)
					{
						var node = (DynamicNode.Method)result;
						var name = node.Name.ToUpper();

						if (name == "AS")
						{
							if (nick != null) throw new DuplicateException("Alias '{0}' was already set while parsing #{1} '{2}'.".FormatWith(nick, i, parser));
							if (node.Arguments == null) throw new ArgumentException("Virtual method 'AS()' cannot be parameterless while parsing '{0}'.".FormatWith(parser));
							if (node.Arguments.Length != 1) throw new ArgumentException("Virtual method 'AS()' can only have one argument while parsing '{0}'.".FormatWith(parser));
							if (node.Arguments[0] == null) throw new ArgumentException("Argument of virtual method 'AS()' cannot be null while parsing '{0}'.".FormatWith(parser));

							nick = Link.Parser.Parse(node.Arguments[0]);
							result = node.Host;
							continue;
						}

						if (name == "ALL")
						{
							if (all) throw new DuplicateException("ALL flag already set while parsing '{0}'.".FormatWith(parser));
							if (node.Arguments != null) throw new ArgumentException("Virtual method 'ALL()' shall be parameterless while parsing '{0}'.".FormatWith(parser));

							all = true;
							result = node.Host;
							continue;
						}
					}

					if (result is DynamicNode.Invoke)
					{
						var node = (DynamicNode.Invoke)result;
						if (node.Arguments != null && node.Arguments.Length == 1)
						{
							var arg = node.Arguments[0];
							var provider = arg as ICoreCommandProvider;

							if (provider != null)
							{
								arg = provider.GenerateCoreCommand();
								if (arg == null) throw new CannotCreateException(
									"Cannot generate core command for '{0}'.".FormatWith(provider));
							}
							if (arg is ICommand)
							{
								main = Link.Parser.Parse(arg, Parameters);
								main = string.Format("({0})", main);
								capture = false;

								if (provider != null) ((ICommand)arg).Dispose();
								break;
							}
							if (arg is string)
							{
								var tmp = (string)arg;
								capture = true;

								if (tmp.ToUpper().IndexOf("SELECT ") >= 0) capture = false;
								if (tmp.ToUpper().IndexOf("INSERT ") >= 0) capture = false;
								if (tmp.ToUpper().IndexOf("DELETE ") >= 0) capture = false;
								if (tmp.ToUpper().IndexOf("UPDATE ") >= 0) capture = false;

								if (!capture)
								{
									main = string.Format("({0})", tmp);
									break;
								}
								capture = false;
							}
						}
					}

					if (result is DynamicNode.GetIndexed)
					{
						var node = (DynamicNode.GetIndexed)result;
						if (node.Indexes != null && node.Indexes.Length == 1)
						{
							var arg = node.Indexes[0];
							var provider = arg as ICoreCommandProvider;

							if (provider != null)
							{
								arg = provider.GenerateCoreCommand();
								if (arg == null) throw new CannotCreateException(
									"Cannot generate core command for '{0}'.".FormatWith(provider));
							}
							if (arg is ICommand)
							{
								main = Link.Parser.Parse(arg, Parameters);
								main = string.Format("({0})", main);
								capture = false;

								if (provider != null) ((ICommand)arg).Dispose();
								break;
							}
							if (arg is string)
							{
								var tmp = (string)arg;

								if (tmp.ToUpper().IndexOf("SELECT ") >= 0) capture = false;
								if (tmp.ToUpper().IndexOf("INSERT ") >= 0) capture = false;
								if (tmp.ToUpper().IndexOf("DELETE ") >= 0) capture = false;
								if (tmp.ToUpper().IndexOf("UPDATE ") >= 0) capture = false;

								if (!capture)
								{
									main = string.Format("({0})", tmp);
									break;
								}
							}
						}
					}

					if (result is ICoreCommandProvider)
					{
						var arg = ((ICoreCommandProvider)result).GenerateCoreCommand();
						if (arg == null) throw new CannotCreateException(
							"Cannot generate core command for '{0}'."
							.FormatWith((ICoreCommandProvider)result));

						main = Link.Parser.Parse(arg, Parameters);
						main = string.Format("({0})", main);
						capture = false;

						arg.Dispose();
						break;
					}

					if (result is ICommand)
					{
						main = Link.Parser.Parse(result, Parameters);
						main = string.Format("({0})", main);
						capture = false;
						break;
					}

					main = Link.Parser.Parse(result, pc: Parameters);
					break;
				}

				main = main.Validated("Select");
				nick = nick.Validated("Alias", canbeNull: true);

				if (nick != null && capture) Aliases.AddCreate(main, nick);
				if (all) main += ".*";

				var str = nick == null ? main : "{0} AS {1}".FormatWith(main, nick);

				TheSelectData = TheSelectData == null ? str : "{0}, {1}".FormatWith(TheSelectData, str);

				parser.Dispose();
			}

			return this;
		}

		/// <summary>
		/// Adds or removes a DISTINCT clause to the SELECT one of this command.
		/// </summary>
		/// <param name="distinct">True to add this clause, false to remove it.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		public IQueryCommand Distinct(bool distinct = true)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			TheDistinctData = distinct;
			return this;
		}

		/// <summary>
		/// Defines the contents of the TOP clause. Any previous ones are removed.
		/// </summary>
		/// <param name="top">An integer with the value to set for the TOP clause. A value of cero
		/// or negative merely removes this clause.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		public IQueryCommand Top(int top)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			if (top <= 0) TheTopData = 0;
			else
			{
				TheTopData = top;
				TheSkipData = 0;
				TheTakeData = 0;
			}

			return this;
		}

		/// <summary>
		/// Gets the current 'Top' value.
		/// </summary>
		public int GetTopValue()
		{
			return TheTopData;
		}

		/// <summary>
		/// Defines the contents of the FROM clause or append the new ones to any previous
		/// specification.
		/// </summary>
		/// <param name="froms">The collection of lambda expressions that resolve into the
		/// elements to include in this clause:
		/// <para>- A string, as in 'x => "name AS alias"', where the alias part is optional.</para>
		/// <para>- A table specification, as in 'x => x.Table.As(alias)', where both the alias part
		/// is optional.</para>
		/// <para>- Any expression that can be parsed into a valid SQL sentence for this clause.</para>
		/// </param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		public IQueryCommand From(params Func<dynamic, object>[] froms)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (froms == null) throw new ArgumentNullException("froms", "Array of specifications cannot be null.");

			for (int i = 0; i < froms.Length; i++)
			{
				string main = null;
				string nick = null; bool capture = true;

				var parser = DynamicParser.Parse(froms[i]);
				var result = parser.Result; if (result == null) throw new ArgumentException(
					"Expression #{0} '{1}' cannot resolve to null.".FormatWith(i, parser));

				while (true)
				{
					if (result is string)
					{
						var node = ((string)result).Trim();
						var tuple = Core.Parser.SplitInMainAndAlias(node);
						main = tuple.Item1;
						nick = tuple.Item2;
						break;
					}

					if (result is DynamicNode.Method)
					{
						var node = (DynamicNode.Method)result;
						var name = node.Name.ToUpper();

						if (name == "AS")
						{
							if (nick != null) throw new DuplicateException("Alias '{0}' was already set while parsing #{1} '{2}'.".FormatWith(nick, i, parser));
							if (node.Arguments == null) throw new ArgumentException("Virtual method 'AS()' cannot be parameterless while parsing '{0}'.".FormatWith(parser));
							if (node.Arguments.Length != 1) throw new ArgumentException("Virtual method 'AS()' can only have one argument while parsing '{0}'.".FormatWith(parser));
							if (node.Arguments[0] == null) throw new ArgumentException("Argument of virtual method 'AS()' cannot be null while parsing '{0}'.".FormatWith(parser));

							nick = Link.Parser.Parse(node.Arguments[0]);
							result = node.Host;
							continue;
						}
					}

					if (result is DynamicNode.Invoke)
					{
						var node = (DynamicNode.Invoke)result;
						if (node.Arguments != null && node.Arguments.Length == 1)
						{
							var arg = node.Arguments[0];
							var provider = arg as ICoreCommandProvider;

							if (provider != null)
							{
								arg = provider.GenerateCoreCommand();
								if (arg == null) throw new CannotCreateException(
									"Cannot generate core command for '{0}'."
									.FormatWith(provider));
							}
							if (arg is ICommand)
							{
								main = Link.Parser.Parse(arg, Parameters);
								main = string.Format("({0})", main);
								capture = false;

								if (provider != null) ((ICommand)arg).Dispose();
								break;
							}
							if (arg is string)
							{
								var tmp = (string)arg;

								if (tmp.ToUpper().IndexOf("SELECT ") >= 0) capture = false;
								if (tmp.ToUpper().IndexOf("INSERT ") >= 0) capture = false;
								if (tmp.ToUpper().IndexOf("DELETE ") >= 0) capture = false;
								if (tmp.ToUpper().IndexOf("UPDATE ") >= 0) capture = false;

								if (!capture)
								{
									main = string.Format("({0})", tmp);
									break;
								}
							}
						}
					}

					if (result is DynamicNode.GetIndexed)
					{
						var node = (DynamicNode.GetIndexed)result;
						if (node.Indexes != null && node.Indexes.Length == 1)
						{
							var arg = node.Indexes[0];
							var provider = arg as ICoreCommandProvider;

							if (provider != null) arg = provider.GenerateCoreCommand();
							if (arg == null) throw new CannotCreateException(
								"Cannot generate core command for '{0}'."
								.FormatWith(provider));

							if (arg is ICommand)
							{
								main = Link.Parser.Parse(arg, Parameters);
								main = string.Format("({0})", main);
								capture = false;

								if (provider != null) ((ICommand)arg).Dispose();
								break;
							}
							if (arg is string)
							{
								var tmp = (string)arg;
								capture = true;

								if (tmp.ToUpper().IndexOf("SELECT ") >= 0) capture = false;
								if (tmp.ToUpper().IndexOf("INSERT ") >= 0) capture = false;
								if (tmp.ToUpper().IndexOf("DELETE ") >= 0) capture = false;
								if (tmp.ToUpper().IndexOf("UPDATE ") >= 0) capture = false;

								if (!capture)
								{
									main = string.Format("({0})", tmp);
									break;
								}
								capture = false;
							}
						}
					}

					if (result is ICoreCommandProvider)
					{
						var arg = ((ICoreCommandProvider)result).GenerateCoreCommand();
						if (arg == null) throw new CannotCreateException(
							"Cannot generate core command for '{0}'."
							.FormatWith((ICoreCommandProvider)result));

						main = Link.Parser.Parse(arg, Parameters);
						main = string.Format("({0})", main);
						capture = false;

						arg.Dispose();
						break;
					}

					if (result is ICommand)
					{
						main = Link.Parser.Parse(result, Parameters);
						main = string.Format("({0})", main);
						capture = false;
						break;
					}

					main = Link.Parser.Parse(result, pc: Parameters);
					break;
				}

				main = main.Validated("From");
				nick = nick.Validated("Alias", canbeNull: true);

				if (nick != null && capture) Aliases.AddCreate(main, nick);

				var str = nick == null ? main : "{0} AS {1}".FormatWith(main, nick);

				TheFromData = TheFromData == null ? str : "{0}, {1}".FormatWith(TheFromData, str);

				parser.Dispose();
			}

			return this;
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
		public IQueryCommand Where(Func<dynamic, object> where)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (where == null) throw new ArgumentNullException("where", "Specification cannot be null.");

			string main = null;
			bool and = true;

			var parser = DynamicParser.Parse(where);
			var result = parser.Result; if (result == null) throw new ArgumentException(
				"Expression '{0}' cannot resolve to null.".FormatWith(parser));

			while (true)
			{
				if (result is string)
				{
					main = ((string)result).Trim();
					if (main.ToUpper().IndexOf("OR ") == 0) { and = false; main = main.Substring(3); }
					if (main.ToUpper().IndexOf("AND ") == 0) { and = true; main = main.Substring(4); }
					break;
				}

				if (result is DynamicNode.Method)
				{
					var node = (DynamicNode.Method)result;
					var name = node.Name.ToUpper();

					if ((name == "AND" || name == "OR") && (node.Host is DynamicNode.Argument))
					{
						if (node.Arguments == null) throw new ArgumentException("Virtual method '{0}()' cannot be parameterless while parsing '{0}'.".FormatWith(name, parser));
						if (node.Arguments.Length != 1) throw new ArgumentException("Virtual method '{0}()' can only have one argument while parsing '{0}'.".FormatWith(name, parser));
						if (node.Arguments[0] == null) throw new ArgumentException("Argument of virtual method '{0}()' cannot be null while parsing '{0}'.".FormatWith(name, parser));

						and = (name == "AND");
						result = node.Arguments[0];
					}
				}

				main = Link.Parser.Parse(result, pc: Parameters);
				break;
			}

			main = main.Validated("Where");

			if (TheWhereData == null) TheWhereData = main;
			else TheWhereData = "{0} {1} {2}".FormatWith(TheWhereData, and ? "AND" : "OR", main);

			parser.Dispose();

			return this;
		}

		/// <summary>
		/// Defines the contents of the JOIN clause or append the new ones to any previous
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
		/// must be the first one in the chain, and whose parameter is a string containing the
		/// join clause to use.</para>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		public IQueryCommand Join(Func<dynamic, object> join)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (join == null) throw new ArgumentNullException("join", "Specifications cannot be null.");

			string type = null;
			string main = null;
			string nick = null;
			string condition = null;

			var parser = DynamicParser.Parse(join);
			var result = parser.Result; if (result == null) throw new ArgumentException(
				"Expression '{0}' cannot resolve to null.".FormatWith(parser));

			if (result is DynamicNode)
			{
				var node = (DynamicNode)result; while (node != null)
				{
					if (node.Host is DynamicNode.Invoke && node.Host.Host is DynamicNode.Argument)
					{
						var host = (DynamicNode.Invoke)node.Host;
						if (host.Arguments == null) throw new ArgumentException("Virtual invocation x(...) cannot be parameterless while parsing '{0}'.".FormatWith(parser));
						if (host.Arguments.Length != 1) throw new ArgumentException("Virtual invocation 'x(...)' can only have one argument while parsing '{0}'.".FormatWith(parser));
						type = Link.Parser.Parse(host.Arguments[0]);
						if (type == null) throw new ArgumentException("Join type cannot resolve into null while parsing '{0}'.".FormatWith(parser));

						node.ChangeHost(node.Host.Host);
						break;
					}

					node = node.Host;
				}
			}

			while (true)
			{
				if (result is string)
				{
					var node = ((string)result).Trim();

					int n = node.ToUpper().IndexOf(" ON "); if (n >= 0)
					{
						condition = node.Substring(n + 4).Trim();
						node = node.Substring(0, n).Trim();
					}

					var tuple = Core.Parser.SplitInMainAndAlias(node);
					main = tuple.Item1;
					nick = tuple.Item2;
					break;
				}

				if (result is DynamicNode.Method)
				{
					var node = (DynamicNode.Method)result;
					var name = node.Name.ToUpper();

					if (name == "AS")
					{
						if (nick != null) throw new DuplicateException("Alias '{0}' was already set while parsing '{1}'.".FormatWith(nick, parser));
						if (node.Arguments == null) throw new ArgumentException("Virtual method 'AS()' cannot be parameterless while parsing '{0}'.".FormatWith(parser));
						if (node.Arguments.Length != 1) throw new ArgumentException("Virtual method 'AS()' can only have one argument while parsing '{0}'.".FormatWith(parser));
						if (node.Arguments[0] == null) throw new ArgumentException("Argument of virtual method 'AS()' cannot be null while parsing '{0}'.".FormatWith(parser));

						nick = Link.Parser.Parse(node.Arguments[0]);
						result = node.Host;
						continue;
					}

					if (name == "ON")
					{
						if (condition != null) throw new DuplicateException("Condition '{0}' was already set while parsing '{1}'.".FormatWith(nick, parser));
						if (node.Arguments == null) throw new ArgumentException("Virtual method 'ON()' cannot be parameterless while parsing '{0}'.".FormatWith(parser));
						if (node.Arguments.Length != 1) throw new ArgumentException("Virtual method 'ON()' can only have one argument while parsing '{0}'.".FormatWith(parser));
						if (node.Arguments[0] == null) throw new ArgumentException("Argument of virtual method 'ON()' cannot be null while parsing '{0}'.".FormatWith(parser));

						condition = Link.Parser.Parse(node.Arguments[0]);
						result = node.Host;
						continue;
					}
				}

				main = Link.Parser.Parse(result, pc: Parameters);
				break;
			}

			if (type == null) type = "JOIN";

			main = main.Validated("Join Table");
			nick = nick.Validated("Alias", canbeNull: true);
			if (nick != null && main.IndexOfAny("() ".ToCharArray()) < 0) Aliases.AddCreate(main, nick);

			var str = string.Format("{0} {1}", type, main);
			if (nick != null) str = string.Format("{0} AS {1}", str, nick);
			if (condition != null) str = string.Format("{0} ON {1}", str, condition);

			TheJoinData = TheJoinData == null ? str : string.Format("{0} {1}", TheJoinData, str);

			parser.Dispose();

			return this;
		}

		/// <summary>
		/// Defines the contents of the GROUP BY clause or append the new ones to any previous
		/// specification.
		/// </summary>
		/// <param name="groupbys">The collection of dynamic lambda expressions that resolve into
		/// the contents of this clause:
		/// <para>- A string as in 'x => "Table.Column"', where the table part is optional.</para>
		/// </param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		public IQueryCommand GroupBy(params Func<dynamic, object>[] groupbys)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (groupbys == null) throw new ArgumentNullException("groupbys", "Array of specifications cannot be null.");

			for (int i = 0; i < groupbys.Length; i++)
			{
				string main = null;

				var parser = DynamicParser.Parse(groupbys[i]);
				var result = parser.Result; if (result == null) throw new ArgumentException(
					"Expression #{0} '{1}' cannot resolve to null.".FormatWith(i, parser));

				while (true)
				{
					if (result is string)
					{
						main = ((string)result).Trim();
						break;
					}

					main = Link.Parser.Parse(result, pc: Parameters);
					break;
				}

				main = main.Validated("Group By");
				TheGroupByData = TheGroupByData == null ? main : "{0}, {1}".FormatWith(TheGroupByData, main);

				parser.Dispose();
			}

			return this;
		}

		/// <summary>
		/// Defines the contents of the HAVING clause that follows the GROUP BY one, or append the
		/// new ones to any previous specification.
		/// <para>By default if any previous contents exist the new ones are appended using an AND
		/// operator. However, the virtual extension methods 'x => x.And(...)' and 'x => x.Or(...)'
		/// can be used to specify what logical operator to use.</para>
		/// </summary>
		/// <param name="having">The dynamic lambda expression that resolves into the contents of
		/// this clause.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		public IQueryCommand Having(Func<dynamic, object> having)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (having == null) throw new ArgumentNullException("having", "Specification cannot be null.");

			string main = null;
			bool and = true;

			var parser = DynamicParser.Parse(having);
			var result = parser.Result; if (result == null) throw new ArgumentException(
				"Expression '{0}' cannot resolve to null.".FormatWith(parser));

			while (true)
			{
				if (result is string)
				{
					main = ((string)result).Trim();
					if (main.ToUpper().IndexOf("OR ") == 0) { and = false; main = main.Substring(3); }
					if (main.ToUpper().IndexOf("AND ") == 0) { and = true; main = main.Substring(4); }
					break;
				}

				if (result is DynamicNode.Method)
				{
					var node = (DynamicNode.Method)result;
					var name = node.Name.ToUpper();

					if ((name == "AND" || name == "OR") && (node.Host is DynamicNode.Argument))
					{
						if (node.Arguments == null) throw new ArgumentException("Virtual method '{0}()' cannot be parameterless while parsing '{0}'.".FormatWith(name, parser));
						if (node.Arguments.Length != 1) throw new ArgumentException("Virtual method '{0}()' can only have one argument while parsing '{0}'.".FormatWith(name, parser));
						if (node.Arguments[0] == null) throw new ArgumentException("Argument of virtual method '{0}()' cannot be null while parsing '{0}'.".FormatWith(name, parser));

						and = (name == "AND");
						result = node.Arguments[0];
					}
				}

				main = Link.Parser.Parse(result, pc: Parameters);
				break;
			}

			main = main.Validated("Having");

			if (TheHavingData == null) TheHavingData = main;
			else TheHavingData = "{0} {1} {2}".FormatWith(TheHavingData, and ? "AND" : "OR", main);

			parser.Dispose();

			return this;
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
		public IQueryCommand OrderBy(params Func<dynamic, object>[] orderbys)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (orderbys == null) throw new ArgumentNullException("orderbys", "Array of specifications cannot be null.");

			for (int i = 0; i < orderbys.Length; i++)
			{
				string main = null;
				bool ascending = true; bool orderSet = false;

				var parser = DynamicParser.Parse(orderbys[i]);
				var result = parser.Result; if (result == null) throw new ArgumentException(
					"Expression #{0} '{1}' cannot resolve to null.".FormatWith(i, parser));

				while (true)
				{
					if (result is string)
					{
						main = ((string)result).Trim();
						break;
					}

					if (result is DynamicNode.Method)
					{
						var node = (DynamicNode.Method)result;
						var name = node.Name.ToUpper();

						if (name == "ASC" || name == "ASCENDING" || name == "DESC" || name == "DESCENDING")
						{
							if (orderSet) throw new DuplicateException("Order '{0}' is already set while parsing #{1} '{2}'.".FormatWith(ascending ? "ASC" : "DESC", i, parser));
							if (node.Arguments != null) throw new ArgumentException("Virtual method '{0}()' must be parameterless while parsing '{0}'.".FormatWith(name, parser));

							ascending = (name == "ASC" || name == "ASCENDING");
							orderSet = true;
							result = node.Host;
							continue;
						}
					}

					main = Link.Parser.Parse(result, pc: Parameters);
					break;
				}

				main = main.Validated("Order By");

				if (orderSet) main = "{0} {1}".FormatWith(main, ascending ? "ASC" : "DESC");
				TheOrderByData = TheOrderByData == null ? main : "{0}, {1}".FormatWith(TheOrderByData, main);

				parser.Dispose();
			}

			return this;
		}

		/// <summary>
		/// Defines the contents of the SKIP clause. Any previous ones are removed.
		/// </summary>
		/// <param name="skip">An integer with the value to set for the SKIP clause. A value of cero
		/// or negative merely removes this clause.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		public IQueryCommand Skip(int skip)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			if (skip < 0) TheSkipData = 0;
			else
			{
				TheSkipData = skip;
				TheTopData = 0;
			}
			return this;
		}

		/// <summary>
		/// Gets the current 'Skip' value.
		/// </summary>
		public int GetSkipValue()
		{
			return TheSkipData;
		}

		/// <summary>
		/// Defines the contents of the TAKE clause. Any previous ones are removed.
		/// </summary>
		/// <param name="take">An integer with the value to set for the TAKE clause. A value of cero
		/// or negative merely removes this clause.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		public IQueryCommand Take(int take)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			if (take <= 0) TheTakeData = 0;
			else
			{
				TheTakeData = take;
				TheTopData = 0;
			}
			return this;
		}

		/// <summary>
		/// Gets the current 'Take' value.
		/// </summary>
		public int GetTakeValue()
		{
			return TheTakeData;
		}

		/// <summary>
		/// Gets whether the current state of the command is valid for a native Skip/Take
		/// implementation. If not it will be emulated by software.
		/// </summary>
		public virtual bool IsValidForNativeSkipTake()
		{
			if (IsDisposed) return false;
			if (Link.IsDisposed) return false;
			if (Link.Engine.IsDisposed) return false;

			if (!Link.Engine.SupportsNativeSkipTake) return false;
			if (TheSkipData >= 0 && TheTakeData >= 1 && TheOrderByData != null) return false; // TO OVERRIDE!!!
			return false;
		}
	}
}
// ======================================================== 
