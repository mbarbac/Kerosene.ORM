// ======================================================== IDataInsert.cs
namespace Kerosene.ORM.Maps
{
	using Kerosene.Tools;
	using System;

	// ==================================================== 
	/// <summary>
	/// Represents an insert operation for its associated entity.
	/// </summary>
	public interface IDataInsert : IMetaOperation { }

	// ==================================================== 
	/// <summary>
	/// Represents an insert operation for its associated entity.
	/// </summary>
	public interface IDataInsert<T> : IDataInsert, IMetaOperation<T> where T : class { }
}
// ======================================================== 
