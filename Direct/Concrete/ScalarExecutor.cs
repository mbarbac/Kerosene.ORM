// ======================================================== ScalarExecutor.cs
namespace Kerosene.ORM.Direct.Concrete
{
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents an object able to execute a scalar command and to produce the integer
	/// resulting from this execution.
	/// </summary>
	public class ScalarExecutor : Core.Concrete.ScalarExecutor, IScalarExecutor
	{
		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="cmd">The command this instance will be associated with.</param>
		public ScalarExecutor(Core.IScalarCommand cmd)
			: base(cmd)
		{
			var link = base.Link as IDataLink;
			if (link == null) throw new InvalidOperationException(
				"Link '{0}' of command '{1}' is not a direct link.".FormatWith(base.Link, cmd));
		}

		/// <summary>
		/// The link of the command this executor is associated with.
		/// </summary>
		public new IDataLink Link
		{
			get { return (IDataLink)base.Link; }
		}

		/// <summary>
		/// Invoked to execute the associated command and returns the integer produced by that
		/// execution.
		/// </summary>
		protected override int OnExecute()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (Command.IsDisposed) throw new ObjectDisposedException(Command.ToString());
			if (Command.Link.IsDisposed) throw new ObjectDisposedException(Command.Link.ToString());

			var surrogate = new SurrogateDirect(Command);
			int result = surrogate.OnExecuteScalar();

			return result;
		}
	}
}
// ======================================================== 
