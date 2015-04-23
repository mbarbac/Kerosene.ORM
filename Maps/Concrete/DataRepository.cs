namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;

	// ==================================================== 
	/// <summary>
	/// Extends the <see cref="IDataRepository"/> interface.
	/// </summary>
	internal interface IUberRepository : IDataRepository
	{
		/// <summary>
		/// The collection of maps registered into this instance.
		/// </summary>
		UberMapSet UberMaps { get; }

		/// <summary>
		/// Returns the map registeres to manage the entities of the given type or, if such
		/// map is not found, creates a new one using the table name given or, is such is null,
		/// tries to automatically locate a suitable table in the database and, if so, creates
		/// a weak map for that type if weak maps are are enabled. Returns null if finally a
		/// map cannot be located.
		/// </summary>
		/// <param name="type">The type of the entities of the map to locate.</param>
		/// <param name="table">A dynamic lambda expression that resolves into the name of the
		/// primary table where to find, at least, the identity columns associated with the map.
		/// If this argument is null then a number of pluralization rules are automatically used
		/// based upon the name of the type.</param>
		/// <returns>The requested map, or null.</returns>
		IUberMap LocateUberMap(Type type, Func<dynamic, object> table = null);

		/// <summary>
		/// The list of pending change operations annotated into this instance.
		/// </summary>
		UberOperationList UberOperations { get; }

		/// <summary>
		/// Whether the internal collector of invalid entities is enabled or not.
		/// </summary>
		bool IsCollectorEnabled { get; }

		/// <summary>
		/// Enables the internal collector of invalid entities, or resumes its operation.
		/// <para>This method is intended for specialized scenarios only.</para>
		/// </summary>
		void EnableCollector();

		/// <summary>
		/// Enables the internal collector of invalid entities, or resumes its operation.
		/// <para>This method is intended for specialized scenarios only.</para>
		/// </summary>
		/// <param name="milliseconds">The interval at which the collector is fired.</param>
		/// <param name="enableGC">Whether to force a CLR Garbage Collection before the internal
		/// collector is fired or not.</param>
		void EnableCollector(int milliseconds, bool enableGC);

		/// <summary>
		/// Suspends or disables the operations of the internal collector of invalid entities.
		/// <para>This method is intended for specialized scenarios only.</para>
		/// </summary>
		void DisableCollector();
	}

	// ==================================================== 
	/// <summary>
	/// Represents a repository for a set of maps between their POCO classes and their related
	/// primary tables in the underlying database-alike service, implementing both the Dynamic
	/// Repository and the Dynamic Unit Of Work patterns.
	/// </summary>
	public class DataRepository : IDataRepository, IUberRepository
	{
		bool _IsDisposed = false;
		ulong _SerialId = 0;
		IDataLink _Link = null;
		bool _WeakMapsEnabled = UberHelper.EnableWeakMaps;
		bool _TrackEntities = UberHelper.TrackEntities;
		UberMapSet _UberMaps = new UberMapSet();
		UberOperationList _UberOperations = new UberOperationList();
		System.Timers.Timer _Timer = null;
		int _Interval = UberHelper.CollectorInterval;
		bool _EnableGC = UberHelper.EnableCollectorGC;
		HashSet<Type> _DiscardedTypes = new HashSet<Type>();

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="link">The link with the underlying database this repository will use.</param>
		public DataRepository(IDataLink link)
		{
			if (link == null) throw new ArgumentNullException("link", "Data Link cannot be null.");
			if (link.IsDisposed) throw new ObjectDisposedException(link.ToString());
			_Link = link;
			_SerialId = ++(UberHelper.RepositoryLastSerial);

			if (UberHelper.EnableCollector) EnableCollector();
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
			if (_Timer != null) { _Timer.Stop(); _Timer.Dispose(); }

			if (disposing)
			{
				if (_UberOperations != null && !_IsDisposed) DiscardChanges();
				if (_UberMaps != null && !_IsDisposed) ClearMaps();
				if (_DiscardedTypes != null && !_IsDisposed) _DiscardedTypes.Clear();
			}

			_UberOperations = null;
			_UberMaps = null;
			_Link = null;
			_Timer = null;
			_DiscardedTypes = null;

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
		/// Returns a new repository associated with the given link containing a copy of the
		/// original maps and customizations existing in the original one. Cloned maps will not
		/// be validated and can be modified as needed.
		/// </summary>
		/// <param name="link">The link the new respository will be associated with.</param>
		/// <returns>A new respository.</returns>
		public DataRepository Clone(IDataLink link)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (link == null) throw new ArgumentNullException("link", "Data Link cannot be null.");
			if (link.IsDisposed) throw new ObjectDisposedException(link.ToString());

			var cloned = new DataRepository(link);
			OnClone(cloned); return cloned;
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

			temp.DisableCollector(); lock (_UberMaps.SyncRoot)
			{
				foreach (var map in _UberMaps) map.Clone(temp);
				temp._WeakMapsEnabled = _WeakMapsEnabled;
				temp._TrackEntities = _TrackEntities;
				temp._Interval = _Interval;
				temp._EnableGC = _EnableGC;
			}
			if (_TrackEntities) temp.EnableCollector();
		}

		/// <summary>
		/// The serial id assigned to this instance.
		/// </summary>
		public ulong SerialId
		{
			get { return _SerialId; }
		}

		/// <summary>
		/// The database-alike service link this instance is associated with.
		/// </summary>
		public IDataLink Link
		{
			get { return _Link; }
		}

		/// <summary>
		/// The collection of maps registered into this instance.
		/// </summary>
		internal UberMapSet UberMaps
		{
			get { return _UberMaps; }
		}
		UberMapSet IUberRepository.UberMaps
		{
			get { return this.UberMaps; }
		}

		/// <summary>
		/// The collection of maps registered into this repository.
		/// </summary>
		public IEnumerable<IDataMap> Maps
		{
			get { return UberMaps; }
		}

		/// <summary>
		/// Clears and disposes all the maps registered into this instance.
		/// </summary>
		public void ClearMaps()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			lock (UberMaps.SyncRoot)
			{
				var maps = UberMaps.ToArray(); foreach (var map in maps) map.Dispose();
				Array.Clear(maps, 0, maps.Length); maps = null;
				UberMaps.Clear();
			}
		}

		/// <summary>
		/// Whether weak maps are enabled or not for this instance.
		/// <para>Weak maps are created automatically when an entity type is referenced by any
		/// map operation and there was no registered map for that type. Weak maps are disposed
		/// if a regular non-weak map is registered (created) explicitly.</para>
		/// </summary>
		public bool WeakMapsEnabled
		{
			get { return _WeakMapsEnabled; }
			set { _WeakMapsEnabled = value; }
		}

		/// <summary>
		/// Returns the map registeres to manage the entities of the given type or, if such
		/// map is not found, creates a new one using the table name given or, is such is null,
		/// tries to automatically locate a suitable table in the database and, if so, creates
		/// a weak map for that type if weak maps are are enabled. Returns null if finally a
		/// map cannot be located.
		/// </summary>
		/// <param name="type">The type of the entities of the map to locate.</param>
		/// <param name="table">A dynamic lambda expression that resolves into the name of the
		/// primary table where to find, at least, the identity columns associated with the map.
		/// If this argument is null then a number of pluralization rules are automatically used
		/// based upon the name of the type.</param>
		/// <returns>The requested map, or null.</returns>
		internal IUberMap LocateUberMap(Type type, Func<dynamic, object> table = null)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (type == null) throw new ArgumentNullException("type", "Type cannot be null.");

			// For performance purposes when auto-maps as there is little control on what types
			// are passed...
			if (!type.IsClass) return null;
			if (_DiscardedTypes.Contains(type)) return null;

			var holder = ProxyGenerator.Holders.FindByExtendedType(type);
			if (holder != null) type = holder.ExtendedType.BaseType;

			IUberMap map = null; lock (UberMaps.SyncRoot)
			{
				map = UberMaps.FindByType(type); if (map == null)
				{
					var generic = typeof(DataMap<>);
					var concrete = generic.MakeGenericType(new Type[] { type });
					var constructor = concrete.GetConstructor(
						new Type[] { typeof(DataRepository), typeof(string), typeof(bool) });

					string name = null; if (table != null)
					{
						// If a table is given then the program knows what it wants...
						name = DynamicInfo.ParseName(table);
						map = (IUberMap)constructor.Invoke(new object[] { this, name, false });
					}
					else if (WeakMapsEnabled)
					{
						// Otherwise we implicitly want a weak map, if they are allowed...
						name = UberHelper.LocateTableName(Link, type.Name);
						if (name != null)
							map = (IUberMap)constructor.Invoke(new object[] { this, name, true });
					}
				}
			}

			if (map == null) _DiscardedTypes.Add(type);
			return map;
		}
		IUberMap IUberRepository.LocateUberMap(Type type, Func<dynamic, object> table)
		{
			return this.LocateUberMap(type, table);
		}

		/// <summary>
		/// Resets the internal collection of types that have been discarded and will not be
		/// considered again as candidates to be found in the database.
		/// <para>This method is mostly provided for debugging purposes only.</para>
		/// </summary>
		public void ResetDiscardedTypes()
		{
			if (_DiscardedTypes != null) _DiscardedTypes.Clear();
		}

		/// <summary>
		/// Returns the map registeres to manage the entities of the given type or, if such map
		/// is not found, creates a new one using the table name given or, is such is null, tries
		/// to automatically locate a suitable table in the database and, if so, creates a weak
		/// map for that type if weak maps are are enabled. Returns null if finally a map cannot
		/// be located.
		/// </summary>
		/// <param name="type">The type of the entities of the map to locate.</param>
		/// <param name="table">A dynamic lambda expression that resolves into the name of the
		/// primary table where to find, at least, the identity columns associated with the map.
		/// If this argument is null then a number of pluralization rules are automatically used
		/// based upon the name of the type.</param>
		/// <returns>The requested map, or null.</returns>
		public IDataMap LocateMap(Type type, Func<dynamic, object> table = null)
		{
			if (!type.IsClass) throw new InvalidOperationException("Type '{0}' is not a class type.".FormatWith(type.EasyName()));
			return this.LocateUberMap(type, table);
		}

		/// <summary>
		/// Returns the map registeres to manage the entities of the given type or, if such map
		/// is not found, creates a new one using the table name given or, is such is null, tries
		/// to automatically locate a suitable table in the database and, if so, creates a weak
		/// map for that type if weak maps are are enabled. Returns null if finally a map cannot
		/// be located.
		/// </summary>
		/// <typeparam name="T">The type of the entities of the map to locate.</typeparam>
		/// <param name="table">A dynamic lambda expression that resolves into the name of the
		/// primary table where to find, at least, the identity columns associated with the map.
		/// If this argument is null then a number of pluralization rules are automatically used
		/// based upon the name of the type.</param>
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
		/// Whether the maps registered into this instance keep track of the entities they have
		/// managed in their internal caches, or not. The setter cascades the new value to all
		/// the registered maps.
		/// </summary>
		public bool TrackEntities
		{
			get { return _TrackEntities; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());

				var enabled = IsCollectorEnabled;
				DisableCollector();

				lock (UberMaps.SyncRoot)
				{
					foreach (var map in UberMaps) map.TrackChildEntities = value;
				}
				if (enabled && value) EnableCollector();

				_TrackEntities = value;
			}
		}

		/// <summary>
		/// The current collection of entities in a valid state tracked by the maps registered
		/// into this instance, if any.
		/// </summary>
		public IEnumerable<IMetaEntity> Entities
		{
			get
			{
				// There might be maps in this repo that are explicitly tracking their
				// entities, so we do have to iterate through the maps in this instance...

				if (!IsDisposed)
				{
					var enabled = IsCollectorEnabled; DisableCollector();

					IUberMap[] maps = null; lock (UberMaps.SyncRoot) { maps = UberMaps.ToArray(); }
					foreach (var map in maps)
					{
						if (map.IsDisposed) continue;
						if (!map.TrackEntities) continue;

						foreach (var meta in map.Entities) yield return meta;
					}
					Array.Clear(maps, 0, maps.Length);

					if (enabled) EnableCollector();
				}
			}
		}

		/// <summary>
		/// Clears the caches of tracked entities maintained by the maps registered into this
		/// instance and, optionally, detaches those entities.
		/// </summary>
		/// <param name="detach">True to forcibly detach the entities found in the caches.</param>
		public void ClearEntities(bool detach = true)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			lock (UberMaps.SyncRoot)
			{
				var maps = UberMaps.ToArray();
				foreach (var map in maps) map.ClearEntities(detach);
				Array.Clear(maps, 0, maps.Length);
			}
		}

		/// <summary>
		/// Gets or creates a valid map for the given type, or throws an exception if such was
		/// not possible.
		/// </summary>
		private DataMap<T> GetValidMap<T>() where T : class
		{
			// LocateMap() invokes LocateUberMap() that takes care of finding the appropriate base
			// type of the extended ones when needed...
			var map = LocateMap<T>();
			if (map == null) throw new NotFoundException(
				"Map for type '{0}' cannot be retrieved for this '{1}'."
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
		/// this clause.
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
		/// The list of pending change operations annotated into this instance.
		/// </summary>
		internal UberOperationList UberOperations
		{
			get { return _UberOperations; }
		}
		UberOperationList IUberRepository.UberOperations
		{
			get { return this.UberOperations; }
		}

		/// <summary>
		/// Executes the change operations annotated into this instance against the underlying
		/// database as a single logical operation that either succeeds or fails as a whole.
		/// </summary>
		public void ExecuteChanges()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			bool mapsTaken = false;
			bool opsTaken = false;
			try
			{
				try
				{
					DebugEx.IndentWriteLine("\n--- Executing changes for '{0}'...".FormatWith(this));
					Monitor.Enter(UberMaps.SyncRoot, ref mapsTaken); if (!mapsTaken) throw new InvalidOperationException("Cannot obtain an exclusive lock on the list of maps...");
					Monitor.Enter(UberOperations.SyncRoot, ref opsTaken); if (!opsTaken) throw new InvalidOperationException("Cannot obtain an exclusive lock on the list of operations...");

					Link.Transaction.Start(); foreach (var op in UberOperations) op.Execute();
					Link.Transaction.Commit();
				}
				finally { DebugEx.Unindent(); }
				try
				{
					DebugEx.IndentWriteLine("\n--- Clearing structures for '{0}'...".FormatWith(this));
					while (UberOperations.Count != 0)
					{
						var op = UberOperations[UberOperations.Count - 1];
						var ops = UberOperations.Where(x => object.ReferenceEquals(x.Entity, op.Entity)).ToList();
						foreach (var temp in ops) UberOperations.Remove(temp);

						if (op is IDataDelete)
						{
							op.Map.UberEntities.Remove(op.MetaEntity);
							op.MetaEntity.Clear();
						}
					}
				}
				finally { DebugEx.Unindent(); }
				try
				{
					DebugEx.IndentWriteLine("\n--- Refreshing '{0}'...".FormatWith(this));
					foreach (var map in UberMaps)
					{
						var list = map.UberEntities.Where(x => x.Entity != null && x.ToRefresh).ToList();
						foreach (var meta in list) if (meta.Entity != null && meta.ToRefresh) map.RefreshNow(meta.Entity);
						list.Clear(); list = null;
					}
				}
				finally { DebugEx.Unindent(); }
			}
			catch (Exception e)
			{
				try
				{
					DebugEx.IndentWriteLine("\n----- Aborting changes for '{0}'...".FormatWith(this));
					Link.Transaction.Abort();
				}
				catch { }
				finally { DebugEx.Unindent(); }
				try
				{
					foreach (var map in UberMaps)
					{
						var list = map.UberEntities.Where(x => x.Entity != null && x.ToRefresh).ToList();
						foreach (var meta in list) if (meta.Entity != null && meta.ToRefresh) map.RefreshNow(meta.Entity);
						list.Clear(); list = null;
					}
				}
				finally { }

				throw e;
			}
			finally
			{
				DiscardChanges();
				if (opsTaken) Monitor.Exit(UberOperations.SyncRoot);
				if (mapsTaken) Monitor.Exit(UberMaps.SyncRoot);
			}
		}

		/// <summary>
		/// Discards all the pending change operations that may have been annotated into this
		/// repository.
		/// </summary>
		public void DiscardChanges()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			lock (UberMaps.SyncRoot)
			{
				lock (UberOperations.SyncRoot)
				{
					var array = UberOperations.ToArray();
					foreach (var op in array) op.OnDiscard();
					Array.Clear(array, 0, array.Length);
					UberOperations.Clear(); // Redundant..
				}
				foreach (var map in UberMaps)
				{
					lock (map.UberEntities.SyncRoot)
					{
						foreach (var meta in map.UberEntities)
						{
							meta.UberOperation = null;
							meta.ToRefresh = false;
						}
					}
				}
			}
		}

		/// <summary>
		/// Whether the internal collector of invalid entities is enabled or not.
		/// </summary>
		public bool IsCollectorEnabled
		{
			get { return _Timer == null ? false : _Timer.Enabled; }
		}

		/// <summary>
		/// Enables the internal collector of invalid entities, or resumes its operation.
		/// <para>This method is intended for specialized scenarios only.</para>
		/// </summary>
		public void EnableCollector()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			if (_Timer == null)
			{
				_Timer = new System.Timers.Timer();
				_Timer.Elapsed += new System.Timers.ElapsedEventHandler(Collector);
			}
			_Timer.Interval = _Interval;
			_Timer.Enabled = true;
		}

		/// <summary>
		/// Enables the internal collector of invalid entities, or resumes its operation.
		/// <para>This method is intended for specialized scenarios only.</para>
		/// </summary>
		/// <param name="milliseconds">The interval at which the collector is fired.</param>
		/// <param name="enableGC">Whether to force a CLR Garbage Collection before the internal
		/// collector is fired or not.</param>
		public void EnableCollector(int milliseconds, bool enableGC)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			if (milliseconds < 0) milliseconds = UberHelper.CollectorInterval;
			if (milliseconds < UberHelper.DEFAULT_COLLECTOR_MIN_INTERVAL)
				milliseconds = UberHelper.DEFAULT_COLLECTOR_MIN_INTERVAL;

			_Interval = milliseconds;
			_EnableGC = enableGC;

			EnableCollector();
		}

		/// <summary>
		/// Suspends or disables the operations of the internal collector of invalid entities.
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

			DebugEx.IndentWriteLine("\n- Collector fired for '{0}'...", this);
			var enabled = IsCollectorEnabled;
			DisableCollector();

			if (_EnableGC) GC.Collect();

			IUberMap[] maps = null; if (Monitor.TryEnter(UberMaps.SyncRoot))
			{
				maps = UberMaps.ToArray();
				Monitor.Exit(UberMaps.SyncRoot);
			}
			foreach (var map in maps)
			{
				if (map.IsDisposed) continue;
				if (Monitor.TryEnter(map.UberEntities.SyncRoot))
				{
					map.CollectInvalidEntities();
					Monitor.Exit(map.UberEntities.SyncRoot);
				}
			}
			Array.Clear(maps, 0, maps.Length);

			if (enabled && !IsDisposed) EnableCollector();
			DebugEx.Unindent();
		}
	}
}
