// ======================================================== IDataMap.cs
namespace Kerosene.ORM.Maps
{
	using Kerosene.Tools;
	using System;
	using System.Collections.Generic;

	// ==================================================== 
	/// <summary>
	/// Represents how a map will discover what database columns to take into consideration for
	/// its structure and operations.
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
	/// Represents a map registered in a given repository between the entities of the type it
	/// is associated with and their database representation.
	/// </summary>
	public interface IDataMap : IDisposableEx
	{
		/// <summary>
		/// Returns a new instance associated with the given repository that contains a copy
		/// of the structure and rules of the original map.
		/// </summary>
		/// <param name="repo">The repository the new map will be registered into.</param>
		/// <returns>A new map.</returns>
		IDataMap Clone(IDataRepository repo);

		/// <summary>
		/// The repository this map is registered into.
		/// </summary>
		IDataRepository Repository { get; }

		/// <summary>
		/// Whether this map is considered a weak one.
		/// <para>Weak maps are created automatically when an entity type is referenced by any
		/// map operation and there was no registered map for that type. Weak maps are disposed
		/// if a regular non-weak map is registered (created) explicitly.</para>
		/// </summary>
		bool IsWeakMap { get; }

		/// <summary>
		/// The type of the entities managed by this map.
		/// <para>Note that there is a strict one to one correspondence between a map and a type
		/// for a given repository, and no covariance or contravariance are taken into account.</para>
		/// </summary>
		Type EntityType { get; }

		/// <summary>
		/// The name of the master table in the database where to find at least the identity
		/// columns of the associated entities.
		/// </summary>
		string Table { get; }

		/// <summary>
		/// If not null a dynamic lambda expression that contains the logic of the WHERE clause
		/// to use to differentiate among entities of different types that may share the master
		/// table.
		/// </summary>
		Func<dynamic, object> Discriminator { get; set; }

		/// <summary>
		/// How the map will discover what database columns to take into consideration for its
		/// structure and operations.
		/// </summary>
		MapDiscoveryMode DiscoveryMode { get; set; }

		/// <summary>
		/// The collection of members that have been explicitly defined for this map.
		/// </summary>
		IMapMemberCollection Members { get; }

		/// <summary>
		/// The collection of columns to take into consideration for the operations of this map.
		/// </summary>
		IMapColumnCollection Columns { get; }

		/// <summary>
		/// The instance that represents the database column to be used for row version control
		/// if its name property is not null.
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
		/// The cache of entities of this map, excluding collected or invalid ones.
		/// </summary>
		IEnumerable<IMetaEntity> MetaEntities { get; }

		/// <summary>
		/// Clears the cache of entities this map maintains.
		/// </summary>
		void ClearEntities();

		/// <summary>
		/// Creates a new entity with the appropriate type for the requested map.
		/// <para>This method is invoked to generate instances that support virtual lazy
		/// properties when needed. Client applications can use but it is not needed.</para>
		/// </summary>
		/// <returns>A new entity.</returns>
		object NewEntity();

		/// <summary>
		/// Attaches the given entity to this map.
		/// </summary>
		/// <param name="entity">The entity to attach.</param>
		void Attach(object entity);

		/// <summary>
		/// Detaches the given entity from this map. Returns true if it has been detached, or
		/// false otherwise.
		/// </summary>
		/// <param name="entity">The entity to detach.</param>
		bool Detach(object entity);

		/// <summary>
		/// Creates a new query operation for entities managed by the map.
		/// </summary>
		/// <returns>A new operation.</returns>
		IDataQuery Query();

		/// <summary>
		/// Creates a new query operation for entities managed by the map, and sets the initial
		/// contents of its WHERE clause:
		/// </summary>
		/// <param name="where">he dynamic lambda expression that resolves into the contents of
		/// this clause.
		/// <para>- By default, if any previous contents exist the new ones are appended using an
		/// AND logical operator. However, the virtual extension methods 'x => x.And(...)' and
		/// 'x => x.Or(...)' can be used to specify the concrete logical operator to use for
		/// concatenation purposes.</para>
		/// </param>
		/// <returns>A new operation.</returns>
		IDataQuery Where(Func<dynamic, object> where);

		/// <summary>
		/// Finds inmediately a suitable entity by either returning the first entity in the
		/// cache that meets the given specifications or, if no one is found in the cache,
		/// querying the underlying database for it. Returns null if no entity can be found
		/// neither in the cache nor in the database.
		/// </summary>
		/// <param name="specs">A collection of dynamic lambda expressions each containing the
		/// name of column and the value to find for that column 'x => x.Column == Value'.</param>
		/// <returns>The requested entity, or null.</returns>
		object FindNow(params Func<dynamic, object>[] specs);

