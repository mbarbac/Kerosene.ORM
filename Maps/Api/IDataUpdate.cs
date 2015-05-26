using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;

namespace Kerosene.ORM.Maps
{
	// ====================================================
	/// <summary>
	/// Represents an update operation for its associated entity.
	/// </summary>
	public interface IDataUpdate : IDataSave { }

	// ==================================================== 
	/// <summary>
	/// Represents an update operation for its associated entity.
	/// </summary>
	public interface IDataUpdate<T> : IDataSave<T>, IDataUpdate where T : class { }
}
