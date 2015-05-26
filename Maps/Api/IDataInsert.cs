using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;

namespace Kerosene.ORM.Maps
{
	// ====================================================
	/// <summary>
	/// Represents an insert operation for its associated entity.
	/// </summary>
	public interface IDataInsert : IDataSave { }

	// ==================================================== 
	/// <summary>
	/// Represents an insert operation for its associated entity.
	/// </summary>
	public interface IDataInsert<T> : IDataSave<T>, IDataInsert where T : class { }
}
