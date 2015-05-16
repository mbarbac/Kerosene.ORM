using Kerosene.Tools;
using System;
using System.Data;
using System.Linq;
using System.Threading;

namespace Kerosene.ORM.Direct.Concrete
{
	// ==================================================== 
	/// <summary>
	/// Represents an agnostic direct connection with an underlying database.
	/// </summary>
	public class DataLink : Core.Concrete.DataLink, IDataLink
	{
		string _ConnectionString = null;
		string _Server = null;
		string _Database = null;
		IDbConnection _DbConnection = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="engine">The engine this instance will be associated with.</param>
		/// <param name="mode">The default initial mode to use when needed to create the nestable
		/// transtransaction object maintained by this instance.</param>
		public DataLink(
			IDataEngine engine,
			Core.NestableTransactionMode mode = Core.NestableTransactionMode.Database)
			: base(engine, mode) { }

		/// <summary>
		/// Invoked to obtain the string type for string representation purposes.
		/// </summary>
		protected override string ToStringType()
		{
			return "Direct:" + base.ToStringType();
		}

		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		public DataLink Clone()
		{
			var cloned = new DataLink(Engine, DefaultTransactionMode);
			OnClone(cloned); return cloned;
		}
		IDataLink IDataLink.Clone()
		{
			return this.Clone();
		}
		Core.IDataLink Core.IDataLink.Clone()
		{
			return this.Clone();
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		/// <summary>
		/// Invoked when cloning this object to set its state at this point of the inheritance
		/// chain.
		/// </summary>
		/// <param name="cloned">The cloned object.</param>
		protected override void OnClone(object cloned)
		{
			base.OnClone(cloned);
			var temp = cloned as DataLink;
			if (temp == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' one."
				.FormatWith(cloned.Sketch(), typeof(DataLink).EasyName()));

			temp._ConnectionString = _ConnectionString;
			temp._Server = _Server;
			temp._Database = _Database;
		}

		/// <summary>
		/// The engine this link is associated with.
		/// </summary>
		public new IDataEngine Engine
		{
			get { return (IDataEngine)base.Engine; }
		}
		Core.IDataEngine Core.IDataLink.Engine
		{
			get { return this.Engine; }
		}

		/// <summary>
		/// The nestable transaction this instance maintains, created on-demand if needed (for
		/// instance if the previous reference is disposed).
		/// </summary>
		public new INestableTransaction Transaction
		{
			get { return (INestableTransaction)base.Transaction; }
		}
		Core.INestableTransaction Core.IDataLink.Transaction
		{
			get { return this.Transaction; }
		}

		/// <summary>
		/// Factory method invoked to create a new transaction when needed.
		/// </summary>
		protected override Core.INestableTransaction CreateTransaction()
		{
			return new NestableTransaction(this, DefaultTransactionMode);
		}

		/// <summary>
		/// Gets or sets the connection string this instance is currently using.
		/// <para>The setter accepts:</para>
		/// <para>- Null to use the default connection string entry from the configuration files.</para>
		/// <para>- The name of one connection string entry in the configuration files.</para>
		/// <para>- The actual contents of the connection string.</para>
		/// </summary>
		public string ConnectionString
		{
			get { return _ConnectionString; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (IsOpen) throw new InvalidOperationException("This '{0}' is currently connected.".FormatWith(this));

				value = value.NullIfTrimmedIsEmpty();
				var cn = Configuration.ConnectionStringEx.Find(value); if (cn == null)
				{
					if (value == null) throw new ArgumentException(
						"Connection string is null or empty, or no default connection string entry is found.");
				}
				else value = cn.ConnectionString;

				_ConnectionString = value;

				var prefix = "server=";
				var source = value.ToLower();
				var i = source.IndexOf(prefix); if (i >= 0)
				{
					_Server = value.Substring(i + prefix.Length);
					i = _Server.IndexOf(';'); if (i >= 0) _Server = _Server.Substring(0, i);
				}
				else _Server = null;

				prefix = "database=";
				source = value.ToLower();
				i = source.IndexOf(prefix); if (i >= 0)
				{
					_Database = value.Substring(i + prefix.Length);
					i = _Database.IndexOf(';'); if (i >= 0) _Database = _Database.Substring(0, i);
				}
				else _Database = null;
			}
		}

		/// <summary>
		/// The server this link is connected to, or null if this information is not available.
		/// </summary>
		public string Server
		{
			get { return _Server; }
		}

		/// <summary>
		/// The database this link is connected to, or null if this information is not available.
		/// </summary>
		public string Database
		{
			get { return _Database; }
		}

		/// <summary>
		/// The actual connection this instance is using, or null if it is not connected.
		/// </summary>
		public IDbConnection DbConnection
		{
			get { return _DbConnection; }
		}

		/// <summary>
		/// Opens the connection against the database-alike service.
		/// <para>The framework invokes this method automatically when needed.</para>
		/// <para>Invoking this method in an opened link may throw an exception.</para>
		/// </summary>
		public override void Open()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			if (IsOpen) return;
			if (DbConnection != null) return;

			if (ConnectionString == null) throw new InvalidOperationException(
				"No connection string in this link '{0}'.".FormatWith(this));

			_DbConnection = Engine.ProviderFactory.CreateConnection();
			_DbConnection.ConnectionString = _ConnectionString;

			if (_Retries < 0)
			{
				var info = Configuration.ORMConfiguration.GetInfo();
				if (info != null &&
					info.DataLink != null &&
					info.DataLink.Retries != null) _Retries = (int)info.DataLink.Retries;

				if (_Retries < MIN_RETRIES) _Retries = MIN_RETRIES;
			}
			if (_RetryInterval < 0)
			{
				var info = Configuration.ORMConfiguration.GetInfo();
				if (info != null &&
					info.DataLink != null &&
					info.DataLink.RetryInterval != null) _RetryInterval = (int)info.DataLink.RetryInterval;

				if (_RetryInterval < MIN_RETRY_INTERVAL) _RetryInterval = MIN_RETRY_INTERVAL;
			}

			Exception e = null;
			int retries = _Retries; while (retries >= 0)
			{
				try { _DbConnection.Open(); }
				catch (Exception x) { e = x; }

				if (_DbConnection.State == ConnectionState.Open) break;

				if (--retries < 0)
				{
					if (e != null) throw e;
					throw new CannotExecuteException("Cannot open connection for this '{0}'.".FormatWith(this));
				}
				Thread.Sleep(_RetryInterval);
			}
		}
		int _Retries = -1;
		int _RetryInterval = -1;
		
