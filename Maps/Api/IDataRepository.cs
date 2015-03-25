// ======================================================== IDataRepository.cs
namespace Kerosene.ORM.Maps
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Collections.Generic;

	// ==================================================== 
	/// <summary>
	/// Represents a view on the state and contents of an underlying database, related to the
	/// maps associated with this instance, that implements both the Dynamic Repository and the
	/// Dynamic Unit Of Work patterns.
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
		/// The database-alike service link this instance is associated with.
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
		/// Clears and disposes all the maps registered into this instance, making all their
		/// managed entities to become detached ones.
		/// </summary>
		void ClearMaps();

		/// <summary>
		/// Gets the collection of cached entities managed by the maps registered into this
		/// instance, excluding the collected or invalid ones.
		/// </summary>
		IEnumerable<IMetaEntity> MetaEntities { get; }

		/// <summary>
		/// Clears the caches of managed entities of all the maps registered into this instance,
		/// making them all become detached entities.
		/// </summary>
		void ClearEntities();

		/// <summary>
		/// Creates a new entity with the appropriate type for the requested map.
		/// <para>This method is invoked to generate instances that support virtual lazy
		/// properties when needed. Client applications can use but it is not needed.</para>
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map to use.</typeparam>
		/// <returns>A new entity.</returns>
		T NewEntity<T>() where T : class;

		/// <summary>
		/// Attaches the given entity into this map.
		/// <para>Note that the framework supports attaching several entities that share the
		/// same identity columns, which are treated as a group for the relevant operations.</para>
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <param name="entity">The entity to attach into this instance.</param>
		void Attach<T>(T entity) where T : class;

		/// <summary>
		/// Removes the given entity from this map, making it become a detached one. Returns true
		/// if the entity has been removed, or false otherwise.
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <param name="entity">The entity to detach from this instance.</param>
		/// <returns>True if the instance has been removed, false otherwise.</returns>
		bool Detach<T>(T entity) where T : class;

		/// <summary>
		/// Creates a new query command for the entities managed by this map.
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <returns>A new query command.</returns>
		IDataQuery<T> Query<T>() where T : class;

		/// <summary>
		/// Creates a new query command for the entities managed by this map, and sets the initial
		/// contents of its WHERE clause.
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <param name="where">The dynamic lambda expression that resolves into the contents of
		/// this clause.
		/// <returns>A new query command.</returns>
		IDataQuery<T> Where<T>(Func<dynamic, object> where) where T : class;

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
		T FindNow<T>(params Func<dynamic, object>[] specs) where T : class;

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
		T RefreshNow<T>(T entity) where T : class;

		/// <summary>
		/// Creates a new insert operation for the given entity.
		/// <para>The new command must be firstly submitted into the associated repository in
		/// order it to be executed when all pending change operations annotated into that
		/// repository are executed as a group.</para>
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <param name="entity">The entity to be inserted.</param>
		/// <returns>A new command.</returns>
		IDataInsert<T> Insert<T>(T entity) where T : class;

		/// <summary>
		/// Convenience method to create the operation associated with the given entity, to
		/// submit it into the repository, and to execute inmediately all the pending change
		/// operations annotated into it.
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <param name="entity">The entity affected by the operation.</param>
		void InsertNow<T>(T entity) where T : class;

		/// <summary>
		/// Creates a new delete operation for the given entity.
		/// <para>The new command must be firstly submitted into the associated repository in
		/// order it to be executed when all pending change operations annotated into that
		/// repository are executed as a group.</para>
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <param name="entity">The entity to be inserted.</param>
		/// <returns>A new command.</returns>
		IDataDelete<T> Delete<T>(T entity) where T : class;

		/// <summary>
		/// Convenience method to create the operation associated with the given entity, to
		/// submit it into the repository, and to execute inmediately all the pending change
		/// operations annotated into it.
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <param name="entity">The entity affected by the operation.</param>
		void DeleteNow<T>(T entity) where T : class;

		/// <summary>
		/// Creates a new delete operation for the given entity.
		/// <para>The new command must be firstly submitted into the associated repository in
		/// order it to be executed when all pending change operations annotated into that
		/// repository are executed as a group.</para>
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <param name="entity">The entity to be inserted.</param>
		/// <returns>A new command.</returns>
		IDataUpdate<T> Update<T>(T entity) where T : class;

		/// <summary>
		/// Convenience method to create the operation associated with the given entity, to
		/// submit it into the repository, and to execute inmediately all the pending change
		/// operations annotated into it.
		/// </summary>
		/// <typeparam name="T">The type of the managed entities.</typeparam>
		/// <param name="entity">The entity affected by the operation.</param>
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
