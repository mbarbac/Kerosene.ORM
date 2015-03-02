// ======================================================== Command.cs
namespace Kerosene.ORM.Core.Concrete
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Represents an abstract command that can be executed against an underlying database-alike
	/// service.
	/// </summary>
	public abstract class Command : ICommand
	{
		bool _IsDisposed = false;
		IDataLink _Link = null;
		IParameterCollection _Parameters = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="link">The link this command is associated with.</param>
		protected Command(IDataLink link)
		{
			if (link == null) throw new ArgumentNullException("link", "Link cannot be null.");
			if (link.IsDisposed) throw new ObjectDisposedException(link.ToString());
			_Link = link;

			_Parameters = _Link.Engine.CreateParameterCollection();
			if (_Parameters == null) throw new CannotCreateException(
				"Cannot create a new collection of parameters for this instance.");
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

		~Command()
		{
			if (!IsDisposed) OnDispose(false);
		}

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		protected virtual void OnDispose(bool disposing)
		{
			if (disposing)
			{
				if (_Parameters != null && !_Parameters.IsDisposed) _Parameters.Dispose();
			}
			_Parameters = null;
			_Link = null;

			_IsDisposed = true;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			return this.TraceString();
		}

		/// <summary>
		/// Generates a trace string for this command.
		/// <para>The text returned might be incomplete and should not be used until the value of
		/// the '<see cref="CanBeExecuted"/>' property is true.</para>
		/// </summary>
		/// <param name="iterable">True to generate the iterable version, false to generate the
		/// scalar one.</param>
		/// <returns>The requested trace string.</returns>
		public string TraceString(bool iterable)
		{
			var str = GetCommandText(iterable);
			var pars = Parameters;

			var temp = IsDisposed
				? string.Format("disposed::{0}({1})", GetType().EasyName(), str)
				: (str.NullIfTrimmedIsEmpty() == null
					? string.Format("empty::{0}", GetType().EasyName())
					: str);

			if (pars != null && pars.Count != 0) temp = string.Format("{0} -- {1}", temp, pars);
			return temp;
		}

		/// <summary>
		/// Generates a trace string for this command.
		/// <para>The text returned might be incomplete and should not be used until the value of
		/// the '<see cref="CanBeExecuted"/>' property is true.</para>
		/// </summary>
		/// <returns>The requested trace string.</returns>
		public string TraceString()
		{
			bool iterable = (this is IRawCommand);
			return TraceString(iterable);
		}

		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		ICommand ICommand.Clone()
		{
			throw new NotSupportedException(
				"Abstract ICommand::{0}.Clone() invoked.".FormatWith(GetType().EasyName()));
		}
		object ICloneable.Clone()
		{
			throw new NotSupportedException(
				"Abstract ICloneable::{0}.Clone() invoked.".FormatWith(GetType().EasyName()));
		}

		/// <summary>
		/// Invoked when cloning this object to set its state at this point of the inheritance
		/// chain.
		/// </summary>
		/// <param name="cloned">The cloned object.</param>
		protected virtual void OnClone(object cloned)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			var temp = cloned as Command;
			if (cloned == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' one."
				.FormatWith(cloned.Sketch(), typeof(Command).EasyName()));

			temp.Parameters.AddRange(this.Parameters, cloneNotOrphans: true);
		}

		/// <summary>
		/// The data link this command is associated with.
		/// </summary>
		public IDataLink Link
		{
			get { return _Link; }
		}

		/// <summary>
		/// The collection of parameters of this command.
		/// </summary>
		public IParameterCollection Parameters
		{
			get { return _Parameters; }
		}

		/// <summary>
		/// Whether the state and contents of this command permits its execution.
		/// </summary>
		public abstract bool CanBeExecuted { get; }

		/// <summary>
		/// Generates a string containing the command to be executed on the underlying database.
		/// <para>The text returned might be incomplete and should not be used until the value of
		/// the '<see cref="CanBeExecuted"/>' property is true.</para>
		/// </summary>
		/// <param name="iterable">True to generate the iterable version, false to generate the
		/// scalar one.</param>
		/// <returns>The requested command string.</returns>
		/// <remarks>This method must not throw an exception if this instance is disposed.</remarks>
		public abstract string GetCommandText(bool iterable);
	}
}
// ======================================================== 
