// ======================================================== INestableTransaction.cs
namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents the mode of a nestable transaction.
	/// </summary>
	public enum NestableTransactionMode
	{
		/// <summary>
		/// The transaction has database scope.
		/// </summary>
		Database = 0,

		/// <summary>
		/// The transaction has global scope.
		/// </summary>
		GlobalScope = 1,
	}

	// ==================================================== 
	/// <summary>
	/// Represents an abstract nestable transaction.
	/// </summary>
	public interface INestableTransaction : IDisposableEx
	{
		/// <summary>
		/// The link this transaction is associated with.
		/// </summary>
		IDataLink Link { get; }

		/// <summary>
		/// The current mode of this instance.
		/// <para>The setter fails if the transaction is active.</para>
		/// </summary>
		NestableTransactionMode Mode { get; set; }

		/// <summary>
		/// Whether this transaction is active or not.
		/// </summary>
		bool IsActive { get; }

		/// <summary>
		/// The current nesting level of this transaction.
		/// <para>A level of 0 means that the transaction is not active.</para>
		/// </summary>
		int Level { get; }

		/// <summary>
		/// Starts a new underlying transaction or, if it is active, increases its nesting
		/// level.
		/// </summary>
		void Start();

		/// <summary>
		/// Commits the underlying transation if it is a first-level one, or decreases its
		/// nesting level.
		/// </summary>
		void Commit();

		/// <summary>
		/// Inconditionally aborts this transaction redardless of its nesting level.
		/// </summary>
		void Abort();
	}
}
// ======================================================== 
