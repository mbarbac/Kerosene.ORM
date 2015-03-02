// ======================================================== IDataQueryEnumerator.cs
namespace Kerosene.ORM.Maps
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;

	// ==================================================== 
	/// <summary>
	/// Represents an object able to execute a data query command and to enumerate through the
	/// entities produced by its execution.
	/// </summary>
	public interface IDataQueryEnumerator : IDisposableEx, IEnumerator
	{
		/// <summary>
		/// The query command this enumerator is associated with.
		/// </summary>
		IDataQuery Command { get; }

		/// <summary>
		/// Executes the associated query and returns a list containing the results.
		/// </summary>
		/// <returns>A list containing the results requested.</returns>
		IList ToList();

		/// <summary>
		/// Executes the associated query and returns aa arrays containing the results.
		/// </summary>
		/// <returns>An array containing the results requested.</returns>
		object[] ToArray();

		/// <summary>
		/// Executes the associated query and returns the first instance produced, or null if
		/// no one was produced.
		/// </summary>
		/// <returns>The first instance produced, or null.</returns>
		object First();

		/// <summary>
		/// Executes the associated query command and returns the first instance produced, or
		/// null if no one was produced.
		/// <para>
		/// This method is provided as a fall-back mechanism because it retrieves all possible
		/// results discarding them until the last one is found. Client applications have to
		/// reconsider the logic of their command to avoid using this method if possible.
		/// </para>
		/// </summary>
		/// <returns>The first instance produced, or null.</returns>
		object Last();
	}

	// ==================================================== 
	/// <summary>
	/// Represents an object able to execute a data query command and to enumerate through the
	/// entities produced by its execution.
	/// </summary>
	/// <typeparam name="T">The type of the entities managed by the associated map.</typeparam>
	public interface IDataQueryEnumerator<T> : IDataQueryEnumerator, IEnumerator<T> where T : class
	{
		/// <summary>
		/// The query command this enumerator is associated with.
		/// </summary>
		new IDataQuery<T> Command { get; }

		/// <summary>
		/// Executes the associated query and returns a list containing the results.
		/// </summary>
		/// <returns>A list containing the results requested.</returns>
		new List<T> ToList();

		/// <summary>
		/// Executes the associated query and returns aa arrays containing the results.
		/// </summary>
		/// <returns>An array containing the results requested.</returns>
		new T[] ToArray();

		/// <summary>
		/// Executes the associated query and returns the first instance produced, or null if
		/// no one was produced.
		/// </summary>
		/// <returns>The first instance produced, or null.</returns>
		new T First();

		/// <summary>
		/// Executes the associated query command and returns the first instance produced, or
		/// null if no one was produced.
		/// <para>
		/// This method is provided as a fall-back mechanism because it retrieves all possible
		/// results discarding them until the last one is found. Client applications have to
		/// reconsider the logic of their command to avoid using this method if possible.
		/// </para>
		/// </summary>
		/// <returns>The first instance produced, or null.</returns>
		new T Last();
	}
}
// ======================================================== 
