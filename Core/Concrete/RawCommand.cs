// ======================================================== RawCommand.cs
namespace Kerosene.ORM.Core.Concrete
{
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents a command whose contents can be set explicitly as needed.
	/// <para>Instances of this type are usefull to express logic not supported by other command
	/// types, as for instance specific functions or constructions supported by the dialect of
	/// the underlying database engine, and also to invoke stored procedures.</para>
	/// </summary>
	public class RawCommand : CommandEnumSca, IRawCommand
	{
		protected string TheTextData = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="link">The link this instance will be associated with.</param>
		public RawCommand(IDataLink link) : base(link) { }

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
		public RawCommand Clone()
		{
			var cloned = new RawCommand(Link); OnClone(cloned);
			return cloned;
		}
		IRawCommand IRawCommand.Clone()
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
			var temp = cloned as RawCommand;
			if (cloned == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' one."
				.FormatWith(cloned.Sketch(), typeof(RawCommand).EasyName()));

			temp.TheTextData = TheTextData;
		}

		/// <summary>
		/// Whether the state and contents of this command permits its execution.
		/// </summary>
		public override bool CanBeExecuted
		{
			get { return (IsDisposed || TheTextData == null) ? false : true; }
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
			return TheTextData ?? string.Empty;
		}

		/// <summary>
		/// Sets the contents of this command with the text and arguments given. Any previous
		/// contents and arguments are removed.
		/// </summary>
		/// <param name="text">The new text of the command. Embedded arguments are specified
		/// using the standard '{n}' positional format.</param>
		/// <param name="args">An optional collection containing the arguments specified in the
		/// text set into this command.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		public IRawCommand Set(string text, params object[] args)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			TheTextData = null;
			Parameters.Clear();

			return this.Append(text, args);
		}

		/// <summary>
		/// Sets the contents of this command parsing the dynamic lambda expression given. Any
		/// previous contents and arguments are removed.
		/// </summary>
		/// <param name="spec">A dynamic lambda expression that resolves into the logic of this
		/// command. Embedded arguments are extracted and captured automatically in order to
		/// avoid injection attacks.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		public IRawCommand Set(Func<dynamic, object> spec)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			TheTextData = null;
			Parameters.Clear();

			return this.Append(spec);
		}

		/// <summary>
		/// Appends to the previous contents the new text and arguments given.
		/// </summary>
		/// <param name="text">The text to append to this command. Embedded arguments are specified
		/// using the standard '{n}' positional format.</param>
		/// <param name="args">An optional collection containing the arguments specified in the
		/// text to append to this command.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		public IRawCommand Append(string text, params object[] args)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (text == null) throw new ArgumentNullException("spec", "Text cannot be null.");

			if (args != null)
			{
				for (int i = 0; i < args.Length; i++)
				{
					IParameter par = Parameters.AddCreate(args[i] is IParameter
						? ((IParameter)args[i]).Value
						: args[i]);

					var old = "{{{0}}}".FormatWith(i);
					text = text.Replace(old, par.Name);
				}
			}

			TheTextData = TheTextData == null ? text : string.Format("{0}{1}", TheTextData, text);
			return this;
		}

		/// <summary>
		/// Appends to the previous contents the new ones obtained by parsing the dynamic lambda
		/// expression given.
		/// </summary>
		/// <param name="spec">A dynamic lambda expression that resolves into the logic of this
		/// command. Embedded arguments are extracted and captured automatically in order to
		/// avoid injection attacks.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		public IRawCommand Append(Func<dynamic, object> spec)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (spec == null) throw new ArgumentNullException("spec", "Specification cannot be null.");

			var text = Link.Engine.Parser.Parse(spec, Parameters);
			TheTextData = TheTextData == null
				? text
				: string.Format("{0}{1}", TheTextData, text);

			return this;
		}
	}
}
// ======================================================== 
