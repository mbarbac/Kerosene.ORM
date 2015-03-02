// ======================================================== IRawCommand.cs
namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents a generic command where its contents can be explicitly set as needed.
	/// <para>Instances of this type are usefull to express logic not supported by other command
	/// types, and also to invoke stored procedures.</para>
	/// </summary>
	public interface IRawCommand : ICommand, IEnumerableCommand, IScalarCommand
	{
		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		new IRawCommand Clone();

		/// <summary>
		/// Sets the contents of this command removing any previous ones it may had.
		/// </summary>
		/// <param name="text">The new text of the command. Embedded arguments are specified
		/// using the standard positional '{n}' format.</param>
		/// <param name="args">An optional collection containing the arguments to be used by
		/// this command.</param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IRawCommand Set(string text, params object[] args);

		/// <summary>
		/// Sets the contents of this command removing any previous ones it may had.
		/// </summary>
		/// <param name="spec">A dynamic lambda expression that when parsed specified the new
		/// contents of this command.</param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IRawCommand Set(Func<dynamic, object> spec);

		/// <summary>
		/// Appends to the contents of the command the new ones specified.
		/// </summary>
		/// <param name="text">The text to append to this command. Embedded arguments are
		/// specified using the standard positional '{n}' format.</param>
		/// <param name="args">An optional collection containing the arguments to be used by
		/// this command.</param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IRawCommand Append(string text, params object[] args);

		/// <summary>
		/// Appends to the contents of the command the new ones specified.
		/// </summary>
		/// <param name="spec">A dynamic lambda expression that when parsed specified the contents
		/// to be appended to the previous ones of this command.</param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IRawCommand Append(Func<dynamic, object> spec);
	}
}
// ======================================================== 
