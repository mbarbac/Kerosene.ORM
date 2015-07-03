using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;

namespace Kerosene.ORM.Maps
{
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
