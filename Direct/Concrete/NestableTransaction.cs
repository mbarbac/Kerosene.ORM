// ======================================================== NestableTransaction.cs
namespace Kerosene.ORM.Direct.Concrete
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
	public class NestableTransaction : Core.Concrete.NestableTransaction, INestableTransaction
	{
		IDbTransaction _DbTransaction = null;
		TransactionScope _TransactionScope = null;
		bool _LinkOpenedByTransaction = false;
		int _Level = 0;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="link">The link this nestable transaction is associated with.</param>
		/// <param name="mode">The initial transactional mode.</param>
		public NestableTransaction(
			IDataLink link,
			Core.NestableTransactionMode mode = Core.NestableTransactionMode.Database)
			: base(link, mode) { }

		/// <summary>
		/// The database-alike service link this instance is associated with.
		/// </summary>
		public new IDataLink Link
		{
			get { return (IDataLink)base.Link; }
		}
		Core.IDataLink Core.INestableTransaction.Link
		{
			get { return this.Link; }
		}

		/// <summary>
		/// The database transaction this instance is currently using, or null.
		/// </summary>
		public IDbTransaction DbTransaction
		{
			get { return _DbTransaction; }
		}

		/// <summary>
		/// The transaction scope this instance is currently using, or null.
		/// </summary>
		public TransactionScope TransactionScope
		{
			get { return _TransactionScope; }
		}

		/// <summary>
		/// The current nesting level of this transaction.
		/// <para>A level of 0 means that the transaction is not active.</para>
		/// </summary>
		public override int Level
		{
			get { return _Level; }
		}

		/// <summary>
		/// Starts a new underlying transaction or, if it is active, increases its nesting
		/// level.
		/// </summary>
		public override void Start()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (Link.IsDisposed) throw new ObjectDisposedException(Link.ToString());

			if (_Level > 0) _Level++;

			else
			{
				if (Mode == Core.NestableTransactionMode.Database)
				{
					if (!Link.IsOpen) { Link.Open(); _LinkOpenedByTransaction = true; }
					_DbTransaction = Link.DbConnection.BeginTransaction();
				}
				else if (Mode == Core.NestableTransactionMode.GlobalScope)
				{
					_TransactionScope = new TransactionScope();
					if (!Link.IsOpen) { Link.Open(); _LinkOpenedByTransaction = true; }
				}
				else
				{
					throw new InvalidOperationException(
						"Unsupported transaction mode '{0}'.".FormatWith(Mode));
				}
				_Level = 1;
			}
		}

		/// <summary>
		/// Commits the underlying transation if it is a first-level one, or decreases its
		/// nesting level.
		/// <para>If this transaction was not active this method has no effects.</para>
		/// </summary>
		public override void Commit()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (Link.IsDisposed) throw new ObjectDisposedException(Link.ToString());

			if (_Level == 0) return;

			if (_Level == 1)
			{
				if (Mode == Core.NestableTransactionMode.Database)
				{
					_DbTransaction.Commit(); _DbTransaction.Dispose(); _DbTransaction = null;
					if (_LinkOpenedByTransaction) { Link.Close(); _LinkOpenedByTransaction = false; }
				}
				else if (Mode == Core.NestableTransactionMode.GlobalScope)
				{
					_TransactionScope.Complete(); _TransactionScope.Dispose(); _TransactionScope = null;
					if (_LinkOpenedByTransaction) { Link.Close(); _LinkOpenedByTransaction = false; }
				}
				else
				{
					throw new InvalidOperationException(
						"Unsupported transaction mode '{0}'.".FormatWith(Mode));
				}
				_Level = 0;
			}

			else _Level--;
		}

		/// <summary>
		/// Inconditionally aborts this transaction redardless of its nesting level.
		/// <para>If this transaction was not active this method has no effects.</para>
		/// </summary>
		public override void Abort()
		{
			if (IsDisposed) return;
			if (Link.IsDisposed) return;

			if (_DbTransaction != null)
			{
				_DbTransaction.Rollback(); _DbTransaction.Dispose(); _DbTransaction = null;
				if (_LinkOpenedByTransaction) Link.Close();
			}

			if (_TransactionScope != null)
			{
				_TransactionScope.Dispose(); _TransactionScope = null;
				if (_LinkOpenedByTransaction) Link.Close();
			}

			_LinkOpenedByTransaction = false;
			_Level = 0;
		}
	}
}
// ======================================================== 
