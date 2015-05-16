using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;

namespace Kerosene.ORM.Maps
{
	// ====================================================
	/// <summary>
	/// Represents a command related to the entities managed by the map it is associated with.
	/// </summary>
	public interface IMetaCommand : IDisposableEx, ICoreCommandProvider
	{
		/// <summary>
		/// The map this command is associated with.
		/// </summary>
		IDataMap Map { get; }

		/// <summary>
		/// Whether the state and contents maintained in this instance permits the execution
		/// of this command or not.
		/// </summary>
		bool CanBeExecuted { get; }

		/// <summary>
		/// Generates a trace string for this command built by generating the actual text of the
		/// command in a syntax the underlying database can understand, and appending to it the
		/// name and value of parameters the command will use, if any.
		/// </summary>
		/// <returns>The requested trace string.</returns>
		string TraceString();
	}

	// ====================================================
	/// <summary>
	/// Represents a command related to the entities managed by the map it is associated with.
	/// </summary>
	public interface IMetaCommand<T> : IMetaCommand where T : class
	{
		/// <summary>
		/// The map this command is associated with.
		/// </summary>
		new IDataMap<T> Map { get; }
	}
}
