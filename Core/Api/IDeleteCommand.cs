// ======================================================== IDeleteCommand.cs
namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents a delete operation against the underlying database.
	/// </summary>
	public interface IDeleteCommand : ICommand, IEnumerableCommand, IScalarCommand, ITableNameProvider
	{
		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		new IDeleteCommand Clone();

		/// <summary>
		/// Defines the contents of the WHERE clause, or appends the new ones to any previous
		/// specification.
		/// </summary>
		/// <param name="where">The dynamic lambda expression that resolves into the contents of
		/// this clause.
		/// <para>- By default, if any previous contents exist the new ones are appended using an
		/// AND logical operator. However, the virtual extension methods 'x => x.And(...)' and
		/// 'x => x.Or(...)' can be used to specify the concrete logical operator to use for
		/// concatenation purposes.</para>
		/// </param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IDeleteCommand Where(Func<dynamic, object> where);
	}
}
// ======================================================== 
