// ======================================================== IMetaCommand.cs
namespace Kerosene.ORM.Maps
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;

	// ==================================================== 
	/// <summary>
	/// Represents a command that can be executed against the map it is associated with.
	/// </summary>
	public interface IMetaCommand : IDisposableEx
	{
		/// <summary>
		/// The map this command is associated with.
		/// </summary>
		IDataMap Map { get; }

		/// <summary>
		/// Whether the state and contents of this command permits its execution.
		/// </summary>
		bool CanBeExecuted { get; }

		/// <summary>
		/// Generates a trace string for this command.
		///  <para>The text returned might be incomplete and should not be used until the value of
		/// the '<see cref="CanBeExecuted"/>' property is true.</para>
		/// <returns>The requested trace string.</returns>
		string TraceString();
	}

	// ==================================================== 
	/// <summary>
	/// Represents a command that can be executed against the map it is associated with.
	/// </summary>
	public interface IMetaCommand<T> : IMetaCommand where T : class
	{
		/// <summary>
		/// The map this command is associated with.
		/// </summary>
		new IDataMap<T> Map { get; }
	}
}
// ======================================================== 
