// ======================================================== IScalarExecutor.cs
namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents an object able to execute a scalar command and to return the interger
	/// produced by that execution.
	/// </summary>
	public interface IScalarExecutor : IDisposableEx
	{
		/// <summary>
		/// The command this instance is associated with.
		/// </summary>
		IScalarCommand Command { get; }

		/// <summary>
		/// Executes the associated command and returns the integer produced by that execution.
		/// </summary>
		/// <returns>An integer.</returns>
		int Execute();
	}
}
// ======================================================== 
