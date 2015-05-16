using Kerosene.Tools;
using System;
using System.Linq;

namespace Kerosene.ORM.Core
{
	// ==================================================== 
	/// <summary>
	/// Represents an update command.
	/// </summary>
	public interface IUpdateCommand : IEnumerableCommand, IScalarCommand, ITableNameProvider
	{
		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		new IUpdateCommand Clone();

		/// <summary>
		/// Defines the contents of the WHERE clause or append the new ones to any previous
		/// specification.
		/// <para>By default if any previous contents exist the new ones are appended using an AND
		/// operator. However, the virtual extension methods 'x => x.And(...)' and 'x => x.Or(...)'
		/// can be used to specify what logical operator to use.</para>
		/// </summary>
		/// <param name="where">The dynamic lambda expression that resolves into the contents of
		/// this clause.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		IUpdateCommand Where(Func<dynamic, object> where);

		/// <summary>
		/// Defines the names and values of the columns affected by this command, or adds the new
		/// ones to any previous one that may exist.
		/// </summary>
		/// <param name="columns">A collection of dynamic lambda expressions resolving into the
		/// column and values affected by this command using a 'x => x.Column = Value' syntax,
		/// where the value part can be any valid SQL sentence.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		IUpdateCommand Columns(params Func<dynamic, object>[] columns);
	}
}
