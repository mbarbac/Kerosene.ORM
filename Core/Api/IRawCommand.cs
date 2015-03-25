// ======================================================== IRawCommand.cs
namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;

	// ==================================================== 
	/// <summary>
	/// Represents a command whose contents can be set explicitly as needed.
	/// <para>Instances of this type are usefull to express logic not supported by other command
	/// types, as for instance specific functions or constructions supported by the dialect of
	/// the underlying database engine, and also to invoke stored procedures.</para>
	/// </summary>
	public interface IRawCommand : IEnumerableCommand, IScalarCommand
	{
		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		new IRawCommand Clone();

		/// <summary>
		/// Sets the contents of this command with the text and arguments given. Any previous
		/// contents and arguments are removed.
		/// </summary>
		/// <param name="text">The new text of the command. Embedded arguments are specified
		/// using the standard '{n}' positional format.</param>
		/// <param name="args">An optional collection containing the arguments specified in the
		/// text set into this command.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		IRawCommand Set(string text, params object[] args);

		/// <summary>
		/// Sets the contents of this command parsing the dynamic lambda expression given. Any
		/// previous contents and arguments are removed.
		/// </summary>
		/// <param name="spec">A dynamic lambda expression that resolves into the logic of this
		/// command. Embedded arguments are extracted and captured automatically in order to
		/// avoid injection attacks.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		IRawCommand Set(Func<dynamic, object> spec);

		/// <summary>
		/// Appends to the previous contents the new text and arguments given.
		/// </summary>
		/// <param name="text">The text to append to this command. Embedded arguments are specified
		/// using the standard '{n}' positional format.</param>
		/// <param name="args">An optional collection containing the arguments specified in the
		/// text to append to this command.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		IRawCommand Append(string text, params object[] args);

		/// <summary>
		/// Appends to the previous contents the new ones obtained by parsing the dynamic lambda
		/// expression given.
		/// </summary>
		/// <param name="spec">A dynamic lambda expression that resolves into the logic of this
		/// command. Embedded arguments are extracted and captured automatically in order to
		/// avoid injection attacks.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		IRawCommand Append(Func<dynamic, object> spec);
	}
}
// ======================================================== 
