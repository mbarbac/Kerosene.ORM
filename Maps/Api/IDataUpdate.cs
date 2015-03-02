// ======================================================== IDataUpdate.cs
namespace Kerosene.ORM.Maps
{
	using Kerosene.Tools;
	using System;

	// ==================================================== 
	/// <summary>
	/// Represents an update operation for its associated entity.
	/// </summary>
	public interface IDataUpdate : IMetaOperation { }

	// ==================================================== 
	/// <summary>
	/// Represents an update operation for its associated entity.
	/// </summary>
	public interface IDataUpdate<T> : IDataUpdate, IMetaOperation<T> where T : class { }
}
// ======================================================== 
