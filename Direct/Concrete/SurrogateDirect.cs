// ======================================================== SurrogateDirect.cs
namespace Kerosene.ORM.Direct.Concrete
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Data;
	using System.Linq;
	using System.Text;
	using System.Transactions;

	// ==================================================== 
	/// <summary>
	/// Acts as a surrogate for direct enumerator or direct executor operations.
	/// </summary>
	internal class SurrogateDirect : IDisposableEx
	{
		bool _IsDisposed = false;
		Core.ICommand _Command = null;
		bool _LinkOpenedBySurrogate = false;
		IDbCommand _DbCommand = null;
		IDataReader _DataReader = null;
		Core.ISchema _Schema = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="command">The command this instance is associated with.</param>
		internal SurrogateDirect(Core.ICommand command)
		{
			if ((_Command = command) == null) throw new ArgumentNullException(
				"command", "Command cannot be null.");

			if (_Command.Link == null) throw new InvalidOperationException(
				"Link of command '{0}' is null."
				.FormatWith(_Command));

			if (!(_Command.Link is Direct.IDataLink)) throw new InvalidOperationException(
				"Link '{0}' of command '{1}' is not a direct link."
				.FormatWith(_Command.Link, _Command));
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

		~SurrogateDirect()
		{
			if (!IsDisposed) OnDispose(false);
		}

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		protected virtual void OnDispose(bool disposing)
		{
			if (_DbCommand != null)
			{
				_DbCommand.Cancel(); if (disposing) _DbCommand.Dispose();
				_DbCommand = null;
			}
			if (_DataReader != null)
			{
				if (!_DataReader.IsClosed) _DataReader.Close(); if (disposing) _DataReader.Dispose();
				_DataReader = null;
			}
			if (Link != null)
			{
				if (Link.IsOpen && _LinkOpenedBySurrogate) Link.Close();
				_LinkOpenedBySurrogate = false;
			}

			_Command = null;
			_Schema = null;

			_IsDisposed = true;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			string str = string.Format("{0}({1}, {2})",
				GetType().EasyName(),
				Command == null ? string.Empty : Command.ToString(),
				Link == null ? string.Empty : Link.ToString());

			return IsDisposed ? string.Format("disposed::{0}", str) : str;
		}

		/// <summary>
		/// The command this surrogate refers to.
		/// </summary>
		internal Core.ICommand Command
		{
			get { return _Command; }
		}

		/// <summary>
		/// The direct data link the command of this surrogate refers to, or null.
		/// </summary>
		internal IDataLink Link
		{
			get { return (_Command == null ? null : _Command.Link) as IDataLink; }
		}

		/// <summary>
		/// Adds the command parameters to the database command.
		/// </summary>
		private void OnInvokeAddParameters()
		{
			if (_Command.Parameters.Count == 0) return;

			var nstr = Link.Parser.Parse(null, pc: null, nulls: true);
			var comp = Link.Engine.CaseSensitiveNames ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

			// Positional parameters...
			if (Link.Engine.PositionalParameters)
			{
				var pars = new List<Core.IParameter>(); foreach (var par in _Command.Parameters)
				{
					if (par.Value == null) _DbCommand.CommandText = _DbCommand.CommandText.Replace(par.Name, nstr);
					else pars.Add(par);
				}

				var list = new List<Tuple<int, Core.IParameter>>(); foreach (var par in pars)
				{
					var i = _DbCommand.CommandText.IndexOf(par.Name, comp);
					list.Add(new Tuple<int, Core.IParameter>(i, par));
				}
				list.Sort((a, b) => a.Item1.CompareTo(b.Item1));

				foreach (var tuple in list)
				{
					var temp = _DbCommand.CreateParameter();
					temp.ParameterName = tuple.Item2.Name;
					temp.Value = Link.Engine.TryTransform(tuple.Item2.Value);
					_DbCommand.Parameters.Add(temp);
				}

				list.Clear(); list = null;
				pars.Clear(); pars = null;
			}

			// Non-positional parameters...
			else
			{
				foreach (var par in _Command.Parameters)
				{
					if (par.Value == null) _DbCommand.CommandText = _DbCommand.CommandText.Replace(par.Name, nstr);
					else
					{
						var temp = _DbCommand.CreateParameter();
						temp.ParameterName = par.Name;
						temp.Value = Link.Engine.TryTransform(par.Value);
						_DbCommand.Parameters.Add(temp);
					}
				}
			}
		}

		/// <summary>
		/// Enlist the invocation into the connection's transaction if needed.
		/// </summary>
		private void OnInvokeAddTransaction()
		{
			if (Transaction.Current != null) return; // Managed externally by the global scope...

			var flags = TypeEx.InstancePublicAndHidden;
			object inner = null;

			if (DynamicInfo.TryRead(Link.DbConnection, x => x.InnerConnection, out inner, flags) == null)
			{
				object current = null;
				if (DynamicInfo.TryRead(inner, x => x.CurrentTransaction, out current, flags) == null)
				{
					object parent = null;
					if (DynamicInfo.TryRead(current, x => x.Parent, out parent, flags) == null)
					{
						if (parent != null) _DbCommand.Transaction = (IDbTransaction)parent;
					}
				}
			}
		}

		/// <summary>
		/// Invokes the execution or enumeration of the command.
		/// </summary>
		private void Invoke(bool iterable, Action action)
		{
			if (Command.IsDisposed) throw new ObjectDisposedException(Command.ToString());
			if (Link.IsDisposed) throw new ObjectDisposedException(Link.ToString());

			if (!Command.CanBeExecuted) throw new CannotExecuteException(
				"Command '{0}' cannot be executed.".FormatWith(Command));

			try
			{
				if (!Link.IsOpen)
				{
					_LinkOpenedBySurrogate = true;
					Link.Open();
				}
				if (Link.DbConnection == null) throw new EmptyException(
					"Link '{0}' of command '{1}' cannot be connected.".FormatWith(Link, _Command));

				var cmd = _Command.GetCommandText(iterable);
				if (cmd == null) throw new InvalidOperationException(
					"Cannot generate the text of command '{0}'.".FormatWith(_Command));

				_DbCommand = Link.DbConnection.CreateCommand();
				_DbCommand.CommandText = cmd;

				OnInvokeAddParameters();
				OnInvokeAddTransaction();

				action();
			}
			catch
			{
				try { OnDispose(true); }
				catch { }

				throw;
			}
			finally
			{
				if (_DbCommand != null)
				{
					_DbCommand.Dispose();
					_DbCommand = null;
				}
			}
		}

		/// <summary>
		/// Invoked to execute the scalar command and to return the integer that execution
		/// produces.
		/// </summary>
		internal int OnExecuteScalar()
		{
			int r = 0; Invoke(iterable: false, action: () => { r = _DbCommand.ExecuteNonQuery(); });
			Dispose();
			return r;
		}

		/// <summary>
		/// Invokes to start the execution of the enumerable command and to return the schema
		/// that describes the structure of the records to be produced by that execution.
		/// </summary>
		internal Core.ISchema OnReaderStart()
		{
			_Schema = null; Invoke(iterable: true, action: () =>
			{
				_DataReader = _DbCommand.ExecuteReader(CommandBehavior.KeyInfo);

				var table = _DataReader.GetSchemaTable(); if (table == null)
				{
					var s = "Cannot obtain schema for command '{0}'.".FormatWith(_Command);
					if (!_DbCommand.CommandText.ToUpper().Contains("OUTPUT")) s += " Have you used an 'OUTPUT' clause?";
					Dispose();
					throw new InvalidOperationException(s);
				}

				var name = _Command is Core.ITableNameProvider
					? ((Core.ITableNameProvider)_Command).TableName
					: null;

				_Schema = new Core.Concrete.Schema(Link.Engine.CaseSensitiveNames);

				for (int i = 0; i < table.Rows.Count; i++)
				{
					DataRow row = table.Rows[i];
					string meta = null;
					object value = null;

					bool hidden = false; if (table.Columns.Contains("IsHidden"))
					{
						value = row[table.Columns["IsHidden"]];
						if (!(value is DBNull)) hidden = (bool)value;
					}
					if (hidden) continue;

					var entry = new Core.Concrete.SchemaEntry(); for (int j = 0; j < table.Columns.Count; j++)
					{
						meta = table.Columns[j].ColumnName;
						value = row[j] is DBNull ? null : row[j];
						entry[meta] = value;
					}

					if (entry.TableName == null && name != null) entry.TableName = name;

					_Schema.Add(entry);
				}
				table.Dispose(); table = null;

				if (_Schema.Count == 0) throw new InvalidOperationException(
					"Schema is empty after executing command '{0}'".FormatWith(_Command));

				if (_Command is Core.IElementAliasProvider)
					_Schema.Aliases.AddRange(
						((Core.IElementAliasProvider)_Command).Aliases,
						cloneNotOrphans: true);
			});
			return _Schema;
		}

		/// <summary>
		/// Invoked to retrieve the next available record produced by the execution of the
		/// enumerable command, or null if there are no more records available.
		/// </summary>
		internal Core.IRecord OnReaderNext()
		{
			Core.IRecord record = null;
			try
			{
				if (_DataReader.Read())
				{
					record = new Core.Concrete.Record(_Schema);

					for (int i = 0; i < _Schema.Count; i++)
						record[i] = _DataReader.IsDBNull(i) ? null : _DataReader.GetValue(i);

					return record;
				}
				else
				{
					OnDispose(true); return null;
				}
			}
			catch (Exception e)
			{
				try { OnDispose(true); }
				catch { }

				throw e;
			}
		}

		/// <summary>
		/// Invoked when reseting the enumeration of the command.
		/// </summary>
		internal void OnReaderReset()
		{
			if (_DbCommand != null)
			{
				_DbCommand.Cancel(); _DbCommand.Dispose();
				_DbCommand = null;
			}
			if (_DataReader != null)
			{
				if (!_DataReader.IsClosed) _DataReader.Close(); _DataReader.Dispose();
				_DataReader = null;
			}
			if (Link != null)
			{
				if (Link.IsOpen && _LinkOpenedBySurrogate) Link.Close();
				_LinkOpenedBySurrogate = false;
			}
		}
	}
}
// ======================================================== 