		/// <summary></summary>
		public const int MIN_RETRIES = 3;
		/// <summary></summary>
		public const int MIN_RETRY_INTERVAL = 50;

		/// <summary>
		/// Closes the connection that might be opened against the database-alike service.
		/// <para>The framework invokes this method automatically when needed.</para>
		/// </summary>
		public override void Close()
		{
			if (Transaction != null && Transaction.IsActive) Transaction.Abort();

			if (_DbConnection != null)
			{
				if (_DbConnection.State != ConnectionState.Closed) _DbConnection.Close();
				_DbConnection.Dispose();
				_DbConnection = null;
			}
		}

		/// <summary>
		/// Whether this connection can be considered opened or not.
		/// </summary>
		public override bool IsOpen
		{
			get { return (DbConnection == null) ? false : (DbConnection.State == ConnectionState.Open); }
		}

		/// <summary>
		/// Factory method invoked to create an enumerator to execute the given enumerable
		/// command.
		/// </summary>
		/// <param name="command">The command to execute.</param>
		/// <returns>An enumerator able to execute de command.</returns>
		public IEnumerableExecutor CreateEnumerableExecutor(Core.IEnumerableCommand command)
		{
			if (command == null) throw new ArgumentNullException("command", "Command cannot be null.");
			if (command.IsDisposed) throw new ObjectDisposedException(command.ToString());

			if (!object.ReferenceEquals(this, command.Link))
				throw new InvalidOperationException(
					"This link '{0}' is not the same as the link of command '{1}'."
					.FormatWith(this, command));

			return new EnumerableExecutor(command);
		}
		Core.IEnumerableExecutor Core.IDataLink.CreateEnumerableExecutor(Core.IEnumerableCommand command)
		{
			return this.CreateEnumerableExecutor(command);
		}

		/// <summary>
		/// Factory method invoked to create an executor to execute the given scalar command.
		/// </summary>
		/// <param name="command">The command to execute.</param>
		/// <returns>An executor able to execute de command.</returns>
		public IScalarExecutor CreateScalarExecutor(Core.IScalarCommand command)
		{
			if (command == null) throw new ArgumentNullException("command", "Command cannot be null.");
			if (command.IsDisposed) throw new ObjectDisposedException(command.ToString());

			if (!object.ReferenceEquals(this, command.Link))
				throw new InvalidOperationException(
					"This link '{0}' is not the same as the link of command '{1}'."
					.FormatWith(this, command));

			return new ScalarExecutor(command);
		}
		Core.IScalarExecutor Core.IDataLink.CreateScalarExecutor(Core.IScalarCommand command)
		{
			return this.CreateScalarExecutor(command);
		}
	}
}
