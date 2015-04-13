// ======================================================== InsertCommand.cs
namespace Kerosene.ORM.Core.Concrete
{
	using Kerosene.Tools;
	using System;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Represents an insert command.
	/// </summary>
	public class InsertCommand : CommandEnumSca, IInsertCommand
	{
		string _TableName = null;
		protected string TheDataColumns = null;
		protected string TheDataValues = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="link">The link this instance will be associated with.</param>
		/// <param name="table">A dynamic lamnda expression that resolves into the name of the
		/// table affected by this operation.</param>
		public InsertCommand(IDataLink link, Func<dynamic, object> table)
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
		public InsertCommand Clone()
		{
			var cloned = new InsertCommand(Link, x => TableName);
			OnClone(cloned); return cloned;
		}
		IInsertCommand IInsertCommand.Clone()
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
			var temp = cloned as InsertCommand;
			if (cloned == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' one."
				.FormatWith(cloned.Sketch(), typeof(InsertCommand).EasyName()));

			temp.TheDataColumns = TheDataColumns;
			temp.TheDataValues = TheDataValues;
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

			sb.Append("INSERT");
			if (TableName != null) sb.AppendFormat(" INTO {0}", TableName);
			if (TheDataColumns != null) sb.AppendFormat(" ({0})", TheDataColumns);
			if (iterable) sb.Append(" OUTPUT INSERTED.*");
			if (TheDataValues != null) sb.AppendFormat(" VALUES ({0})", TheDataValues);

			return sb.ToString();
		}

		/// <summary>
		/// The name of the primary table this instance refers to.
		/// </summary>
		public string TableName
		{
			get { return _TableName; }
		}

		/// <summary>
		/// Defines the names and values of the columns affected by this command, or adds the new
		/// ones to any previous one that may exist.
		/// </summary>
		/// <param name="columns">A collection of dynamic lambda expressions resolving into the
		/// column and values affected by this command using a 'x => x.Column = Value' syntax,
		/// where the value part can be any valid SQL sentence.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		public IInsertCommand Columns(params Func<dynamic, object>[] columns)
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

				TheDataColumns = TheDataColumns == null ? main : "{0}, {1}".FormatWith(TheDataColumns, main);
				TheDataValues = TheDataValues == null ? value : "{0}, {1}".FormatWith(TheDataValues, value);

				parser.Dispose();
			}

			return this;
		}
	}
}
// ======================================================== 
