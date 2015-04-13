// ======================================================== UpdateCommand.cs
namespace Kerosene.ORM.Core.Concrete
{
	using Kerosene.Tools;
	using System;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Represents an update command.
	/// </summary>
	public class UpdateCommand : CommandEnumSca, IUpdateCommand
	{
		string _TableName = null;
		protected string TheWhereData = null;
		protected string TheDataColumns = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="link">The link this instance will be associated with.</param>
		/// /// <param name="table">A dynamic lamnda expression that resolves into the name of the
		/// table affected by this operation.</param>
		public UpdateCommand(IDataLink link, Func<dynamic, object> table)
			: base(link)
		{
			_TableName = Link.Engine.Parser.Parse(table);
			_TableName = _TableName.Validated("Table");
		}

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		protected override void OnDispose(bool disposing)
		{
			base.OnDispose(disposing);
		}

		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		public UpdateCommand Clone()
		{
			var cloned = new UpdateCommand(Link, x => TableName);
			OnClone(cloned); return cloned;
		}
		IUpdateCommand IUpdateCommand.Clone()
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
			var temp = cloned as UpdateCommand;
			if (cloned == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' one."
				.FormatWith(cloned.Sketch(), typeof(UpdateCommand).EasyName()));

			temp.TheWhereData = TheWhereData;
			temp.TheDataColumns = TheDataColumns;
		}

		/// <summary>
		/// The name of the primary table this instance refers to.
		/// </summary>
		public string TableName
		{
			get { return _TableName; }
		}

		/// <summary>
		/// Whether the state and contents of this command permits its execution.
		/// </summary>
		public override bool CanBeExecuted
		{
			get { return (IsDisposed || TableName == null || TheDataColumns == null) ? false : true; }
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

			sb.Append("UPDATE");
			if (TableName != null) sb.AppendFormat(" {0}", TableName);
			if (TheDataColumns != null) sb.AppendFormat(" SET {0}", TheDataColumns);
			if (iterable) sb.Append(" OUTPUT INSERTED.*");
			if (TheWhereData != null) sb.AppendFormat(" WHERE {0}", TheWhereData);

			return sb.ToString();
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
		public IUpdateCommand Where(Func<dynamic, object> where)
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

				main = Link.Engine.Parser.Parse(result, pc: Parameters);
				break;
			}

			main = main.Validated("Where");

			if (TheWhereData == null) TheWhereData = main;
			else TheWhereData = "{0} {1} {2}".FormatWith(TheWhereData, and ? "AND" : "OR", main);

			parser.Dispose();

			return this;
		}

		/// <summary>
		/// Defines the names and values of the columns affected by this command, or adds the new
		/// ones to any previous one that may exist.
		/// </summary>
		/// <param name="columns">A collection of dynamic lambda expressions resolving into the
		/// column and values affected by this command using a 'x => x.Column = Value' syntax,
		/// where the value part can be any valid SQL sentence.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		public IUpdateCommand Columns(params Func<dynamic, object>[] columns)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (columns == null) throw new ArgumentNullException("columns", "Array of specifications cannot be null.");

			for (int i = 0; i < columns.Length; i++)
			{
				string main = null;
				string value = null;

				var parser = DynamicParser.Parse(columns[i]);
				var result = parser.Result; if (result == null) throw new ArgumentException(
					"Expression #{0} '{1}' cannot resolve to null.".FormatWith(i, parser));

				while (true)
				{
					if (result is string)
					{
						var node = ((string)result).Trim();
						var parts = node.Split('='); if (parts.Length < 2)
							throw new ArgumentException(
								"Specification #{0} contains no '=' symabol while parsing '{1}'.".FormatWith(i, parser));

						main = parts[0].Trim();
						value = parts[1].Trim();
						break;
					}

					if (result is DynamicNode.SetMember)
					{
						var node = (DynamicNode.SetMember)result;
						var host = Link.Engine.Parser.Parse(node.Host);

						main = host == null ? node.Name : "{0}.{1}".FormatWith(host, node.Name);
						value = Link.Engine.Parser.Parse(node.Value, Parameters);
						break;
					}

					if (result is DynamicNode.Binary)
					{
						var node = (DynamicNode.Binary)result;
						if (node.Operation == ExpressionType.Equal)
						{
							main = Link.Engine.Parser.Parse(node.Left);
							value = Link.Engine.Parser.Parse(node.Right, Parameters);
							break;
						}
					}

					throw new ArgumentException(
						"Invalid column specification while parsing #{0} '{1}'.".FormatWith(i, parser));
				}

				main = main.Validated("Column");
				value = value.Validated("Value");

				var str = "{0} = {1}".FormatWith(main, value);
				TheDataColumns = TheDataColumns == null ? str : "{0}, {1}".FormatWith(TheDataColumns, str);

				parser.Dispose();
			}

			return this;
		}
	}
}
// ======================================================== 
