namespace Kerosene.ORM.Maps
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Collections.Generic;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents a repository for a set of maps between their POCO classes and their related
	/// primary tables in the underlying database-alike service, implementing both the Dynamic
	/// Repository and the Dynamic Unit Of Work patterns.
	/// </summary>
	public interface IDataRepository : IDisposableEx
	{
		/// <summary>
		/// Returns a new repository associated with the given link containing a copy of the
		/// original maps and customizations existing in the original one. Cloned maps will not
		/// be validated and can be modified as needed.
		/// </summary>
		/// <param name="link">The link the new respository will be associated with.</param>
		/// <returns>A new respository.</returns>
		IDataRepository Clone(IDataLink link);

		/// <summary>
		/// The database-alike service link this instance is associated with.
		/// </summary>
		IDataLink Link { get; }

		/// <summary>
		/// The collection of maps registered into this repository.
		/// </summary>
		IEnumerable<IDataMap> Maps { get; }

		/// <summary>
		/// Clears and disposes all the maps registered into this instance.
		/// </summary>
		void ClearMaps();

		/// <summary>
		/// Whether weak maps are enabled or not for this instance.
		/// <para>Weak maps are created automatically when an entity type is referenced by any
		/// map operation and there was no registered map for that type. Weak maps are disposed
		/// if a regular non-weak map is registered (created) explicitly.</para>
		/// </summary>
		bool WeakMapsEnabled { get; set; }

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
		IDataMap LocateMap(Type type, Func<dynamic, object> table = null);

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
		IDataMap<T> LocateMap<T>(Func<dynamic, object> table = null) where T : class;

		/// <summary>
		/// Whether the maps registered into this instance keep track of the entities they have
		/// managed in their internal caches, or not. The setter cascades the new value to all
		/// the registered maps.
		/// </summary>
		bool TrackEntities { get; set; }

		/// <summary>
		/// The current collection of entities in a valid state tracked by the maps registered
		/// into this instance, if any.
		/// </summary>
		IEnumerable<IMetaEntity> Entities { get; }

		/// <summary>
		/// Clears the caches of tracked entities maintained by the maps registered into this
		/// instance and, optionally, detaches those entities.
		/// </summary>
		/// <param name="detach">True to forcibly detach the entities found in the caches.</param>
		void ClearEntities(bool detach = true);

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
		/// Executes the change operations annotated into this instance against the underlying
		/// database as a single logical operation that either succeeds or fails as a whole.
		/// </summary>
		void ExecuteChanges();

		/// <summary>
		/// Discards all the pending change operations that may have been annotated into this
		/// repository.
		/// </summary>
		void DiscardChanges();
	}
}
