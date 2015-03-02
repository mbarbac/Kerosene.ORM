// ======================================================== CommandEnumExec.cs
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
	/// Represents an abstract enumerable and exectutable command that can be executed against an
	/// underlying database-alike service.
	/// </summary>
	public abstract class CommandEnumSca : CommandEnum, IScalarCommand
	{
		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="link">The link this command is associated with.</param>
		protected CommandEnumSca(IDataLink link) : base(link) { }

		/// <summary>
		/// Creates a new object able to execute this command.
		/// </summary>
		/// <returns>A new executor.</returns>
		public IScalarExecutor GetExecutor()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return Link.CreateScalarExecutor(this);
		}

		/// <summary>
		/// Executes this command and returns the integer that execution produces.
		/// </summary>
		/// <returns>An integer.</returns>
		public int Execute()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			var x = GetExecutor();
			var r = x.Execute();

			x.Dispose();
			return r;
		}
	}
}
// ======================================================== 
