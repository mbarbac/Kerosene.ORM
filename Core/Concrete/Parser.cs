namespace Kerosene.ORM.Core.Concrete
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Represents the ability of parsing any arbitrary object, including null references and
	/// dynamic lambda expressions,	and translate it into a string representation the database
	/// engine can understand.
	/// </summary>
	public class Parser : IParser
	{
		bool _IsDisposed = false;
		IDataEngine _Engine = null;

		/// <summary>
		/// Initializes a new instance associated with the given engine.
		/// </summary>
		/// <param name="engine">The engine this instance is associated with.</param>
		public Parser(IDataEngine engine)
		{
			if (engine == null) throw new ArgumentNullException("engine", "Engine cannot be null.");
			if (engine.IsDisposed) throw new ObjectDisposedException(engine.ToString());
			_Engine = engine;
		}

		/// <summary>
		/// Whether this instance has been disposed or not.
		/// </summary>
		public bool IsDisposed
		{
			get { return _IsDisposed; }
		}

		/// <summary>
		/// Disposes this instance.
		/// </summary>
		public void Dispose()
		{
			if (!IsDisposed) { OnDispose(true); GC.SuppressFinalize(this); }
		}

		~Parser()
		{
			if (!IsDisposed) OnDispose(false);
		}

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		protected virtual void OnDispose(bool disposing)
		{
			_Engine = null;
			_IsDisposed = true;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			var str = string.Format("{0}({1})",
				GetType().EasyName(),
				_Engine.Sketch());

			return IsDisposed ? string.Format("disposed::{0}", str) : str;
		}

		/// <summary>
		/// The data engine this parser is associated with.
		/// </summary>
		public IDataEngine Engine
		{
			get { return _Engine; }
		}

		/// <summary>
		/// Parses the given object, including any arbitrary command logic expressed as a
		/// dynamic lambda expression, and returns an string that can be understood by the
		/// underlying database engine.
		/// </summary>
		/// <param name="obj">The object to parse. It can be any object or reference, including
		/// null ones and dynamic lambda expressions.</param>
		/// <param name="pc">If not null the collection of parameters where to place the ones
		/// extracted from the object to parse. If null their string representation is used
		/// instead.</param>
		/// <param name="nulls">If true null references are accepted. Otherwise an exception is
		/// thrown.</param>
		/// <returns>A string contained the parsed input in a syntax that can be understood by
		/// the underlying database engine.</returns>
		public string Parse(object obj, IParameterCollection pc = null, bool nulls = true)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			DynamicParser parser = null; Func<string> dispatch = () =>
			{
				if (obj != null)
				{
					if (obj is Delegate)
					{
						parser = DynamicParser.Parse((Delegate)obj);
						obj = parser.Result;
					}
				}

				if (obj != null)
				{
					if (obj is ICoreCommandProvider) return OnParseCoreCommandProvider((ICoreCommandProvider)obj, pc, nulls);
					if (obj is ICommand) return OnParseCommand((ICommand)obj, pc, nulls);
					if (obj is DynamicNode.Argument) return OnParseArgument((DynamicNode.Argument)obj);
					if (obj is DynamicNode.GetMember) return OnParseGetMember((DynamicNode.GetMember)obj, pc, nulls);
					if (obj is DynamicNode.GetIndexed) return OnParseGetIndexedMember((DynamicNode.GetIndexed)obj, pc, nulls);
					if (obj is DynamicNode.SetMember) return OnParseSetMember((DynamicNode.SetMember)obj, pc, nulls);
					if (obj is DynamicNode.SetIndexed) return OnParseSetIndexedMember((DynamicNode.SetIndexed)obj, pc, nulls);
					if (obj is DynamicNode.Unary) return OnParseUnary((DynamicNode.Unary)obj, pc, nulls);
					if (obj is DynamicNode.Binary) return OnParseBinary((DynamicNode.Binary)obj, pc, nulls);
					if (obj is DynamicNode.Method) return OnParseMethod((DynamicNode.Method)obj, pc, nulls);
					if (obj is DynamicNode.Invoke) return OnParseInvoke((DynamicNode.Invoke)obj, pc, nulls);
					if (obj is DynamicNode.Convert) return OnParseConvert((DynamicNode.Convert)obj, pc, nulls);
				}

				return OnParseConstant(obj, pc);
			};

			if (obj == null && !nulls) throw new ArgumentNullException("obj", "Null nodes are not accepted.");

			var str = dispatch(); if (parser != null) parser.Dispose();
			return str;
		}

		/// <summary>
		/// Parsers a command provider by generating the underlying command and disposing it
		/// afterwards.
		/// </summary>
		public virtual string OnParseCoreCommandProvider(ICoreCommandProvider obj, IParameterCollection pc, bool nulls)
		{
			var cmd = obj.GenerateCoreCommand();
			if (cmd == null) return string.Empty;

			var str = OnParseCommand(cmd, pc, nulls);
			cmd.Dispose();
			return str;
		}

		/// <summary>
		/// Parsers a command capturing its original parameters if the receiving collection
		/// is not null.
		/// </summary>
		public virtual string OnParseCommand(ICommand obj, IParameterCollection pc, bool nulls)
		{
			var str = obj.GetCommandText(iterable: false);
			if (str == null) return string.Empty;

			if (obj.Parameters != null && obj.Parameters.Count != 0)
			{
				if (pc != null) // Capturing the original parameters...
				{
					foreach (var par in obj.Parameters)
					{
						var item = pc.AddCreate(par.Value);
						str = str.Replace(par.Name, item.Name);
					}
				}
				else // Border case: no receiving collection...
				{
					foreach (var par in obj.Parameters)
					{
						var value = Engine.TryTransform(par.Value);
						str = str.Replace(par.Name, Parse(value, null, nulls));
					}
				}
			}

			return str;
		}

		/// <summary>
		/// Parsers a value or constant, capturing it into a parameter if such collection is
		/// available.
		/// </summary>
		protected virtual string OnParseConstant(object obj, IParameterCollection pc)
		{
			if (obj == null) return OnParseNull();

			if (pc != null) return pc.AddCreate(obj).Name;
			else return obj.ToString();
		}

		/// <summary>
		/// Parsers a null value.
		/// </summary>
		protected virtual string OnParseNull()
		{
			return "NULL";
		}

		/// <summary>
		/// Parsers a dynamic argument.
		/// </summary>
		protected virtual string OnParseArgument(DynamicNode.Argument obj)
		{
			if (!Core.Parser.ComplexTags) return null;

			if (obj.Name.Length <= 1) return null;
			return obj.Name;
		}

		/// <summary>
		/// Parsers a get member operation.
		/// </summary>
		protected virtual string OnParseGetMember(DynamicNode.GetMember obj, IParameterCollection pc, bool nulls)
		{
			string host = obj.Host == null ? null : Parse(obj.Host, pc, nulls);
			string name = host == null ? obj.Name : "{0}.{1}".FormatWith(host, obj.Name);
			return name;
		}

		/// <summary>
		/// Parsers a set member operation.
		/// </summary>
		protected virtual string OnParseSetMember(DynamicNode.SetMember obj, IParameterCollection pc, bool nulls)
		{
			string host = obj.Host == null ? null : Parse(obj.Host, pc, nulls);
			string name = host == null ? obj.Name : "{0}.{1}".FormatWith(host, obj.Name);
			string value = Parse(obj.Value, pc, nulls);
			return "{0} = ({1})".FormatWith(name, value);
		}

		/// <summary>
		/// Parsers a get indexed operation.
		/// </summary>
		protected virtual string OnParseGetIndexedMember(DynamicNode.GetIndexed obj, IParameterCollection pc, bool nulls)
		{
			if (obj.Indexes == null || obj.Indexes.Length == 0) return string.Empty;

			StringBuilder sb = new StringBuilder(); foreach (var index in obj.Indexes)
			{
				if (index is string) sb.Append((string)index);
				else sb.Append(Parse(index, pc, nulls));
			}

			string host = obj.Host == null ? null : Parse(obj.Host, pc, nulls);
			string name = host == null ? sb.ToString() : "{0}{1}".FormatWith(host, sb.ToString());
			return name;
		}

		/// <summary>
		/// Parsers a set indexed operation.
		/// </summary>
		protected virtual string OnParseSetIndexedMember(DynamicNode.SetIndexed obj, IParameterCollection pc, bool nulls)
		{
			if (obj.Indexes == null || obj.Indexes.Length == 0) return string.Empty;

			StringBuilder sb = new StringBuilder(); foreach (var index in obj.Indexes)
			{
				if (index is string) sb.Append((string)index);
				else sb.Append(Parse(index, pc, nulls));
			}

			string host = obj.Host == null ? null : Parse(obj.Host, pc);
			string name = host == null ? sb.ToString() : "{0}{1}".FormatWith(host, sb.ToString());
			string value = Parse(obj.Value, pc, nulls);
			return "{0} = ({1})".FormatWith(name, value);
		}

		/// <summary>
		/// Parsers a binary operation.
		/// </summary>
		protected virtual string OnParseBinary(DynamicNode.Binary obj, IParameterCollection pc, bool nulls)
		{
			string oper = ""; switch (obj.Operation)
			{
				case ExpressionType.Add: oper = "+"; break;
				case ExpressionType.Subtract: oper = "-"; break;
				case ExpressionType.Multiply: oper = "*"; break;
				case ExpressionType.Divide: oper = "/"; break;
				case ExpressionType.Modulo: oper = "%"; break;
				case ExpressionType.Power: oper = "^"; break;

				case ExpressionType.And: oper = "AND"; break;
				case ExpressionType.Or: oper = "OR"; break;

				case ExpressionType.GreaterThan: oper = ">"; break;
				case ExpressionType.GreaterThanOrEqual: oper = ">="; break;
				case ExpressionType.LessThan: oper = "<"; break;
				case ExpressionType.LessThanOrEqual: oper = "<="; break;

				case ExpressionType.Equal: oper = (obj.Right == null) ? "IS" : "="; break;
				case ExpressionType.NotEqual: oper = (obj.Right == null) ? "IS NOT" : "!="; break;

				default:
					throw new ArgumentException("Not supported binary operation '{0}'.".FormatWith(obj));
			}
			string left = this.Parse(obj.Left, pc, nulls);
			string right = this.Parse(obj.Right, pc, nulls);
			return "({0} {1} {2})".FormatWith(left, oper, right);
		}

		/// <summary>
		/// Parsers an unary operation.
		/// </summary>
		protected virtual string OnParseUnary(DynamicNode.Unary obj, IParameterCollection pc, bool nulls)
		{
			switch (obj.Operation)
			{
				// This are artifacts generated by the parser...
				case ExpressionType.IsTrue:
				case ExpressionType.IsFalse:
					return this.Parse(obj.Target, pc, nulls);

				case ExpressionType.Not:
					return "(NOT {0})".FormatWith(Parse(obj.Target, pc, nulls));

				case ExpressionType.Negate:
					return "-{0}".FormatWith(this.Parse(obj.Target, pc, nulls));
			}
			throw new ArgumentException("Not supported unary operation '{0}'".FormatWith(obj));
		}

		/// <summary>
		/// Parses a method invocation.
		/// This method is used to implement the virtual extensions feature, including:
		/// <para>Argument level:</para>
		/// <para>- x.Not(...) => (NOT ...)</para>
		/// <para>- x.Distinct(expression) => DISTINCT expression</para>
		/// <para>Element level:</para>
		/// <para>- x.Element.As(name) => Element AS name</para>
		/// <para>- x.Element.In(arg, ...) => Element IN (arg, ...)</para>
		/// <para>- x.Element.NotIn(arg, ...) => NOT Element IN (arg, ...)</para>
		/// <para>- x.Element.InList(...) / .NotInList(...) => Interprets the single argument as a list</para>
		/// <para>- x.Element.Between(arg1, arg2) => Element BETWEEN arg1 AND arg2</para>
		/// <para>- x.Element.Like(arg) => Element LIKE arg</para>
		/// <para>- x.Element.NotLike(arg) => Element NOT LIKE arg</para>
		/// <para>Default case:</para>
		/// <para>- The default case where the name of the method and its arguments are parsed as-is.</para>
		/// </summary>
		protected virtual string OnParseMethod(DynamicNode.Method obj, IParameterCollection pc, bool nulls)
		{
			string name = obj.Name.ToUpper();
			string parent = obj.Host == null ? null : Parse(obj.Host, pc, nulls);
			string item = null;
			StringBuilder sb = new StringBuilder();
			string str = null;
			int i = 0;
			IEnumerable iter = null;

			// Root-level methods...
			if (parent == null)
			{
				switch (name)
				{
					case "NOT":
						if (obj.Arguments == null) throw new ArgumentException("NOT() argument list is null.");
						if (obj.Arguments.Length != 1) throw new ArgumentException("NOT() requires just one argument.");
						item = Parse(obj.Arguments[0], pc, nulls);
						return "(NOT {0})".FormatWith(item);

					case "DISTINCT":
						if (obj.Arguments == null) throw new ArgumentException("DISTINCT() argument list is null.");
						if (obj.Arguments.Length != 1) throw new ArgumentException("DISTINCT() requires just one argument.");
						item = Parse(obj.Arguments[0], pc, nulls);
						return "DISTINCT {0}".FormatWith(item);
				}
			}

			// Item-level methods...
			switch (name)
			{
				case "AS":
					if (obj.Arguments == null) throw new ArgumentException("AS() argument list is null.");
					if (obj.Arguments.Length != 1) throw new ArgumentException("AS() requires just one argument.");
					item = Parse(obj.Arguments[0], pc, nulls);
					return "{0} AS {1}".FormatWith(parent, item);

				case "IN":
					if (obj.Arguments == null) throw new ArgumentException("IN() argument list is null.");
					if (obj.Arguments.Length == 0) throw new ArgumentException("IN() requires at least one argument.");
					for (i = 0; i < obj.Arguments.Length; i++)
					{
						str = Parse(obj.Arguments[i], pc, nulls);
						item = item == null ? str : "{0}, {1}".FormatWith(item, str);
					}
					return "{0} IN ({1})".FormatWith(parent, item);

				case "NOTIN":
					if (obj.Arguments == null) throw new ArgumentException("NOTIN() argument list is null.");
					if (obj.Arguments.Length == 0) throw new ArgumentException("NOTIN() requires at least one argument.");
					for (i = 0; i < obj.Arguments.Length; i++)
					{
						str = Parse(obj.Arguments[i], pc, nulls);
						item = item == null ? str : "{0}, {1}".FormatWith(item, str);
					}
					return "NOT {0} IN ({1})".FormatWith(parent, item);

				case "INLIST":
					if (obj.Arguments == null) throw new ArgumentException("INLIST() argument list is null.");
					if (obj.Arguments.Length != 1) throw new ArgumentException("INLIST() requires just one argument.");
					iter = obj.Arguments[0] as IEnumerable; if (iter == null) throw new ArgumentException("Argument '{0}' is not an iterable one.".FormatWith(obj.Arguments[0].Sketch()));
					foreach (var temp in iter)
					{
						str = Parse(temp, pc, nulls);
						item = item == null ? str : "{0}, {1}".FormatWith(item, str);
					}
					return "{0} IN ({1})".FormatWith(parent, item);

				case "NOTINLIST":
					if (obj.Arguments == null) throw new ArgumentException("NOTINLIST() argument list is null.");
					if (obj.Arguments.Length != 1) throw new ArgumentException("NOTINLIST() requires just one argument.");
					iter = obj.Arguments[0] as IEnumerable; if (iter == null) throw new ArgumentException("Argument '{0}' is not an iterable one.".FormatWith(obj.Arguments[0].Sketch()));
					foreach (var temp in iter)
					{
						str = Parse(temp, pc, nulls);
						item = item == null ? str : "{0}, {1}".FormatWith(item, str);
					}
					return "NOT {0} IN ({1})".FormatWith(parent, item);

				case "BETWEEN":
					if (obj.Arguments == null) throw new ArgumentException("BETWEEN() argument list is null.");
					if (obj.Arguments.Length != 2) throw new ArgumentException("BETWEEN() requires two arguments.");
					item = Parse(obj.Arguments[0], pc, nulls);
					str = Parse(obj.Arguments[1], pc, nulls);
					return "{0} BETWEEN ({1}) AND ({2})".FormatWith(parent, item, str);

				case "LIKE":
					if (obj.Arguments == null) throw new ArgumentException("LIKE() argument list is null.");
					if (obj.Arguments.Length != 1) throw new ArgumentException("LIKE() requires just one argument.");
					item = Parse(obj.Arguments[0], pc, nulls);
					return "{0} LIKE ({1})".FormatWith(parent, item);

				case "NOTLIKE":
					if (obj.Arguments == null) throw new ArgumentException("NOTLIKE() argument list is null.");
					if (obj.Arguments.Length != 1) throw new ArgumentException("NOTLIKE() requires just one argument.");
					item = Parse(obj.Arguments[0], pc, nulls);
					return "{0} NOT LIKE ({1})".FormatWith(parent, item);
			}

			// Intercepting "rounded" escape syntax because the "tag" is interpreted as a method name...
			DynamicNode node = obj; while (node.Host != null) node = node.Host;
			if (((DynamicNode.Argument)node).Name == obj.Name)
			{
				node = new DynamicNode.Invoke((DynamicNode.Argument)node, obj.Arguments);
				item = OnParseInvoke((DynamicNode.Invoke)node, pc, nulls);
				node.Dispose();

				string host = obj.Host == null ? null : Parse(obj.Host, pc, nulls);
				string temp = host == null ? item : "{0}{1}".FormatWith(host, item);
				return temp;
			}

			// Default case...
			name = parent == null ? obj.Name : "{0}.{1}".FormatWith(parent, obj.Name);
			sb.AppendFormat("{0}(", name); if (obj.Arguments != null)
			{
				for (i = 0; i < obj.Arguments.Length; i++)
				{
					if (i != 0) sb.Append(", ");
					sb.Append(Parse(obj.Arguments[i], pc, nulls));
				}
			}
			sb.Append(")");
			return sb.ToString();
		}

		/// <summary>
		/// Parsers an invocation operation.
		/// </summary>
		protected virtual string OnParseInvoke(DynamicNode.Invoke obj, IParameterCollection pc, bool nulls)
		{
			if (obj.Arguments == null || obj.Arguments.Length == 0) return string.Empty;

			StringBuilder sb = new StringBuilder(); foreach (var arg in obj.Arguments)
			{
				if (arg is string) sb.Append((string)arg);
				else sb.Append(Parse(arg, pc, nulls));
			}

			string host = obj.Host == null ? null : Parse(obj.Host, pc, nulls);
			string name = host == null ? sb.ToString() : "{0}{1}".FormatWith(host, sb.ToString());
			return name;
		}

		/// <summary>
		/// Parsers a conversion operation.
		/// </summary>
		protected virtual string OnParseConvert(DynamicNode.Convert obj, IParameterCollection pc, bool nulls)
		{
			return Parse(obj.Target, pc, nulls);
		}
	}
}
