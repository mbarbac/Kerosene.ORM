using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Kerosene.ORM.Maps.Concrete
{
	// ====================================================
	/// <summary>
	/// Represents the metadata associated with an entity that can become a managed one.
	/// </summary>
	public class MetaEntity : Attribute, IMetaEntity
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
		IUberMap _UberMap = null;
		Type _EntityType = null;
		ProxyHolder _ProxyHolder = null; bool _ProxyCaptured = false;
		bool _Completed = false;
		IRecord _Record = null; string _CollectionId = null;
		Dictionary<string, HashSet<object>> _ChildDependencies = new Dictionary<string, HashSet<object>>();

		/// <summary>
		/// Avoids the constructor to be called for no entity.
		/// </summary>
		private MetaEntity() { }

		/// <summary>
		/// Clears the properties of this instance.
		/// </summary>
		internal void Reset(bool childs = true, bool remove = true, bool collectionId = true, bool record = true, bool map = true)
		{
			Completed = false;
			if (childs) ChildDependencies.Clear();
			if (remove && _UberMap != null && !_UberMap.IsDisposed) _UberMap.MetaEntities.Remove(this);
			if (collectionId) CollectionId = null;
			if (record) Record = null;
			if (map) UberMap = null;
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
			if (!HasValidEntity) return "Collected";
			if (UberMap == null) return "Detached";

			if (Repository != null)
			{
				var op = Repository.UberOperations.FindLastEntity(Entity);
				if (op != null)
				{
					if (op is IDataDelete) return "ToDelete";
					if (op is IDataInsert) return "ToInsert";
					if (op is IDataUpdate) return "ToUpdate";
					if (op is IDataSave) return "ToSave";
					return "Unknown";
				}
			}

			return "Ready";
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
		/// The serial number assigned to this instance.
		/// </summary>
		public ulong SerialId
		{
			get { return _SerialId; }
		}

		/// <summary>
		/// The actual entity this metadata is associated with, or null if it has been collected
		/// or if it is invalid.
		/// </summary>
		public object Entity
		{
			get { return !HasValidEntity ? null : _WeakReference.Target; }
		}

		/// <summary>
		/// Whether the entity this instance refers to is a valid one or not.
		/// </summary>
		internal bool HasValidEntity
		{
			get
			{
				bool invalid = _WeakReference == null || !_WeakReference.IsAlive || _WeakReference.Target == null;

				if (invalid) ChildDependencies.Clear();
				return !invalid;
			}
		}

		/// <summary>
		/// The map that is managing this entity, if any.
		/// Setting this property has NO effect on whether this instace is added into or removed
		/// from the map's tracked entities collection.
		/// </summary>
		internal IUberMap UberMap
		{
			get { return _UberMap; }
			set { _UberMap = value; }
		}

		/// <summary>
		/// The repository associated with this entity, if any.
		/// </summary>
		internal DataRepository Repository
		{
			get { return UberMap == null ? null : UberMap.Repository;}
		}

		/// <summary>
		/// The map that is currently managing this instance, if any.
		/// </summary>
		public IDataMap Map
		{
			get { return this.UberMap; }
		}

		/// <summary>
		/// Captures the proxy holder associated with this instance.
		/// </summary>
		void CaptureProxy()
		{
			if (!_ProxyCaptured)
			{
				lock (ProxyGenerator.ProxyLock) { _ProxyHolder = ProxyGenerator.ProxyHolders.Find(_EntityType); }
				_ProxyCaptured = true;
				if (_ProxyHolder != null) _EntityType = _ProxyHolder.ProxyType.BaseType;
			}
		}

		/// <summary>
		/// The type of the underlying entity, or null.
		/// </summary>
		public Type EntityType
		{
			get { CaptureProxy(); return _EntityType; }
		}

		/// <summary>
		/// The proxy holder associated with the type of the underlying entity, or null.
		/// </summary>
		internal ProxyHolder ProxyHolder
		{
			get { CaptureProxy(); return _ProxyHolder; }
		}

		/// <summary>
		/// The proxy type of the underlying entity, or null.
		/// </summary>
		internal Type ProxyType
		{
			get { return ProxyHolder == null ? null : ProxyHolder.ProxyType; }
		}

		/// <summary>
		/// The last record retrieved from or persisted to the database.
		/// </summary>
		internal IRecord Record
		{
			get { return _Record; }
			set
			{
				if (object.ReferenceEquals(_Record, value)) return;
				if (_Record != null) _Record.Dispose();
				_Record = value;
			}
		}

		/// <summary>
		/// The id assigned to this instance by the collection where it is registered, if any.
		/// This property is under the control of the map's entity collection, and shall not be
		/// set directly.
		/// </summary>
		internal string CollectionId
		{
			get { return _CollectionId; }
			set { _CollectionId = value; }
		}

		/// <summary>
		/// The collection of entities captured as child dependencies of this instance, if any,
		/// organized by the name of the child property that holds them.
		/// </summary>
		internal Dictionary<string, HashSet<object>> ChildDependencies
		{
			get { return _ChildDependencies; }
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
	}

	// ====================================================
	internal static partial class Uber
	{
		/// <summary>
		/// Captures the current child entities of this instance for the given member.
		/// </summary>
		internal static void CaptureMetaMemberChilds(this MetaEntity meta, IUberMember member)
		{
			var entity = meta.Entity;

			if (member.DependencyMode == MemberDependencyMode.Child &&
				member.ElementInfo.CanRead &&
				member.ElementInfo.ElementType.IsListAlike())
			{
				var type = member.ElementInfo.ElementType.ListAlikeMemberType();
				if (type != null && (type.IsClass || type.IsInterface))
				{
					HashSet<object> childs = null;
					if (!meta.ChildDependencies.TryGetValue(member.Name, out childs))
						meta.ChildDependencies.Add(member.Name, (childs = new HashSet<object>()));

					childs.Clear(); if (meta.UberMap.Repository.TrackChildEntities)
					{
						var iter = member.ElementInfo.GetValue(entity) as IEnumerable;
						foreach (var obj in iter) if (obj != null) childs.Add(obj);
					}
				}
			}
		}
	}
}
