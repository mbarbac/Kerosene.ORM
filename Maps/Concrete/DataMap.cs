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
		/// <para>Only the non-lazy members are processed by this method, as they are processed
		/// by their overriden getters when needed.</para>
		/// </summary>
		/// <param name="meta">The meta-entity whose members are to be completed.</param>
		void CompleteMembers(MetaEntity meta);

		/// <summary>
		/// Creates a new temporal record, associated with the ID schema, whose contents are
		/// loaded from the source record given. Returns null if the source record contains not
		/// all the id columns, or if there are any inconsistencies.
		/// </summary>
		IRecord ExtractId(IRecord source);

		/// <summary>
		/// The collection of entities managed by this map, if it is tracking entities.
		/// </summary>
		UberEntitySet UberEntities { get; }

		/// <summary>
		/// Collects and removes the invalid entities in the cache of this map, if any.
		/// </summary>
		void CollectInvalidEntities();

		/// <summary>
		/// Whether to track or not the child entities of the members defined.
		/// </summary>
		bool TrackChildEntities { get; set; }
	}

	// ==================================================== 
	/// <summary>
	/// Represents a map between the type of the entities it is associated with and their
	/// database representation.
	/// </summary>
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
		bool _TrackEntities = UberHelper.TrackEntities;
		bool _TrackChildEntities = UberHelper.TrackChildEntities;
		UberEntitySet _UberEntities = new UberEntitySet();

		string _Table = null;
		bool _IsWeakMap = false;
		Func<dynamic, object> _Discriminator = null;
		MapDiscoveryMode _DiscoveryMode = MapDiscoveryMode.Auto;
		MapMemberCollection<T> _Members = null;
		MapColumnCollection<T> _Columns = null;
		MapVersionColumn<T> _VersionColumn = null;

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
				var temp = repo.UberMaps.FindByType(EntityType);
				if (temp != null)
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
				_TrackEntities = repo.TrackEntities;
				_SerialId = ++UberHelper.DataMapLastSerial;

				_Members = new MapMemberCollection<T>(this);
				_Columns = new MapColumnCollection<T>(this);
				_VersionColumn = new MapVersionColumn<T>(this);
			}
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
		/// Initializes a new instance.
		/// </summary>
		/// <param name="repo">The repository this map will be registered into.</param>
		/// <param name="table">The name of the primary table for this map.</param>
		/// <param name="weak">Whether this map shall be considered a weak map or not.</param>
		public DataMap(IDataRepository repo, string table, bool weak = false)
		{
			if (repo == null) throw new ArgumentNullException("repo", "Repository cannot be null.");
			var temp = repo as DataRepository;
			if (temp == null) throw new InvalidCastException(
				"Repository '{0}' is not a valid 'DataRepository' instance.".FormatWith(repo.Sketch()));

			OnInitialize(temp, table, weak);
		}

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
		/// <param name="table">A dynamic lambda expression that resolves into the name of the
		/// primary table for this map.</param>
		/// <param name="weak">Whether this map shall be considered a weak map or not.</param>
		public DataMap(IDataRepository repo, Func<dynamic, object> table, bool weak = false)
		{
			if (repo == null) throw new ArgumentNullException("repo", "Repository cannot be null.");
			var temp = repo as DataRepository;
			if (temp == null) throw new InvalidCastException(
				"Repository '{0}' is not a valid 'DataRepository' instance.".FormatWith(repo.Sketch()));

			if (table == null) throw new ArgumentNullException("table", "Table specification cannot be null.");
			var name = DynamicInfo.ParseName(table);

			OnInitialize(temp, name, weak);
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
				if (_UberEntities != null)
				{
					lock (_UberEntities.SyncRoot)
					{
						foreach (var meta in _UberEntities) meta.Clear();
						_UberEntities.Clear();
					}
				}

				if (_Repository != null && !_Repository.IsDisposed)
				{
					_Repository.DiscardChanges();

					lock (_Repository.UberMaps.SyncRoot)
					{
						_Repository.UberMaps.Remove(this);
					}
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
			var str = string.Format("{0}:{1}[{2}{3} => {4}{5}({6})]",
				SerialId,
				GetType().EasyName(),
				EntityType.EasyName(),
				ProxyHolder == null ? string.Empty : ":Proxy",
				IsWeakMap ? "Weak:" : string.Empty,
				Table ?? string.Empty,
				Repository.Sketch());

			return IsDisposed ? "disposed::{0}".FormatWith(str) : str;
		}

		/// <summary>
		/// Returns a new map that is associated with the repository given, and that contains
		/// a copy of the structure and rules of the original one. Maps created using this
		/// method are not considered weak ones.
		/// </summary>
		/// <param name="repo">The repository the new map will be associated with.</param>
		/// <returns>A new map.</returns>
		public DataMap<T> Clone(DataRepository repo)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (repo == null) throw new ArgumentNullException("repo", "Repository cannot be null.");
			if (repo.IsDisposed) throw new ObjectDisposedException(repo.ToString());

			var cloned = new DataMap<T>(repo, Table, IsWeakMap);
			OnClone(cloned); return cloned;
		}
		public DataMap<T> Clone(IDataRepository repo)
		{
			if (repo == null) throw new ArgumentNullException("repo", "Repository cannot be null");
			var temp = repo as DataRepository; if (temp == null)
				throw new InvalidCastException(
				"Repository '{0}' is not a valid 'DataRepository' instance.".FormatWith(repo.Sketch()));

			return this.Clone(temp);
		}
		IDataMap<T> IDataMap<T>.Clone(IDataRepository repo)
		{
			if (repo == null) throw new ArgumentNullException("repo", "Repository cannot be null");
			var temp = repo as DataRepository; if (temp == null)
				throw new InvalidCastException(
				"Repository '{0}' is not a valid 'DataRepository' instance.".FormatWith(repo.Sketch()));

			return this.Clone(temp);
		}
		IDataMap IDataMap.Clone(IDataRepository repo)
		{
			if (repo == null) throw new ArgumentNullException("repo", "Repository cannot be null");
			var temp = repo as DataRepository; if (temp == null)
				throw new InvalidCastException(
				"Repository '{0}' is not a valid 'DataRepository' instance.".FormatWith(repo.Sketch()));

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

			// _TrackEntities / _TrackChildEntities: not cloned, values inherited from the
			// state of the repo where the map is being cloned...

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
		/// Whether this map is considered a weak map or not.
		/// <para>Weak maps are created automatically when an entity type is referenced by any
		/// map operation and there was no registered map for that type. Weak maps are disposed
		/// if a regular non-weak map is registered (created) explicitly.</para>
		/// </summary>
		public bool IsWeakMap
		{
			get { return _IsWeakMap; }
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
		/// The name of the primary table in the underlying database the entities managed by
		/// this map are associated with.
		/// </summary>
		public string Table
		{
			get { return _Table; }
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
		/// How the map will discover the columns in the database that will be associated with
		/// the type.
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
		/// The collection of members in the type that have been explicitly associated with
		/// the map.
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
		/// The collection of columns in the primary table that have been explicitly associated
		/// with the map.
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
		/// If not empty represents the column in the primary table that will be used for row
		/// version control purposes.
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

			var selects = Validate_GenerateSelects();
			var schema = Validate_GetPreliminarySchema(selects);
			Validate_DiscardEntriesAndAddColumns(schema);
			Validate_NormalizeSchemaNames(schema);
			Validate_VerifyMapDefinitions(schema);
			Validate_CaptureWorkingSchemas(schema);
			Validate_GenerateProxyHolder();
			Validate_CaptureEntityConstructor();

			_IsValidated = true;
		}
		private List<string> Validate_GenerateSelects()
		{
			bool sensitive = Repository.Link.Engine.CaseSensitiveNames;
			List<string> selects = new List<string>();
			string str = null;

			// If mode is auto the list will be empty to select all columns. If mode is explicit
			// then we will use only the columns that have been defined explicitly...
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

				// In explicit mode having an empty selects' list is obvioulsy an error...
				if (selects.Count == 0) throw new EmptyException(
					"No candidate columns defined in this map '{0}' for Explicit Discovery Mode"
					.FormatWith(this));
			}

			return selects;
		}
		private ISchema Validate_GetPreliminarySchema(List<string> selects)
		{
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

			return schema;
		}
		private void Validate_DiscardEntriesAndAddColumns(ISchema schema)
		{
			bool sensitive = Repository.Link.Engine.CaseSensitiveNames;
			List<ISchemaEntry> entries = new List<ISchemaEntry>(schema);
			foreach (var entry in entries)
			{
				// See if column is excluded or has been defined...
				var col = Columns.FirstOrDefault<MapColumn<T>>(x => string.Compare(x.Name, entry.ColumnName, !sensitive) == 0);
				if (col != null)
				{
					if (col.Excluded) schema.Remove(entry);
					continue;
				}

				// See if column is defined as the row version control one...
				if (string.Compare(VersionColumn.Name, entry.ColumnName, !sensitive) == 0)
					continue;

				// See if column is defined for a member...
				bool found = false;
				foreach (var member in Members)
					foreach (var mcol in member.Columns)
						if (string.Compare(mcol.Name, entry.ColumnName, !sensitive) == 0) found = true;
				if (found) continue;

				// Discarding entry if explicit mode is set (we have not found it defined...)
				if (DiscoveryMode == MapDiscoveryMode.Explicit)
				{
					schema.Remove(entry);
					continue;
				}

				// But in mode Auto we will add a new column instance to keep track of it...
				col = Columns.Add(x => entry.ColumnName);
				col.AutoDiscovered = true;
			}

			if (schema.Count == 0) throw new InvalidOperationException(
				"Schema is empty after removing columns for map '{0}'.".FormatWith(this));

			entries.Clear();
			entries = null;
		}
		private void Validate_NormalizeSchemaNames(ISchema schema)
		{
			List<ISchemaEntry> entries = new List<ISchemaEntry>(schema);

			foreach (var entry in entries) schema.Remove(entry);
			foreach (var entry in entries)
			{
				// We need not to keep track of table's name, and in any case it would make harder
				// other future processess...
				entry.TableName = null;
				schema.Add(entry);
			}

			entries.Clear();
			entries = null;
		}
		private void Validate_VerifyMapDefinitions(ISchema schema)
		{
			ISchemaEntry temp = null;

			if (VersionColumn.Name != null)
			{
				// Can use FindEntry as columns should have unique names in the primary table...
				temp = schema.FindEntry(VersionColumn.Name, raise: true);
				if (temp == null) throw new NotFoundException(
					"Row version column '{0}' not found in the generated schema."
					.FormatWith(VersionColumn.Name));
			}

			foreach (var col in Columns)
			{
				if (col.Excluded) continue;
				if (col.AutoDiscovered) continue;

				// Can use FindEntry as columns should have unique names in the primary table...
				temp = schema.FindEntry(col.Name, raise: true);
				if (temp == null) throw new NotFoundException(
					"Column '{0}' not found in the generated schema."
					.FormatWith(col.Name));
			}

			foreach (var member in Members)
			{
				foreach (var mcol in member.Columns)
				{
					// Can use FindEntry as columns should have unique names in primary table...
					temp = schema.FindEntry(mcol.Name, raise: true);
					if (temp == null) throw new NotFoundException(
						"Column '{0}' for member '{1}' not found in the generated schema."
						.FormatWith(mcol.Name, member.Name));
				}
			}
		}
		private void Validate_CaptureWorkingSchemas(ISchema schema)
		{
			_Schema = schema;
			if (_Schema.Count == 0) throw new EmptyException(
				"Generated schema is empty for map '{0}'.".FormatWith(this));

			_SchemaId = _Schema.Clone();
			_SchemaId.Clear();
			_SchemaId.AddRange(_Schema.PrimaryKeyColumns(), cloneNotOrphans: true);
			if (_SchemaId.Count == 0) _SchemaId.AddRange(_Schema.UniqueValuedColumns(), cloneNotOrphans: true);
			if (_SchemaId.Count == 0) throw new EmptyException(
				"Generated schema '{0}' does not contain identity columns for map '{1}'."
				.FormatWith(_Schema, this));
		}
		private void Validate_GenerateProxyHolder()
		{
			_ProxyHolder = ProxyGenerator.Locate(this);
		}
		private void Validate_CaptureEntityConstructor()
		{
			var type = ProxyHolder != null ? ProxyHolder.ExtendedType : EntityType;
			var cons = type.GetConstructors(TypeEx.InstancePublicAndHidden);
			foreach (var con in cons)
			{
				var pars = con.GetParameters();
				if (pars.Length == 0) { _ConstructorInfo = con; break; }
			}
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

			// Processing the record's entries...
			for (int i = 0; i < record.Count; i++)
			{
				var entry = record.Schema[i];

				// Rules set for columns...
				var col = Columns.Find(x => string.Compare(x.Name, entry.ColumnName, !sensitive) == 0);
				if (col != null && col.WriteEnabled)
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

				// Rules set for members...
				foreach (var member in Members)
				{
					var mcol = member.Columns.Find(x => string.Compare(x.Name, entry.ColumnName, !sensitive) == 0);
					if (mcol != null && mcol.WriteEnabled)
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

			// Transfering the value for the row version column from the metadata record to
			// the external record, if needed - this is a way to emulate a storage for that
			// value even if there is no member in the type suitable to hold it...
			if (VersionColumn.Name != null)
			{
				try
				{
					var meta = MetaEntity.Locate(entity);
					var cache = meta.Record.Schema.FindEntry(VersionColumn.Name, raise: false); if (cache == null) return;
					var entry = record.Schema.FindEntry(VersionColumn.Name, raise: false); if (entry == null) return;

					int n = meta.Record.Schema.IndexOf(cache);
					int k = record.Schema.IndexOf(entry);
					record[k] = meta.Record[n];
				}
				catch { }
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

			// Processing the record's entries...
			for (int i = 0; i < record.Count; i++)
			{
				var entry = record.Schema[i];

				// Rules set for columns...
				var col = Columns.Find(x => string.Compare(x.Name, entry.ColumnName, !sensitive) == 0);
				if (col != null && col.LoadEnabled)
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

				// Rules set for members...
				foreach (var member in Members)
				{
					var mcol = member.Columns.Find(x => string.Compare(x.Name, entry.ColumnName, !sensitive) == 0);
					if (mcol != null && mcol.LoadEnabled)
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

			// There are no mechanisms to load into the entity the value of the row version
			// column. Actually, it may happen that there is no member suitable to hold its
			// value.... so we do nothing.
		}
		void IUberMap.LoadEntity(IRecord record, object entity)
		{
			this.LoadEntity(record, (T)entity);
		}

		/// <summary>
		/// Completes the members of the given meta-entity.
		/// <para>Only the non-lazy members are processed by this method, as they are processed
		/// by their overriden getters when needed.</para>
		/// </summary>
		/// <param name="meta">The meta-entity whose members are to be completed.</param>
		internal void CompleteMembers(MetaEntity meta)
		{
			T entity = (T)meta.Entity;
			if (entity == null) return;
			if (meta.Record == null) return;
			if (meta.Completed) return;

			meta.Completed = true; foreach (var member in Members)
			{
				if (member.CompleteMember == null) continue; // Nothing to do...
				if (member.LazyProperty != null) continue; // Deferred to lazy loading...

				member.CompleteMember(meta.Record, entity);

				if (UberHelper.TrackChildEntities &&
					member.DependencyMode == MemberDependencyMode.Child &&
					member.ElementInfo.CanRead &&
					member.ElementInfo.ElementType.IsListAlike())
				{
					var type = member.ElementInfo.ElementType.ListAlikeMemberType();
					if (type != null && type.IsClass)
					{
						if (!meta.ChildDependencies.ContainsKey(member.Name))
							meta.ChildDependencies.Add(member.Name, new HashSet<object>());

						var childs = meta.ChildDependencies[member.Name];
						childs.Clear();

						var iter = member.ElementInfo.GetValue(entity) as IEnumerable;
						foreach (var obj in iter) childs.Add(obj);
					}
				}
			}
		}
		void IUberMap.CompleteMembers(MetaEntity meta)
		{
			this.CompleteMembers(meta);
		}

		/// <summary>
		/// Whether this instance keeps track of the entities it has managed in its internal
		/// cache, or not.
		/// </summary>
		public bool TrackEntities
		{
			get { return _TrackEntities; }
			set
			{
				if (IsDisposed) return; // Just return, there is no need to throw an exception...
				_TrackEntities = value;
				if (value == false) ClearEntities(detach: false); // False to avoid losing their state...
			}
		}

		/// <summary>
		/// Whether to track or not the child entities of the members defined.
		/// </summary>
		public bool TrackChildEntities
		{
			get { return _TrackChildEntities; }
			set { _TrackChildEntities = value; }
		}

		/// <summary>
		/// The collection of entities managed by this map, if it is tracking entities.
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
		/// Collects and removes the invalid entities in the cache of this map, if any.
		/// </summary>
		internal void CollectInvalidEntities()
		{
			lock (_UberEntities.SyncRoot)
			{
				var list = _UberEntities.Where(x => !x.HasValidEntity).ToArray();
				foreach (var meta in list)
				{
					// There is no need to check the repo's operations as these keep a hard
					// reference to the entity, and so the GC should not collect those that
					// are annotated there...

					DebugEx.IndentWriteLine("\n- Collecting '{0}'...", meta);
					UberEntities.Remove(meta);
					meta.Clear();
					DebugEx.Unindent();
				}
			}
		}
		void IUberMap.CollectInvalidEntities()
		{
			this.CollectInvalidEntities();
		}

		/// <summary>
		/// The current collection of entities in a valid state tracked by this map, if any.
		/// </summary>
		public IEnumerable<IMetaEntity> Entities
		{
			get
			{
				if (!IsDisposed) CollectInvalidEntities();
				return UberEntities;
			}
		}

		/// <summary>
		/// Clears the cache of tracked entities maintained by this map and optionally, detaches
		/// those entities.
		/// </summary>
		/// <param name="detach">True to forcibly detach the entities found in the cache.</param>
		public void ClearEntities(bool detach = true)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			lock (_UberEntities.SyncRoot)
			{
				if (detach)
				{
					// If any entity has a pending operation we need to discard them all as we
					// have no way to control all possible side effects...

					bool discard = false;
					foreach (var meta in _UberEntities)
						if (_Repository.UberOperations.IndexOf(meta) >= 0) { discard = true; break; }

					if (discard) _Repository.DiscardChanges();

					foreach (var meta in _UberEntities) meta.Clear();
				}
				_UberEntities.Clear();
			}
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
		/// </summary>
		/// <param name="entity">The entity to attach into this instance.</param>
		public void Attach(T entity)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (entity == null) throw new ArgumentNullException("entity", "Entity cannot be null.");
			Validate();

			lock (_UberEntities.SyncRoot)
			{
				var meta = MetaEntity.Locate(entity);
				if (object.ReferenceEquals(meta.Map, this)) return;
				if (meta.Map != null)
					throw new NotOrphanException("Entity '{0}' cannot be attached to this map '{1}'."
					.FormatWith(meta, this));

				var record = new Core.Concrete.Record(Schema);
				WriteRecord(entity, record);
				meta.Record = record;
				meta.UberMap = this;

				if (TrackEntities) UberEntities.Add(meta);
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

			bool r = false; lock (_UberEntities.SyncRoot)
			{
				var meta = MetaEntity.Locate(entity);
				if (object.ReferenceEquals(meta.Map, this))
				{
					r = _UberEntities.Remove(meta); if (r || !TrackEntities) // !TrackEntities: entity has not to be on the cache...
					{
						// If there is a pending operation on the entity we need to discard them all
						// as we have no way to control all possible side effects...
						bool discard = _Repository.UberOperations.IndexOf(meta) >= 0;
						if (discard) _Repository.DiscardChanges();

						meta.Clear();
						r = true; // To cover the case when (!TrackEntities)...
					}
				}
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
		/// Returns true if the given record contains, potentially among others, all the identity
		/// columns found for this map when it was validated.
		/// </summary>
		bool HasIdColumns(IRecord record)
		{
			if (record == null) return false;
			if (record.Schema == null) return false;

			foreach (var entry in SchemaId)
			{
				var temp = record.Schema.FindEntry(entry.ColumnName, raise: false);
				if (temp == null) return false;
			}
			return true;
		}

		/// <summary>
		/// Returns true if the given record contains only the identity columns found for this
		/// map when it was validated.
		/// </summary>
		bool HasIdColumnsOnly(IRecord record)
		{
			if (!HasIdColumns(record)) return false;

			foreach (var entry in record.Schema)
			{
				var temp = SchemaId.FindEntry(entry.ColumnName, raise: false);
				if (temp == null) return false;
			}
			return true;
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

			// The specification in a record form...
			var record = Record.Create(specs);

			// Let's see if we can find it in the cache...
			if (TrackEntities)
			{
				// If the record is an id spec we can go fast...
				if (HasIdColumnsOnly(record))
				{
					lock (UberEntities.SyncRoot)
					{
						var node = UberEntities.GetNode(record); if (node != null)
						{
							var meta = node.Find(x => x.HasValidEntity);
							if (meta != null)
							{
								record.Dispose(disposeSchema: true);
								return (T)meta.Entity;
							}
						}
					}
				}

				// The default scenario is to iterate through the cache...
				lock (UberEntities.SyncRoot)
				{
					foreach (var meta in UberEntities)
					{
						if (!meta.HasValidEntity) continue;
						if (meta.Record == null) continue;

						var changes = record.Changes(meta.Record); if (changes == null)
						{
							record.Dispose(disposeSchema: true);
							return (T)meta.Entity;
						}
						changes.Dispose(disposeSchema: true);
					}
				}
			}

			// If not found in the cache, or cache with no tracked entities...
			var cmd = Query().Top(1);
			var tag = new DynamicNode.Argument("x");

			for (int i = 0; i < record.Count; i++)
			{
				var left = new DynamicNode.GetMember(tag, record.Schema[i].ColumnName);
				var bin = new DynamicNode.Binary(left, ExpressionType.Equal, record[i]);
				cmd.Where(x => bin);
				bin.Dispose();
				left.Dispose();
			}
			tag.Dispose();
			record.Dispose(disposeSchema: true);

			T obj = cmd.First();
			return obj;
		}
		object IDataMap.FindNow(params Func<dynamic, object>[] specs)
		{
			return this.FindNow(specs);
		}

		/// <summary>
		/// Creates a new temporal record, associated with the ID schema, whose contents are
		/// loaded from the source record given. Returns null if the source record contains not
		/// all the id columns, or if there are any inconsistencies.
		/// </summary>
		internal IRecord ExtractId(IRecord source)
		{
			var id = new Core.Concrete.Record(SchemaId); for (int i = 0; i < SchemaId.Count; i++)
			{
				var name = SchemaId[i].ColumnName;
				var entry = source.Schema.FindEntry(name, raise: false);
				if (entry == null)
				{
					return null;
				}

				int ix = source.Schema.IndexOf(entry);
				id[i] = source[ix];
			}
			return id;
		}
		IRecord IUberMap.ExtractId(IRecord source)
		{
			return this.ExtractId(source);
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

			// We will always work with attached entities (only fails if it was already attached
			// to other map, and in this case we ar happy it to fail)...
			Attach(entity);

			// Some common data...
			var meta = MetaEntity.Locate(entity);
			var rec = new Core.Concrete.Record(Schema); WriteRecord(entity, rec);
			var id = ExtractId(rec);
			if (id == null) throw new
				InvalidOperationException("Cannot obtain identity from entity '{0}'".FormatWith(meta));

			// Prepare to refresh everything in the cache, including the captured childs...
			lock (UberEntities.SyncRoot)
			{
				// Old identity...
				var node = UberEntities.GetNode(meta.Record);
				if (node != null) foreach (var temp in node) temp.Completed = false;

				// New identity (eventually)...
				node = id == null ? null : UberEntities.GetNode(id);
				if (node != null) foreach (var temp in node) temp.Completed = false;
			}

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

			// If not found in the database we¡ll force it to become into a detached state...
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
