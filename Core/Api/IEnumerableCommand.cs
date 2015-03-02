// ======================================================== IEnumerableCommand.cs
namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents a command that when executed will produce a collection of records as the
	/// result of that execution.
	/// </summary>
	public interface IEnumerableCommand : ICommand, IEnumerable
	{
		/// <summary>
		/// Creates a new object able to execute this command.
		/// </summary>
		/// <returns>A new enumerator.</returns>
		new IEnumerableExecutor GetEnumerator();

		/// <summary>
		/// Creates a new enumerator for this command and sets its converter in the same
		/// operation.
		/// </summary>
		/// <param name="converter">The converter to set, or null to clear it.</param>
		/// <returns>The new enumerator.</returns>
		IEnumerableExecutor ConvertBy(Func<IRecord, object> converter);

		/// <summary>
		/// Executes this and returns a list with the results.
		/// </summary>
		/// <returns>A list with the results of the execution.</returns>
		List<object> ToList();

		/// <summary>
		/// Executes this command and returns an array with the results.
		/// </summary>
		/// <returns>An array with the results of the execution.</returns>
		object[] ToArray();

		/// <summary>
		/// Executes this command and returns the first result produced from the database, or
		/// null if it produced no results.
		/// </summary>
		/// <returns>The first result produced, or null.</returns>
		object First();

		/// <summary>
		/// Executes this command and returns the last result produced from the database, or
		/// null if it produced no results.
		/// <para>
		/// This method is provided as a fall-back mechanism as it retrieves all possible results
		/// discarding them until the last one is found. Client applications may want to modify
		/// the logic of the command to avoid using it.
		/// </para>
		/// </summary>
		/// <returns>The first result produced, or null.</returns>
		object Last();
	}
}
// ======================================================== 
