// ======================================================== DataRepository.cs
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
	using System.Threading;

	// ==================================================== 
	/// <summary>
	/// Extends the <see cref="IDataRepository"/> interface.
	/// </summary>
	internal interface IUberRepository : IDataRepository
	{
		/// <summary>
		/// The internal list of maps registered into this instance.
		/// </summary>
		List<IUberMap> UberMaps { get; }

		/// <summary>
		/// Whether the list of maps is locked or not.
		/// </summary>
		bool IsMapsLocked { get; }

		/// <summary>
		/// Executes the given action under a lock on the list of maps.
		/// </summary>
		void WithMapsLock(Action action);

		/// <summary>
		/// Gets the map registered to manage the entities of the given type, or null if no
		/// such map can be found.
		/// </summary>
		/// <param name="type">The type of the entities managed by the map to find.</param>
		/// <returns>The requested map, or null.</returns>
		IUberMap GetUberMap(Type type);

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
		/// The internal list of pending operations.
		/// </summary>
		List<IUberOperation> UberOperations { get; }

		/// <summary>
		/// Whether the list of pending operations is locked or not.
		/// </summary>
		bool IsOperationsLocked { get; }

		/// <summary>
		/// Executes the given action under a lock on the list of pending operations.
		/// </summary>
		void WithOperationsLock(Action action);

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
	/// Represents a repository for mapped entities, implementing both the dynamic repository
	/// and the dynamic unit of work patterns.
	/// </summary>
	public class DataRepository : IDataRepository, IUberRepository
	{
		bool _IsDisposed = false;
		ulong _SerialId = 0;
		IDataLink _Link = null;
		List<IUberOperation> _Operations = new List<IUberOperation>();
		List<Type> _DiscardedTypes = new List<Type>();
		Action<Exception> _OnExecuteChangesError = null;
		List<IUberMap> _Maps = new List<IUberMap>();
		bool _WeakMapsEnabled = UberHelper.DEFAULT_ENABLE_WEAK_MAPS;
		System.Timers.Timer _Timer = null;
		int _Interval = UberHelper.DEFAULT_COLLECTOR_INTERVAL;
		bool _EnableGC = UberHelper.DEFAULT_COLLECTOR_GC_ENABLED;

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

			EnableCollector();
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
				if (_Operations != null)
				{
					lock (_Operations)
					{
						var list = new List<IUberOperation>(_Operations);
						foreach (var op in list) op.Dispose();
						list.Clear(); list = null;
					}
				}
				if (_Maps != null)
				{
					lock (_Maps)
					{
						var list = new List<IUberMap>(_Maps);
						foreach (var map in list) map.Dispose();
						list.Clear(); list = null;
					}
				}
			}

			if (_DiscardedTypes != null) _DiscardedTypes.Clear(); _DiscardedTypes = null;
			if (_Operations != null) _Operations.Clear(); _Operations = null;
			if (_Maps != null) _Maps.Clear(); _Maps = null;
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

			DataRepository temp = null; WithMapsLock(() =>
			{
				temp = new DataRepository(link);
				temp.DisableCollector();

				foreach (var map in _Maps) map.Clone(temp);
				temp._WeakMapsEnabled = _WeakMapsEnabled;
				temp._Interval = _Interval;
				temp._EnableGC = _EnableGC;
				temp._OnExecuteChangesError = _OnExecuteChangesError;

				temp.EnableCollector();
			});
			return temp;
		}
		IDataRepository IDataRepository.Clone(IDataLink link)
		{
			return this.Clone(link);
		}

		/// <summary>
		/// The serial id assigned to this instance.
		/// </summary>
		public ulong SerialId
		{
			get { return _SerialId; }
		}

		/// <summary>
		/// The data link that represents the connection with the underlying database that this
		/// repository uses.
		/// </summary>
		public IDataLink Link
		{
			get { return _Link; }
		}

		/// <summary>
		/// The internal list of maps registered into this instance.
		/// </summary>
		internal List<IUberMap> UberMaps
		{
			get { return _Maps; }
		}
		List<IUberMap> IUberRepository.UberMaps
		{
			get { return this.UberMaps; }
		}

		/// <summary>
		/// Whether the list of maps is locked or not.
		/// </summary>
		internal bool IsMapsLocked
		{
			get { return Monitor.IsEntered(((ICollection)_Maps).SyncRoot); }
		}
		bool IUberRepository.IsMapsLocked
		{
			get { return this.IsMapsLocked; }
		}

		/// <summary>
		/// Executes the given action under a lock on the list of maps.
		/// </summary>
		internal void WithMapsLock(Action action)
		{
			var enabled = IsCollectorEnabled; if (enabled) DisableCollector();
			lock (((ICollection)_Maps).SyncRoot) { action(); }
			if (enabled) EnableCollector();
		}
		void IUberRepository.WithMapsLock(Action action)
		{
			this.WithMapsLock(action);
		}

		/// <summary>
		/// The collection of maps registered into this instance.
		/// </summary>
		public IEnumerable<IDataMap> Maps
		{
			get { return _Maps; }
		}

		/// <summary>
		/// Gets the map registered to manage the entities of the given type, or null if no
		/// such map can be found.
		/// </summary>
		/// <param name="type">The type of the entities managed by the map to find.</param>
		/// <returns>The requested map, or null.</returns>
		internal IUberMap GetUberMap(Type type)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (type == null) throw new ArgumentNullException("type", "Type cannot be null.");

			var holder = ProxyGenerator.Holders.Find(x => x.ExtendedType == type);
			if (holder != null) type = holder.ExtendedType.BaseType;

			IUberMap map = null; WithMapsLock(() =>
			{
				map = _Maps.Find(x => x.EntityType == type);
			});
			return map;
		}
		IUberMap IUberRepository.GetUberMap(Type type)
		{
			return this.GetUberMap(type);
		}

		/// <summary>
		/// Gets the map registered to manage the entities of the given type, or null if no
		/// such map can be found.
		/// </summary>
		/// <param name="type">The type of the entities managed by the map to find.</param>
		/// <returns>The requested map, or null.</returns>
		public IDataMap GetMap(Type type)
		{
			return GetUberMap(type);
		}

		/// <summary>
		/// Gets the map registered to manage the entities of the given type, or null if no
		/// such map can be found.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to find.</typeparam>
		/// <returns>The requested map, or null.</returns>
		public DataMap<T> GetMap<T>() where T : class
		{
			return (DataMap<T>)GetUberMap(typeof(T));
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

			if (_DiscardedTypes.Contains(type)) return null;

			var holder = ProxyGenerator.Holders.Find(x => x.ExtendedType == type);
			if (holder != null) type = holder.ExtendedType.BaseType;

			IUberMap map = null; WithMapsLock(() =>
			{
				map = _Maps.Find(x => x.EntityType == type); if (map == null)
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
			});

			if (map == null) _DiscardedTypes.Add(type);
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
			return (IDataMap)RetrieveUberMap(type, table);
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
			return (DataMap<T>)RetrieveUberMap(typeof(T), table);
		}
		IDataMap<T> IDataRepository.RetrieveMap<T>(Func<dynamic, object> table)
		{
			return this.RetrieveMap<T>(table);
		}

		/// <summary>
		/// Clears and disposes all the maps registered into this instance.
		/// </summary>
		public void ClearMaps()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			WithMapsLock(() =>
			{
				var list = new List<IUberMap>(_Maps);
				foreach (var map in list) map.Dispose();
				list.Clear(); list = null;
				_Maps.Clear();
			});
		}

		/// <summary>
		/// Clears the cache of entities of all the maps registered into the instance.
		/// </summary>
		public void ClearEntities()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			WithMapsLock(() => { foreach (var map in _Maps) map.ClearEntities(); });
		}

		/// <summary>
		/// Gets a safe collection containing the meta entities managed by the maps registeted
		/// into this repository, excluding collected or detached ones.
		/// </summary>
		public IEnumerable<MetaEntity> MetaEntities
		{
			get
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());

				var list = new List<MetaEntity>(); WithMapsLock(() =>
				{
					foreach (var map in _Maps) list.AddRange(map.MetaEntities);
				});
				foreach (var meta in list) yield return meta;
				list.Clear(); list = null;
			}
		}
		IEnumerable<IMetaEntity> IDataRepository.MetaEntities
		{
			get { return this.MetaEntities; }
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
		/// Attaches the given entity to this map.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <param name="entity">The entity to attach.</param>
		public void Attach<T>(T entity) where T : class
		{
			GetValidMap<T>().Attach(entity);
		}

		/// <summary>
		/// Detaches the given entity from this map. Returns true if it has been detached, or
		/// false otherwise.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <param name="entity">The entity to detach.</param>
		public bool Detach<T>(T entity) where T : class
		{
			return GetValidMap<T>().Detach(entity);
		}

		/// <summary>
		/// Creates a new query operation for entities managed by the map.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <returns>A new operation.</returns>
		public DataQuery<T> Query<T>() where T : class
		{
			return GetValidMap<T>().Query();
		}
		IDataQuery<T> IDataRepository.Query<T>()
		{
			return this.Query<T>();
		}

		/// <summary>
		/// Creates a new query operation for entities managed by the map, and sets the initial
		/// contents of its WHERE clause:
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <param name="where">he dynamic lambda expression that resolves into the contents of
		/// this clause.
		/// <para>- By default, if any previous contents exist the new ones are appended using an
		/// AND logical operator. However, the virtual extension methods 'x => x.And(...)' and
		/// 'x => x.Or(...)' can be used to specify the concrete logical operator to use for
		/// concatenation purposes.</para>
		/// </param>
		/// <returns>A new operation.</returns>
		public DataQuery<T> Where<T>(Func<dynamic, object> where) where T : class
		{
			return Query<T>().Where(where);
		}
		IDataQuery<T> IDataRepository.Where<T>(Func<dynamic, object> where)
		{
			return this.Where<T>(where);
		}

		/// <summary>
		/// Finds inmediately a suitable entity by either returning the first entity in the
		/// cache that meets the given specifications or, if no one is found in the cache,
		/// querying the underlying database for it. Returns null if no entity can be found
		/// neither in the cache nor in the database.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <param name="specs">A collection of dynamic lambda expressions each containing the
		/// name of column and the value to find for that column 'x => x.Column == Value'.</param>
		/// <returns>The requested entity, or null.</returns>
		public T FindNow<T>(params Func<dynamic, object>[] specs) where T : class
		{
			return GetValidMap<T>().FindNow(specs);
		}

		/// <summary>
		/// Refreshes inmediately from the database the given entity, even if it wwas not
		/// attached to the map before, along with all the entities in the cache that share
		/// the same identity.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <param name="entity">The entity to refresh</param>
		/// <returns>A refreshed entity, or null if no one was found in the database.</returns>
		public T RefreshNow<T>(T entity) where T : class
		{
			return GetValidMap<T>().RefreshNow(entity);
		}

		/// <summary>
		/// Creates a new insert operation for the entity managed by the map.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <param name="entity">The entity affected by this operation.</param>
		/// <returns>A new operation.</returns>
		public DataInsert<T> Insert<T>(T entity) where T : class
		{
			return GetValidMap<T>().Insert(entity);
		}
		IDataInsert<T> IDataRepository.Insert<T>(T entity)
		{
			return this.Insert<T>(entity);
		}

		/// <summary>
		/// Convenience method to execute inmediately an insert operation with the given entity,
		/// along with all other pending change operations that might be annotated into this
		/// repository.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <param name="entity">The entity affected by this operation.</param>
		public void InsertNow<T>(T entity) where T : class
		{
			var cmd = Insert<T>(entity);
			cmd.Submit();
			ExecuteChanges();
		}

		/// <summary>
		/// Creates a new delete operation for the entity managed by the map.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <param name="entity">The entity affected by this operation.</param>
		/// <returns>A new operation.</returns>
		public DataDelete<T> Delete<T>(T entity) where T : class
		{
			return GetValidMap<T>().Delete(entity);
		}
		IDataDelete<T> IDataRepository.Delete<T>(T entity)
		{
			return this.Delete<T>(entity);
		}

		/// <summary>
		/// Convenience method to execute inmediately a delete operation with the given entity,
		/// along with all other pending change operations that might be annotated into this
		/// repository.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <param name="entity">The entity affected by this operation.</param>
		public void DeleteNow<T>(T entity) where T : class
		{
			var cmd = Delete<T>(entity);
			cmd.Submit();
			ExecuteChanges();
		}

		/// <summary>
		/// Creates a new update operation for the entity managed by the map.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <param name="entity">The entity affected by this operation.</param>
		/// <returns>A new operation.</returns>
		public DataUpdate<T> Update<T>(T entity) where T : class
		{
			return GetValidMap<T>().Update(entity);
		}
		IDataUpdate<T> IDataRepository.Update<T>(T entity)
		{
			return this.Update<T>(entity);
		}

		/// <summary>
		/// Convenience method to execute inmediately an update operation with the given entity,
		/// along with all other pending change operations that might be annotated into this
		/// repository.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <param name="entity">The entity affected by this operation.</param>
		public void UpdateNow<T>(T entity) where T : class
		{
			var cmd = Update<T>(entity);
			cmd.Submit();
			ExecuteChanges();
		}

		/// <summary>
		/// The internal list of pending operations.
		/// </summary>
		internal List<IUberOperation> UberOperations
		{
			get { return _Operations; }
		}
		List<IUberOperation> IUberRepository.UberOperations
		{
			get { return this.UberOperations; }
		}

		/// <summary>
		/// Whether the list of pending operations is locked or not.
		/// </summary>
		internal bool IsOperationsLocked
		{
			get { return Monitor.IsEntered(((ICollection)_Operations).SyncRoot); }
		}
		bool IUberRepository.IsOperationsLocked
		{
			get { return this.IsOperationsLocked; }
		}

		/// <summary>
		/// Executes the given action under a lock on the list of pending operations.
		/// </summary>
		internal void WithOperationsLock(Action action)
		{
			var enabled = IsCollectorEnabled; if (enabled) DisableCollector();
			lock (((ICollection)_Operations).SyncRoot) { action(); }
			if (enabled) EnableCollector();
		}
		void IUberRepository.WithOperationsLock(Action action)
		{
			this.WithOperationsLock(action);
		}

		/// <summary>
		/// Executes against the underlying database all pending change operations that may have
		/// been annotated into this repository.
		/// </summary>
		public void ExecuteChanges()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			Exception ex = null;

			try
			{
				DebugEx.IndentWriteLine("\n--- Executing changes for '{0}'...".FormatWith(this));
				Monitor.Enter(_Maps);
				Monitor.Enter(_Operations);

				Link.Transaction.Start(); foreach (var op in _Operations)
				{
					DebugEx.IndentWriteLine("\n- Executing '{0}'...", op);
					try { op.Execute(); }
					finally { DebugEx.Unindent(); }
				}
				Link.Transaction.Commit();

				DebugEx.IndentWriteLine("\n--- Refreshing changes for '{0}'...".FormatWith(this));
				
				foreach (var meta in MetaEntities)
				{
					var obj = meta.Entity; if (obj == null) continue;

					meta.MemberChilds.Clear();

					if (!meta.ToRefresh) continue;
					if (meta.Operation != null && meta.Operation is IDataDelete) return;

					meta.Map.RefreshNow(obj);
				}
				DebugEx.Unindent();
			}
			catch (Exception e)
			{
				try
				{
					Link.Transaction.Abort();

					foreach (var meta in MetaEntities)
						if (meta.Entity != null) meta.Map.RefreshNow(meta.Entity);
				}
				catch { }

				if (_OnExecuteChangesError != null) ex = e;
				else throw;
			}
			finally
			{
				DiscardChanges();

				Monitor.Exit(_Operations);
				Monitor.Exit(_Maps);
				DebugEx.Unindent();

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

			WithMapsLock(() =>
			{
				foreach (var map in _Maps) map.WithEntitiesLock(() =>
				{
					foreach (var meta in map.MetaEntities) meta.ToRefresh = false;
				});
				WithOperationsLock(() =>
				{
					var list = new List<IUberOperation>(_Operations);
					foreach (var op in list) op.Dispose();
					list.Clear(); list = null;
					_Operations.Clear();
				});
			});
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

			if (milliseconds < 0) milliseconds = UberHelper.DEFAULT_COLLECTOR_INTERVAL;
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

			var enabled = IsCollectorEnabled; if (enabled) DisableCollector();

			if (!IsMapsLocked) WithMapsLock(() =>
			{
				DebugEx.IndentWriteLine("\n- Collector fired for '{0}'...", this);

				if (_EnableGC) GC.Collect(); foreach (var map in _Maps)
				{
					if (!map.IsEntitiesLocked) map.RemoveInvalidEntities();
				}

				DebugEx.Unindent();
			});

			if (enabled && !IsDisposed) EnableCollector();
		}
	}
}
// ======================================================== 
