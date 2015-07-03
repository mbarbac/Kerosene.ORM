using Kerosene.Tools;
using System;
using System.Linq;
using System.Text;

namespace Kerosene.ORM.Core.Concrete
{
	// ==================================================== 
	/// <summary>
	/// Represents an abstract nestable transaction associated with a given
	/// <see cref="IDataLink"/>.
	/// </summary>
	public abstract class NestableTransaction : INestableTransaction
	{
		bool _IsDisposed = false;
		IDataLink _Link = null;
		NestableTransactionMode _Mode = NestableTransactionMode.Database;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="link">The link this nestable transaction is associated with.</param>
		/// <param name="mode">The initial transactional mode.</param>
		protected NestableTransaction(
			IDataLink link,
			NestableTransactionMode mode = NestableTransactionMode.Database)
		{
			if (link == null) throw new ArgumentNullException("link", "Link cannot be null.");
			if (link.IsDisposed) throw new ObjectDisposedException(link.ToString());

			_Link = link;
			_Mode = mode;
		}

		/// <summary>
		/// Whether this instance has been disposed or not.
		/// </summary>
		public bool IsDisposed
		{
			get { return _IsDisposed; }
		}

		/// <summary>
		/// Disposes this instance.
		/// </summary>
		public void Dispose()
		{
			if (!IsDisposed) { OnDispose(true); GC.SuppressFinalize(this); }
		}

		/// <summary></summary>
		~NestableTransaction()
		{
			if (!IsDisposed) OnDispose(false);
		}

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		protected virtual void OnDispose(bool disposing)
		{
			if (disposing)
			{
				try
				{
					if (_Link != null && !_Link.IsDisposed && this.IsActive) Abort();
				}
				catch { }

				_Link = null;
			}

			_IsDisposed = true;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendFormat("{0}(", GetType().EasyName());
			if (Level > 0) sb.AppendFormat("Level:{0}", Level);
			sb.AppendFormat(", Mode:{0}", Mode);
			sb.AppendFormat(", Link:{0}", Link == null ? string.Empty : Link.ToString());
			sb.Append(")");

			var str = sb.ToString();
			return IsDisposed ? string.Format("disposed::{0}({1})", GetType().EasyName(), str) : str;
		}

		/// <summary>
		/// The database-alike service link this instance is associated with.
		/// </summary>
		public IDataLink Link
		{
			get { return _Link; }
		}

		/// <summary>
		/// The current mode of this instance.
		/// <para>The setter fails if the transaction is active.</para>
		/// <para>The setter may also fail if the mode is not supported by the concrete instance.</para>
		/// </summary>
		public NestableTransactionMode Mode
		{
			get { return _Mode; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (IsActive) throw new InvalidOperationException(
					"Cannot change the mode of this '{0}' because it is active.".FormatWith(this));

				_Mode = value;
			}
		}

		/// <summary>
		/// Whether this transaction is active or not.
		/// </summary>
		public bool IsActive
		{
			get { return Level > 0 ? true : false; }
		}

		/// <summary>
		/// The current nesting level of this transaction.
		/// <para>A level of 0 means that the transaction is not active.</para>
		/// </summary>
		public abstract int Level { get; }

		/// <summary>
		/// Starts a new underlying transaction or, if it is active, increases its nesting
		/// level.
		/// </summary>
		public abstract void Start();

		/// <summary>
		/// Commits the underlying transation if it is a first-level one, or decreases its
		/// nesting level.
		/// <para>If this transaction was not active this method has no effects.</para>
		/// </summary>
		public abstract void Commit();

		/// <summary>
		/// Inconditionally aborts this transaction redardless of its nesting level.
		/// <para>If this transaction was not active this method has no effects.</para>
		/// </summary>
		public abstract void Abort();
	}
}
