using Kerosene.Tools;
using System;
using System.Linq;

namespace Kerosene.ORM.Core.Concrete
{
	// ==================================================== 
	/// <summary>
	/// Represents an agnostic connection with a database-alike service.
	/// </summary>
	public abstract class DataLink : IDataLink
	{
		static ulong _LastSerialId = 0;

		bool _IsDisposed = false;
		ulong _SerialId = 0;
		IDataEngine _Engine = null;
		INestableTransaction _Transaction = null;
		NestableTransactionMode _DefaultTransactionMode = NestableTransactionMode.Database;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="engine">The engine this instance will be associated with.</param>
		/// <param name="mode">The default initial mode to use when needed to create the nestable
		/// transtransaction object maintained by this instance.</param>
		protected DataLink(
			IDataEngine engine,
			NestableTransactionMode mode = NestableTransactionMode.Database)
		{
			if (engine == null) throw new ArgumentNullException("engine", "Engine cannot be null.");
			if (engine.IsDisposed) throw new ObjectDisposedException(engine.ToString());

			_Engine = engine;
			_SerialId = ++_LastSerialId;
			_DefaultTransactionMode = mode;
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
		~DataLink()
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
					if (_Transaction != null && !_Transaction.IsDisposed)
					{
						_Transaction.Abort();
						_Transaction.Dispose();
					}
				}
				catch { }

				try { if (IsOpen) Close(); }
				catch { }
			}

			_Engine = null;
			_Transaction = null;

			_IsDisposed = true;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			string str = string.Format("{0}:{1}({2})",
				SerialId,
				ToStringType(),
				Engine.Sketch());

