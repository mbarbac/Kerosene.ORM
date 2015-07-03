using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Kerosene.ORM.Maps
{
	// ====================================================
	/// <summary>
	/// Represents an object able to execute a query command and return the entities produced
	/// as the result of that execution.
	/// </summary>
	public interface IDataQueryEnumerator : IEnumerator, IDisposableEx
	{
		/// <summary>
		/// The command associated with this instance.
		/// </summary>
		IDataQuery Command { get; }

		/// <summary>
		/// Executes the associated command and returns a list with the results.
		/// </summary>
		/// <returns>A list with the results of the execution.</returns>
		IList ToList();

		/// <summary>
		/// Executes the associated command and returns an array with the results.
		/// </summary>
		/// <returns>An array with the results of the execution.</returns>
		object[] ToArray();

		/// <summary>
		/// Executes the associated command and returns the first result produced from the
		/// database, or null if it produced no results.
		/// </summary>
		/// <returns>The first result produced, or null.</returns>
		object First();

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
		object Last();
	}

	// ==================================================== 
	/// <summary>
	/// Represents an object able to execute a query command and return the entities produced
	/// as the result of that execution.
	/// </summary>
	public interface IDataQueryEnumerator<T> : IDataQueryEnumerator, IEnumerator<T> where T : class
	{
		/// <summary>
		/// The command associated with this instance.
		/// </summary>
		new IDataQuery<T> Command { get; }

		/// <summary>
		/// Executes the associated command and returns a list with the results.
		/// </summary>
		/// <returns>A list with the results of the execution.</returns>
		new List<T> ToList();

		/// <summary>
		/// Executes the associated command and returns an array with the results.
		/// </summary>
		/// <returns>An array with the results of the execution.</returns>
		new T[] ToArray();

		/// <summary>
		/// Executes the associated command and returns the first result produced from the
		/// database, or null if it produced no results.
		/// </summary>
		/// <returns>The first result produced, or null.</returns>
		new T First();

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
		new T Last();
	}
}
