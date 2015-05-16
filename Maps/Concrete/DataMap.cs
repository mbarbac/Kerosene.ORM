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
using System.Threading;

namespace Kerosene.ORM.Maps.Concrete
{
	// ====================================================
	internal interface IUberMap : IDataMap
	{
		/// <summary>
		/// The repository this map is registered into.
		/// </summary>
		new DataRepository Repository { get; }

		/// <summary>
		/// The object that can be used to synchronize operations on this map.
		/// </summary>
		object MasterLock { get; }

		/// <summary>
		/// The link reference held by the associated repository, if any.
		/// </summary>
		IDataLink Link { get; }

		/// <summary>
		/// Wheter this map is considered a weak one or not.
		/// </summary>
		new bool IsWeakMap { get; set; }

		/// <summary>
		/// Whether tracking of child entities for dependency properties is enabled for this
		/// map or not.
		/// </summary>
		bool TrackChildEntities { get; set; }

		/// <summary>
		/// The collection of members of the type for which either a dependency and/or a
		/// completion method has been defined.
		/// </summary>
		new IUberMemberCollection Members { get; }

		/// <summary>
		/// The collection of columns in the database associated with this map.
		/// </summary>
		new IUberColumnCollection Columns { get; }

		/// <summary>
		/// Represents the column to be used for row version control, if any.
		/// </summary>
		new IUberVersionColumn VersionColumn { get; }

		/// <summary>
		/// The proxy holder created for the type of the entities managed by this instance,
		/// or null.
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
		/// The collection of entities in this map.
		/// </summary>
		MetaEntityCollection MetaEntities { get; }

		/// <summary>
		/// Collects and removes the invalid entities in the cache of this map, if any.
		/// </summary>
		void CollectInvalidEntities();

		/// <summary>
		/// Writes into the target record the contents from the source entity. Only the existing
		/// columns in the target record are taken into consideration.
		/// </summary>
		void WriteRecord(object entity, IRecord record);

		/// <summary>
		/// Loads into the target entity the contents from the source record. Only the existing
		/// columns in the source record are taken into consideration.
		/// </summary>
		void LoadEntity(IRecord record, object entity);

		/// <summary>
		/// Completes the members of the given meta-entity. Only the non-lazy members are
		/// processed by this method, as they are processed by their overriden getters when
		/// needed.
		/// </summary>
		void CompleteMembers(MetaEntity meta);

		/// <summary>
		/// Creates a new temporal record, associated with the ID schema, whose contents are
		/// loaded from the source record given. Returns null if the source record contains not
		/// all the id columns, or if there are any inconsistencies.
		/// </summary>
		IRecord ExtractId(IRecord source);

		/// <summary>
		/// Removes the given entity from this map, making it become a detached one. Returns true
		/// if the entity has been removed, or false otherwise.
		/// </summary>
		bool Detach(MetaEntity meta);
	}

	// ====================================================
	/// <summary>
	/// Represents a map between entities of a POCO class and a primary table in an underlying
	/// database-alike service.
	/// </summary>
	public class DataMap<T> : IDataMap<T>, IUberMap where T : class
	{
		bool _IsDisposed = false;
		ulong _SerialId = 0;
		DataRepository _Repository = null;
		object _MasterLock = null;
		bool _IsWeakMap = false;
		string _Table = null;
		Func<dynamic, object> _Discriminator = null;
		MapDiscoveryMode _DiscoveryMode = MapDiscoveryMode.Auto;
		internal MapMemberCollection<T> _Members = null;
		internal MapColumnCollection<T> _Columns = null;
		internal MapVersionColumn<T> _VersionColumn = null;
		bool _TrackEntities = Uber.TrackEntities;
		bool _TrackChildEntities = Uber.TrackChildEntities;
		bool _IsValidated = false;
		ProxyHolder _ProxyHolder = null;
		ISchema _Schema = null;
		ISchema _SchemaId = null;
		ConstructorInfo _Constructor = null;
		MetaEntityCollection _MetaEntities = new MetaEntityCollection();

