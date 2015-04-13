// ======================================================== DataQueryEnumerator.cs
namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;

	// ==================================================== 
	/// <summary>
	/// Represents an object able to execute a query command and return the entities produced
	/// as the result of that execution.
	/// </summary>
	public class DataQueryEnumerator<T> : IDataQueryEnumerator<T>, IEnumerator<T> where T : class
	{
		bool _IsDisposed = false;
		DataQuery<T> _Command = null;
		IQueryCommand _CoreCommand = null;
		IEnumerableExecutor _Enumerator = null;
		T _Current = null;
		bool _Indented = false;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="cmd">The query this enumerator is associated with.</param>
		internal DataQueryEnumerator(DataQuery<T> cmd)
		{
			if (cmd == null) throw new ArgumentNullException("cmd", "Command cannot be null.");
			if (cmd.IsDisposed) throw new ObjectDisposedException(cmd.ToString());

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

		~DataQueryEnumerator()
		{
			if (!IsDisposed) OnDispose(false);
		}

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		protected virtual void OnDispose(bool disposing)
		{
			if (disposing) Reset();

			if (_Enumerator != null) _Enumerator.Dispose(); _Enumerator = null;
			if (_CoreCommand != null) _CoreCommand.Dispose(); _CoreCommand = null;
			_Command = null;
			_Current = null;

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
				Command == null ? string.Empty : Command.ToString());

			return IsDisposed ? "disposed::{0}".FormatWith(str) : str;
		}

		/// <summary>
		/// The command associated with this instance.
		/// </summary>
		public DataQuery<T> Command
		{
			get { return _Command; }
		}
		IDataQuery<T> IDataQueryEnumerator<T>.Command
		{
			get { return this.Command; }
		}
		IDataQuery IDataQueryEnumerator.Command
		{
			get { return this.Command; }
		}

		/// <summary>
		/// Gets the current object being enumerated, or null.
		/// </summary>
		public T Current
		{
			get { return _Current; }
		}
		object IEnumerator.Current
		{
			get { return this.Current; }
		}

		/// <summary>
		/// Executes the command if it has not been executed yet in this instance. Returns true
		/// if a new record is available, or false otherwise.
		/// </summary>
		public bool MoveNext()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			// First execution...
			if (_CoreCommand == null)
			{
				_CoreCommand = _Command.GenerateCoreCommand(); if (_CoreCommand == null)
					throw new CannotCreateException(
						"Cannot create a core query command for this '{0}'."
						.FormatWith(this));

				_Enumerator = _CoreCommand.GetEnumerator(); if (_Enumerator == null)
					throw new CannotCreateException(
						"Cannot create a core query enumerator for this '{0}'."
						.FormatWith(this));

				DebugEx.IndentWriteLine("\n- Query: entering '{0}'...".FormatWith(_Command.TraceString()));
				_Indented = true;
			}

			// Current iteration...
			_Current = null; bool r = _Enumerator.MoveNext(); if (r)
			{
				var source = _Enumerator.CurrentRecord;
				if (source == null) { Reset(); return false; } // should not happen

				var map = _Command.Map;
				var taken = false;

				// When tracking entities is quite similar to previous versions...
				if (map.TrackEntities)
				{
					Monitor.Enter(map.UberEntities.SyncRoot, ref taken);
					var node = map.UberEntities.GetNode(source); if (node != null)
					{
						foreach (var meta in node)
						{
							T obj = (T)meta.Entity; if (obj == null)
							{
								// Rare, but the GC may have kicked-in in between...
								DebugEx.IndentWriteLine("\n- Query: meta invalid '{0}'.".FormatWith(meta));
								DebugEx.Unindent();
								continue;
							}

							// A current shall exist... and better it be of the proxy type...
							if (_Current == null) _Current = obj;
							else
								if (map.ProxyType != null &&
									map.ProxyType != _Current.GetType() &&
									map.ProxyType == obj.GetType())
									_Current = obj;

							if (meta.Completed)
							{
								DebugEx.IndentWriteLine("\n- Query: meta completed '{0}'.".FormatWith(meta));
								DebugEx.Unindent();
								continue;
							}

							DebugEx.IndentWriteLine("\n- Query: hydrating meta '{0}'.".FormatWith(meta));
							meta.Record = source.Clone();
							meta.ToRefresh = false;
							map.LoadEntity(meta.Record, obj);
							map.CompleteMembers(meta);
							DebugEx.Unindent();
						}
						node = null; // Beware: it is the real node ref, better not to mesh with it!
					}
				}

				// When not found in the cache, or when no tracking entities...
				if (_Current == null)
				{
					DebugEx.IndentWriteLine("\n- Query: new entity '{0}({1})'".FormatWith(map.EntityType.EasyName(), source));
					_Current = map.NewEntity(); var meta = MetaEntity.Locate(_Current);
					meta.Record = source.Clone();
					meta.ToRefresh = false;
					meta.UberMap = map; if (map.TrackEntities) map.UberEntities.Add(meta);
					map.LoadEntity(meta.Record, _Current);
					map.CompleteMembers(meta);
					DebugEx.Unindent();
				}

				// Finally, free the lock if acquired, and free the temporal record...
				if (taken) Monitor.Exit(map.UberEntities.SyncRoot);
				source.Dispose();
			}
			else Reset(); return r;
		}

		/// <summary>
		/// Resets this enumerator preparing it for a fresh new execution.
		/// </summary>
		public void Reset()
		{
			if (_Enumerator != null) _Enumerator.Dispose(); _Enumerator = null;
			if (_CoreCommand != null) _CoreCommand.Dispose(); _CoreCommand = null;
			if (_Indented) { DebugEx.Unindent(); _Indented = false; }
			_Current = null;
		}

		/// <summary>
		/// Executes the associated command and returns a list with the results.
		/// </summary>
		/// <returns>A list with the results of the execution.</returns>
		public List<T> ToList()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (_Command.IsDisposed) throw new ObjectDisposedException(_Command.ToString());

			var map = _Command.Map;
			if (map.IsDisposed) throw new ObjectDisposedException(map.ToString());
			var repo = map.Repository; if (repo.IsDisposed) throw new ObjectDisposedException(repo.ToString());
			var link = repo.Link; if (link.IsDisposed) throw new ObjectDisposedException(link.ToString());

			var opened = link.IsOpen; if (!opened) link.Open();

			var list = new List<T>();
			Reset(); while (MoveNext()) list.Add(Current);
			Reset();

			if (!opened) link.Close();
			return list;
		}
		IList IDataQueryEnumerator.ToList()
		{
			return this.ToList();
		}

		/// <summary>
		/// Executes the associated command and returns an array with the results.
		/// </summary>
		/// <returns>An array with the results of the execution.</returns>
		public T[] ToArray()
		{
			return this.ToList().ToArray();
		}
		object[] IDataQueryEnumerator.ToArray()
		{
			return this.ToArray();
		}

		/// <summary>
		/// Executes the associated command and returns the first result produced from the
		/// database, or null if it produced no results.
		/// </summary>
		/// <returns>The first result produced, or null.</returns>
		public T First()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (_Command.IsDisposed) throw new ObjectDisposedException(_Command.ToString());

			var map = _Command.Map;
			if (map.IsDisposed) throw new ObjectDisposedException(map.ToString());
			var repo = map.Repository; if (repo.IsDisposed) throw new ObjectDisposedException(repo.ToString());
			var link = repo.Link; if (link.IsDisposed) throw new ObjectDisposedException(link.ToString());

			var open = link.IsOpen; if (!open) link.Open();

			var obj = (T)null;
			Reset(); if (MoveNext()) obj = Current;
			Reset();

			if (!open) link.Close();
			return obj;
		}
		object IDataQueryEnumerator.First()
		{
			return this.First();
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
		public T Last()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (_Command.IsDisposed) throw new ObjectDisposedException(_Command.ToString());

			var map = _Command.Map;
			if (map.IsDisposed) throw new ObjectDisposedException(map.ToString());
			var repo = map.Repository; if (repo.IsDisposed) throw new ObjectDisposedException(repo.ToString());
			var link = repo.Link; if (link.IsDisposed) throw new ObjectDisposedException(link.ToString());

			var open = link.IsOpen; if (!open) link.Open();

			var obj = (T)null;
			Reset(); while (MoveNext()) obj = Current;
			Reset();

			if (!open) link.Close();
			return obj;
		}
		object IDataQueryEnumerator.Last()
		{
			return this.Last();
		}
	}
}
// ======================================================== 
