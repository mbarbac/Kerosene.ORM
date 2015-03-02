// ======================================================== MetaEntity.cs
namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.ORM.Core;
	using Kerosene.ORM.Core.Concrete;
	using Kerosene.Tools;
	using System;
	using System.Collections;
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
		/// Resets the contents and dependencies of this instance making it to become a detached
		/// one again.
		/// </summary>
		void Reset();

		/// <summary>
		/// Whether the entity this instance refers to is valid or not.
		/// </summary>
		bool HasValidEntity { get; }

		/// <summary>
		/// The normalized type of the underlying entity this instance was created for.
		/// </summary>
		Type EntityType { get; }

		/// <summary>
		/// The map annotated into this instance.
		/// </summary>
		IUberMap Map { get; set; }

		/// <summary>
		/// The proxy holder reference held by the associated map, is any.
		/// </summary>
		ProxyHolder ProxyHolder { get; }

		/// <summary>
		/// The repository reference held by the associated map, if any.
		/// </summary>
		DataRepository Repository { get; }

		/// <summary>
		/// The operation annotated into this instance.
		/// </summary>
		IUberOperation Operation { get; set; }

		/// <summary>
		/// The record reference this instance carries.
		/// </summary>
		IRecord Record { get; }

		/// <summary>
		/// Sets the record reference this instance will carry.
		/// </summary>
		/// <param name="record">The record, or null.</param>
		/// <param name="disposeOld">True to dispose any previous record, if any.</param>
		void SetRecord(IRecord record, bool disposeOld);

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
		/// Maintains the child dependencies of the members of the underlying entity.
		/// </summary>
		Dictionary<string, List<object>> MemberChilds { get; }
	}

	// ==================================================== 
	/// <summary>
	/// Represents the metadata associated with a given entity that permits it to be managed
	/// by the maps framework.
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
				"Entity {0}({1}) is not a class.".FormatWith(type.EasyName(), entity.Sketch()));

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
					meta._ProxyHolder = ProxyGenerator.Holders.FirstOrDefault(x => x.ExtendedType == type);
				}

				return meta;
			}
		}

		ulong _SerialId = 0;
		WeakReference _WeakReference = null;
		Type _EntityType = null;
		ProxyHolder _ProxyHolder = null;
		IUberMap _Map = null;
		IUberOperation _Operation = null;
		IRecord _Record = null;
		bool _Completed = false;
		bool _ToRefresh = false;
		Dictionary<string, List<object>> _MemberChilds = new Dictionary<string, List<object>>();

		private MetaEntity() { }

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
			return string.Format("{0}{1}{2}{3}",
				SerialId,
				ToStringType(),
				ToStringState(),
				ToStringData());
		}

		private string ToStringType()
		{
			return string.Format(":{0}{1}",
				EntityType.EasyName(),
				ProxyHolder != null ? ":Proxy" : string.Empty);
		}
		private string ToStringState()
		{
			return string.Format(":{0}", State.ToString());
		}
		private string ToStringData()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(":[");

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
		/// Resets the contents and dependencies of this instance making it to become a detached
		/// one again.
		/// </summary>
		internal void Reset()
		{
			Completed = false;
			_ToRefresh = false;
			_MemberChilds.Clear();

			if (_Record != null)
			{
				_Record.Dispose();
				_Record = null;
			}
			if (_Operation != null)
			{
				_Operation.Dispose();
				_Operation = null;
			}
			if (_Map != null)
			{
				_Map.WithEntitiesLock(() => { _Map.UberEntities.Remove(this); });
				_Map = null;
			}
		}
		void IUberEntity.Reset()
		{
			this.Reset();
		}

		/// <summary>
		/// The serial number assigned to this instance.
		/// </summary>
		public ulong SerialId
		{
			get { return _SerialId; }
		}

		/// <summary>
		/// The actual entity this metadata instance is associated with, or null if that entity
		/// has been collected.
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
				return (_WeakReference == null || !_WeakReference.IsAlive || _WeakReference.Target == null)
					? false
					: true;
			}
		}
		bool IUberEntity.HasValidEntity
		{
			get { return this.HasValidEntity; }
		}

		/// <summary>
		/// The normalized type of the underlying entity this instance was created for.
		/// </summary>
		internal Type EntityType
		{
			get
			{
				if (_ProxyHolder != null) return _ProxyHolder.ExtendedType.BaseType;
				return _EntityType;
			}
		}
		Type IUberEntity.EntityType
		{
			get { return this.EntityType; }
		}

		/// <summary>
		/// The state of the underlying entity.
		/// </summary>
		public MetaState State
		{
			get
			{
				if (!HasValidEntity) return MetaState.Collected;
				if (_Map == null) return MetaState.Detached;

				if (_Operation != null)
				{
					if (_Operation is IDataInsert) return MetaState.ToInsert;
					if (_Operation is IDataUpdate) return MetaState.ToUpdate;
					if (_Operation is IDataDelete) return MetaState.ToDelete;
					throw new InvalidOperationException(
						"Unknown operation '{0}'.".FormatWith(_Operation));
				}

				return MetaState.Ready;
			}
		}

		/// <summary>
		/// The map annotated into this instance.
		/// </summary>
		internal IUberMap Map
		{
			get { return _Map; }
			set { _Map = value; }
		}
		IUberMap IUberEntity.Map
		{
			get { return this.Map; }
			set { this.Map = value; }
		}

		/// <summary>
		/// The proxy holder reference held by the associated map, is any.
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
		/// The repository reference held by the associated map, if any.
		/// </summary>
		internal DataRepository Repository
		{
			get { return Map == null ? null : Map.Repository; }
		}
		DataRepository IUberEntity.Repository
		{
			get { return this.Repository; }
		}

		/// <summary>
		/// The operation annotated into this instance.
		/// </summary>
		internal IUberOperation Operation
		{
			get { return _Operation; }
			set { _Operation = value; }
		}
		IUberOperation IUberEntity.Operation
		{
			get { return this.Operation; }
			set { this.Operation = value; }
		}

		/// <summary>
		/// The record reference this instance carries.
		/// </summary>
		internal IRecord Record
		{
			get { return _Record; }
		}
		IRecord IUberEntity.Record
		{
			get { return this.Record; }
		}

		/// <summary>
		/// Sets the record reference this instance will carry.
		/// </summary>
		/// <param name="record">The record, or null.</param>
		/// <param name="disposeOld">True to dispose any previous record, if any.</param>
		internal void SetRecord(IRecord record, bool disposeOld)
		{
			if (object.ReferenceEquals(_Record, record)) return;
			if (disposeOld && _Record != null) _Record.Dispose();
			_Record = record;
		}
		void IUberEntity.SetRecord(IRecord record, bool disposeOld)
		{
			this.SetRecord(record, disposeOld);
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
					if (value == false)
					{
						if (ProxyHolder != null)
						{
							foreach (var lazy in ProxyHolder.LazyProperties)
							{
								// We only operate on the lazy properties for setting their flag
								// to false - setting it to true is done by their getter's
								lazy.LazyCompletedFlag.SetValue(entity, false);
							}
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
		/// Maintains the child dependencies of the members of the underlying entity.
		/// </summary>
		internal Dictionary<string, List<object>> MemberChilds
		{
			get { return _MemberChilds; }
		}
		Dictionary<string, List<object>> IUberEntity.MemberChilds
		{
			get { return this.MemberChilds; }
		}
	}
}
// ======================================================== 
