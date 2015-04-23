namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;

	// ==================================================== 
	/// <summary>
	/// Represents an object able to execute an enumerable command and to produce a collection
	/// of strong-typed instances that result from this execution. Public properties and fields
	/// of the receiving type are populated with the values of the matching columns from the
	/// records obtained from the database.
	/// </summary>
	public interface IEnumerableExecutorTo<T> : IDisposableEx, IEnumerator<T> where T : class
	{
		/// <summary>
		/// Returns a new enumerator for this instance.
		/// <para>Hack to permit this instance to be enumerated in order to simplify its usage
		/// and syntax.</para>
		/// </summary>
		/// <returns>A self-reference.</returns>
		IEnumerableExecutorTo<T> GetEnumerator();

		/// <summary>
		/// The command this instance is associated with.
		/// </summary>
		IEnumerableCommand Command { get; }

		/// <summary>
		/// Gets the schema of the records produced by the execution of the associated command.
		/// This property is null until the command has been executed, or when this instance
		/// has been disposed.
		/// </summary>
		ISchema Schema { get; }

		/// <summary>
		/// Gets the current record produced by the last iteration of the command. This property
		/// is null if this instance is disposed, if the command has not been executed yet, or
		/// if there are no more records available.
		/// </summary>
		IRecord CurrentRecord { get; }

		/// <summary>
		/// Executes the associated command and returns a list with the results.
		/// </summary>
		/// <returns>A list with the results of the execution.</returns>
		List<T> ToList();

		/// <summary>
		/// Executes the associated command and returns an array with the results.
		/// </summary>
		/// <returns>An array with the results of the execution.</returns>
		T[] ToArray();

		/// <summary>
		/// Executes the associated command and returns the first result produced from the
		/// database, or null if it produced no results.
		/// </summary>
		/// <returns>The first result produced, or null.</returns>
		T First();

		/// <summary>
		/// Executes the associated command and returns the last result produced from the
		/// database, or null if it produced no results.
		/// <para>
		/// - Note that the concrete implementation of this method may emulate this capability
		/// by retrieving all possible records and discarding them until the last one is found.
		/// Client applications may want to modify the logic of the command to avoid using it.
		/// </para>
		/// </summary>
		/// <returns>The first result produced, or null.</returns>
		T Last();
	}
}
