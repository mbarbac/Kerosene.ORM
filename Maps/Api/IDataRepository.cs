using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;
using System.Collections.Generic;

namespace Kerosene.ORM.Maps
{
	// ====================================================
	/// <summary>
	/// Represents a repository for a set of maps between POCO classes and their associated
	/// primary tables in the underlying database-alike service, implementing both the Dynamic
	/// Repository and Dynamic Unit of Work patterns.
	/// </summary>
	public interface IDataRepository : IDisposableEx
	{
		/// <summary>
		/// Returns a new instance that is associated with the new given link and that contains
		/// a copy of the maps and customizations of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		IDataRepository Clone(IDataLink link);

		/// <summary>
		/// The link with the underlying database-alike service this instance is associated with.
		/// </summary>
		IDataLink Link { get; }

		/// <summary>
		/// The collection of maps registered into this repository.
		/// </summary>
		IEnumerable<IDataMap> Maps { get; }

		/// <summary>
		/// Clears and disposes all the maps registered into this instance, and reverts its
		/// managed entities, if any is tracked, to a detached state.
		/// </summary>
		void ClearMaps();

		/// <summary>
		/// Whether weak maps are enabled for this instance or not.
		/// </summary>
		bool WeakMapsEnabled { get; set; }

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
		IDataMap LocateMap(Type type, Func<dynamic, object> table = null);

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
		IDataMap<T> LocateMap<T>(Func<dynamic, object> table = null) where T : class;

		/// <summary>
		/// Whether tracking of entities is enabled or disabled, in principle, for the maps that
		/// are registered into this instance. The setter cascades the new value into all the
		/// maps registered at the moment when the new value is set.
		/// </summary>
		bool TrackEntities { get; set; }

		/// <summary>
		/// The collection of entities in a valid state tracked by the maps registered into
		/// this instance.
		/// </summary>
		IEnumerable<IMetaEntity> Entities { get; }

		/// <summary>
		/// Clears the caches of all the maps registered into this instance and, optionally,
		/// detaches the entities that were tracked.
		/// </summary>
		/// <param name="detach">True to also detach the entities removed from the caches of
		/// the maps.</param>
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
		/// this clause.</param>
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
