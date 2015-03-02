// ======================================================== ICommand.cs
namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents an abstract command that can be executed against an underlying database-alike
	/// service.
	/// </summary>
	public interface ICommand : IDisposableEx, ICloneable
	{
		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		new ICommand Clone();

		/// <summary>
		/// The data link this command is associated with.
		/// </summary>
		IDataLink Link { get; }

		/// <summary>
		/// The collection of parameters of this command.
		/// </summary>
		IParameterCollection Parameters { get; }

		/// <summary>
		/// Whether the state and contents of this command permits its execution.
		/// </summary>
		bool CanBeExecuted { get; }

		/// <summary>
		/// Generates a string containing the command to be executed on the underlying database.
		/// <para>The text returned might be incomplete and should not be used until the value of
		/// the '<see cref="CanBeExecuted"/>' property is true.</para>
		/// </summary>
		/// <param name="iterable">True to generate the iterable version, false to generate the
		/// scalar one.</param>
		/// <returns>The requested command string.</returns>
		/// <remarks>This method must not throw an exception if this instance is disposed.</remarks>
		string GetCommandText(bool iterable);

		/// <summary>
		/// Generates a trace string for this command.
		/// <para>The text returned might be incomplete and should not be used until the value of
		/// the '<see cref="CanBeExecuted"/>' property is true.</para>
		/// </summary>
		/// <param name="iterable">True to generate the iterable version, false to generate the
		/// scalar one.</param>
		/// <returns>The requested trace string.</returns>
		string TraceString(bool iterable);

		/// <summary>
		/// Generates a trace string for this command.
		/// <para>The text returned might be incomplete and should not be used until the value of
		/// the '<see cref="CanBeExecuted"/>' property is true.</para>
		/// </summary>
		/// <returns>The requested trace string.</returns>
		string TraceString();
	}
}
// ======================================================== 
