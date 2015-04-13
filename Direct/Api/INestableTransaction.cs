// ======================================================== INestableTransaction.cs
namespace Kerosene.ORM.Direct
{
	using Kerosene.Tools;
	using System;
	using System.Data;
	using System.Linq;
	using System.Transactions;

	// ==================================================== 
	/// <summary>
	/// Represents an abstract nestable transaction associated with a given link in a direct
	/// connection scenario.
	/// </summary>
	public interface INestableTransaction : Core.INestableTransaction
	{
		/// <summary>
		/// The database-alike service link this instance is associated with.
		/// </summary>
		new IDataLink Link { get; }

		/// <summary>
		/// The database transaction this instance is currently using, or null.
		/// </summary>
		IDbTransaction DbTransaction { get; }

		/// <summary>
		/// The transaction scope this instance is currently using, or null.
		/// </summary>
		TransactionScope TransactionScope { get; }
	}
}
// ======================================================== 
