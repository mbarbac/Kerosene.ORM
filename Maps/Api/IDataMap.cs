using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;
using System.Collections.Generic;

namespace Kerosene.ORM.Maps
{
	// ==================================================== 
	/// <summary>
	/// Represents how a map will discover the columns in the database that will be associated
	/// with the type.
	/// </summary>
	public enum MapDiscoveryMode
	{
		/// <summary>
		/// All database columns whose names match the names of members of the type being mapped
		/// are taken into consideration.
		/// </summary>
		Auto,

		/// <summary>
		/// Only the columns that have been explicitly defined in the map, either by themselves
		/// or through a member definition, are taken into consideration.
		/// </summary>
		Explicit
	}

	// ====================================================
	/// <summary>
	/// Represents a map between entities of a POCO class and a primary table in an underlying
	/// database-alike service.
	/// </summary>
	public interface IDataMap : IDisposableEx
	{
		/// <summary>
		/// Returns a new instance that is associated with the new given repository and that
		/// contains a copy of the customizations of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		IDataMap Clone(IDataRepository repo);

		/// <summary>
		/// The repository this map is registered into.
		/// </summary>
		IDataRepository Repository { get; }

		/// <summary>
		/// The name of the primary table in the underlying database the entities managed by
		/// this map are associated with.
		/// </summary>
		string Table { get; set; }

		/// <summary>
		/// If not null a dynamic lambda expression that resolves into the logic to add into
		/// the WHERE clauses sent to the database to discriminate among entities of different
		/// types that may share the primary table.
		/// </summary>
		Func<dynamic, object> Discriminator { get; set; }

		/// <summary>
		/// Wheter this map is considered a weak one or not.
		/// </summary>
		bool IsWeakMap { get; }

		/// <summary>
		/// How the map will discover the columns in the database that will be associated with
		/// the type.
		/// </summary>
		MapDiscoveryMode DiscoveryMode { get; set; }

		/// <summary>
		/// The type of the entities managed by this map.
		/// </summary>
		Type EntityType { get; }

		/// <summary>
		/// The collection of members of the type for which either a dependency and/or a
		/// completion method has been defined.
		/// </summary>
		IMapMemberCollection Members { get; }

		/// <summary>
		/// The collection of columns in the database associated with this map.
		/// </summary>
		IMapColumnCollection Columns { get; }

		/// <summary>
		/// Represents the column to be used for row version control, if any.
		/// </summary>
		IMapVersionColumn VersionColumn { get; }

		/// <summary>
		/// Whether this map has been validated against the underlying database or not.
		/// </summary>
		bool IsValidated { get; }

		/// <summary>
		/// Validates this map so that it becomes usable for map operations.
		/// <para>
		/// If this map is already validated then this operation has no effects. Once a map is
		/// validated then it does not allow any further changes in its rules or structure.
		/// Validation is carried automatically by the framework when needed, but can also be
		/// invoked explicitly by client applications in order to lock the map and disable any
		/// further modification to it.
		/// </para>
		/// </summary>
		void Validate();

		/// <summary>
		/// Whether tracking of entities is enabled or disabled for this map.
		/// </summary>
		bool TrackEntities { get; set; }

		/// <summary>
		/// The collection of entities in a valid state tracked by this map.
		/// </summary>
		IEnumerable<IMetaEntity> Entities { get; }

		/// <summary>
		/// Clears the cache of this map and, optionally, detaches the entities that were
		/// tracked.
		/// </summary>
		/// <param name="detach">True to also detach the entities removed from the cache.</param>
		void ClearEntities(bool detach = true);

		/// <summary>
		/// Creates a new entity with the appropriate type for the requested map.
		/// <para>This method is invoked to generate instances that support virtual lazy
		/// properties when needed. Client applications can use but it is not needed.</para>
		/// </summary>
		/// <returns>A new entity.</returns>
		object NewEntity();

