// ======================================================== DataMapT.cs
namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;
	using System.Runtime.Serialization;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Extends the <see cref="IDataMap"/> interface.
	/// </summary>
	internal interface IUberMap : IDataMap
	{
		/// <summary>
		/// The repository this map is registered into.
		/// </summary>
		new DataRepository Repository { get; }

		/// <summary>
		/// The link reference held by the associated repository, if any.
		/// </summary>
		IDataLink Link { get; }

		/// <summary>
		/// The collection of members that have been explicitly defined for this map.
		/// </summary>
		new IUberMemberCollection Members { get; }

		/// <summary>
		/// The collection of columns to take into consideration for the operations of this map.
		/// </summary>
		new IUberColumnCollection Columns { get; }

		/// <summary>
		/// The instance that represents the database column to be used for row version control
		/// if its name property is not null.
		/// </summary>
		new IUberVersionColumn VersionColumn { get; }

		/// <summary>
		/// The collection of entities maintained by this map.
		/// </summary>
		UberEntitySet UberEntities { get; }

		/// <summary>
		/// The proxy holder created to manage the entities of this map, if any.
		/// </summary>
		ProxyHolder ProxyHolder { get; }

		/// <summary>
		/// The extended type created to manage the entities of this map, if any.
		/// </summary>
		Type ProxyType { get; }

		/// <summary>
		/// The database schema of the records to be obtained for this map.
		/// </summary>
		ISchema Schema { get; }

		/// <summary>
		/// The schema defining the identity columns for this map.
		/// </summary>
		ISchema SchemaId { get; }

		/// <summary>
		/// Writes into the record the values obtained from the entity.
		/// <para>Only the columns in the target record are taken into consideration.</para>
		/// </summary>
		/// <param name="entity">The source entity.</param>
		/// <param name="record">The target record.</param>
		void WriteRecord(object entity, IRecord record);

		/// <summary>
		/// Loads into the entity the values obtained from the record.
		/// <para>Only the columns in the source record are taken into consideration.</para>
		/// </summary>
		/// <param name="record">The source record.</param>
		/// <param name="entity">The target entity.</param>
		void LoadEntity(IRecord record, object entity);

		/// <summary>
		/// Completes the members of the given meta-entity.
		/// <para>Only the non-lazy members are processed by this method.</para>
		/// <para>Lazy members are completed through their getters when invoked.</para>
		/// </summary>
		/// <param name="meta">The meta-entity whose members are to be completed.</param>
		void CompleteMembers(MetaEntity meta);

		/// <summary>
		/// Creates a new temporal record containing the identity columns only. Note that the
		/// schema of the new record is the same as the SchemaId reference of this map, and
		/// hence it should not be disposed.
		/// </summary>
		IRecord ExtractId(IRecord source);
	}

	// ==================================================== 
	/// <summary>
	/// Represents a map between the type of the entities it manages and their database
	/// representation.
	/// </summary>
	/// <typeparam name="T">The type of the entities managed by this map.</typeparam>
	public class DataMap<T> : IDataMap<T>, IUberMap where T : class
	{
		bool _IsDisposed = false;
		ulong _SerialId = 0;
		DataRepository _Repository = null;
		bool _IsValidated = false;
		ISchema _Schema = null;
		ISchema _SchemaId = null;
		ProxyHolder _ProxyHolder = null;
		ConstructorInfo _ConstructorInfo = null;
		UberEntitySet _UberEntities = new UberEntitySet();

		string _Table = null;
		bool _IsWeakMap = false;
		Func<dynamic, object> _Discriminator = null;
		MapDiscoveryMode _DiscoveryMode = MapDiscoveryMode.Auto;
		MapMemberCollection<T> _Members = null;
		MapColumnCollection<T> _Columns = null;
		MapVersionColumn<T> _VersionColumn = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="repo">The repository this map will be registered into.</param>
		/// <param name="table">A dynamic lambda expression that resolves into the name of the
		/// primary table for this map.</param>
		/// <param name="weak">Whether this map shall be considered a weak map or not.</param>
		public DataMap(DataRepository repo, Func<dynamic, object> table, bool weak = false)
		{
			if (table == null) throw new ArgumentNullException("table", "Table specification cannot be null.");
			var name = DynamicInfo.ParseName(table);
			OnInitialize(repo, name, weak);
		}

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="repo">The repository this map will be registered into.</param>
		/// <param name="table">The name of the primary table for this map.</param>
		/// <param name="weak">Whether this map shall be considered a weak map or not.</param>
		public DataMap(DataRepository repo, string table, bool weak = false)
		{
			OnInitialize(repo, table, weak);
		}

		/// <summary>
		/// Invoked when initializing this instance.
		/// </summary>
		void OnInitialize(DataRepository repo, string table, bool weak)
		{
			if (repo == null) throw new ArgumentNullException("repo", "Repository cannot be null.");
			if (repo.IsDisposed) throw new ObjectDisposedException(repo.ToString());
			if (repo.Link.IsDisposed) throw new ObjectDisposedException(repo.Link.ToString());
			table = table.Validated("Table Name");

			lock (repo.UberMaps.SyncRoot)
			{
				var temp = repo.GetMap<T>(); if (temp != null)
				{
					if (temp.IsWeakMap && !weak && UberHelper.EnableWeakMaps)
					{
						temp.Dispose();
						repo.UberMaps.Add(this);
					}
					else throw new DuplicateException(
						"A map for type '{0}' is already registered in '{1}'."
						.FormatWith(EntityType.EasyName(), repo));
				}
				else repo.UberMaps.Add(this);

				_Repository = repo;
				_Table = table;
				_IsWeakMap = weak;
				_SerialId = ++UberHelper.DataMapLastSerial;

				_Members = new MapMemberCollection<T>(this);
				_Columns = new MapColumnCollection<T>(this);
				_VersionColumn = new MapVersionColumn<T>(this);
			}
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

		~DataMap()
		{
			if (!IsDisposed) OnDispose(false);
		}

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		protected virtual void OnDispose(bool disposing)
		{
			if (disposing)
			{
				if (_Repository != null && !_Repository.IsDisposed)
				{
					if (_Repository.UberOperations != null)
					{
						lock (_Repository.UberOperations.SyncRoot)
						{
							var list = _Repository.UberOperations
								.Where(x => object.ReferenceEquals(x.Map, this))
								.ToArray();

							foreach (var op in list) op.Dispose();
						}
					}
					if (_Repository.UberMaps != null)
					{
						lock (_Repository.UberMaps.SyncRoot)
						{
							_Repository.UberMaps.Remove(this);
						}
					}
				}
				if (_UberEntities != null)
				{
					_UberEntities.Clear(); // Also resets/disposes all the entities...
				}
				if (_Members != null) _Members.Dispose();
				if (_Columns != null) _Columns.Dispose();
				if (_VersionColumn != null) _VersionColumn.Dispose();
			}

			_Members = null;
			_Columns = null;
			_VersionColumn = null;

			_Discriminator = null;
			_IsValidated = false;
			_ConstructorInfo = null;
			_Schema = null;
			_SchemaId = null;
			_ProxyHolder = null;

			_UberEntities = null;
			_Repository = null;

			_IsDisposed = true;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			var str = string.Format("{0}:{1}({2}{3} => {4}({5}){6})",
				SerialId,
				GetType().EasyName(),
				EntityType.EasyName(),
				ProxyHolder != null ? ":Proxy" : string.Empty,
				Table ?? "-",
				Repository.Sketch(),
				IsWeakMap ? "(weak)" : string.Empty);

			return IsDisposed ? "disposed::{0}".FormatWith(str) : str;
		}

		/// <summary>
		/// Returns a new instance associated with the given repository that contains a copy
		/// of the structure and rules of the original map.
		/// </summary>
		/// <param name="repo">The repository the new map will be registered into.</param>
		/// <returns>A new map.</returns>
		public DataMap<T> Clone(DataRepository repo)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (repo == null) throw new ArgumentNullException("repo", "Repository cannot be null.");
			if (repo.IsDisposed) throw new ObjectDisposedException(repo.ToString());

			var cloned = new DataMap<T>(repo, Table, IsWeakMap);
			OnClone(cloned); return cloned;
		}
		IDataMap<T> IDataMap<T>.Clone(IDataRepository repo)
		{
			if (repo == null) throw new ArgumentNullException("repo", "Repository cannot be null.");
			var temp = repo as DataRepository; if (temp == null)
				throw new InvalidCastException("Repository '{0}' is not a DataRepository instance.".FormatWith(repo));

			return this.Clone(temp);
		}
		IDataMap IDataMap.Clone(IDataRepository repo)
		{
			if (repo == null) throw new ArgumentNullException("repo", "Repository cannot be null.");
			var temp = repo as DataRepository; if (temp == null)
				throw new InvalidCastException("Repository '{0}' is not a DataRepository instance.".FormatWith(repo));

			return this.Clone(temp);
		}

		/// <summary>
		/// Invoked when cloning this object to set its state at this point of the inheritance
		/// chain.
		/// </summary>
		/// <param name="cloned">The cloned object.</param>
		protected virtual void OnClone(object cloned)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			var temp = cloned as DataMap<T>;
			if (cloned == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' one."
				.FormatWith(cloned.Sketch(), typeof(DataMap<T>).EasyName()));

			temp._Discriminator = _Discriminator;
			temp._DiscoveryMode = _DiscoveryMode;
			temp._Members = _Members.Clone(temp);
			temp._Columns = _Columns.Clone(temp);
			temp._VersionColumn = _VersionColumn.Clone(temp);
		}

		/// <summary>
		/// The serial id assigned to this instance.
		/// </summary>
		public ulong SerialId
		{
			get { return _SerialId; }
		}

		/// <summary>
		/// The repository this map is registered into.
		/// </summary>
		public DataRepository Repository
		{
			get { return _Repository; }
		}
		IDataRepository IDataMap.Repository
		{
			get { return this.Repository; }
		}

		/// <summary>
		/// The link reference held by the associated repository, if any.
		/// </summary>
		public IDataLink Link
		{
			get { return Repository == null ? null : Repository.Link; }
		}

		/// <summary>
		/// The type of the entities managed by this map.
		/// </summary>
		public Type EntityType
		{
			get { return typeof(T); }
		}

		/// <summary>
		/// The name of the primary table in the underlying database associated with the entities
		/// managed by this map.
		/// </summary>
		public string Table
		{
			get { return _Table; }
		}

		/// <summary>
		/// Whether this map is considered a weak one.
		/// <para>Weak maps are created automatically when an entity type is referenced by any
		/// map operation and there was no registered map for that type. Weak maps are disposed
		/// if a regular non-weak map is registered (created) explicitly.</para>
		/// </summary>
		public bool IsWeakMap
		{
			get { return _IsWeakMap; }
		}

		/// <summary>
		/// If not null a dynamic lambda expression that resolves into the logic to add into
		/// the WHERE clauses sent to the database to discriminate among entities of different
		/// types that may share the primary table.
		/// </summary>
		public Func<dynamic, object> Discriminator
		{
			get { return _Discriminator; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (IsValidated) throw new InvalidOperationException("This map '{0}' is validated.".FormatWith(this));

				_Discriminator = value;
			}
		}

		/// <summary>
		/// How the map will discover what database columns to take into consideration for its
		/// structure and operations.
		/// </summary>
		public MapDiscoveryMode DiscoveryMode
		{
			get { return _DiscoveryMode; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (IsValidated) throw new InvalidOperationException("This map '{0}' is validated.".FormatWith(this));

				_DiscoveryMode = value;
			}
		}

		/// <summary>
		/// Represents the collection of members that have been explicitly defined for this
		/// map.
		/// </summary>
		public MapMemberCollection<T> Members
		{
			get { return _Members; }
		}
		IMapMemberCollection<T> IDataMap<T>.Members
		{
			get { return this.Members; }
		}
		IMapMemberCollection IDataMap.Members
		{
			get { return this.Members; }
		}
		IUberMemberCollection IUberMap.Members
		{
			get { return this.Members; }
		}

		/// <summary>
		/// Represents the collection of columns in the primary table of the database this map
		/// is taken into consideration for its operations.
		/// </summary>
		public MapColumnCollection<T> Columns
		{
			get { return _Columns; }
		}
		IMapColumnCollection<T> IDataMap<T>.Columns
		{
			get { return this.Columns; }
		}
		IMapColumnCollection IDataMap.Columns
		{
			get { return this.Columns; }
		}
		IUberColumnCollection IUberMap.Columns
		{
			get { return this.Columns; }
		}

		/// <summary>
		/// Represents the column in the primary table in the database that will be used for row
		/// version control, if any.
		/// </summary>
		public MapVersionColumn<T> VersionColumn
		{
			get { return _VersionColumn; }
		}
		IMapVersionColumn<T> IDataMap<T>.VersionColumn
		{
			get { return this.VersionColumn; }
		}
		IMapVersionColumn IDataMap.VersionColumn
		{
			get { return this.VersionColumn; }
		}
		IUberVersionColumn IUberMap.VersionColumn
		{
			get { return this.VersionColumn; }
		}

		/// <summary>
		/// Whether this map has been validated against the underlying database or not.
		/// </summary>
		public bool IsValidated
		{
			get { return _IsValidated; }
		}

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
		public void Validate()
		{
			if (IsValidated) return;

			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (_Repository.IsDisposed) throw new ObjectDisposedException(_Repository.ToString());
			if (_Repository.Link.IsDisposed) throw new ObjectDisposedException(_Repository.Link.ToString());

			#region GENERATE SELECTS...

			bool sensitive = Repository.Link.Engine.CaseSensitiveNames;
			List<string> selects = new List<string>();
			string str = null;

			if (_DiscoveryMode == MapDiscoveryMode.Explicit)
			{
				if (VersionColumn.Name != null)
				{
					selects.Add(VersionColumn.Name);
				}
				foreach (var col in Columns)
				{
					if (col.Excluded) continue;

					str = selects.Find(x => string.Compare(x, col.Name, !sensitive) == 0);
					if (str == null) selects.Add(col.Name);
				}
				foreach (var member in Members)
				{
					foreach (var mcol in member.Columns)
					{
						str = selects.Find(x => string.Compare(x, mcol.Name, !sensitive) == 0);
						if (str == null) selects.Add(mcol.Name);
					}
				}

				if (selects.Count == 0) throw new EmptyException(
					"No candidate columns defined in this map '{0}' for Explicit Discovery Mode"
					.FormatWith(this));
			}

			#endregion

			#region OBTAIN PRELIMINARY SCHEMA...

			var cmd = Link.From(x => Table);
			cmd.Top(1);
			foreach (var select in selects) cmd.Select(x => select);

			var iter = cmd.GetEnumerator(); bool r = iter.MoveNext();
			var schema = iter.Schema;
			var record = iter.CurrentRecord; if (record != null) record.Dispose();
			iter.Reset();
			iter.Dispose();

			if (schema.Count == 0) throw new EmptyException(
				"Schema is empty for map '{0}'.".FormatWith(this));

			#endregion

			#region DISCARDING ENTRIES OR ADDING NEW COLUMNS...

			List<ISchemaEntry> entries = new List<ISchemaEntry>(schema);
			foreach (var entry in entries)
			{
				// See if column is excluded or has been defined...
				var col = Columns.FirstOrDefault<MapColumn<T>>(
					x => string.Compare(x.Name, entry.ColumnName, !sensitive) == 0);

				if (col != null)
				{
					if (col.Excluded) schema.Remove(entry);
					continue;
				}

				// See if column is defined as the row version control one...
				if (string.Compare(VersionColumn.Name, entry.ColumnName, !sensitive) == 0) continue;

				// See if column is defined for a member...
				bool found = false;
				foreach (var member in Members)
					foreach (var mcol in member.Columns)
						if (string.Compare(mcol.Name, entry.ColumnName, !sensitive) == 0) found = true;
				if (found) continue;

				// Discarding entry if explicit mode is set...
				if (DiscoveryMode == MapDiscoveryMode.Explicit)
				{
					schema.Remove(entry);
					continue;
				}

				// Adding and auto-column...
				col = Columns.Add(x => entry.ColumnName);
				col.AutoDiscovered = true;
			}
			if (schema.Count == 0) throw new InvalidOperationException(
				"Schema is empty after removing columns for map '{0}'.".FormatWith(this));

			entries.Clear();
			entries = null;

			#endregion

			#region UPLOADING MASTER TABLE NAME...

			entries = new List<ISchemaEntry>(schema);
			foreach (var entry in entries) schema.Remove(entry);
			foreach (var entry in entries) { entry.TableName = Table; schema.Add(entry); }

			#endregion

			#region VALIDATING MAP DEFINITIONS...

			ISchemaEntry temp = null;

			if (VersionColumn.Name != null)
			{
				// Can use FindEntry as in a map columns should have unique names...
				temp = schema.FindEntry(VersionColumn.Name);
				if (temp == null) throw new NotFoundException(
					"Row version column '{0}' not found in the generated schema.".FormatWith(VersionColumn.Name));
			}
			foreach (var col in Columns)
			{
				if (col.Excluded) continue;
				if (col.AutoDiscovered) continue;

				// Can use FindEntry as in a map columns should have unique names...
				temp = schema.FindEntry(col.Name);
				if (temp == null) throw new NotFoundException(
					"Column '{0}' not found in the generated schema.".FormatWith(col.Name));
			}
			foreach (var member in Members)
			{
				foreach (var mcol in member.Columns)
				{
					// Can use FindEntry as in a map columns should have unique names...
					temp = schema.FindEntry(mcol.Name);
					if (temp == null) throw new NotFoundException(
						"Column '{0}' for member '{1}' not found in the generated schema.".FormatWith(mcol.Name, member.Name));
				}
			}

			#endregion

			#region CAPTURING THE WORKING SCHEMAS...

			_Schema = schema;
			if (_Schema.Count == 0) throw new EmptyException(
				"Generated schema is empty for map '{0}'.".FormatWith(this));

			_SchemaId = _Schema.Clone();
			_SchemaId.Clear();
			_SchemaId.AddRange(_Schema.PrimaryKeyColumns());
			if (_SchemaId.Count == 0) _SchemaId.AddRange(_Schema.UniqueValuedColumns(), cloneNotOrphans: true);
			if (_SchemaId.Count == 0) throw new EmptyException(
				"Generated schema '{0}' does not contain identity columns for map '{1}'.".FormatWith(_Schema, this));

			#endregion

			#region GENERATING A PROXY HOLDER IF NEEDED...

			_ProxyHolder = ProxyGenerator.Locate(this);

			#endregion

			#region CAPTURING PARAMERTERLESS CONSTRUCTOR IF ANY...

			var type = ProxyHolder != null ? ProxyHolder.ExtendedType : EntityType;
			var cons = type.GetConstructors(TypeEx.InstancePublicAndHidden);
			foreach (var con in cons)
			{
				var pars = con.GetParameters();
				if (pars.Length == 0) { _ConstructorInfo = con; break; }
			}

			#endregion

			_IsValidated = true;
		}

		/// <summary>
		/// The proxy holder created to manage the entities of this map, if any.
		/// </summary>
		internal ProxyHolder ProxyHolder
		{
			get { return _ProxyHolder; }
		}
		ProxyHolder IUberMap.ProxyHolder
		{
			get { return this.ProxyHolder; }
		}

		/// <summary>
		/// The extended type created to manage the entities of this map, if any.
		/// </summary>
		internal Type ProxyType
		{
			get { return ProxyHolder == null ? null : ProxyHolder.ExtendedType; }
		}
		Type IUberMap.ProxyType
		{
			get { return this.ProxyType; }
		}

		/// <summary>
		/// The database schema of the records to be obtained for this map.
		/// </summary>
		internal ISchema Schema
		{
			get { return _Schema; }
		}
		ISchema IUberMap.Schema
		{
			get { return this.Schema; }
		}

		/// <summary>
		/// The schema defining the identity columns for this map.
		/// </summary>
		internal ISchema SchemaId
		{
			get { return _SchemaId; }
		}
		ISchema IUberMap.SchemaId
		{
			get { return this.SchemaId; }
		}

		/// <summary>
		/// Writes into the record the values obtained from the entity.
		/// <para>Only the columns in the target record are taken into consideration.</para>
		/// </summary>
		/// <param name="entity">The source entity.</param>
		/// <param name="record">The target record.</param>
		internal void WriteRecord(T entity, IRecord record)
		{
			bool sensitive = Link.Engine.CaseSensitiveNames;
			for (int i = 0; i < record.Count; i++)
			{
				var entry = record.Schema[i];

				foreach (var col in Columns)
				{
					if (col.WriteEnabled &&
						string.Compare(col.Name, entry.ColumnName, !sensitive) == 0)
					{
						if (col.WriteRecord != null)
						{
							var value = col.WriteRecord(entity);
							record[i] = value;
						}
						else if (col.ElementInfo != null && col.ElementInfo.CanRead)
						{
							var value = col.ElementInfo.GetValue(entity);
							record[i] = value;
						}
					}
				}

				foreach (var member in Members)
				{
					foreach (var mcol in member.Columns)
					{
						if (mcol.WriteEnabled &&
							string.Compare(mcol.Name, entry.ColumnName, !sensitive) == 0)
						{
							if (mcol.WriteRecord != null)
							{
								var value = mcol.WriteRecord(entity);
								record[i] = value;
							}
							else if (mcol.ElementInfo != null && mcol.ElementInfo.CanRead)
							{
								var value = mcol.ElementInfo.GetValue(entity);
								record[i] = value;
							}
						}
					}
				}
			}

			// This extra block is used to transfer from the entity's record to the target one
			// the row version column in case such is not kept in any regular member or column...
			if (VersionColumn.Name != null)
			{
				// Only if the target record requests the version column...
				var entry = record.Schema.Find(x => x.ColumnName == VersionColumn.Name);
				if (entry != null)
				{
					// And only if the entity has a record, and if it is not the target one...
					var meta = MetaEntity.Locate(entity);
					if (meta.Record != null && !object.ReferenceEquals(record, meta.Record))
					{
						int n = record.Schema.IndexOf(entry);
						record[VersionColumn.Name] = meta.Record[VersionColumn.Name];
					}
				}
			}
		}
		void IUberMap.WriteRecord(object entity, IRecord record)
		{
			this.WriteRecord((T)entity, record);
		}

		/// <summary>
		/// Loads into the entity the values obtained from the record.
		/// <para>Only the columns in the source record are taken into consideration.</para>
		/// </summary>
		/// <param name="record">The source record.</param>
		/// <param name="entity">The target entity.</param>
		internal void LoadEntity(IRecord record, T entity)
		{
			bool sensitive = Link.Engine.CaseSensitiveNames;
			for (int i = 0; i < record.Count; i++)
			{
				var entry = record.Schema[i];

				foreach (var col in Columns)
				{
					if (col.LoadEnabled &&
						string.Compare(col.Name, entry.ColumnName, !sensitive) == 0)
					{
						if (col.LoadEntity != null)
						{
							var value = record[i];
							col.LoadEntity(value, entity);
						}
						else if (col.ElementInfo != null && col.ElementInfo.CanWrite)
						{
							var value = record[i].ConvertTo(col.ElementInfo.ElementType);
							col.ElementInfo.SetValue(entity, value);
						}
					}
				}

				foreach (var member in Members)
				{
					foreach (var mcol in member.Columns)
					{
						if (mcol.LoadEnabled &&
							string.Compare(mcol.Name, entry.ColumnName, !sensitive) == 0)
						{
							if (mcol.LoadEntity != null)
							{
								var value = record[i];
								mcol.LoadEntity(value, entity);
							}
							else if (mcol.ElementInfo != null && mcol.ElementInfo.CanWrite)
							{
								var value = record[i].ConvertTo(mcol.ElementInfo.ElementType);
								mcol.ElementInfo.SetValue(entity, value);
							}
						}
					}
				}
			}
		}
		void IUberMap.LoadEntity(IRecord record, object entity)
		{
			this.LoadEntity(record, (T)entity);
		}

		/// <summary>
		/// Completes the members of the given meta-entity.
		/// <para>Only the non-lazy members are processed by this method.</para>
		/// <para>Lazy members are completed through their getters when invoked.</para>
		/// </summary>
		/// <param name="meta">The meta-entity whose members are to be completed.</param>
		internal void CompleteMembers(MetaEntity meta)
		{
			if (meta.Record == null) return;
			T entity = (T)meta.Entity; if (entity == null) return;

			if (meta.Completed) return;
			meta.Completed = true; // Signals, and avoid re-entrance...

			foreach (var member in _Members)
			{
				if (member.CompleteMember == null) continue; // Nothing to do...
				if (member.LazyProperty != null) continue; // Deferred to lazy getter...

				member.CompleteMember(meta.Record, entity);

				if (UberHelper.TrackChildEntities &&
					member.DependencyMode == MemberDependencyMode.Child &&
					member.ElementInfo.CanRead &&
					member.ElementInfo.ElementType.IsListAlike())
				{
					var type = member.ElementInfo.ElementType.ListAlikeMemberType();
					if (type != null && type.IsClass)
					{
						if (!meta.TrackedChilds.ContainsKey(member.Name))
							meta.TrackedChilds.Add(member.Name, new HashSet<object>());

						var childs = meta.TrackedChilds[member.Name];
						childs.Clear();

						var iter = member.ElementInfo.GetValue(entity) as IEnumerable;
						foreach (var item in iter) childs.Add(item);
					}
				}
			}
		}
		void IUberMap.CompleteMembers(MetaEntity meta)
		{
			this.CompleteMembers(meta);
		}

		/// <summary>
		/// Creates a new temporal record containing the identity columns only. Note that the
		/// schema of the new record is the same as the SchemaId reference of this map, and
		/// hence it should not be disposed.
		/// </summary>
		internal IRecord ExtractId(IRecord source)
		{
			var id = new Core.Concrete.Record(SchemaId); for (int i = 0; i < SchemaId.Count; i++)
			{
				var name = SchemaId[i].ColumnName;
				var entry = source.Schema.FindEntry(Table, name);
				if (entry == null) entry = source.Schema.FindEntry(name);
				if (entry == null) throw new NotFoundException(
					"Identity column '{0}' not found in '{1}'.".FormatWith(name, source));

				int index = source.Schema.IndexOf(entry);
				id[i] = source[index];
			}
			return id;
		}
		IRecord IUberMap.ExtractId(IRecord source)
		{
			return this.ExtractId(source);
		}

		/// <summary>
		/// The collection of entities maintained by this map.
		/// </summary>
		internal UberEntitySet UberEntities
		{
			get { return _UberEntities; }
		}
		UberEntitySet IUberMap.UberEntities
		{
			get { return this.UberEntities; }
		}

		/// <summary>
		/// Gets the collection of cached entities managed by this map, excluding the collected
		/// or invalid ones.
		/// </summary>
		public IEnumerable<IMetaEntity> MetaEntities
		{
			get
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());

				lock (UberEntities.SyncRoot)
				{
					foreach (var meta in UberEntities) if (meta.HasValidEntity) yield return meta;
				}
			}
		}

		/// <summary>
		/// Clears the cache of managed entities of this map, making them all to become detached
		/// ones.
		/// </summary>
		public void ClearEntities()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			lock (UberEntities.SyncRoot) { UberEntities.Clear(); }
		}

		/// <summary>
		/// Creates a new entity with the appropriate type for the requested map.
		/// <para>This method is invoked to generate instances that support virtual lazy
		/// properties when needed. Client applications can use but it is not needed.</para>
		/// </summary>
		/// <returns>A new entity.</returns>
		public T NewEntity()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			Validate();

			T obj = null; if (_ConstructorInfo != null) obj = (T)_ConstructorInfo.Invoke(null);
			else
			{
				var type = ProxyHolder != null ? ProxyHolder.ExtendedType : EntityType;
				obj = (T)FormatterServices.GetUninitializedObject(type);
			}

			return obj;
		}
		object IDataMap.NewEntity()
		{
			return this.NewEntity();
		}

		/// <summary>
		/// Attaches the given entity into this map.
		/// <para>Note that the framework supports attaching several entities that share the
		/// same identity columns, which are treated as a group for the relevant operations.</para>
		/// </summary>
		/// <param name="entity">The entity to attach into this instance.</param>
		public void Attach(T entity)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (entity == null) throw new ArgumentNullException("entity", "Entity cannot be null.");
			Validate();

			lock (UberEntities.SyncRoot)
			{
				var meta = MetaEntity.Locate(entity);
				if (object.ReferenceEquals(meta.Map, this)) return;
				if (meta.Map != null) throw new NotOrphanException(
					"Entity '{0}' cannot be attached to this map '{1}'."
					.FormatWith(meta, this));

				var record = new Core.Concrete.Record(Schema);
				WriteRecord(entity, record);
				meta.Record = record;
				meta.UberMap = this;
				UberEntities.Add(meta);
			}
		}
		void IDataMap.Attach(object entity)
		{
			this.Attach((T)entity);
		}

		/// <summary>
		/// Removes the given entity from this map, making it become a detached one. Returns true
		/// if the entity has been removed, or false otherwise.
		/// </summary>
		/// <param name="entity">The entity to detach from this instance.</param>
		/// <returns>True if the instance has been removed, false otherwise.</returns>
		public bool Detach(T entity)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (entity == null) throw new ArgumentNullException("entity", "Entity cannot be null.");
			Validate();

			bool r = false; lock (UberEntities.SyncRoot)
			{
				var meta = MetaEntity.Locate(entity);
				r = UberEntities.Remove(meta);
				if (r) meta.Reset();
			}
			return r;
		}
		bool IDataMap.Detach(object entity)
		{
			return this.Detach((T)entity);
		}

		/// <summary>
		/// Creates a new query command for the entities managed by this map.
		/// </summary>
		/// <returns>A new query command.</returns>
		public DataQuery<T> Query()
		{
			return new DataQuery<T>(this);
		}
		IDataQuery<T> IDataMap<T>.Query()
		{
			return this.Query();
		}
		IDataQuery IDataMap.Query()
		{
			return this.Query();
		}

		/// <summary>
		/// Creates a new query command for the entities managed by this map, and sets the initial
		/// contents of its WHERE clause.
		/// </summary>
		/// <param name="where">The dynamic lambda expression that resolves into the contents of
		/// this clause.
		/// <returns>A new query command.</returns>
		public DataQuery<T> Where(Func<dynamic, object> where)
		{
			return this.Query().Where(where);
		}
		IDataQuery<T> IDataMap<T>.Where(Func<dynamic, object> where)
		{
			return this.Where(where);
		}
		IDataQuery IDataMap.Where(Func<dynamic, object> where)
		{
			return this.Where(where);
		}

		/// <summary>
		/// Finds and returns inmediately a suitable entity that meets the conditions given, by
		/// looking for it in the managed cache and, if it cannot be found there, querying the
		/// database for it. Returns null if such entity cannot be found neither in the cache
		/// nor in the database.
		/// </summary>
		/// <param name="specs">A collection of dynamic lambda expressions each containing the
		/// name and value to find for a column, as in: 'x => x.Column == Value'.</param>
		/// <returns>The requested entity, or null.</returns>
		public T FindNow(params Func<dynamic, object>[] specs)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (specs == null) throw new ArgumentNullException("specs", "Specifications array cannot be null.");
			Validate();

			var rec = Record.Create(specs);
			var nodes = UberEntities.NodeList(rec); if (nodes != null)
			{
				var meta = nodes.Where(x => x.HasValidEntity).FirstOrDefault();
				if (meta != null)
				{
					rec.Dispose(disposeSchema: true);
					return (T)meta.Entity;
				}
			}

			var cmd = Query().Top(1);
			var tag = new DynamicNode.Argument("x");

			for (int i = 0; i < rec.Count; i++)
			{
				var left = new DynamicNode.GetMember(tag, rec.Schema[i].ColumnName);
				var bin = new DynamicNode.Binary(left, ExpressionType.Equal, rec[i]);
				cmd.Where(x => bin);
				bin.Dispose();
				left.Dispose();
			}
			tag.Dispose();
			rec.Dispose(disposeSchema: true);

			T obj = cmd.First();
			return obj;
		}
		object IDataMap.FindNow(params Func<dynamic, object>[] specs)
		{
			return this.FindNow(specs);
		}

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
		public T RefreshNow(T entity)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (entity == null) throw new ArgumentNullException("entity", "Entity cannot be null.");
			Validate();

			var meta = MetaEntity.Locate(entity);
			if (meta.UberMap == null) Attach(entity);

			// Old identity...
			var nodes = UberEntities.NodeList(meta.Record);
			if (nodes != null) foreach (var temp in nodes) temp.Completed = false;

			// New identity (eventually)...
			var rec = new Core.Concrete.Record(Schema); WriteRecord(entity, rec);
			var id = ExtractId(rec);
			nodes = UberEntities.NodeList(id);
			if (nodes != null) foreach (var temp in nodes) temp.Completed = false;

			// Querying the database...
			var cmd = Query().Top(1);
			var tag = new DynamicNode.Argument("x");

			for (int i = 0; i < id.Count; i++)
			{
				var left = new DynamicNode.GetMember(tag, id.Schema[i].ColumnName);
				var bin = new DynamicNode.Binary(left, ExpressionType.Equal, id[i]);
				cmd.Where(x => bin);
				bin.Dispose();
				left.Dispose();
			}
			id.Dispose();
			rec.Dispose();
			tag.Dispose();

			T obj = cmd.First(); cmd.Dispose();

			if (obj == null) Detach(entity);
			return obj;
		}
		object IDataMap.RefreshNow(object entity)
		{
			return this.RefreshNow((T)entity);
		}

		/// <summary>
		/// Creates a new insert operation for the given entity.
		/// <para>The new command must be firstly submitted into the associated repository in
		/// order it to be executed when all pending change operations annotated into that
		/// repository are executed as a group.</para>
		/// </summary>
		/// <param name="entity">The entity to be inserted.</param>
		/// <returns>A new command.</returns>
		public DataInsert<T> Insert(T entity)
		{
			return new DataInsert<T>(this, entity);
		}
		IDataInsert<T> IDataMap<T>.Insert(T entity)
		{
			return this.Insert(entity);
		}
		IDataInsert IDataMap.Insert(object entity)
		{
			return this.Insert((T)entity);
		}

		/// <summary>
		/// Creates a new delete operation for the given entity.
		/// <para>The new command must be firstly submitted into the associated repository in
		/// order it to be executed when all pending change operations annotated into that
		/// repository are executed as a group.</para>
		/// </summary>
		/// <param name="entity">The entity to be inserted.</param>
		/// <returns>A new command.</returns>
		public DataDelete<T> Delete(T entity)
		{
			return new DataDelete<T>(this, entity);
		}
		IDataDelete<T> IDataMap<T>.Delete(T entity)
		{
			return this.Delete(entity);
		}
		IDataDelete IDataMap.Delete(object entity)
		{
			return this.Delete((T)entity);
		}

		/// <summary>
		/// Creates a new delete operation for the given entity.
		/// <para>The new command must be firstly submitted into the associated repository in
		/// order it to be executed when all pending change operations annotated into that
		/// repository are executed as a group.</para>
		/// </summary>
		/// <param name="entity">The entity to be inserted.</param>
		/// <returns>A new command.</returns>
		public DataUpdate<T> Update(T entity)
		{
			return new DataUpdate<T>(this, entity);
		}
		IDataUpdate<T> IDataMap<T>.Update(T entity)
		{
			return this.Update(entity);
		}
		IDataUpdate IDataMap.Update(object entity)
		{
			return this.Update((T)entity);
		}
	}
}
// ======================================================== 
