// ======================================================== IScalarCommand.cs
namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents a command that when executed will return an integer as the result of that
	/// execution.
	/// </summary>
	public interface IScalarCommand : ICommand
	{
		/// <summary>
		/// Creates a new object able to execute this command.
		/// </summary>
		/// <returns>A new executor.</returns>
		IScalarExecutor GetExecutor();

		/// <summary>
		/// Executes this command and returns the integer that execution produces.
		/// </summary>
		/// <returns>An integer.</returns>
		int Execute();
	}
}
// ======================================================== 
