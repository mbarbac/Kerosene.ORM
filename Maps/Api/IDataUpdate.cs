// ======================================================== IDataUpdate.cs
namespace Kerosene.ORM.Maps
{
	using Kerosene.Tools;
	using System;

	// ==================================================== 
	/// <summary>
	/// Represents an insert operation for its associated entity.
	/// </summary>
	public interface IDataUpdate : IMetaOperation { }

	// ==================================================== 
	/// <summary>
	/// Represents an insert operation for its associated entity.
	/// </summary>
	public interface IDataUpdate<T> : IMetaOperation<T>, IDataUpdate where T : class { }
}
// ======================================================== 
