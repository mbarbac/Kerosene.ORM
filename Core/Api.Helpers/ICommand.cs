// ======================================================== ICommand.cs
namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;

	// ==================================================== 
	/// <summary>
	/// Represents an abstract command to be executed against the underlying database-alike
	/// service it is associated with.
	/// </summary>
	public interface ICommand : IDisposableEx, ICloneable
	{
		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		new ICommand Clone();

		/// <summary>
		/// The database-alike service link this instance is associated with.
		/// </summary>
		IDataLink Link { get; }

		/// <summary>
		/// Generates a trace string for this command built by generating the actual text of the
		/// command in a syntax the underlying database can understand, and appending to it the
		/// name and value of parameters the command will use, if any.
		/// </summary>
		/// <param name="iterable">True to indicate the method to generate the enumerable version
		/// of the command, if possible, or false to generate the scalar one.</param>
		/// <returns>The requested trace string.</returns>
		string TraceString(bool iterable);

		/// <summary>
		/// Generates a trace string for this command built by generating the actual text of the
		/// command in a syntax the underlying database can understand, and appending to it the
		/// name and value of parameters the command will use, if any.
		/// <para>This method generates either the enumerable version of the command, or rather
		/// the scalar one, using the default version for its concrete type.</para>
		/// </summary>
		/// <returns>The requested trace string.</returns>
		string TraceString();

		/// <summary>
		/// Whether the state and contents maintained in this instance permits the execution
		/// of this command or not.
		/// </summary>
		bool CanBeExecuted { get; }

		/// <summary>
		/// The collection of parameters of this command.
		/// </summary>
		IParameterCollection Parameters { get; }

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
	}
}
// ======================================================== 
