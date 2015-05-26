using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Kerosene.ORM.Maps.Concrete
{
	// ====================================================
	/// <summary>
	/// Represents a repository for a set of maps between POCO classes and their associated
	/// primary tables in the underlying database-alike service, implementing both the Dynamic
	/// Repository and Dynamic Unit of Work patterns.
	/// </summary>
	public class DataRepository : IDataRepository
	{
		bool _IsDisposed = false;
		ulong _SerialId = 0;
		IDataLink _Link = null;
		object _MasterLock = new object();
		UberMapCollection _UberMaps = null;
		bool _WeakMapsEnabled = Uber.EnableWeakMaps;
		bool _TrackEntities = Uber.TrackEntities;
		bool _TrackChildEntities = Uber.TrackChildEntities;
		System.Timers.Timer _Timer = null;
		bool _TimerDisposed = false;
		int _Interval = Uber.CollectorInterval;
		bool _EnableGC = Uber.EnableCollectorGC;
		UberOperationList _UberOperations = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="link">The link this repository will be associated with.</param>
		public DataRepository(IDataLink link)
		{
			if (link == null) throw new ArgumentNullException("link", "Data Link cannot be null.");
			if (link.IsDisposed) throw new ObjectDisposedException(link.ToString());

			_Link = link;
			_SerialId = ++Uber.RepositoryLastSerial;
			_UberMaps = new UberMapCollection(this);
			_UberOperations = new UberOperationList(this);

			if (Uber.EnableCollector) EnableCollector();
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

		/// <summary>
		/// </summary>
		~DataRepository()
		{
			if (!IsDisposed) OnDispose(false);
		}

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		protected virtual void OnDispose(bool disposing)
		{
			if (_Timer != null)
			{
				_TimerDisposed = true;
				_Timer.Stop(); _Timer.Dispose();
			}

			if (disposing)
			{
				if (_UberOperations != null) { DiscardChanges(); _UberOperations.Dispose(); }
				if (_UberMaps != null) { ClearMaps(); _UberMaps.Dispose(); }
			}

			_UberOperations = null;
			_UberMaps = null;
			_Link = null;
			_Timer = null;

			_IsDisposed = true;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			var str = string.Format("{0}:{1}({2})",
				SerialId,
				GetType().EasyName(),
				Link.Sketch());

			return IsDisposed ? "disposed::{0}".FormatWith(str) : str;
		}

		/// <summary>
		/// Returns a new instance that will be associated with the new given link and that will
		/// contain a copy of the maps and customizations of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		public DataRepository Clone(IDataLink link)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (link == null) throw new ArgumentNullException("link", "Link cannot be null.");
			if (link.IsDisposed) throw new ObjectDisposedException(link.ToString());

			var cloned = new DataRepository(link); OnClone(cloned);
			return cloned;
		}
		IDataRepository IDataRepository.Clone(IDataLink link)
		{
			return this.Clone(link);
		}

		/// <summary>
		/// Invoked when cloning this object to set its state at this point of the inheritance
		/// chain.
		/// </summary>
		/// <param name="cloned">The cloned object.</param>
		protected virtual void OnClone(object cloned)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			var temp = cloned as DataRepository;
			if (cloned == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' one."
				.FormatWith(cloned.Sketch(), typeof(DataRepository).EasyName()));

			var enabled = temp.IsCollectorEnabled; if (enabled) temp.DisableCollector();
			lock (MasterLock)
			{
				foreach (var map in _UberMaps) map.Clone(temp);
				temp._WeakMapsEnabled = _WeakMapsEnabled;
				temp._TrackEntities = _TrackEntities;
				temp._TrackChildEntities = _TrackChildEntities;
				temp._Interval = _Interval;
				temp._EnableGC = _EnableGC;
			}
			if (enabled) temp.EnableCollector();
		}

		/// <summary>
		/// The serial id assigned to this instance.
		/// </summary>
		public ulong SerialId
		{
			get { return _SerialId; }
		}

		/// <summary>
		/// The link with the underlying database-alike service this instance is associated with.
		/// </summary>
		public IDataLink Link
		{
			get { return _Link; }
		}

		/// <summary>
		/// The object that can be used to synchronize operations related to the repository and
		/// associated elements.
		/// </summary>
		internal object MasterLock
		{
			get { return _MasterLock; }
		}

		/// <summary>
		/// The collection of maps registered into this repository.
		/// </summary>
		internal UberMapCollection UberMaps
		{
			get { return _UberMaps; }
		}

		/// <summary>
		/// The collection of maps registered into this repository.
		/// </summary>
		public IEnumerable<IDataMap> Maps
		{
			get { return UberMaps; }
		}

		/// <summary>
		/// Whether weak maps are enabled for this instance or not.
		/// </summary>
		public bool WeakMapsEnabled
		{
			get { return _WeakMapsEnabled; }
			set { _WeakMapsEnabled = value; }
		}

		/// <summary>
		/// Locates the map registered to manage the entities of the given type, or tries to
		/// create a new weak map otherwise if possible. Returns null if no map is found and
		/// no weak map can be created.
		/// </summary>
		/// <param name="type">The type of the entities managed by the map to locate.</param>
		/// <param name="table">A dynamic lambda expression that resolves into the name of the
		/// primary table, used when creating a weak map. If null the a number of suitable
		/// names are tried based upon the name of the type.</param>
		/// <returns>The requested map, or null.</returns>
		internal IUberMap LocateUberMap(Type type, Func<dynamic, object> table = null)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (type == null) throw new ArgumentNullException("type", "Type cannot be null.");

			lock (ProxyGenerator.ProxyLock)
			{
				var holder = ProxyGenerator.ProxyHolders.Find(type);
				if (holder != null) type = holder.ProxyType.BaseType;
			}

			IUberMap map = null; lock (MasterLock)
			{
				map = _UberMaps.Find(type); if (map == null)
				{
					var generic = typeof(DataMap<>);
					var concrete = generic.MakeGenericType(new Type[] { type });
					var cons = concrete.GetConstructor(new Type[] { typeof(DataRepository), typeof(string) });

					if (table != null)
					{
						var name = DynamicInfo.ParseName(table);
						map = (IUberMap)cons.Invoke(new object[] { this, name });
						map.IsWeakMap = true;
					}
					else if (WeakMapsEnabled)
					{
						var name = Uber.FindTableName(Link, type.Name);
						if (name != null)
						{
							map = (IUberMap)cons.Invoke(new object[] { this, name });
							map.IsWeakMap = true;
						}
					}
				}
			}

			return map;
		}

		/// <summary>
		/// Locates the map registered to manage the entities of the given type, or tries to
		/// create a new weak map otherwise if possible. Returns null if no map is found and
		/// no weak map can be created.
		/// </summary>
		/// <param name="type">The type of the entities managed by the map to locate.</param>
		/// <param name="table">A dynamic lambda expression that resolves into the name of the
		/// primary table, used when creating a weak map. If null the a number of suitable
		/// names are tried based upon the name of the type.</param>
		/// <returns>The requested map, or null.</returns>
		public IDataMap LocateMap(Type type, Func<dynamic, object> table = null)
		{
			return LocateUberMap(type, table);
		}

		/// <summary>
		/// Locates the map registered to manage the entities of the given type, or tries to
		/// create a new weak map otherwise if possible. Returns null if no map is found and
		/// no weak map can be created.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to locate.</typeparam>
		/// <param name="table">A dynamic lambda expression that resolves into the name of the
		/// primary table, used when creating a weak map. If null the a number of suitable
		/// names are tried based upon the name of the type.</param>
		/// <returns>The requested map, or null.</returns>
		public DataMap<T> LocateMap<T>(Func<dynamic, object> table = null) where T : class
		{
			return (DataMap<T>)LocateMap(typeof(T), table);
		}
		IDataMap<T> IDataRepository.LocateMap<T>(Func<dynamic, object> table)
		{
			return this.LocateMap<T>(table);
		}

		/// <summary>
		/// Clears and disposes all the maps registered into this instance, and reverts its
		/// managed entities, if any is tracked, to a detached state.
		/// </summary>
		public void ClearMaps()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			lock (MasterLock)
			{
				var maps = UberMaps.ToArray(); foreach (var map in maps) map.Dispose();
				Array.Clear(maps, 0, maps.Length);
				UberMaps.Clear();
			}
		}

		/// <summary>
		/// Whether tracking of entities is enabled or disabled, in principle, for the maps that
		/// are registered into this instance. The setter cascades the new value into all the
		/// maps registered at the moment when the new value is set.
		/// </summary>
		public bool TrackEntities
		{
			get { return _TrackEntities; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				lock (MasterLock)
				{
					foreach (var map in UberMaps) map.TrackEntities = value;
				}
				_TrackEntities = value;
			}
		}

		/// <summary>
		/// Whether to track child entities for dependency properties or not.
		/// </summary>
		public bool TrackChildEntities
		{
			get { return _TrackChildEntities; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());

				lock (MasterLock)
				{
					foreach (var map in UberMaps) map.TrackChildEntities = value;
				}
				_TrackChildEntities = value;
			}
		}

		/// <summary>
		/// The collection of tracked entities that are in a valid state of the maps of this repository.
		/// </summary>
		public IEnumerable<object> Entities
		{
			get
			{
				if (!IsDisposed)
				{
					foreach (var map in UberMaps)
						foreach (var obj in map.Entities) yield return obj;
				}
			}
		}

		/// <summary>
		/// Clears the cache of tracked entities of the maps on this repository.
		/// </summary>
		public void ClearEntities()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			lock (MasterLock)
			{
				foreach (var map in UberMaps) map.ClearEntities();
			}
		}

		/// <summary>
		/// Gets or creates a valid map for the given type of entities, or throws an exception
		/// if such map cannot be found.
		/// </summary>
		/// <typeparam name="T">The type of the entities of the map to find.</typeparam>
		/// <returns>The requested map.</returns>
		DataMap<T> GetValidMap<T>() where T : class
		{
			var map = LocateMap<T>();
			if (map == null) throw new NotFoundException(
				"Map for type '{0}' cannot be found for this '{1}'."
				.FormatWith(typeof(T).EasyName(), this));

			return map;
		}

		/// <summary>
		/// Creates a new entity with the appropriate type for the requested map.
		/// <para>This method is invoked to generate instances that support virtual lazy
		/// properties when needed. Client applications can use but it is not needed.</para>
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <returns>A new entity.</returns>
		public T NewEntity<T>() where T : class
		{
			return GetValidMap<T>().NewEntity();
		}

		/// <summary>
		/// Attaches the given entity into this map.
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <param name="entity">The entity to attach into this instance.</param>
		public void Attach<T>(T entity) where T : class
		{
			GetValidMap<T>().Attach(entity);
		}

		/// <summary>
		/// Removes the given entity from this map, making it become a detached one. Returns true
		/// if the entity has been removed, or false otherwise.
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <param name="entity">The entity to detach from this instance.</param>
		/// <returns>True if the instance has been removed, false otherwise.</returns>
		public bool Detach<T>(T entity) where T : class
		{
			return GetValidMap<T>().Detach(entity);
		}

		/// <summary>
		/// Creates a new query command for the entities managed by this map.
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <returns>A new query command.</returns>
		public DataQuery<T> Query<T>() where T : class
		{
			return GetValidMap<T>().Query();
		}
		IDataQuery<T> IDataRepository.Query<T>()
		{
			return this.Query<T>();
		}

		/// <summary>
		/// Creates a new query command for the entities managed by this map, and sets the initial
		/// contents of its WHERE clause.
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <param name="where">The dynamic lambda expression that resolves into the contents of
		/// this clause.</param>
		/// <returns>A new query command.</returns>
		public DataQuery<T> Where<T>(Func<dynamic, object> where) where T : class
		{
			return Query<T>().Where(where);
		}
		IDataQuery<T> IDataRepository.Where<T>(Func<dynamic, object> where)
		{
			return this.Where<T>(where);
		}

		/// <summary>
		/// Finds and returns inmediately a suitable entity that meets the conditions given, by
		/// looking for it in the managed cache and, if it cannot be found there, querying the
		/// database for it. Returns null if such entity cannot be found neither in the cache
		/// nor in the database.
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <param name="specs">A collection of dynamic lambda expressions each containing the
		/// name and value to find for a column, as in: 'x => x.Column == Value'.</param>
		/// <returns>The requested entity, or null.</returns>
		public T FindNow<T>(params Func<dynamic, object>[] specs) where T : class
		{
			return GetValidMap<T>().FindNow(specs);
		}

		/// <summary>
		/// Refreshes inmediately the contents of the given entity (and potentially of its
		/// dependencies), along with all the entities in the cache that share the same
		/// identity.
		/// <para>Returns null if the entity cannot be found any longer in the database, or
		/// a refreshed entity otherwise. In the later case it is NOT guaranteed that the one
		/// returned is the same as the original one, but potentially any other suitable one.</para>
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <param name="entity">The entitt to refresh.</param>
		/// <returns>A refreshed entity, or null.</returns>
		public T RefreshNow<T>(T entity) where T : class
		{
			return GetValidMap<T>().RefreshNow(entity);
		}

		/// <summary>
		/// Creates a new delete operation for the given entity.
		/// <para>The new command must be firstly submitted into the associated repository in
		/// order it to be executed when all pending change operations annotated into that
		/// repository are executed as a group.</para>
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <param name="entity">The entity to be inserted.</param>
		/// <returns>A new command.</returns>
		public DataDelete<T> Delete<T>(T entity) where T : class
		{
			return GetValidMap<T>().Delete(entity);
		}
		IDataDelete<T> IDataRepository.Delete<T>(T entity)
		{
			return this.Delete<T>(entity);
		}

		/// <summary>
		/// Convenience method to create the operation associated with the given entity, to
		/// submit it into the repository, and to execute inmediately all the pending change
		/// operations annotated into it.
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <param name="entity">The entity affected by the operation.</param>
		public void DeleteNow<T>(T entity) where T : class
		{
			var cmd = Delete<T>(entity);
			cmd.Submit();
			ExecuteChanges();
		}

		/// <summary>
		/// Creates a new save (insert/update) operation for the given entity.
		/// <para>The new command must be firstly submitted into the associated repository in
		/// order it to be executed when all pending change operations annotated into that
		/// repository are executed as a group.</para>
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <param name="entity">The entity to be inserted.</param>
		/// <returns>A new command.</returns>
		public DataSave<T> Save<T>(T entity) where T : class
		{
			return GetValidMap<T>().Save(entity);
		}
		IDataSave<T> IDataRepository.Save<T>(T entity)
		{
			return this.Save<T>(entity);
		}

		/// <summary>
		/// Convenience method to create the operation associated with the given entity, to
		/// submit it into the repository, and to execute inmediately all the pending change
		/// operations annotated into it.
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <param name="entity">The entity affected by the operation.</param>
		public void SaveNow<T>(T entity) where T : class
		{
			var cmd = Save<T>(entity);
			cmd.Submit();
			ExecuteChanges();
		}

		/// <summary>
		/// Creates a new insert operation for the given entity.
		/// <para>The new command must be firstly submitted into the associated repository in
		/// order it to be executed when all pending change operations annotated into that
		/// repository are executed as a group.</para>
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <param name="entity">The entity to be inserted.</param>
		/// <returns>A new command.</returns>
		public DataInsert<T> Insert<T>(T entity) where T : class
		{
			return GetValidMap<T>().Insert(entity);
		}
		IDataInsert<T> IDataRepository.Insert<T>(T entity)
		{
			return this.Insert<T>(entity);
		}

		/// <summary>
		/// Convenience method to create the operation associated with the given entity, to
		/// submit it into the repository, and to execute inmediately all the pending change
		/// operations annotated into it.
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <param name="entity">The entity affected by the operation.</param>
		public void InsertNow<T>(T entity) where T : class
		{
			var cmd = Insert<T>(entity);
			cmd.Submit();
			ExecuteChanges();
		}

		/// <summary>
		/// Creates a new delete operation for the given entity.
		/// <para>The new command must be firstly submitted into the associated repository in
		/// order it to be executed when all pending change operations annotated into that
		/// repository are executed as a group.</para>
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <param name="entity">The entity to be inserted.</param>
		/// <returns>A new command.</returns>
		public DataUpdate<T> Update<T>(T entity) where T : class
		{
			return GetValidMap<T>().Update(entity);
		}
		IDataUpdate<T> IDataRepository.Update<T>(T entity)
		{
			return this.Update<T>(entity);
		}

		/// <summary>
		/// Convenience method to create the operation associated with the given entity, to
		/// submit it into the repository, and to execute inmediately all the pending change
		/// operations annotated into it.
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <param name="entity">The entity affected by the operation.</param>
		public void UpdateNow<T>(T entity) where T : class
		{
			var cmd = Update<T>(entity);
			cmd.Submit();
			ExecuteChanges();
		}

		/// <summary>
		/// The list of operations submitted into this instance.
		/// </summary>
		internal UberOperationList UberOperations
		{
			get { return _UberOperations; }
		}

		/// <summary>
		/// A temporary log of changes.
		/// </summary>
		internal List<ChangeEntry> ChangeEntries { get; private set; }

		/// <summary>
		/// Executes the change operations annotated into this instance against the underlying
		/// database as a single logical operation that either succeeds or fails as a whole.
		/// </summary>
		public void ExecuteChanges()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			lock (MasterLock)
			{
				ChangeEntries = new List<ChangeEntry>();
				try
				{
					DebugEx.IndentWriteLine("\n--- Executing changes for '{0}'...".FormatWith(this));

					Link.Transaction.Start();
					foreach (var op in UberOperations) op.OnExecute();
					Link.Transaction.Commit();

					DebugEx.Unindent();
					DebugEx.IndentWriteLine("\n--- Refreshing changes for '{0}'...".FormatWith(this));

					ChangeEntries.Reverse();
					foreach (var entry in ChangeEntries) entry.MetaEntity.Completed = false;
					foreach (var entry in ChangeEntries)
					{
						if (entry.MetaEntity.Completed) continue;
						if (entry.ChangeType == ChangeType.Delete) continue;
						entry.UberMap.RefreshNow(entry.Entity);
					}
				}
				catch (Exception e)
				{
					DebugEx.Unindent();
					DebugEx.IndentWriteLine("\n--- Reverting changes for '{0}' because '{1}'...", this, e.ToDisplayString());

					Link.Transaction.Abort();

					ChangeEntries.Reverse();
					foreach (var entry in ChangeEntries)
					{
						switch (entry.ChangeType)
						{
							case ChangeType.Delete: entry.UberMap.Attach(entry.Entity); break;
							case ChangeType.Insert: entry.UberMap.Detach(entry.Entity); break;
						}
					}
					foreach (var entry in ChangeEntries) entry.MetaEntity.Completed = false;
					foreach (var entry in ChangeEntries)
					{
						if (entry.MetaEntity.Completed) continue;
						if (entry.ChangeType == ChangeType.Delete) continue;
						entry.UberMap.RefreshNow(entry.Entity);
					}

					throw;
				}
				finally
				{
					if (ChangeEntries != null) { foreach (var entry in ChangeEntries) entry.Dispose(); ChangeEntries.Clear(); } ChangeEntries = null;
					DiscardChanges();
					DebugEx.Unindent();
				}
			}
		}

		/// <summary>
		/// Discards all the pending change operations that may have been annotated into this
		/// repository.
		/// </summary>
		public void DiscardChanges()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			lock (MasterLock)
			{
				var ops = UberOperations.ToArray();
				foreach (var op in ops) op.Dispose();
				Array.Clear(ops, 0, ops.Length);
				UberOperations.Clear();
			}
		}

		/// <summary>
		/// Whether the internal collector of entities is enabled or not.
		/// </summary>
		public bool IsCollectorEnabled
		{
			get { return _Timer == null ? false : _Timer.Enabled; }
		}

		/// <summary>
		/// Enables the internal collector of entities, or resumes its operation.
		/// <para>This method is intended for specialized scenarios only.</para>
		/// </summary>
		public void EnableCollector()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			if (!_TimerDisposed)
			{
				if (_Timer == null)
				{
					_Timer = new System.Timers.Timer();
					_Timer.Elapsed += new System.Timers.ElapsedEventHandler(Collector);
				}
				_Timer.Interval = _Interval;
				_Timer.Enabled = true;
			}
		}

		/// <summary>
		/// Enables the internal collector of entities, or resumes its operation.
		/// <para>This method is intended for specialized scenarios only.</para>
		/// </summary>
		/// <param name="milliseconds">The interval at which the collector is fired.</param>
		/// <param name="enableGC">Whether to force a CLR Garbage Collection before the internal
		/// collector is fired or not.</param>
		public void EnableCollector(int milliseconds, bool enableGC)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			if (milliseconds < 0) milliseconds = Uber.CollectorInterval;
			if (milliseconds < Uber.DEFAULT_COLLECTOR_MIN_INTERVAL)
				milliseconds = Uber.DEFAULT_COLLECTOR_MIN_INTERVAL;

			_Interval = milliseconds;
			_EnableGC = enableGC;

			EnableCollector();
		}

		/// <summary>
		/// Suspends or disables the operations of the internal collector of entities.
		/// <para>This method is intended for specialized scenarios only.</para>
		/// </summary>
		public void DisableCollector()
		{
			if (_Timer != null) _Timer.Stop();
		}

		/// <summary>
		/// The internal collector of invalid entities.
		/// </summary>
		private void Collector(object source, System.Timers.ElapsedEventArgs args)
		{
			if (IsDisposed) return;

			lock (MasterLock)
			{
				DebugEx.IndentWriteLine("\n- Collector fired for '{0}'", this);

				var enabled = IsCollectorEnabled; if (enabled) DisableCollector();
				if (_EnableGC) GC.Collect();

				if (_UberMaps != null)
					foreach(var map in _UberMaps) map.CollectInvalidEntities();

				if (enabled && _Timer != null) EnableCollector();

				DebugEx.Unindent();
			}
		}
	}
}
