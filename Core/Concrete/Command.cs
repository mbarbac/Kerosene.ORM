namespace Kerosene.ORM.Core.Concrete
{
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents an abstract command to be executed against a database-alike service.
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
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		ICommand ICommand.Clone()
		{
			throw new NotSupportedException(
				"Abstract ICoreCommand::{0}.Clone() invoked.".FormatWith(GetType().EasyName()));
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
		/// The database-alike service link this instance is associated with.
		/// </summary>
		public IDataLink Link
		{
			get { return _Link; }
		}

		/// <summary>
		/// Whether the state and contents maintained in this instance permits the execution
		/// of this command or not.
		/// </summary>
		public abstract bool CanBeExecuted { get; }

		/// <summary>
		/// The collection of parameters of this command.
		/// </summary>
		public IParameterCollection Parameters
		{
			get { return _Parameters; }
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
		public abstract string GetCommandText(bool iterable);

		/// <summary>
		/// Generates a trace string for this command built by generating the actual text of the
		/// command in a syntax the underlying database can understand, and appending to it the
		/// name and value of parameters the command will use, if any.
		/// </summary>
		/// <param name="iterable">True to indicate the method to generate the enumerable version
		/// of the command, if possible, or false to generate the scalar one.</param>
		/// <returns>The requested trace string.</returns>
		public string TraceString(bool iterable)
		{
			var str = GetCommandText(iterable).NullIfTrimmedIsEmpty();
			var pars = Parameters;

			var temp = str ?? string.Empty;
			if (pars != null && pars.Count != 0) temp = string.Format("{0} -- {1}", temp, pars);

			if (IsDisposed) temp = string.Format("disposed::{0}({1})", GetType().EasyName(), temp);
			else if (str == null) temp = string.Format("empty::{0}({1})", GetType().EasyName(), temp);

			return temp;
		}

		/// <summary>
		/// Generates a trace string for this command built by generating the actual text of the
		/// command in a syntax the underlying database can understand, and appending to it the
		/// name and value of parameters the command will use, if any.
		/// <para>This method generates either the enumerable version of the command, or rather
		/// the scalar one, using the default version for its concrete type.</para>
		/// </summary>
		/// <returns>The requested trace string.</returns>
		public string TraceString()
		{
			bool iterable = (this is IQueryCommand || this is IRawCommand);
			return TraceString(iterable);
		}
	}
}
