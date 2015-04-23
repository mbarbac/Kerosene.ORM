namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;

	// ==================================================== 
	/// <summary>
	/// Represents an abstract command that when executed against the underlying database-alike
	/// service it is associated with produces an integer as the result of its execution
	/// </summary>
	public interface IScalarCommand : ICommand
	{
		/// <summary>
		/// Creates a new object able to execute this command.
		/// </summary>
		/// <returns>A new executor.</returns>
		IScalarExecutor GetExecutor();

		/// <summary>
		/// Executes this command and returns the integer produced by that execution.
		/// </summary>
		/// <returns>An integer.</returns>
		int Execute();
	}
}
