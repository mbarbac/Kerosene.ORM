using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;

namespace Kerosene.ORM.Maps
{
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
