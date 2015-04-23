namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;

	// ==================================================== 
	/// <summary>
	/// Represents an abstract command that when executed against the underlying database-alike
	/// service it is associated with produces a collection of records as the result of its
	/// execution
	/// </summary>
	public interface IEnumerableCommand : ICommand, IEnumerable
	{
		/// <summary>
		/// Creates a new object able to execute this command.
		/// </summary>
		/// <returns>A new enumerator.</returns>
		new IEnumerableExecutor GetEnumerator();

		/// <summary>
		/// Executes this command and returns a list with the results.
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
		/// - Note that the concrete implementation of this method may emulate this capability
		/// by retrieving all possible records and discarding them until the last one is found.
		/// Client applications may want to modify the logic of the command to avoid using it.
		/// </para>
		/// </summary>
		/// <returns>The first result produced, or null.</returns>
		object Last();

		/// <summary>
		/// Creates a new enumerator with an associated converter delegate.
		/// </summary>
		/// <param name="converter">The delegate to use to convert the current record into any
		/// object or reference that will become the 'Current' property while at the current
		/// iteration.</param>
		/// <returns>A new enumerator.</returns>
		IEnumerableExecutor ConvertBy(Func<IRecord, object> converter);

		/// <summary>
		/// Creates a new enumerator that returns the strong typed instances that results from
		/// the conversion of the records produced by the database into instances of the given
		/// type. The values of its public properties and fields are populated with the ones of
		/// the matching columns from the database.
		/// </summary>
		/// <typeparam name="T">The type of the receiving instances.</typeparam>
		/// <returns>A new enumerator.</returns>
		IEnumerableExecutorTo<T> ConvertTo<T>() where T : class;
	}
}
