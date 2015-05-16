using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Kerosene.ORM.Maps.Concrete
{
	// ====================================================
	internal interface IUberEntity : IMetaEntity
	{
		/// <summary>
		/// Whether the entity this instance refers to is valid or not.
		/// </summary>
		bool HasValidEntity { get; }

		/// <summary>
		/// The type of the underlying entity, or null.
		/// </summary>
		Type EntityType { get; }

		/// <summary>
		/// The proxy type of the underlying entity, or null.
		/// </summary>
		Type ProxyType { get; }

		/// <summary>
		/// The proxy holder associated with the type of the underlying entity, or null.
		/// </summary>
		ProxyHolder ProxyHolder { get; }

		/// <summary>
		/// The map that is managing the underlying entity, or null if it is a detached one.
		/// </summary>
		IUberMap UberMap { get; set; }

		/// <summary>
		/// The repository associated with this entity, if any.
		/// </summary>
		DataRepository Repository { get; }

		/// <summary>
		/// The last operation annotated for this entity.
		/// </summary>
		IUberOperation UberOperation { get; set; }

		/// <summary>
		/// The record associated with this instance, if any.
		/// <para>The setter flags this instance to capture the record's identity string again.</para>
		/// </summary>
		IRecord Record { get; set; }

		/// <summary>
		/// Returns the identity string associated with the current record of this instance,
		/// or null.
		/// </summary>
		string IdentityString { get; }

		/// <summary>
		/// The collection of objects, if any, captured as child dependencies of the underlying
		/// instance, organized by the name of the property that holds them.
		/// </summary>
		Dictionary<string, HashSet<object>> ChildDependencies { get; }

		/// <summary>
		/// Whether the members defined for the underlying object can be considered as completed
		/// or not.
		/// </summary>
		bool Completed { get; set; }
	}

	// ====================================================
	/// <summary>
	/// Represents the metadata associated with a managed entity.
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

			lock (entity) // Yes, I know... but I don't want to use a global lock!
			{
				AttributeCollection list = TypeDescriptor.GetAttributes(entity);
				MetaEntity meta = list.OfType<MetaEntity>().FirstOrDefault();

				if (meta == null && create)
				{
					meta = new MetaEntity(); TypeDescriptor.AddAttributes(entity, meta);
					meta._SerialId = ++Uber.MetaEntityLastSerial;
					meta._EntityType = type;
				}
				if (meta != null && !meta.HasValidEntity)
				{
					meta._WeakReference = new WeakReference(entity);
				}
				return meta;
			}
		}

		ulong _SerialId = 0;
		WeakReference _WeakReference = null;
		Type _EntityType = null;
		ProxyHolder _ProxyHolder = null; bool _ProxyCaptured = false;
		IUberMap _UberMap = null;
		IUberOperation _UberOperation = null;
		IRecord _Record = null;
		string _IdentityString = null;
		bool _Completed = false;
		Dictionary<string, HashSet<object>> _ChildDependencies = new Dictionary<string, HashSet<object>>();

		private MetaEntity() { }

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			return string.Format("{0}:{1}:{2}({3})",
				SerialId,
				ToStringState(),
				ToStringType(),
				ToStringData());
		}
		string ToStringState()
		{
			return State.ToString();
		}
		string ToStringType()
		{
			var type = EntityType;
			var str = type == null ? "-" : type.EasyName();

			var proxy = ProxyHolder;
			if (proxy != null) str += ":Proxy";

			return str;
		}
		string ToStringData()
		{
			StringBuilder sb = new StringBuilder();

			if (Record != null && !Record.IsDisposed)
			{
				bool disposed = Record.Schema == null || Record.Schema.IsDisposed;
				sb.Append("["); for (int i = 0; i < Record.Count; i++)
				{
					var value = Record[i];
					var name = disposed || Record.Schema[i].IsDisposed
						? "#{0}".FormatWith(i)
						: Record.Schema[i].ColumnName.Sketch();

					if (i != 0) sb.Append(", ");
					sb.AppendFormat("{0}={1}", name, value.Sketch());
				}
				sb.Append("]");
			}
			else if (HasValidEntity) sb.Append(Entity.Sketch());

			return sb.ToString();
		}

		/// <summary>
		/// This hack is needed to force instances of this class to behave with reference type
		/// semantics for IndexOf() and Remove() purposes.
		/// </summary>
		public override bool Equals(object obj)
		{
			return object.ReferenceEquals(this, obj);
		}

		/// <summary>
		/// This hack is needed to force instances of this class to behave with reference type
		/// semantics for IndexOf() and Remove() purposes.
		/// </summary>
		public override int GetHashCode()
		{
			return base.GetHashCode();
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
		/// or if it is not available.
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
				bool invalid = _WeakReference == null || !_WeakReference.IsAlive || _WeakReference.Target == null;
				return !invalid;
			}
		}
		bool IUberEntity.HasValidEntity
		{
			get { return this.HasValidEntity; }
		}

		/// <summary>
		/// The type of the underlying entity, or null.
		/// </summary>
		public Type EntityType
		{
			get { CaptureProxy(); return _EntityType; }
		}

		/// <summary>
		/// The proxy type of the underlying entity, or null.
		/// </summary>
		internal Type ProxyType
		{
			get { CaptureProxy(); return _ProxyHolder == null ? null : _ProxyHolder.ProxyType; }
		}
		Type IUberEntity.ProxyType
		{
			get { return this.ProxyType; }
		}

		/// <summary>
		/// Captures the proxy holder associated with this instance.
		/// </summary>
		void CaptureProxy()
		{
			if (!_ProxyCaptured)
			{
				_ProxyHolder = ProxyGenerator.ProxyHolders.Find(_EntityType);
				_ProxyCaptured = true;
				if (_ProxyHolder != null) _EntityType = _ProxyHolder.ProxyType.BaseType;
			}
		}

		/// <summary>
		/// The proxy holder associated with the type of the underlying entity, or null.
		/// </summary>
		internal ProxyHolder ProxyHolder
		{
			get { CaptureProxy(); return _ProxyHolder; }
		}
		ProxyHolder IUberEntity.ProxyHolder
		{
			get { return this.ProxyHolder; }
		}

		/// <summary>
		/// The map that is managing the underlying entity, or null if it is a detached one.
		/// </summary>
		public IDataMap Map
		{
			get { return this.UberMap; }
		}

		/// <summary>
		/// The map that is managing the underlying entity, or null if it is a detached one.
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
		/// The repository associated with this entity, if any.
		/// </summary>
		internal DataRepository Repository
		{
			get { return UberMap == null ? null : UberMap.Repository; }
		}
		DataRepository IUberEntity.Repository
		{
			get { return this.Repository; }
		}

		/// <summary>
		/// The last operation annotated for this entity.
		/// </summary>
		internal IUberOperation UberOperation
		{
			get { return _UberOperation; }
			set { _UberOperation = null; }
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
				if (UberMap == null) return MetaState.Detached;

				if (UberOperation != null)
				{
					if (UberOperation is IDataDelete) return MetaState.ToDelete;
					if (UberOperation is IDataInsert) return MetaState.ToInsert;
					if (UberOperation is IDataUpdate) return MetaState.ToUpdate;

					throw new InvalidCastException(
						"Unknown operation type for '{0}'.".FormatWith(UberOperation));
				}

				return MetaState.Ready;
			}
		}

		/// <summary>
		/// The record associated with this instance, if any.
		/// <para>The setter flags this instance to capture the record's identity string again.</para>
		/// </summary>
		internal IRecord Record
		{
			get { return _Record; }
			set
			{
				if (object.ReferenceEquals(_Record, value)) return;
				if (_Record != null) _Record.Dispose();

				_Record = value;
				_IdentityString = _Record == null ? null : _Record.IdentityString();
			}
		}
		IRecord IUberEntity.Record
		{
			get { return this.Record; }
			set { this.Record = value; }
		}

		/// <summary>
		/// Returns the identity string associated with the current record of this instance,
		/// or null.
		/// </summary>
		internal string IdentityString
		{
			get { return _IdentityString; }
		}
		string IUberEntity.IdentityString
		{
			get { return this.IdentityString; }
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
						foreach (var lazy in ProxyHolder.LazyProperties.Items)
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
		/// The collection of objects, if any, captured as child dependencies of the underlying
		/// instance, organized by the name of the property that holds them.
		/// </summary>
		internal Dictionary<string, HashSet<object>> ChildDependencies
		{
			get { return this._ChildDependencies; }
		}
		Dictionary<string, HashSet<object>> IUberEntity.ChildDependencies
		{
			get { return this.ChildDependencies; }
		}
	}
}
