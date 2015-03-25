// ======================================================== IInsertCommand.cs
namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;

	// ==================================================== 
	/// <summary>
	/// Represents an insert command.
	/// </summary>
	public interface IInsertCommand : IEnumerableCommand, IScalarCommand, ITableNameProvider
	{
		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		new IInsertCommand Clone();

		/// <summary>
		/// Defines the names and values of the columns affected by this command, or adds the new
		/// ones to any previous one that may exist.
		/// </summary>
		/// <param name="columns">A collection of dynamic lambda expressions resolving into the
		/// column and values affected by this command using a 'x => x.Column = Value' syntax,
		/// where the value part can be any valid SQL sentence.</param>
		/// <returns>A self-reference to permit a fluent syntax chaining.</returns>
		IInsertCommand Columns(params Func<dynamic, object>[] columns);
	}
}
// ======================================================== 