		/// <summary>
		/// Refreshes inmediately from the database the given entity, even if it wwas not
		/// attached to the map before, along with all the entities in the cache that share
		/// the same identity.
		/// </summary>
		/// <param name="entity">The entity to refresh</param>
		/// <returns>A refreshed entity, or null if no one was found in the database.</returns>
		object RefreshNow(object entity);

		/// <summary>
		/// Creates a new insert operation for the entity managed by the map.
		/// </summary>
		/// <param name="entity">The entity affected by this operation.</param>
		/// <returns>A new operation.</returns>
		IDataInsert Insert(object entity);

		/// <summary>
		/// Creates a new delete operation for the entity managed by the map.
		/// </summary>
		/// <param name="entity">The entity affected by this operation.</param>
		/// <returns>A new operation.</returns>
		IDataDelete Delete(object entity);

		/// <summary>
		/// Creates a new update operation for the entity managed by the map.
		/// </summary>
		/// <param name="entity">The entity affected by this operation.</param>
		/// <returns>A new operation.</returns>
		IDataUpdate Update(object entity);
	}

	// ==================================================== 
	/// <summary>
	/// Represents a map registered in a given repository between the entities of the type it
	/// is associated with and their database representation.
	/// </summary>
	public interface IDataMap<T> : IDataMap where T : class
	{
		/// <summary>
		/// Returns a new instance associated with the given repository that contains a copy
		/// of the structure and rules of the original map.
		/// </summary>
		/// <param name="repo">The repository the new map will be registered into.</param>
		/// <returns>A new map.</returns>
		new IDataMap<T> Clone(IDataRepository repo);

		/// <summary>
		/// The collection of members that have been explicitly defined for this map.
		/// </summary>
		new IMapMemberCollection<T> Members { get; }

		/// <summary>
		/// The collection of columns to take into consideration for the operations of this map.
		/// </summary>
		new IMapColumnCollection<T> Columns { get; }

		/// <summary>
		/// The instance that represents the database column to be used for row version control
		/// if its name property is not null.
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
		/// Attaches the given entity to this map.
		/// </summary>
		/// <param name="entity">The entity to attach.</param>
		void Attach(T entity);

		/// <summary>
		/// Detaches the given entity from this map. Returns true if it has been detached, or
		/// false otherwise.
		/// </summary>
		/// <param name="entity">The entity to detach.</param>
		bool Detach(T entity);

		/// <summary>
		/// Creates a new query operation for entities managed by the map.
		/// </summary>
		/// <returns>A new operation.</returns>
		new IDataQuery<T> Query();

		/// <summary>
		/// Creates a new query operation for entities managed by the map, and sets the initial
		/// contents of its WHERE clause:
		/// </summary>
		/// <param name="where">he dynamic lambda expression that resolves into the contents of
		/// this clause.
		/// <para>- By default, if any previous contents exist the new ones are appended using an
		/// AND logical operator. However, the virtual extension methods 'x => x.And(...)' and
		/// 'x => x.Or(...)' can be used to specify the concrete logical operator to use for
		/// concatenation purposes.</para>
		/// </param>
		/// <returns>A new operation.</returns>
		new IDataQuery<T> Where(Func<dynamic, object> where);

		/// <summary>
		/// Finds inmediately a suitable entity by either returning the first entity in the
		/// cache that meets the given specifications or, if no one is found in the cache,
		/// querying the underlying database for it. Returns null if no entity can be found
		/// neither in the cache nor in the database.
		/// </summary>
		/// <param name="specs">A collection of dynamic lambda expressions each containing the
		/// name of column and the value to find for that column 'x => x.Column == Value'.</param>
		/// <returns>The requested entity, or null.</returns>
		new T FindNow(params Func<dynamic, object>[] specs);

		/// <summary>
		/// Refreshes inmediately from the database the given entity, even if it wwas not
		/// attached to the map before, along with all the entities in the cache that share
		/// the same identity.
		/// </summary>
		/// <param name="entity">The entity to refresh</param>
		/// <returns>A refreshed entity, or null if no one was found in the database.</returns>
		T RefreshNow(T entity);

		/// <summary>
		/// Creates a new insert operation for the entity managed by the map.
		/// </summary>
		/// <param name="entity">The entity affected by this operation.</param>
		/// <returns>A new operation.</returns>
		IDataInsert<T> Insert(T entity);

		/// <summary>
		/// Creates a new delete operation for the entity managed by the map.
		/// </summary>
		/// <param name="entity">The entity affected by this operation.</param>
		/// <returns>A new operation.</returns>
		IDataDelete<T> Delete(T entity);

		/// <summary>
		/// Creates a new update operation for the entity managed by the map.
		/// </summary>
		/// <param name="entity">The entity affected by this operation.</param>
		/// <returns>A new operation.</returns>
		IDataUpdate<T> Update(T entity);
	}
}
// ======================================================== 