		/// <summary>
		/// Attaches the given entity into this map.
		/// </summary>
		/// <param name="entity">The entity to attach into this instance.</param>
		void Attach(object entity);

		/// <summary>
		/// Removes the given entity from this map, making it become a detached one. Returns true
		/// if the entity has been removed, or false otherwise.
		/// </summary>
		/// <param name="entity">The entity to detach from this instance.</param>
		/// <returns>True if the instance has been removed, false otherwise.</returns>
		bool Detach(object entity);

		/// <summary>
		/// Creates a new query command for the entities managed by this map.
		/// </summary>
		/// <returns>A new query command.</returns>
		IDataQuery Query();

		/// <summary>
		/// Creates a new query command for the entities managed by this map, and sets the initial
		/// contents of its WHERE clause.
		/// </summary>
		/// <param name="where">The dynamic lambda expression that resolves into the contents of
		/// this clause.
		/// <returns>A new query command.</returns>
		IDataQuery Where(Func<dynamic, object> where);

		/// <summary>
		/// finds and returns inmediately a suitable entity that meets the conditions given, by
		/// looking for it in the managed cache and, if it cannot be found there, querying the
		/// database for it. returns null if such entity cannot be found neither in the cache
		/// nor in the database.
		/// </summary>
		/// <param name="specs">a collection of dynamic lambda expressions each containing the
		/// name and value to find for a column, as in: 'x => x.column == value'.</param>
		/// <returns>the requested entity, or null.</returns>
		object FindNow(params Func<dynamic, object>[] specs);

		/// <summary>
		/// Refreshes inmediately the contents of the given entity (and potentially of its
		/// dependencies), along with all the entities in the cache that share the same
		/// identity.
		/// <para>Returns null if the entity cannot be found any longer in the database, or
		/// a refreshed entity otherwise. In the later case it is NOT guaranteed that the one
		/// returned is the same as the original one, but potentially any other suitable one.</para>
		/// </summary>
		/// <param name="entity">The entitt to refresh.</param>
		/// <returns>A refreshed entity, or null.</returns>
		object RefreshNow(object entity);

		/// <summary>
		/// Creates a new insert operation for the given entity.
		/// <para>The new command must be firstly submitted into the associated repository in
		/// order it to be executed when all pending change operations annotated into that
		/// repository are executed as a group.</para>
		/// </summary>
		/// <param name="entity">The entity to be inserted.</param>
		/// <returns>A new command.</returns>
		IDataInsert Insert(object entity);

		/// <summary>
		/// Creates a new delete operation for the given entity.
		/// <para>The new command must be firstly submitted into the associated repository in
		/// order it to be executed when all pending change operations annotated into that
		/// repository are executed as a group.</para>
		/// </summary>
		/// <param name="entity">The entity to be inserted.</param>
		/// <returns>A new command.</returns>
		IDataDelete Delete(object entity);

		/// <summary>
		/// Creates a new delete operation for the given entity.
		/// <para>The new command must be firstly submitted into the associated repository in
		/// order it to be executed when all pending change operations annotated into that
		/// repository are executed as a group.</para>
		/// </summary>
		/// <param name="entity">The entity to be inserted.</param>
		/// <returns>A new command.</returns>
		IDataUpdate Update(object entity);
	}

	// ====================================================
	/// <summary>
	/// Represents a map between entities of a POCO class and a primary table in an underlying
	/// database-alike service.
	/// </summary>
	public interface IDataMap<T> : IDataMap where T : class
	{
		/// <summary>
		/// Returns a new instance that is associated with the new given repository and that
		/// contains a copy of the customizations of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		new IDataMap<T> Clone(IDataRepository repo);

		/// <summary>
		/// The collection of members of the type for which either a dependency and/or a
		/// completion method has been defined.
		/// </summary>
		new IMapMemberCollection<T> Members { get; }

