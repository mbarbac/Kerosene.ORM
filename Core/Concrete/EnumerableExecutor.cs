using Kerosene.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kerosene.ORM.Core.Concrete
{
	// ==================================================== 
	/// <summary>
	/// Represents an object able to execute an enumerable command and to produce the collection
	/// of records resulting from this execution.
	/// </summary>
	public abstract class EnumerableExecutor : IEnumerableExecutor
	{
		bool _IsDisposed = false;
		IEnumerableCommand _Command = null;
		ISchema _Schema = null;
		IRecord _CurrentRecord = null;
		Func<IRecord, object> _Converter = null;

		bool _Started = false;
		object _Current = null;
		bool _TakeOnGoing = false;
		int _TakeRemaining = -1;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="cmd">The command this instance will be associated with.</param>
		protected EnumerableExecutor(IEnumerableCommand cmd)
		{
			if (cmd == null) throw new ArgumentNullException("command", "Command cannot be null.");
			if (cmd.IsDisposed) throw new ObjectDisposedException(cmd.ToString());
			if (cmd.Link.IsDisposed) throw new ObjectDisposedException("Link '{0}' of command '{1}' is disposed.".FormatWith(cmd.Link, cmd));

			_Command = cmd;
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
		~EnumerableExecutor()
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
				if (!IsDisposed) Reset();
			}
			_Command = null;
			_Schema = null;
			_CurrentRecord = null;
			_Converter = null;

			_Started = false;
			_Current = null;
			_TakeOnGoing = false;
			_TakeRemaining = -1;

			_IsDisposed = true;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			var str = string.Format("{0}({1})",
				GetType().EasyName(),
				Command == null ? string.Empty : Command.ToString()); str = null;

			return IsDisposed ? string.Format("disposed::{0}", str) : str;
		}

		/// <summary>
		/// Returns a new enumerator for this instance.
		/// <para>Hack to permit this instance to be enumerated in order to simplify its usage
		/// and syntax.</para>
		/// </summary>
		/// <returns>A self-reference.</returns>
		public IEnumerableExecutor GetEnumerator()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return this;
		}

		/// <summary>
		/// The command this instance is associated with.
		/// </summary>
		public IEnumerableCommand Command
		{
			get { return _Command; }
		}

		/// <summary>
		/// The link of the command this enumerator is associated with.
		/// </summary>
		public IDataLink Link
		{
			get { return _Command == null ? null : _Command.Link; }
		}

		/// <summary>
		/// Gets the schema of the records produced by the execution of the associated command.
		/// This property is null until the command has been executed, or when this instance
		/// has been disposed.
		/// </summary>
		public ISchema Schema
		{
			get { return _Schema; }
		}

		/// <summary>
		/// Gets the current record produced by the last iteration of the command. This property
		/// is null if this instance is disposed, if the command has not been executed yet, or
		/// if there are no more records available.
		/// </summary>
		public IRecord CurrentRecord
		{
			get { return _CurrentRecord; }
		}

		/// <summary>
		/// Gets the current object being enumerated, or null if this enumerator has not been
		/// executed yet, or if there are not more objects available.
		/// </summary>
		public object Current
		{
			get { return _Current; }
		}

		/// <summary>
		/// If not null this property is the delegate to invoke each iteration to convert the
		/// record returned by the database into whatever instance the delegate returns, that
		/// becomes the new value held by the 'Current' property of this instance.
		/// </summary>
		public Func<IRecord, object> Converter
		{
			get { return _Converter; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				_Converter = value;
			}
		}

		/// <summary>
		/// Executes the command if it has not been executed yet in this instance. Returns true
		/// if a new record is available, or false otherwise.
		/// </summary>
		public bool MoveNext()
		{
			// Always needed...
			_CurrentRecord = null;
			_Current = null;

			// First execution...
			if (!_Started)
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (_Command.IsDisposed) throw new ObjectDisposedException(_Command.ToString());
				if (_Command.Link.IsDisposed) throw new ObjectDisposedException(_Command.Link.ToString());

				if (!Command.CanBeExecuted) throw new CannotExecuteException(
					"State of command '{0}' is not ready for execution.".FormatWith(Command));

				_Started = true;
				_TakeOnGoing = false;
				_TakeRemaining = -1;

				_Schema = OnReaderStart();
				if (_Schema == null) throw new InvalidOperationException("Schema resulting from execution of '{0}' is null.".FormatWith(this));
				if (_Schema.Count == 0) throw new EmptyException("Schema resulting of execution from '{0}' is empty.".FormatWith(this));

				// Emulation of skip/take if needed...
				var cmd = _Command as IQueryCommand; if (cmd != null)
				{
					int skip = cmd.GetSkipValue();
					int take = cmd.GetTakeValue();
					bool valid = cmd.IsValidForNativeSkipTake();

					if (!valid && skip > 0)
					{
						for (int i = 0; i < skip; i++)
						{
							if (OnReaderNext() == null)
							{
								Reset(); return false;
							}
						}
					}

					if (!valid && take > 0)
					{
						_TakeOnGoing = true;
						_TakeRemaining = take;
					}
				}
			}

			// Current iteration...
			if (_TakeOnGoing)
			{
				if (_TakeRemaining > 0)
				{
					_CurrentRecord = OnReaderNext();
					_TakeRemaining--;
				}
				else _CurrentRecord = null;
			}
			else _CurrentRecord = OnReaderNext();

			// Finalizing...
			if (_CurrentRecord == null)
			{
				Reset(); return false;
			}
			else
			{
				_Current = _Converter == null ? _CurrentRecord : _Converter(_CurrentRecord);
				return true;
			}
		}

		/// <summary>
		/// Invoked to execute the command returning the schema that describes the record to be
		/// produced by that execution.
		/// </summary>
		/// <returns>The schema of the records to be produced.</returns>
		protected abstract ISchema OnReaderStart();

		/// <summary>
		/// Invoked to retrieve the next available record, or null if there are no more records
		/// available.
		/// </summary>
		/// <returns>The next record produced, or null.</returns>
		protected abstract IRecord OnReaderNext();

		/// <summary>
		/// Resets this enumerator preparing it for a fresh new execution.
		/// </summary>
		public void Reset()
		{
			OnReset();

			_Started = false;
			_Current = null;
			_CurrentRecord = null;
			_TakeOnGoing = false;
			_TakeRemaining = -1;
		}

		/// <summary>
		/// Invoked to reset this enumerator so that is can execute its associated command again.
		/// </summary>
		protected abstract void OnReset();

		/// <summary>
		/// Executes the associated command and returns a list with the results.
		/// </summary>
		/// <returns>A list with the results of the execution.</returns>
		public List<object> ToList()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			var link = Command.Link;
			var open = link.IsOpen; if (!open) link.Open();

			var list = new List<object>();
			Reset(); while (MoveNext()) list.Add(Current);
			Reset();

			if (!open) link.Close();
			return list;
		}

		/// <summary>
		/// Executes the associated command and returns an array with the results.
		/// </summary>
		/// <returns>An array with the results of the execution.</returns>
		public object[] ToArray()
		{
			return ToList().ToArray();
		}

		/// <summary>
		/// Executes the associated command and returns the first result produced from the
		/// database, or null if it produced no results.
		/// </summary>
		/// <returns>The first result produced, or null.</returns>
		public object First()
		{
			if (IsDisposed) throw new ObjectDisposedException(ToString());

			var link = Command.Link;
			var open = link.IsOpen; if (!open) link.Open();

			object temp = null;
			Reset(); if (MoveNext()) temp = Current;
			Reset();

			if (!open) link.Close();
			return temp;
		}

		/// <summary>
		/// Executes the associated command and returns the last result produced from the
		/// database, or null if it produced no results.
		/// <para>
		/// - Note that the concrete implementation of this method may emulate this capability
		/// by retrieving all possible records and discarding them until the last one is found.
		/// Client applications may want to modify the logic of the command to avoid using it.
		/// </para>
		/// </summary>
		/// <returns>The first result produced, or null.</returns>
		public object Last()
		{
			if (IsDisposed) throw new ObjectDisposedException(ToString());

			var link = Command.Link;
			var open = link.IsOpen; if (!open) link.Open();

			object temp = null;
			Reset(); while (MoveNext()) temp = Current;
			Reset();

			if (!open) link.Close();
			return temp;
		}
	}
}