			return IsDisposed ? string.Format("disposed::{0}", str) : str;
		}

		/// <summary>
		/// Invoked to obtain the string type for string representation purposes.
		/// </summary>
		protected virtual string ToStringType()
		{
			return GetType().EasyName();
		}

		/// <summary>
		/// The serial id assigned to this instance.
		/// </summary>
		public ulong SerialId
		{
			get { return _SerialId; }
		}

		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		IDataLink IDataLink.Clone()
		{
			throw new NotSupportedException(
				"Abstract ICloneable::{0}.Clone() invoked.".FormatWith(GetType().EasyName()));
		}
		object ICloneable.Clone()
		{
			throw new NotSupportedException(
				"Abstract ICloneable::{0}.Clone() invoked.".FormatWith(GetType().EasyName()));
		}

		/// <summary>
		/// Invoked when cloning this object to set its state at this point of the inheritance
		/// chain.
		/// </summary>
		/// <param name="cloned">The cloned object.</param>
		protected virtual void OnClone(object cloned)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			var temp = cloned as DataLink;
			if (temp == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' one."
				.FormatWith(cloned.Sketch(), typeof(DataLink).EasyName()));

			temp.DefaultTransactionMode = this.DefaultTransactionMode;
		}

		/// <summary>
		/// The engine this link is associated with.
		/// </summary>
		public IDataEngine Engine
		{
			get { return _Engine; }
		}

		/// <summary>
		/// The nestable transaction this instance maintains, created on-demand if needed (for
		/// instance if the previous reference is disposed).
		/// </summary>
		public INestableTransaction Transaction
		{
			get
			{
				if (!IsDisposed && (_Transaction == null || _Transaction.IsDisposed))
				{
					if ((_Transaction = CreateTransaction()) == null)
						throw new CannotCreateException(
							"Cannot create a new transaction for this instance '{0}'."
							.FormatWith(this));
				}
				return _Transaction;
			}
		}

		/// <summary>
		/// Factory method invoked to create a new transaction when needed.
		/// </summary>
		protected abstract INestableTransaction CreateTransaction();

		/// <summary>
		/// Gets or sets the default transaction mode to use when creating a new transaction
		/// for this instance.
		/// <para>The setter may also fail if the mode is not supported by the concrete instance.</para>
		/// </summary>
		public virtual NestableTransactionMode DefaultTransactionMode
		{
			get { return _DefaultTransactionMode; }
			set { _DefaultTransactionMode = value; }
		}

		/// <summary>
		/// Opens the connection against the database-alike service.
		/// <para>The framework invokes this method automatically when needed.</para>
		/// <para>Invoking this method in an opened link may throw an exception.</para>
		/// </summary>
		public abstract void Open();

		/// <summary>
		/// Closes the connection that might be opened against the database-alike service.
		/// <para>The framework invokes this method automatically when needed.</para>
		/// </summary>
		public abstract void Close();

		/// <summary>
		/// Whether this connection can be considered opened or not.
		/// </summary>
		public abstract bool IsOpen { get; }

		/// <summary>
		/// Creates a new raw command for this link.
		/// </summary>
		/// <returns>The new command.</returns>
		public IRawCommand Raw()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return Engine.CreateRawCommand(this);
		}

		/// <summary>
		/// Creates a new raw command for this link and sets its initial contents using the text
		/// and arguments given.
		/// </summary>
		/// <param name="text">The new text of the command. Embedded arguments are specified
		/// using the standard '{n}' positional format.</param>
		/// <param name="args">An optional collection containing the arguments specified in the
		/// text set into this command.</param>
		/// <returns>The new command.</returns>
		public IRawCommand Raw(string text, params object[] args)
		{
			return Raw().Set(text, args);
		}

		/// <summary>
		/// Creates a new raw command for this link and sets its initial contents by parsing the
		/// dynamic lambda expression given.
		/// </summary>
		/// <param name="spec">A dynamic lambda expression that resolves into the logic of this
		/// command. Embedded arguments are extracted and captured automatically in order to
		/// avoid injection attacks.</param>
		/// <returns>The new command.</returns>
		public IRawCommand Raw(Func<dynamic, object> spec)
		{
			return Raw().Set(spec);
		}

		/// <summary>
		/// Creates a new query command for this link.
		/// </summary>
		/// <returns>The new command.</returns>
		public IQueryCommand Query()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return Engine.CreateQueryCommand(this);
		}

		/// <summary>
		/// Creates a new query command for this link and sets the contents of its FROM clause.
		/// </summary>
		/// <param name="froms">The collection of lambda expressions that resolve into the
		/// elements to include in this clause:
		/// <para>- A string, as in 'x => "name AS alias"', where the alias part is optional.</para>
		/// <para>- A table specification, as in 'x => x.Table.As(alias)', where both the alias part
		/// is optional.</para>
		/// <para>- Any expression that can be parsed into a valid SQL sentence for this clause.</para>
		/// </param>
		/// <returns>The new command.</returns>
		public IQueryCommand From(params Func<dynamic, object>[] froms)
		{
			return Query().From(froms);
		}

		/// <summary>
		/// Creates a new query command for this link and sets the contents of its SELECT clause.
		/// </summary>
		/// <param name="selects">The collection of lambda expressions that resolve into the
		/// elements to include into this clause:
		/// <para>- A string, as in 'x => "name AS alias"', where the alias part is optional.</para>
		/// <para>- A table and column specification, as in 'x => x.Table.Column.As(alias)', where
		/// both the table and alias parts are optional.</para>
		/// <para>- A specification for all columns of a table using the 'x => x.Table.All()' syntax.</para>
		/// <para>- Any expression that can be parsed into a valid SQL sentence for this clause.</para>
		/// </param>
		/// <returns>The new command.</returns>
		public IQueryCommand Select(params Func<dynamic, object>[] selects)
		{
			return Query().Select(selects);
		}

		/// <summary>
		/// Creates a new insert command for this link.
		/// </summary>
		/// <param name="table">A dynamic lambda expression that resolves into the table the new
		/// command will refer to.</param>
		/// <returns>The new command.</returns>
		public IInsertCommand Insert(Func<dynamic, object> table)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return Engine.CreateInsertCommand(this, table);
		}

		/// <summary>
		/// Creates a new delete command for this link.
		/// </summary>
		/// <param name="table">A dynamic lambda expression that resolves into the table the new
		/// command will refer to.</param>
		/// <returns>The new command.</returns>
		public IDeleteCommand Delete(Func<dynamic, object> table)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return Engine.CreateDeleteCommand(this, table);
		}

		/// <summary>
		/// Creates a new update command for this link.
		/// </summary>
		/// <param name="table">A dynamic lambda expression that resolves into the table the new
		/// command will refer to.</param>
		/// <returns>The new command.</returns>
		public IUpdateCommand Update(Func<dynamic, object> table)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return Engine.CreateUpdateCommand(this, table);
		}

		/// <summary>
		/// Factory method invoked to create an enumerator to execute the given enumerable
		/// command.
		/// </summary>
		/// <param name="command">The command to execute.</param>
		/// <returns>An enumerator able to execute de command.</returns>
		IEnumerableExecutor IDataLink.CreateEnumerableExecutor(IEnumerableCommand command)
		{
			throw new NotSupportedException(
				"Abstract IDataLink::{0}.CreateEnumerableExecutor(command) invoked."
				.FormatWith(GetType().EasyName()));
		}

		/// <summary>
		/// Factory method invoked to create an executor to execute the given scalar command.
		/// </summary>
		/// <param name="command">The command to execute.</param>
		/// <returns>An executor able to execute de command.</returns>
		IScalarExecutor IDataLink.CreateScalarExecutor(IScalarCommand command)
		{
			throw new NotSupportedException(
				"Abstract IDataLink::{0}.CreateScalarExecutor(command) invoked."
				.FormatWith(GetType().EasyName()));
		}
	}
}
