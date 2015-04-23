namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;

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
	/// Represents an abstract nestable transaction associated with a given
	/// <see cref="IDataLink"/>.
	/// </summary>
	public interface INestableTransaction : IDisposableEx
	{
		/// <summary>
		/// The database-alike service link this instance is associated with.
		/// </summary>
		IDataLink Link { get; }

		/// <summary>
		/// The current mode of this instance.
		/// <para>The setter fails if the transaction is active.</para>
		/// <para>The setter may also fail if the mode is not supported by the concrete instance.</para>
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
		/// <para>If this transaction was not active this method has no effects.</para>
		/// </summary>
		void Commit();

		/// <summary>
		/// Inconditionally aborts this transaction redardless of its nesting level.
		/// <para>If this transaction was not active this method has no effects.</para>
		/// </summary>
		void Abort();
	}
}