		/// <summary>
		/// The collection of columns in the database associated with this map.
		/// </summary>
		new IMapColumnCollection<T> Columns { get; }

		/// <summary>
		/// Represents the column to be used for row version control, if any.
		/// </summary>
		new IMapVersionColumn<T> VersionColumn { get; }

		/// <summary>
		/// Creates a new entity with the appropriate type for the requested map.
		/// <para>This method is invoked to generate instances that support virtual lazy
		/// properties when needed. Client applications can use but it is not needed.</para>
		/// </summary>
		/// <returns>A new entity.</returns>
		new T NewEntity();

		/// <summary>
		/// Attaches the given entity into this map.
		/// </summary>
		/// <param name="entity">The entity to attach into this instance.</param>
		void Attach(T entity);

		/// <summary>
		/// Removes the given entity from this map, making it become a detached one. Returns true
		/// if the entity has been removed, or false otherwise.
		/// </summary>
		/// <param name="entity">The entity to detach from this instance.</param>
		/// <returns>True if the instance has been removed, false otherwise.</returns>
		bool Detach(T entity);

		/// <summary>
		/// Creates a new query command for the entities managed by this map.
		/// </summary>
		/// <returns>A new query command.</returns>
		new IDataQuery<T> Query();

		/// <summary>
		/// Creates a new query command for the entities managed by this map, and sets the initial
		/// contents of its WHERE clause.
		/// </summary>
		/// <param name="where">The dynamic lambda expression that resolves into the contents of
		/// this clause.
		/// <returns>A new query command.</returns>
		new IDataQuery<T> Where(Func<dynamic, object> where);

		/// <summary>
		/// Finds and returns inmediately a suitable entity that meets the conditions given, by
		/// looking for it in the managed cache and, if it cannot be found there, querying the
		/// database for it. Returns null if such entity cannot be found neither in the cache
		/// nor in the database.
		/// </summary>
		/// <param name="specs">A collection of dynamic lambda expressions each containing the
		/// name and value to find for a column, as in: 'x => x.Column == Value'.</param>
		/// <returns>The requested entity, or null.</returns>
		new T FindNow(params Func<dynamic, object>[] specs);

		/// <summary>
		/// Refreshes inmediately the contents of the given entity (and potentially of its
		/// dependencies), along with all the entities in the cache that share the same
		/// identity.
		/// <para>Returns null if the entity cannot be found any longer in the database, or
		/// a refreshed entity otherwise. In the later case it is NOT guaranteed that the one
		/// returned is the same as the original one, but potentially any other suitable one.</para>
		/// </summary>
		/// <param name="entity">The entitt to refresh.</param>
		/// <returns>A refreshed entity, or null.</returns>
		T RefreshNow(T entity);

		/// <summary>
		/// Creates a new insert operation for the given entity.
		/// <para>The new command must be firstly submitted into the associated repository in
		/// order it to be executed when all pending change operations annotated into that
		/// repository are executed as a group.</para>
		/// </summary>
		/// <param name="entity">The entity to be inserted.</param>
		/// <returns>A new command.</returns>
		IDataInsert<T> Insert(T entity);

		/// <summary>
		/// Creates a new delete operation for the given entity.
		/// <para>The new command must be firstly submitted into the associated repository in
		/// order it to be executed when all pending change operations annotated into that
		/// repository are executed as a group.</para>
		/// </summary>
		/// <param name="entity">The entity to be inserted.</param>
		/// <returns>A new command.</returns>
		IDataDelete<T> Delete(T entity);

		/// <summary>
		/// Creates a new delete operation for the given entity.
		/// <para>The new command must be firstly submitted into the associated repository in
		/// order it to be executed when all pending change operations annotated into that
		/// repository are executed as a group.</para>
		/// </summary>
		/// <param name="entity">The entity to be inserted.</param>
		/// <returns>A new command.</returns>
		IDataUpdate<T> Update(T entity);
	}
}
