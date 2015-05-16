using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;

namespace Kerosene.ORM.Maps
{
	// ====================================================
	/// <summary>
	/// Represents an insert operation for its associated entity.
	/// </summary>
	public interface IDataInsert : IMetaOperation { }

	// ==================================================== 
	/// <summary>
	/// Represents an insert operation for its associated entity.
	/// </summary>
	public interface IDataInsert<T> : IMetaOperation<T>, IDataInsert where T : class { }
}
