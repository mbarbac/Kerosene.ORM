using Kerosene.Tools;
using System;
using System.Linq;

namespace Kerosene.ORM.Core.Concrete
{
	// ==================================================== 
	/// <summary>
	/// Represents an object able to execute a scalar command and to produce the integer
	/// resulting from this execution.
	/// </summary>
	public abstract class ScalarExecutor : IScalarExecutor
	{
		bool _IsDisposed = false;
		IScalarCommand _Command = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="cmd">The command this instance will be associated with.</param>
		protected ScalarExecutor(IScalarCommand cmd)
		{
			if (cmd == null) throw new ArgumentNullException("command", "Command cannot be null.");
			if (cmd.IsDisposed) throw new ObjectDisposedException(cmd.ToString());
			if (cmd.Link.IsDisposed) throw new ObjectDisposedException("Link '{0}' of command '{1}' is disposed.".FormatWith(cmd.Link, cmd));

			_Command = cmd;
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

		~ScalarExecutor()
		{
			if (!IsDisposed) OnDispose(false);
		}

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		protected virtual void OnDispose(bool disposing)
		{
			_Command = null;
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
				Command == null ? string.Empty : Command.ToString()); str = null;

			return IsDisposed ? string.Format("disposed::{0}", str) : str;
		}

		/// <summary>
		/// The command this instance is associated with.
		/// </summary>
		public IScalarCommand Command
		{
			get { return _Command; }
		}

		/// <summary>
		/// The link of the command this executor is associated with.
		/// </summary>
		public IDataLink Link
		{
			get { return _Command == null ? null : _Command.Link; }
		}

		/// <summary>
		/// Executes the associated command and returns the integer produced by that execution.
		/// </summary>
		/// <returns>An integer.</returns>
		public int Execute()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (_Command.IsDisposed) throw new ObjectDisposedException(_Command.ToString());
			if (_Command.Link.IsDisposed) throw new ObjectDisposedException(_Command.Link.ToString());

			if (!Command.CanBeExecuted) throw new CannotExecuteException(
				"Command '{0}' cannot be executed.".FormatWith(Command));

			return OnExecute();
		}

		/// <summary>
		/// Invoked to execute the associated command and returns the integer produced by that
		/// execution.
		/// </summary>
		protected abstract int OnExecute();
	}
}
