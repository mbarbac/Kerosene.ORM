// ======================================================== MetaEntity.cs
namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Extends the <see cref="IMetaEntity"/> interface.
	/// </summary>
	internal interface IUberEntity : IMetaEntity
	{
		/// <summary>
		/// Clears the metadata held by this instance.
		/// </summary>
		void Clear();

		/// <summary>
		/// Whether the entity this instance refers to is valid or not.
		/// </summary>
		bool HasValidEntity { get; }

		/// <summary>
		/// The entity type this instance was created for.
		/// </summary>
		Type EntityType { get; }

		/// <summary>
		/// The proxy holder associated with the entity, if any.
		/// </summary>
		ProxyHolder ProxyHolder { get; }

		/// <summary>
		/// The extended type of the associated one, if any.
		/// </summary>
		Type ExtendedType { get; }

		/// <summary>
		/// The map that this instance is managed by, if any.
		/// </summary>
		IUberMap UberMap { get; set; }

		/// <summary>
		/// The repository reference held by the associated map, if any.
		/// </summary>
		DataRepository Repository { get; }

		/// <summary>
		/// The last operation annotated into this entity.
		/// </summary>
		IUberOperation UberOperation { get; set; }

		/// <summary>
		/// The record associated with this instance, if any.
		/// </summary>
		IRecord Record { get; set; }

		/// <summary>
		/// Gets a normalized string that represents the identity columns from the record that
		/// has been associated to this instance, or null.
		/// </summary>
		string RecordId { get; }

		/// <summary>
		/// Whether the members defined for the underlying object can be considered as completed
		/// or not.
		/// </summary>
		bool Completed { get; set; }

		/// <summary>
		/// Whether the underlying entity has been marked to be refreshed as the consequence of
		/// a change operation on any of its dependencies, or not.
		/// </summary>
		bool ToRefresh { get; set; }

		/// <summary>
		/// Gets the collection of objects captured as child dependencies organized by the
		/// property name that holds them.
		/// </summary>
		Dictionary<string, HashSet<object>> ChildDependencies { get; }
	}

	// ==================================================== 
	/// <summary>
	/// Represents the metadata the framework associates with its managed entities.
	/// </summary>
	public class MetaEntity : Attribute, IMetaEntity, IUberEntity
	{
		/// <summary>
		/// Returns the meta entity associated with the given object, creating it if needed and
		/// requested. Returns null otherwise.
		/// </summary>
		/// <param name="entity">The object to obtain its meta entity from.</param>
		/// <param name="create">True to create the meta entity if it did not exist.</param>
		/// <returns>The requested meta entity, or null.</returns>
		public static MetaEntity Locate(object entity, bool create = true)
		{
			if (entity == null) throw new ArgumentNullException("entity", "Entity cannot be null.");

			var type = entity.GetType();
			if (!type.IsClass) throw new InvalidOperationException(
				"Entity '{0}' of type '{1}' is not a class.".FormatWith(entity.Sketch(), type.EasyName()));

			lock (entity)
			{
				AttributeCollection list = TypeDescriptor.GetAttributes(entity);
				MetaEntity meta = list.OfType<MetaEntity>().FirstOrDefault();

				if (meta == null && create)
				{
					meta = new MetaEntity(); TypeDescriptor.AddAttributes(entity, meta);
					meta._SerialId = ++UberHelper.MetaEntityLastSerial;
				}
				if (meta != null && !meta.HasValidEntity)
				{
					meta._WeakReference = new WeakReference(entity);
					meta._EntityType = type;

					if (type.Assembly.GetName().Name == ProxyGenerator.PROXY_ASSEMBLY_NAME)
					{
						meta._ProxyHolder = ProxyGenerator.Holders.FindByExtendedType(type);
						if (meta._ProxyHolder != null) meta._EntityType = type.BaseType;
					}
				}
				return meta;
			}
		}

		ulong _SerialId = 0;
		WeakReference _WeakReference = null;
		Type _EntityType = null;
		ProxyHolder _ProxyHolder = null;
		IUberMap _UberMap = null;
		IRecord _Record = null; string _RecordId = null;
		IUberOperation _UberOperation = null;
		bool _Completed = false;
		bool _ToRefresh = false;
		Dictionary<string, HashSet<object>> _ChildDependencies = new Dictionary<string, HashSet<object>>();

		private MetaEntity() { }

		/// <summary>
		/// Clears the metadata held by this instance.
		/// </summary>
		internal void Clear()
		{
			ChildDependencies.Clear();
			ToRefresh = false;
			Completed = false;
			ToRefresh = false;
			UberOperation = null;
			Record = null;
			UberMap = null;
		}
		void IUberEntity.Clear()
		{
			this.Clear();
		}

		/// <summary>
		/// This hack is needed to force instances of this class to behave as a reference type,
		/// instead of a value type, for IndexOf() and Remove() purposes.
		/// </summary>
		public override bool Equals(object obj)
		{
			return object.ReferenceEquals(this, obj);
		}

		/// <summary>
		/// This hack is needed to force instances of this class to behave as a reference type,
		/// instead of a value type, for IndexOf() and Remove() purposes.
		/// </summary>
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			return string.Format("{0}:{1}:{2}:{3}",
				SerialId,
				ToStringType(),
				State,
				ToStringData());
		}
		private string ToStringType()
		{
			return string.Format("{0}{1}",
				EntityType.EasyName(),
				ProxyHolder != null ? ":Proxy" : string.Empty);
		}
		private string ToStringData()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("[");

			if (_Record != null && !_Record.IsDisposed && _Record.Count != 0)
			{
				bool disposed = _Record.Schema == null || _Record.Schema.IsDisposed;
				bool first = true; for (int i = 0; i < _Record.Count; i++)
				{
					object value = _Record[i];
					string name =
						disposed || Record.Schema[i].IsDisposed
						? "#{0}".FormatWith(i)
						: Record.Schema[i].ColumnName.Sketch();

					if (first) first = false; else sb.Append(", ");
					sb.AppendFormat("{0}={1}", name, value.Sketch());
				}
			}
			else if (HasValidEntity) sb.Append(Entity.Sketch());

			sb.Append("]");
			return sb.ToString();
		}

		/// <summary>
		/// The serial number assigned to this instance.
		/// </summary>
		public ulong SerialId
		{
			get { return _SerialId; }
		}

		/// <summary>
		/// The actual entity this metadata is associated with, or null if it has been collected
		/// or it is not available for any reasons.
		/// </summary>
		public object Entity
		{
			get { return !HasValidEntity ? null : _WeakReference.Target; }
		}

		/// <summary>
		/// Whether the entity this instance refers to is valid or not.
		/// </summary>
		internal bool HasValidEntity
		{
			get
			{
				return (
					_WeakReference == null ||
					!_WeakReference.IsAlive ||
					_WeakReference.Target == null) ? false : true;
			}
		}
		bool IUberEntity.HasValidEntity
		{
			get { return this.HasValidEntity; }
		}

		/// <summary>
		/// The entity type this instance was created for.
		/// </summary>
		public Type EntityType
		{
			get { return _EntityType; }
		}

		/// <summary>
		/// The proxy holder associated with the entity, if any.
		/// </summary>
		internal ProxyHolder ProxyHolder
		{
			get { return _ProxyHolder; }
		}
		ProxyHolder IUberEntity.ProxyHolder
		{
			get { return this.ProxyHolder; }
		}

		/// <summary>
		/// The extended type of the associated one, if any.
		/// </summary>
		internal Type ExtendedType
		{
			get { return _ProxyHolder == null ? null : _ProxyHolder.ExtendedType; }
		}
		Type IUberEntity.ExtendedType
		{
			get { return this.ExtendedType; }
		}

		/// <summary>
		/// The map that is managing this instance, or null if it is a detached one.
		/// </summary>
		public IDataMap Map
		{
			get { return _UberMap; }
		}

		/// <summary>
		/// The map that this instance is managed by, if any.
		/// </summary>
		internal IUberMap UberMap
		{
			get { return _UberMap; }
			set { _UberMap = value; }
		}
		IUberMap IUberEntity.UberMap
		{
			get { return this.UberMap; }
			set { this.UberMap = value; }
		}

		/// <summary>
		/// The repository reference held by the associated map, if any.
		/// </summary>
		internal DataRepository Repository
		{
			get { return _UberMap == null ? null : _UberMap.Repository; }
		}
		DataRepository IUberEntity.Repository
		{
			get { return this.Repository; }
		}

		/// <summary>
		/// The last operation annotated into this entity.
		/// </summary>
		internal IUberOperation UberOperation
		{
			get { return _UberOperation; }
			set { _UberOperation = value; }
		}
		IUberOperation IUberEntity.UberOperation
		{
			get { return this.UberOperation; }
			set { this.UberOperation = value; }
		}

		/// <summary>
		/// The state of the underlying entity.
		/// </summary>
		public MetaState State
		{
			get
			{
				if (!HasValidEntity) return MetaState.Collected;
				if (UberMap == null || UberMap.IsDisposed) return MetaState.Detached;

				if (_UberOperation != null)
				{
					if (_UberOperation is IDataInsert) return MetaState.ToInsert;
					if (_UberOperation is IDataUpdate) return MetaState.ToUpdate;
					if (_UberOperation is IDataDelete) return MetaState.ToDelete;
					throw new InvalidOperationException(
						"Unknown operation '{0}'.".FormatWith(_UberOperation));
				}

				return MetaState.Ready;
			}
		}

		/// <summary>
		/// The record associated with this instance, if any.
		/// </summary>
		internal IRecord Record
		{
			get { return _Record; }
			set
			{
				if (object.ReferenceEquals(_Record, value)) return;
				if (_Record != null) _Record.Dispose();

				_Record = value;
				_RecordId = _Record == null ? null : UberEntitySet.GetKey(_Record);
			}
		}
		IRecord IUberEntity.Record
		{
			get { return this.Record; }
			set { this.Record = value; }
		}

		/// <summary>
		/// Gets a normalized string that represents the identity columns from the record that
		/// has been associated to this instance, or null.
		/// </summary>
		internal string RecordId
		{
			get { return _RecordId; }
		}
		string IUberEntity.RecordId
		{
			get { return this.RecordId; }
		}

		/// <summary>
		/// Whether the members defined for the underlying object can be considered as completed
		/// or not.
		/// </summary>
		internal bool Completed
		{
			get { return _Completed; }
			set
			{
				var entity = Entity; if (entity == null) _Completed = false;
				else
				{
					if (value == false && ProxyHolder != null)
					{
						foreach (var lazy in ProxyHolder.LazyProperties)
						{
							lazy.LazyCompletedFlag.SetValue(entity, false);
						}
					}
					_Completed = value;
				}
			}
		}
		bool IUberEntity.Completed
		{
			get { return this.Completed; }
			set { this.Completed = value; }
		}

		/// <summary>
		/// Whether the underlying entity has been marked to be refreshed as the consequence of
		/// a change operation on any of its dependencies, or not.
		/// </summary>
		internal bool ToRefresh
		{
			get { return _ToRefresh; }
			set { _ToRefresh = value; }
		}
		bool IUberEntity.ToRefresh
		{
			get { return this.ToRefresh; }
			set { this.ToRefresh = value; }
		}

		/// <summary>
		/// Gets the collection of objects captured as child dependencies organized by the
		/// property name that holds them.
		/// </summary>
		internal Dictionary<string, HashSet<object>> ChildDependencies
		{
			get { return _ChildDependencies; }
		}
		Dictionary<string, HashSet<object>> IUberEntity.ChildDependencies
		{
			get { return this.ChildDependencies; }
		}
	}
}
// ======================================================== 
