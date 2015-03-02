// ======================================================== IDataRepository.cs
namespace Kerosene.ORM.Maps
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Collections.Generic;

	// ==================================================== 
	/// <summary>
	/// Represents a repository for mapped entities, implementing both the dynamic repository
	/// and the dynamic unit of work patterns.
	/// </summary>
	public interface IDataRepository : IDisposableEx
	{
		/// <summary>
		/// Returns a new repository associated with the given data link that contains a copy
		/// of the maps that were registered in the original one.
		/// </summary>
		/// <param name="link">The link the new repository will use.</param>
		/// <returns>A new repository.</returns>
		IDataRepository Clone(IDataLink link);

		/// <summary>
		/// The data link that represents the connection with the underlying database that this
		/// repository uses.
		/// </summary>
		IDataLink Link { get; }

		/// <summary>
		/// The collection of maps registered into this instance.
		/// </summary>
		IEnumerable<IDataMap> Maps { get; }

		/// <summary>
		/// Gets the map registered to manage the entities of the given type, or null if no
		/// such map can be found.
		/// </summary>
		/// <param name="type">The type of the entities managed by the map to find.</param>
		/// <returns>The requested map, or null.</returns>
		IDataMap GetMap(Type type);

		/// <summary>
		/// Gets the map registered to manage the entities of the given type, or null if no
		/// such map can be found.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to find.</typeparam>
		/// <returns>The requested map, or null.</returns>
		IDataMap<T> GetMap<T>() where T : class;

		/// <summary>
		/// Whether weak maps are enabled or not for this instance.
		/// <para>Weak maps are created automatically when an entity type is referenced by any
		/// map operation and there was no registered map for that type. Weak maps are disposed
		/// if a regular non-weak map is registered (created) explicitly.</para>
		/// </summary>
		bool WeakMapsEnabled { get; set; }

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
		IDataMap RetrieveMap(Type type, Func<dynamic, object> table = null);

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
		IDataMap<T> RetrieveMap<T>(Func<dynamic, object> table = null) where T : class;

		/// <summary>
		/// Clears and disposes all the maps registered into this instance.
		/// </summary>
		void ClearMaps();

		/// <summary>
		/// Clears the cache of entities of all the maps registered into the instance.
		/// </summary>
		void ClearEntities();

		/// <summary>
		/// Gets a safe collection containing the meta entities managed by the maps registeted
		/// into this repository, excluding collected or detached ones.
		/// </summary>
		IEnumerable<IMetaEntity> MetaEntities { get; }

		/// <summary>
		/// Creates a new entity with the appropriate type for the requested map.
		/// <para>This method is invoked to generate instances that support virtual lazy
		/// properties when needed. Client applications can use but it is not needed.</para>
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <returns>A new entity.</returns>
		T NewEntity<T>() where T : class;

		/// <summary>
		/// Attaches the given entity to this map.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <param name="entity">The entity to attach.</param>
		void Attach<T>(T entity) where T : class;

		/// <summary>
		/// Detaches the given entity from this map. Returns true if it has been detached, or
		/// false otherwise.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <param name="entity">The entity to detach.</param>
		bool Detach<T>(T entity) where T : class;

		/// <summary>
		/// Creates a new query operation for entities managed by the map.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <returns>A new operation.</returns>
		IDataQuery<T> Query<T>() where T : class;

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
		IDataQuery<T> Where<T>(Func<dynamic, object> where) where T : class;

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
		T FindNow<T>(params Func<dynamic, object>[] specs) where T : class;

		/// <summary>
		/// Refreshes inmediately from the database the given entity, even if it wwas not
		/// attached to the map before, along with all the entities in the cache that share
		/// the same identity.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <param name="entity">The entity to refresh</param>
		/// <returns>A refreshed entity, or null if no one was found in the database.</returns>
		T RefreshNow<T>(T entity) where T : class;

		/// <summary>
		/// Creates a new insert operation for the entity managed by the map.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <param name="entity">The entity affected by this operation.</param>
		/// <returns>A new operation.</returns>
		IDataInsert<T> Insert<T>(T entity) where T : class;

		/// <summary>
		/// Convenience method to execute inmediately an insert operation with the given entity,
		/// along with all other pending change operations that might be annotated into this
		/// repository.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <param name="entity">The entity affected by this operation.</param>
		void InsertNow<T>(T entity) where T : class;

		/// <summary>
		/// Creates a new delete operation for the entity managed by the map.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <param name="entity">The entity affected by this operation.</param>
		/// <returns>A new operation.</returns>
		IDataDelete<T> Delete<T>(T entity) where T : class;

		/// <summary>
		/// Convenience method to execute inmediately a delete operation with the given entity,
		/// along with all other pending change operations that might be annotated into this
		/// repository.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <param name="entity">The entity affected by this operation.</param>
		void DeleteNow<T>(T entity) where T : class;

		/// <summary>
		/// Creates a new update operation for the entity managed by the map.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <param name="entity">The entity affected by this operation.</param>
		/// <returns>A new operation.</returns>
		IDataUpdate<T> Update<T>(T entity) where T : class;

		/// <summary>
		/// Convenience method to execute inmediately an update operation with the given entity,
		/// along with all other pending change operations that might be annotated into this
		/// repository.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <param name="entity">The entity affected by this operation.</param>
		void UpdateNow<T>(T entity) where T : class;

		/// <summary>
		/// Executes against the underlying database all pending change operations that may have
		/// been annotated into this repository.
		/// </summary>
		void ExecuteChanges();

		/// <summary>
		/// Discards all the pending change operations that may have been annotated into this
		/// repository.
		/// </summary>
		void DiscardChanges();

		/// <summary>
		/// Maintains a delegate that, if it is not null, will be invoked to process any exception
		/// encountered when executing the pending changes at the database. If it is null then the
		/// exception is thrown.
		/// </summary>
		Action<Exception> OnExecuteChangesError { get; set; }
	}
}
// ======================================================== 
