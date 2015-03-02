// ======================================================== IInsertCommand.cs
namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents an insert operation against the underlying database.
	/// </summary>
	public interface IInsertCommand : ICommand, IEnumerableCommand, IScalarCommand, ITableNameProvider
	{
		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		new IInsertCommand Clone();

		/// <summary>
		/// Defines, or adds to a previous specification, the columns affected by this command
		/// along with its values.
		/// </summary>
		/// <param name="columns">A collection of dynamic lambda expressions resolving into the
		/// column and values affected by this command using a 'x => x.Column = Value' syntax,
		/// where the value part can be any valid SQL sentence.</param>
		/// <returns>This instance to permit a fluent syntax chaining.</returns>
		IInsertCommand Columns(params Func<dynamic, object>[] columns);
	}
}
// ======================================================== 
