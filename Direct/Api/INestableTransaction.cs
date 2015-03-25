// ======================================================== INestableTransaction.cs
namespace Kerosene.ORM.Direct
{
	using Kerosene.Tools;
	using System;
	using System.Data;
	using System.Transactions;

	// ==================================================== 
	/// <summary>
	/// Represents an abstract nestable transaction, in a direct connection scenario, associated
	/// with a given <see cref="IDataLink"/>.
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
