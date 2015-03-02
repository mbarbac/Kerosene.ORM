// ======================================================== DataQueryEnumerator.cs
namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.ORM.Core;
	using Kerosene.ORM.Core.Concrete;
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Represents an object able to execute a data query command and to enumerate through the
	/// entities produced by its execution.
	/// </summary>
	public class DataQueryEnumerator<T> : IDataQueryEnumerator<T>, IEnumerator<T> where T : class
	{
		bool _IsDisposed = false;
		DataQuery<T> _Command = null;
		IQueryCommand _CoreCommand = null;
		IEnumerableExecutor _Enumerator = null;
		T _Current = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="cmd">The query this enumerator is associated with.</param>
		public DataQueryEnumerator(DataQuery<T> cmd)
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
		/// The query command this enumerator is associated with.
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
				if ((_CoreCommand = _Command.GenerateCoreCommand()) == null)
					throw new CannotCreateException(
						"Cannot create a core query command for this '{0}'."
						.FormatWith(this));

				if ((_Enumerator = _CoreCommand.GetEnumerator()) == null)
					throw new CannotCreateException(
						"Cannot create a core query enumerator for this '{0}'."
						.FormatWith(this));

				DebugEx.IndentWriteLine("\n- Query: entering '{0}'..."
					.FormatWith(_Command.TraceString()));
			}

			// Iterations...
			_Current = null; bool r = _Enumerator.MoveNext(); if (r)
			{
				var source = _Enumerator.CurrentRecord;
				if (source == null) { Reset(); return false; }

				var map = _Command.Map; map.WithEntitiesLock(() =>
				{
					var spec = map.ExtractId(source);
					var list = map.CacheList(spec); spec.Dispose(); spec = null;

					if (list.Count != 0) // Updating the entities in the cache...
					{
						foreach (var meta in list)
						{
							T obj = (T)meta.Entity; // Avoiding GC...

							if (obj == null) // GC might kick-in while processing the list...
							{
								DebugEx.IndentWriteLine("\n- Query: collected '{0}'...".FormatWith(meta));
								meta.Reset();
								DebugEx.Unindent();
								continue;
							}

							if (_Current == null) _Current = obj;
							else if (
								map.ProxyType != null &&
								Current.GetType() != map.ProxyType &&
								obj.GetType() == map.ProxyType)
								_Current = obj;

							if (meta.Completed)
							{
								DebugEx.IndentWriteLine("\n- Query: completed '{0}'...".FormatWith(meta));
								DebugEx.Unindent();
								continue;
							}

							DebugEx.IndentWriteLine("\n- Query: hydrating '{0}'...".FormatWith(meta));
							meta.SetRecord(source.Clone(), disposeOld: true);
							meta.ToRefresh = false;
							map.LoadEntity(meta.Record, obj);
							map.CompleteMembers(meta);
							DebugEx.Unindent();
						}
						list.Clear(); list = null;
					}

					if (_Current == null) // Creating a new entity if needed...
					{
						var str = map.EntityType.EasyName();
						if (map.ProxyType != null) str += ":Proxy";
						DebugEx.IndentWriteLine("\n- Query: creating '{0}({1})'...".FormatWith(str, source));

						_Current = map.NewEntity(); var meta = MetaEntity.Locate(_Current, create: true);
						map.UberEntities.Add(meta);
						meta.Map = map;
						meta.SetRecord(source.Clone(), disposeOld: true);
						meta.ToRefresh = false;
						map.LoadEntity(meta.Record, _Current);
						map.CompleteMembers(meta);

						DebugEx.Unindent();
					}
				});

				source.Dispose();
			}
			else Reset();

			// Finishing ...
			return r;
		}

		/// <summary>
		/// Resets this enumerator preparing it for a fresh new execution.
		/// </summary>
		public void Reset()
		{
			if (_Enumerator != null) _Enumerator.Dispose(); _Enumerator = null;
			if (_CoreCommand != null)
			{
				DebugEx.Unindent();
				_CoreCommand.Dispose(); _CoreCommand = null;
			}
			_Current = null;
		}

		/// <summary>
		/// Executes the associated query and returns a list containing the results.
		/// </summary>
		/// <returns>A list containing the results requested.</returns>
		public List<T> ToList()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (_Command.IsDisposed) throw new ObjectDisposedException(_Command.ToString());

			var map = _Command.Map;
			if (map.IsDisposed) throw new ObjectDisposedException(map.ToString());
			var repo = map.Repository; if (repo.IsDisposed) throw new ObjectDisposedException(repo.ToString());
			var link = repo.Link; if (link.IsDisposed) throw new ObjectDisposedException(link.ToString());

			var open = link.IsOpen; if (!open) link.Open();

			var list = new List<T>();
			Reset(); while (MoveNext()) list.Add(Current);
			Reset();

			if (!open) link.Close();
			return list;
		}
		IList IDataQueryEnumerator.ToList()
		{
			return this.ToList();
		}

		/// <summary>
		/// Executes the associated query and returns aa arrays containing the results.
		/// </summary>
		/// <returns>An array containing the results requested.</returns>
		public T[] ToArray()
		{
			return this.ToList().ToArray();
		}
		object[] IDataQueryEnumerator.ToArray()
		{
			return this.ToArray();
		}

		/// <summary>
		/// Executes the associated query and returns the first instance produced, or null if
		/// no one was produced.
		/// </summary>
		/// <returns>The first instance produced, or null.</returns>
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
		/// Executes the associated query command and returns the first instance produced, or
		/// null if no one was produced.
		/// <para>
		/// This method is provided as a fall-back mechanism because it retrieves all possible
		/// results discarding them until the last one is found. Client applications have to
		/// reconsider the logic of their command to avoid using this method if possible.
		/// </para>
		/// </summary>
		/// <returns>The first instance produced, or null.</returns>
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
