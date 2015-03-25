// ======================================================== IMetaOperation.cs
namespace Kerosene.ORM.Maps
{
	using Kerosene.Tools;
	using System;

	// ==================================================== 
	/// <summary>
	/// Represents a change operation associated with a given entity.
	/// </summary>
	public interface IMetaOperation : IMetaCommand
	{
		/// <summary>
		/// The entity affected by this operation.
		/// </summary>
		object Entity { get; }

		/// <summary>
		/// Whether this operation has been submitted or not.
		/// </summary>
		bool IsSubmitted { get; }

		/// <summary>
		/// Submits this operation so that it will be executed, along with all other pending
		/// change operations on its associated repository, when it executes then all against
		/// the underlying database as a single logic unit.
		/// </summary>
		void Submit();
	}

	// ==================================================== 
	/// <summary>
	/// Represents a change operation associated with a given entity.
	/// </summary>
	public interface IMetaOperation<T> : IMetaOperation where T : class
	{
		/// <summary>
		/// The entity affected by this operation.
		/// </summary>
		new T Entity { get; }
	}
}
// ======================================================== 
