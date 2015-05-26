using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;

namespace Kerosene.ORM.Maps
{
	// ====================================================
	/// <summary>
	/// Represents a save (insert or update) operation for its associated entity.
	/// </summary>
	public interface IDataSave : IMetaOperation { }

	// ==================================================== 
	/// <summary>
	/// Represents a save (insert or update) operation for its associated entity.
	/// </summary>
	public interface IDataSave<T> : IMetaOperation<T>, IDataSave where T : class { }
}