		/// <summary>
		/// Invoked when initializing this instance.
		/// </summary>
		protected virtual void OnInitialize(DataRepository repo, string table)
		{
			if (repo == null) throw new ArgumentNullException("repo", "Repository cannot be null.");
			if (repo.IsDisposed) throw new ObjectDisposedException(repo.ToString());

			if (!EntityType.IsClass && !EntityType.IsInterface) throw new InvalidOperationException(
				"Type '{0}' is not a class or an interface."
				.FormatWith(EntityType.EasyName()));

			lock (repo.MasterLock)
			{
				var temp = repo.UberMaps.Find(EntityType); if (temp != null)
				{
					if (temp.IsWeakMap) temp.Dispose();
					else throw new DuplicateException(
						"A map for type '{0}' is already registered in '{1}'."
						.FormatWith(EntityType.EasyName(), repo));
				}

				_SerialId = ++Uber.DataMapLastSerial;
				_Repository = repo;
				_MasterLock = repo.MasterLock;
				_TrackEntities = _Repository.TrackEntities;
				Table = table;

				_Members = new MapMemberCollection<T>(this);
				_Columns = new MapColumnCollection<T>(this);
				_VersionColumn = new MapVersionColumn<T>(this);
				
				repo.UberMaps.Add(this);
			}
		}

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="repo">The repository where this map will be registered into.</param>
		/// <param name="table">The name of the primary table for this map.</param>
		public DataMap(DataRepository repo, string table)
		{
			OnInitialize(repo, table);
		}

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="repo">The repository where this map will be registered into.</param>
		/// <param name="table">A dynamic lambda expression that resolves into the name of the
		/// primary table for this map.</param>
		public DataMap(DataRepository repo, Func<dynamic, object> table)
		{
			if (table == null) throw new ArgumentNullException("table", "Table specification cannot be null.");
			var name = DynamicInfo.ParseName(table);

			OnInitialize(repo, name);
		}

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="repo">The repository where this map will be registered into.</param>
		/// <param name="table">The name of the primary table for this map.</param>
		public DataMap(IDataRepository repo, string table)
		{
			if (repo == null) throw new ArgumentNullException("repo", "Repository cannot be null.");
			var temp = repo as DataRepository;
			if (temp == null) throw new InvalidCastException(
				"Repository '{0}' is not a valid '{1}' instance."
				.FormatWith(repo.Sketch(), typeof(DataRepository).EasyName()));

			OnInitialize(temp, table);
		}

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="repo">The repository where this map will be registered into.</param>
		/// <param name="table">A dynamic lambda expression that resolves into the name of the
		/// primary table for this map.</param>
		public DataMap(IDataRepository repo, Func<dynamic, object> table)
		{
			if (repo == null) throw new ArgumentNullException("repo", "Repository cannot be null.");
			var temp = repo as DataRepository;
			if (temp == null) throw new InvalidCastException(
				"Repository '{0}' is not a valid '{1}' instance."
				.FormatWith(repo.Sketch(), typeof(DataRepository).EasyName()));

			if (table == null) throw new ArgumentNullException("table", "Table specification cannot be null.");
			var name = DynamicInfo.ParseName(table);

			OnInitialize(temp, name);
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

		/// <summary></summary>
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
				if (_MasterLock != null) Monitor.Enter(_MasterLock);

				if (_MetaEntities != null) ClearEntities(detach: true);
				if (_Repository != null && !_Repository.IsDisposed) _Repository.UberMaps.Remove(this);
				if (_Members != null) _Members.OnDispose();
				if (_Columns != null) _Columns.OnDispose();
				if (_VersionColumn != null) _VersionColumn.OnDispose();

				if (_MasterLock != null) Monitor.Exit(_MasterLock);
			}

			_Repository = null;
			_MasterLock = null;
			_Discriminator = null;
			_Members = null;
			_Columns = null;
			_VersionColumn = null;
			_MetaEntities = null;

			_IsValidated = false;
			_ProxyHolder = null;
			_MetaEntities = null;
			_Schema = null;
			_SchemaId = null;
			_Constructor = null;

			_IsDisposed = true;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendFormat("{0}:{1}({2}", SerialId, GetType().EasyName(), EntityType.EasyName());
			if (ProxyHolder != null) sb.Append(":Proxy");
			if (IsWeakMap) sb.Append(":Weak");
			sb.AppendFormat(" => {0}:{1}", Table ?? "-", _Repository.Sketch());
			sb.Append(")");

			var str = sb.ToString();
			return IsDisposed ? "disposed::{0}".FormatWith(str) : str;
		}

		/// <summary>
		/// Returns a new instance that is associated with the new given repository and that
		/// contains a copy of the customizations of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		public DataMap<T> Clone(DataRepository repo)
		{
			var cloned = new DataMap<T>(repo, x => Table); OnClone(cloned);
			return cloned;
		}

