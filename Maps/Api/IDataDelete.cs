// ======================================================== IDataDelete.cs
namespace Kerosene.ORM.Maps
{
	using Kerosene.Tools;
	using System;

	// ==================================================== 
	/// <summary>
	/// Represents a delete operation for its associated entity.
	/// </summary>
	public interface IDataDelete : IMetaOperation { }

	// ==================================================== 
	/// <summary>
	/// Represents a delete operation for its associated entity.
	/// </summary>
	public interface IDataDelete<T> : IMetaOperation<T>, IDataDelete where T : class { }
}
// ======================================================== 
