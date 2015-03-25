// ======================================================== DataRepository.cs
namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
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
		/// Returns the map registered to manage the entities of the given type, or if such map
		/// is not found, tries to create a new one. Returns null if there was no map registered
		/// and if it was impossible to create a new one.
		/// </summary>
		/// <param name="type">The type of the entities managed by the map to retrieve.</param>
		/// <param name="table">A dynamic lambda expression that resolves into the name of the
		/// master table in the database where to find at least the identity columns of the
		/// entities.
		/// <para>If this argument is null then the framework tries to create a new weak map
		/// probing for its table name a number of suitable candidates based upon the name of
		/// the entities' type.</para>
		/// </param>
		/// <returns>The requested map, or null.</returns>
		IUberMap RetrieveUberMap(Type type, Func<dynamic, object> table = null);

		/// <summary>
		/// The collection of operations registered into this instance.
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
	/// Represents a view on the state and contents of an underlying database, related to the
	/// maps associated with this instance, that implements both the Dynamic Repository and the
	/// Dynamic Unit Of Work patterns.
	/// </summary>
	public class DataRepository : IDataRepository, IUberRepository
	{
		bool _IsDisposed = false;
		ulong _SerialId = 0;
		IDataLink _Link = null;
		bool _WeakMapsEnabled = UberHelper.EnableWeakMaps;
		UberMapSet _UberMaps = new UberMapSet();
		UberOperationList _UberOperations = new UberOperationList();
		Action<Exception> _OnExecuteChangesError = null;
		System.Timers.Timer _Timer = null;
		int _Interval = UberHelper.CollectorInterval;
		bool _EnableGC = UberHelper.EnableCollectorGC;

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

			if(UberHelper.EnableCollector) EnableCollector();
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
			_Timer = null;

			if (disposing)
			{
				if (_UberOperations != null)
				{
					lock (_UberOperations.SyncRoot) { _UberOperations.Clear(); }
				}
				if (_UberMaps != null)
				{
					lock (_UberMaps.SyncRoot) { _UberMaps.Clear(); }
				}
			}

			_UberOperations = null;
			_UberMaps = null;
			_OnExecuteChangesError = null;
			_Link = null;

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
		/// Returns a new repository associated with the given data link that contains a copy
		/// of the maps that were registered in the original one.
		/// </summary>
		/// <param name="link">The link the new repository will use.</param>
		/// <returns>A new repository.</returns>
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

			lock (UberMaps.SyncRoot)
			{
				var enabled = temp.IsCollectorEnabled; if (enabled) temp.DisableCollector();

				foreach (var map in UberMaps) map.Clone(temp);
				temp._WeakMapsEnabled = _WeakMapsEnabled;
				temp._OnExecuteChangesError = _OnExecuteChangesError;
				temp._Interval = _Interval;
				temp._EnableGC = _EnableGC;

				if (enabled) temp.EnableCollector();
			}
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
		/// The collection of maps registered into this instance.
		/// </summary>
		public IEnumerable<IDataMap> Maps
		{
			get
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				return UberMaps;
			}
		}

		/// <summary>
		/// Gets the map registered to manage the entities of the given type, or null if no
		/// such map can be found.
		/// </summary>
		/// <param name="type">The type of the entities managed by the map to find.</param>
		/// <returns>The requested map, or null.</returns>
		public IDataMap GetMap(Type type)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (type == null) throw new ArgumentNullException("type", "Type cannot be null.");

			lock (UberMaps.SyncRoot) { return UberMaps.Find(type); }
		}

		/// <summary>
		/// Gets the map registered to manage the entities of the given type, or null if no
		/// such map can be found.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to find.</typeparam>
		/// <returns>The requested map, or null.</returns>
		public DataMap<T> GetMap<T>() where T : class
		{
			return (DataMap<T>)GetMap(typeof(T));
		}
		IDataMap<T> IDataRepository.GetMap<T>()
		{
			return this.GetMap<T>();
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
		/// Returns the map registered to manage the entities of the given type, or if such map
		/// is not found, tries to create a new one. Returns null if there was no map registered
		/// and if it was impossible to create a new one.
		/// </summary>
		/// <param name="type">The type of the entities managed by the map to retrieve.</param>
		/// <param name="table">A dynamic lambda expression that resolves into the name of the
		/// master table in the database where to find at least the identity columns of the
		/// entities.
		/// <para>If this argument is null then the framework tries to create a new weak map
		/// probing for its table name a number of suitable candidates based upon the name of
		/// the entities' type.</para>
		/// </param>
		/// <returns>The requested map, or null.</returns>
		internal IUberMap RetrieveUberMap(Type type, Func<dynamic, object> table = null)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (type == null) throw new ArgumentNullException("type", "Type cannot be null.");
			if (!type.IsClass) throw new InvalidOperationException("Type '{0}' is not a class type.".FormatWith(type.EasyName()));

			var holder = ProxyGenerator.Holders.FindByExtended(type);
			if (holder != null) type = holder.ExtendedType.BaseType;

			IUberMap map = null; lock (UberMaps.SyncRoot)
			{
				map = UberMaps.Find(type); if (map == null)
				{
					var generic = typeof(DataMap<>);
					var concrete = generic.MakeGenericType(new Type[] { type });
					var constructor = concrete.GetConstructor(
						new Type[] { typeof(DataRepository), typeof(string), typeof(bool) });

					string name = null; if (table != null)
					{
						name = DynamicInfo.ParseName(table);
						map = (IUberMap)constructor.Invoke(new object[] { this, name, false });
					}
					else if (WeakMapsEnabled)
					{
						name = UberHelper.LocateTableName(Link, type.Name);
						if (name != null)
						{
							map = (IUberMap)constructor.Invoke(new object[] { this, name, true });
						}
					}
				}
			}
			return map;
		}
		IUberMap IUberRepository.RetrieveUberMap(Type type, Func<dynamic, object> table)
		{
			return this.RetrieveUberMap(type, table);
		}

		/// <summary>
		/// Returns the map registered to manage the entities of the given type, or if such map
		/// is not found, tries to create a new one. Returns null if there was no map registered
		/// and if it was impossible to create a new one.
		/// </summary>
		/// <param name="type">The type of the entities managed by the map to retrieve.</param>
		/// <param name="table">A dynamic lambda expression that resolves into the name of the
		/// master table in the database where to find at least the identity columns of the
		/// entities.
		/// <para>If this argument is null then the framework tries to create a new weak map
		/// probing for its table name a number of suitable candidates based upon the name of
		/// the entities' type.</para>
		/// </param>
		/// <returns>The requested map, or null.</returns>
		public IDataMap RetrieveMap(Type type, Func<dynamic, object> table = null)
		{
			return this.RetrieveUberMap(type, table);
		}

		/// <summary>
		/// Returns the map registered to manage the entities of the given type, or if such map
		/// is not found, tries to create a new one. Returns null if there was no map registered
		/// and if it was impossible to create a new one.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to retrieve.</typeparam>
		/// <param name="table">A dynamic lambda expression that resolves into the name of the
		/// master table in the database where to find at least the identity columns of the
		/// entities.
		/// <para>If this argument is null then the framework tries to create a new weak map
		/// probing for its table name a number of suitable candidates based upon the name of
		/// the entities' type.</para>
		/// </param>
		/// <returns>The requested map, or null.</returns>
		public DataMap<T> RetrieveMap<T>(Func<dynamic, object> table = null) where T : class
		{
			return (DataMap<T>)RetrieveMap(typeof(T), table);
		}
		IDataMap<T> IDataRepository.RetrieveMap<T>(Func<dynamic, object> table)
		{
			return this.RetrieveMap<T>(table);
		}

		/// <summary>
		/// Clears and disposes all the maps registered into this instance, making all their
		/// managed entities to become detached ones.
		/// </summary>
		public void ClearMaps()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			lock (UberMaps.SyncRoot) { UberMaps.Clear(); }
		}

		/// <summary>
		/// Gets the collection of cached entities managed by the maps registered into this
		/// instance, excluding the collected or invalid ones.
		/// </summary>
		public IEnumerable<IMetaEntity> MetaEntities
		{
			get
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());

				lock (UberMaps.SyncRoot)
				{
					foreach (var map in UberMaps)
					{
						foreach (var meta in map.MetaEntities) yield return meta;
					}
				}
			}
		}

		/// <summary>
		/// Clears the caches of managed entities of all the maps registered into this instance,
		/// making them all become detached entities.
		/// </summary>
		public void ClearEntities()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			lock (UberMaps.SyncRoot)
			{
				foreach (var map in UberMaps) map.ClearEntities();
			}
		}

		/// <summary>
		/// Gets or creates a valid map for the given type, or throws an exception if such was
		/// not possible.
		/// </summary>
		private DataMap<T> GetValidMap<T>() where T : class
		{
			var map = RetrieveMap<T>();
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
		/// <para>Note that the framework supports attaching several entities that share the
		/// same identity columns, which are treated as a group for the relevant operations.</para>
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
		/// The collection of operations registered into this instance.
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
		/// Executes against the underlying database all pending change operations that may have
		/// been annotated into this repository.
		/// </summary>
		public void ExecuteChanges()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			Exception ex = null;
			bool mapsTaken = false;
			bool opsTaken = false;
			int level = DebugEx.IndentLevel;

			try
			{
				DebugEx.IndentWriteLine("\n----- Executing changes for '{0}'...".FormatWith(this));
				Monitor.Enter(UberMaps.SyncRoot, ref mapsTaken); if (!mapsTaken) throw new InvalidOperationException("Cannot obtain an exclusive lock on the list of maps...");
				Monitor.Enter(UberOperations.SyncRoot, ref opsTaken); if (!opsTaken) throw new InvalidOperationException("Cannot obtain an exclusive lock on the list of operations...");

				Link.Transaction.Start(); foreach (var op in UberOperations)
				{
					try
					{
						DebugEx.IndentWriteLine("\n- Executing '{0}'...", op);
						op.Execute();
					}
					finally { DebugEx.Unindent(); }
				}
				Link.Transaction.Commit();
				DebugEx.Unindent();

				DebugEx.IndentWriteLine("\n----- Removing deleted of '{0}'...".FormatWith(this));
				foreach (var map in UberMaps)
				{
					var list = map.UberEntities.ToList(x => x.State == MetaState.ToDelete);
					foreach (var meta in list) meta.Reset();
					list.Clear(); list = null;
				}
				DebugEx.Unindent();

				DebugEx.IndentWriteLine("\n----- Refreshing '{0}'...".FormatWith(this));
				foreach (var map in UberMaps)
				{
					var list = map.UberEntities.ToList(x =>
						x.Entity != null &&
						x.UberMap != null &&
						x.ToRefresh);

					foreach (var meta in list)
					{
						var obj = meta.Entity; if (obj == null) continue;
						if (meta.UberOperation != null && (meta.UberOperation is IDataDelete)) continue; // Should be now redundant...

						meta.TrackedChilds.Clear();
						meta.UberMap.RefreshNow(obj);
					}
					list.Clear(); list = null;
				}
				DebugEx.Unindent();
			}
			catch (Exception e)
			{
				try
				{
					DebugEx.IndentWriteLine("\n----- Aborting changes for '{0}'...".FormatWith(this));
					Link.Transaction.Abort();
					DebugEx.Unindent();

					DebugEx.IndentWriteLine("\n----- Recovering state for '{0}'...".FormatWith(this));
					DiscardChanges();
					foreach (var map in UberMaps)
					{
						var list = map.UberEntities.ToList(x =>
							x.Entity != null &&
							x.Map != null);

						foreach (var meta in list)
						{
							var obj = meta.Entity; if (obj == null) continue;
							meta.UberMap.RefreshNow(obj);
						}
						list.Clear(); list = null;
					}
					DebugEx.Unindent();
				}
				catch { }

				if (_OnExecuteChangesError != null) ex = e;
				else throw;
			}
			finally
			{
				DiscardChanges();

				if (opsTaken) Monitor.Exit(UberOperations.SyncRoot);
				if (mapsTaken) Monitor.Exit(UberMaps.SyncRoot);

				DebugEx.IndentLevel = level;

				if (ex != null && _OnExecuteChangesError != null) _OnExecuteChangesError(ex);
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
					UberOperations.Clear();
				}
				foreach (var map in UberMaps)
				{
					lock (map.UberEntities.SyncRoot)
					{
						foreach (var meta in map.UberEntities) meta.ToRefresh = false;
					}
				}
			}
		}

		/// <summary>
		/// Maintains a delegate that, if it is not null, will be invoked to process any exception
		/// encountered when executing the pending changes at the database. If it is null then the
		/// exception is thrown.
		/// </summary>
		public Action<Exception> OnExecuteChangesError
		{
			get { return _OnExecuteChangesError; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				_OnExecuteChangesError = value;
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
			if (milliseconds < UberHelper.CollectorMinInterval)
				milliseconds = UberHelper.CollectorMinInterval;

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

			var enabled = IsCollectorEnabled;
			DisableCollector();

			if (_EnableGC) GC.Collect();

			if (Monitor.TryEnter(UberMaps.SyncRoot))
			{
				DebugEx.IndentWriteLine("\n- Collector fired for '{0}'...", this);
				foreach (var map in UberMaps)
				{
					if (Monitor.TryEnter(map.UberEntities.SyncRoot))
					{
						map.UberEntities.CollectInvalidEntities();
						Monitor.Exit(map.UberEntities.SyncRoot);
					}
				}
				Monitor.Exit(UberMaps.SyncRoot);
				DebugEx.Unindent();
			}

			if (enabled && !IsDisposed) EnableCollector();
		}
	}
}
// ======================================================== 