		/// <summary>
		/// Returns a new instance that is associated with the new given repository and that
		/// contains a copy of the customizations of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		public DataMap<T> Clone(IDataRepository repo)
		{
			if (repo == null) throw new ArgumentNullException("repo", "Repository cannot be null.");
			var temp = repo as DataRepository;
			if (temp == null) throw new InvalidCastException(
				"Repository '{0}' is not a valid '{1}' instance."
				.FormatWith(repo.Sketch(), typeof(DataRepository).EasyName()));

			return Clone(temp);
		}
		IDataMap<T> IDataMap<T>.Clone(IDataRepository repo)
		{
			return this.Clone(repo);
		}
		IDataMap IDataMap.Clone(IDataRepository repo)
		{
			return this.Clone(repo);
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
			temp._TrackEntities = _TrackEntities;
			temp._TrackChildEntities = _TrackChildEntities;

			temp._Columns = _Columns.Clone(temp); // Must come first...
			temp._Members = _Members.Clone(temp);
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
		/// The object that can be used to synchronize operations on this map.
		/// </summary>
		internal object MasterLock
		{
			get { return _MasterLock; }
		}
		object IUberMap.MasterLock
		{
			get { return this.MasterLock; }
		}

		/// <summary>
		/// The link reference held by the associated repository, if any.
		/// </summary>
		public IDataLink Link
		{
			get { return Repository == null ? null : Repository.Link; }
		}

		/// <summary>
		/// The name of the primary table in the underlying database the entities managed by
		/// this map are associated with.
		/// </summary>
		public string Table
		{
			get { return _Table; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (IsValidated) throw new InvalidOperationException("This map '{0}' is validated.".FormatWith(this));
				
				_Table = value.Validated("Table Name");
			}
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
		/// Wheter this map is considered a weak one or not.
		/// </summary>
		public bool IsWeakMap
		{
			get { return _IsWeakMap; }
			internal set { _IsWeakMap = value; }
		}
		bool IUberMap.IsWeakMap
		{
			get { return this.IsWeakMap; }
			set { this.IsWeakMap = value; }
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
		/// The type of the entities managed by this map.
		/// </summary>
		public Type EntityType
		{
			get { return typeof(T); }
		}

		/// <summary>
		/// The collection of members of the type for which either a dependency and/or a
		/// completion method has been defined.
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
		/// The collection of columns in the database associated with this map.
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
		/// Represents the column to be used for row version control, if any.
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

			_Members.OnValidate();
			_Columns.OnValidate();
			_VersionColumn.OnValidate();

			bool sensitive = _Repository.Link.Engine.CaseSensitiveNames;
			List<string> selects = new List<string>();

			// Preparing the list of columns to select...
			if (_DiscoveryMode == MapDiscoveryMode.Explicit)
			{
				foreach (var col in _Columns)
				{
					if (!col.Excluded) selects.Add(col.Name);
				}
				if (selects.Count != 0) throw new EmptyException(
					"No columns defined for this '{0}' for Explicit Discovery Mode."
					.FormatWith(this));
			}

			// Obtaining the preliminary schema...
			var cmd = Link.From(x => Table);
			cmd.Top(1);
			foreach (var select in selects) cmd.Select(x => select);

			var iter = cmd.GetEnumerator();
			var r = iter.MoveNext();
			var schema = iter.Schema;
			var record = iter.CurrentRecord; if (record != null) record.Dispose();
			iter.Reset();
			iter.Dispose();

			if (schema.Count == 0) throw new EmptyException(
				"Database schema retrieved is empty for map '{0}'.".FormatWith(this));

			// Normalizing schema entry names...
			List<ISchemaEntry> entries = new List<ISchemaEntry>(schema);
			foreach (var entry in entries) schema.Remove(entry);
			foreach (var entry in entries) { entry.TableName = null; schema.Add(entry); }
			entries.Clear();

			// Validating columns...
			foreach (var col in _Columns)
			{
				var entry = schema.FindEntry(col.Name);

				if (col.Excluded)
				{
					if (entry != null) schema.Remove(entry);
					continue;
				}
				if (entry == null) throw new NotFoundException(
					"No entry found in schema for column '{0}' in map '{1}'."
					.FormatWith(col.Name, this));
			}

			// Adding newly found columns...
			if (DiscoveryMode == MapDiscoveryMode.Auto)
			{
				foreach (var entry in schema)
				{
					var col = _Columns.Find(x => string.Compare(x.Name, entry.ColumnName, !sensitive) == 0);
					if (col == null)
					{
						col = _Columns.Add(x => entry.ColumnName);
						col.OnValidate();
						col.AutoDiscovered = true;
					}
				}
			}

			// Validating working schemas...
			if ((_Schema = schema).Count == 0) throw new EmptyException(
				"Normalized schema is empty for map '{0}'."
				.FormatWith(this));

			var idlist = _Schema.IdentityList();
			if (idlist.Count == 0) throw new EmptyException(
				"Normalized schema '{0}' contains no identity columns.".FormatWith(_Schema));

			_SchemaId = _Schema.Clone();
			_SchemaId.Clear();
			_SchemaId.AddRange(idlist, cloneNotOrphans: true);

			// Generating a proxy holder if needed...
			_ProxyHolder = ProxyGenerator.Locate(this);

			// Capturing the default constructor, if any...
			var type = _ProxyHolder == null ? EntityType : ProxyHolder.ProxyType;
			var cons = type.GetConstructors(TypeEx.InstancePublicAndHidden);
			foreach (var con in cons)
			{
				var pars = con.GetParameters();
				if (pars.Length == 0) { _Constructor = con; break; }
			}

			_IsValidated = true;
		}

		/// <summary>
		/// The proxy holder created for the type of the entities managed by this instance,
		/// or null.
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
			get { return ProxyHolder == null ? null : ProxyHolder.ProxyType; }
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
		/// Writes into the target record the contents from the source entity. Only the
		/// existing columns in the target record are taken into consideration.
		/// </summary>
		internal void WriteRecord(T entity, IRecord record)
		{
			bool sensitive = Link.Engine.CaseSensitiveNames;
			ISchema schema = record.Schema;
			ISchemaEntry entry = null;
			MapColumn<T> col = null;
			object value = null;

			// Processing the columns of the target record...
			for (int i = 0; i < record.Count; i++)
			{
				entry = schema[i];
				col = _Columns.Find(x => string.Compare(x.Name, entry.ColumnName, !sensitive) == 0);

				if (col != null && !col.Excluded && col.WriteEnabled)
				{
					if (col.WriteRecord != null)
					{
						value = col.WriteRecord(entity);
						record[i] = value;
					}
					else if (col.ElementInfo != null && col.ElementInfo.CanRead)
					{
						value = col.ElementInfo.GetValue(entity);
						record[i] = value;
					}
				}
			}

			// Target record may need the verion column...
			if (VersionColumn.Name == null) return;
			entry = schema.FindEntry(VersionColumn.Name); if (entry == null) return;
			int n = schema.IndexOf(entry);

			col = VersionColumn.Column; if (col.WriteEnabled)
			{
				if (col.WriteRecord != null)
				{
					value = col.WriteRecord(entity);
					record[n] = value;
					return;
				}
				if (col.ElementInfo != null && col.ElementInfo.CanRead)
				{
					value = col.ElementInfo.GetValue(entity);
					record[n] = value;
					return;
				}
			}

			// It may happen version is not stored in the entity, but only on its record...
			var meta = MetaEntity.Locate(entity);
			var cache = meta.Record; if (cache == null) return;

			value = cache[VersionColumn.Name];
			record[n] = value;
		}
		void IUberMap.WriteRecord(object entity, IRecord record)
		{
			this.WriteRecord((T)entity, record);
		}

		/// <summary>
		/// Loads into the target entity the contents from the source record. Only the existing
		/// columns in the source record are taken into consideration.
		/// </summary>
		internal void LoadEntity(IRecord record, T entity)
		{
			bool sensitive = Link.Engine.CaseSensitiveNames;
			ISchema schema = record.Schema;
			ISchemaEntry entry = null;
			MapColumn<T> col = null;
			object value = null;

			// Processing the columns of the target record...
			for (int i = 0; i < record.Count; i++)
			{
				entry = schema[i];
				col = _Columns.Find(x => string.Compare(x.Name, entry.ColumnName, !sensitive) == 0);

				if (col != null && !col.Excluded && col.LoadEnabled)
				{
					if (col.LoadEntity != null)
					{
						value = record[i];
						col.LoadEntity(value, entity);
					}
					else if (col.ElementInfo != null && col.ElementInfo.CanWrite)
					{
						value = record[i].ConvertTo(col.ElementInfo.ElementType);
						col.ElementInfo.SetValue(entity, value);
					}
				}
			}
		}
		void IUberMap.LoadEntity(IRecord record, object entity)
		{
			this.LoadEntity(record, (T)entity);
		}

		/// <summary>
		/// Completes the members of the given meta-entity. Only the non-lazy members are
		/// processed by this method, as they are processed by their overriden getters when
		/// needed.
		/// </summary>
		internal void CompleteMembers(MetaEntity meta)
		{
			T entity = (T)meta.Entity;

			if (entity == null) return;
			if (meta.Record == null) return;
			if (meta.Completed) return;

			meta.Completed = true; foreach (var member in Members)
			{
				if (member.CompleteMember == null) continue;  // nothing to do...
				if (member.LazyProperty != null) continue; // deferred to lazy...

				member.CompleteMember(meta.Record, entity);

				if (TrackChildEntities &&
					member.DependencyMode == MemberDependencyMode.Child &&
					member.ElementInfo.CanRead &&
					member.ElementInfo.ElementType.IsListAlike())
				{
					var type = member.ElementInfo.ElementType.ListAlikeMemberType();
					if (type != null && (type.IsClass || type.IsInterface))
					{
						HashSet<object> childs = null;
						if (!meta.ChildDependencies.TryGetValue(member.Name, out childs))
							meta.ChildDependencies.Add(member.Name, (childs = new HashSet<object>()));

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
		/// Creates a new temporal record, associated with the ID schema, whose contents are
		/// loaded from the source record given. Returns null if the source record contains not
		/// all the id columns, or if there are any inconsistencies.
		/// </summary>
		internal IRecord ExtractId(IRecord source)
		{
			var id = new Core.Concrete.Record(SchemaId); for (int i = 0; i < SchemaId.Count; i++)
			{
				var name = SchemaId[i].ColumnName;
				var entry = source.Schema.FindEntry(name, raise: false); if (entry == null)
				{
					id.Dispose();
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
		/// Whether tracking of entities is enabled for this map or not.
		/// </summary>
		public bool TrackEntities
		{
			get { return _TrackEntities; }
			set
			{
				if (!IsDisposed && (value == false)) ClearEntities(detach: false);
				_TrackEntities = value;
			}
		}

		/// <summary>
		/// Whether tracking of child entities for dependency properties is enabled for this
		/// map or not.
		/// </summary>
		public bool TrackChildEntities
		{
			get { return _TrackChildEntities; }
			set
			{
				if (!IsDisposed && (value == false)) ClearChildEntities();
				_TrackChildEntities = value;
			}
		}

		/// <summary>
		/// The collection of entities in this map.
		/// </summary>
		internal MetaEntityCollection MetaEntities
		{
			get { return _MetaEntities; }
		}
		MetaEntityCollection IUberMap.MetaEntities
		{
			get { return this.MetaEntities; }
		}

		/// <summary>
		/// The collection of entities in a valid state tracked by this map.
		/// </summary>
		public IEnumerable<MetaEntity> Entities
		{
			get
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());

				CollectInvalidEntities();
				return MetaEntities.Items;
			}
		}
		IEnumerable<IMetaEntity> IDataMap.Entities
		{
			get { return this.Entities; }
		}

		/// <summary>
		/// Clears the cache of this map and, optionally, detaches the entities that were
		/// tracked.
		/// </summary>
		/// <param name="detach">True to also detach the entities removed from the cache.</param>
		public void ClearEntities(bool detach = true)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			lock (MasterLock)
			{
				if (detach)
				{
					var metas = _MetaEntities.ToArray();
					foreach (var meta in metas) Detach(meta);
					Array.Clear(metas, 0, metas.Length);
				}
				_MetaEntities.Clear();
			}
		}

		/// <summary>
		/// Clears the child entities of all managed entities.
		/// </summary>
		internal void ClearChildEntities()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			lock (MasterLock)
			{
				foreach (var meta in _MetaEntities.Items) meta.ChildDependencies.Clear();
			}
		}

		/// <summary>
		/// Collects and removes the invalid entities in the cache of this map, if any.
		/// </summary>
		internal void CollectInvalidEntities()
		{
			lock (MasterLock)
			{
				var metas = new List<MetaEntity>();
				foreach (var meta in _MetaEntities.Items) if (!meta.HasValidEntity) metas.Add(meta);
				foreach (var meta in metas)
				{
					DebugEx.IndentWriteLine("\n- Collecting '{0}'...", meta);

					var ops = Repository.UberOperations.FindAllMeta(meta);
					foreach (var op in ops) op.Dispose();
					ops.Clear(); ops = null;

					MetaEntities.Remove(meta);
					meta.UberMap = null;
					meta.UberOperation = null;
					meta.ChildDependencies.Clear();
					meta.Completed = false;
					meta.Record = null;

					DebugEx.Unindent();
				}
				metas.Clear(); metas = null;
			}
		}
		void IUberMap.CollectInvalidEntities()
		{
			this.CollectInvalidEntities();
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

			T obj = null; if (_Constructor != null) obj = (T)_Constructor.Invoke(null);
			else
			{
				var type = ProxyType ?? EntityType;
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

			lock (MasterLock)
			{
				var meta = MetaEntity.Locate(entity);
				if (object.ReferenceEquals(meta.UberMap, this)) return;
				if (meta.UberMap != null)
					throw new NotOrphanException("Entity '{0}' is not orphan.".FormatWith(meta));

				var record = new Core.Concrete.Record(Schema);
				WriteRecord(entity, record);
				meta.Record = record;
				meta.UberMap = this;

				if (TrackEntities) MetaEntities.Add(meta);
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

			var meta = MetaEntity.Locate(entity);
			return Detach(meta);
		}
		bool IDataMap.Detach(object entity)
		{
			return this.Detach((T)entity);
		}

		/// <summary>
		/// Removes the given entity from this map, making it become a detached one. Returns true
		/// if the entity has been removed, or false otherwise.
		/// </summary>
		internal bool Detach(MetaEntity meta)
		{
			bool r = object.ReferenceEquals(meta.UberMap, this); if (r)
			{
				var ops = Repository.UberOperations.FindAllMeta(meta);
				foreach (var op in ops) op.Dispose();
				ops.Clear(); ops = null;

				MetaEntities.Remove(meta);
				meta.UberMap = null;
				meta.UberOperation = null;
				meta.ChildDependencies.Clear();
				meta.Record = null;
				meta.Completed = false;
			}
			return r;
		}
		bool IUberMap.Detach(MetaEntity meta)
		{
			return this.Detach(meta);
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
		/// this clause.</param>
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
		/// finds and returns inmediately a suitable entity that meets the conditions given, by
		/// looking for it in the managed cache and, if it cannot be found there, querying the
		/// database for it. returns null if such entity cannot be found neither in the cache
		/// nor in the database.
		/// </summary>
		/// <param name="specs">a collection of dynamic lambda expressions each containing the
		/// name and value to find for a column, as in: 'x => x.column == value'.</param>
		/// <returns>the requested entity, or null.</returns>
		public T FindNow(params Func<dynamic, object>[] specs)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (specs == null) throw new ArgumentNullException("specs", "Specifications array cannot be null.");
			Validate();

			var record = Record.Create(specs); foreach (var entry in SchemaId)
			{
				// This is a hack to optimize finding the record by identity columns...
				var temp = record.Schema.FindEntry(entry.ColumnName, raise: false);
				if (temp != null) temp.IsPrimaryKeyColumn = true;
			}

			// Let's use the record as the index in the cache if posssible...
			lock (MasterLock)
			{
				var node = MetaEntities.FindNode(record);
				if (node != null)
				{
					var meta = node.Find(x => x.HasValidEntity);
					if (meta != null)
					{
						record.Dispose(disposeSchema: true);
						return (T)meta.Entity;
					}
				}
			}

			// If not an identity record we need to iterate through the cache...
			bool isId = true; foreach (var entry in SchemaId)
			{
				var temp = record.Schema.FindEntry(entry.ColumnName, raise: false);
				if (temp == null) { isId = false; break; }
			}

			if (!isId)
			{
				lock (MasterLock)
				{
					foreach (var meta in MetaEntities.Items)
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

			// If not found in the cache...
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
			var attached = object.ReferenceEquals(meta.Map, this);
			if (!attached) Attach(entity); // By definition we only refresh attached entities...

			var record = meta.Record; lock (MasterLock) // Forcing the refresh of the cache...
			{
				meta.Completed = false;

				var node = MetaEntities.FindNode(record);
				if (node != null) foreach (var item in node) item.Completed = false;
			}

			var id = ExtractId(record);
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
			tag.Dispose();

			T obj = cmd.First(); cmd.Dispose();

			// We don't implicitly attach something not found...
			if (obj == null && !attached) Detach(entity);

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
		IDataUpdate<T> IDataMap<T>.Update(T entitity)
		{
			return this.Update(entitity);
		}
		IDataUpdate IDataMap.Update(object entitity)
		{
			return this.Update((T)entitity);
		}
	}
}
