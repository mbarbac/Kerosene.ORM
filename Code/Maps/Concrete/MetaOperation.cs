using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Kerosene.ORM.Maps.Concrete
{
	// ====================================================
	internal interface IUberOperation : IMetaOperation, IUberCommand
	{
		/// <summary>
		/// The meta entity associated this operation was annotated against.
		/// </summary>
		MetaEntity MetaEntity { get; }

		/// <summary>
		/// Invoked to execute this operation.
		/// </summary>
		void OnExecute(object origin = null);
	}

	// ====================================================
	/// <summary>
	/// Represents a change operation associated with a given entity that can be submitted
	/// into a given repository for its future execution along with all other pending change
	/// operations that may have annotated into that repository.
	/// </summary>
	public abstract class MetaOperation<T> : MetaCommand<T>, IMetaOperation<T>, IUberOperation where T : class
	{
		T _Entity = null;
		MetaEntity _MetaEntity = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="map">The map this command will be associated with.</param>
		/// <param name="entity">The entity affected by this operation.</param>
		protected MetaOperation(DataMap<T> map, T entity) : base(map)
		{
			if (entity == null) throw new ArgumentNullException("entity", "Entity cannot be null.");
			_Entity = entity;
			_MetaEntity = MetaEntity.Locate(entity);
		}

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		protected override void OnDispose(bool disposing)
		{
			if (disposing)
			{
				try
				{
					if (Repository != null && !Repository.IsDisposed)
						lock (Repository.MasterLock) { Repository.UberOperations.Remove(this); }
				}
				catch { }
			}

			_MetaEntity = null;
			_Entity = null;

			base.OnDispose(disposing);
		}

		/// <summary>
		/// Invoked to obtain additional info when tracing an empty command.
		/// </summary>
		protected override string OnTraceCommandEmpty()
		{
			return MetaEntity == null ? string.Empty : MetaEntity.ToString();
		}

		/// <summary>
		/// The entity affected by this operation.
		/// </summary>
		public T Entity
		{
			get { return _Entity; }
		}
		object IMetaOperation.Entity
		{
			get { return this.Entity; }
		}

		/// <summary>
		/// The meta entity associated this operation was annotated against.
		/// </summary>
		internal MetaEntity MetaEntity
		{
			get { return _MetaEntity; }
		}
		MetaEntity IUberOperation.MetaEntity
		{
			get { return this.MetaEntity; }
		}

		/// <summary>
		/// Whether this operation has been submitted already or not.
		/// </summary>
		public bool IsSubmitted
		{
			get
			{
				return (Repository == null || Repository.IsDisposed)
					? false
					: (Repository.UberOperations.FindMeta(MetaEntity) != null);
			}
		}

		/// <summary>
		/// Submits this operation so that it will be executed, along with all other pending
		/// change operations on its associated repository, when it executes then all against
		/// the underlying database as a single logic unit.
		/// </summary>
		public virtual void Submit()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			lock (Repository.MasterLock)
			{
				if (IsSubmitted) return;
				Repository.UberOperations.Add(this);
			}
		}

		/// <summary>
		/// Invoked to execute this operation.
		/// </summary>
		void IUberOperation.OnExecute(object origin)
		{
			throw new NotSupportedException(
				"Abstract IOperation::{0}.OnExecute() invoked."
				.FormatWith(GetType().EasyName()));
		}
	}

	// ====================================================
	internal static partial class Uber
	{
		/// <summary>
		/// Returns a list with the child dependencies that have been removed since the last
		/// time those were captured.
		/// </summary>
		internal static List<object> GetRemovedChilds(this MetaEntity meta, bool forgetRemoved)
		{
			var list = new List<object>();
			var entity = meta.Entity; if (entity != null)
			{
				var type = entity.GetType();
				foreach (var kvp in meta.ChildDependencies)
				{
					// Obtaining the current state...
					var info = ElementInfo.Create(type, kvp.Key, flags: TypeEx.FlattenInstancePublicAndHidden);
					if (!info.CanRead) continue;
					var curr = ((IEnumerable)info.GetValue(entity)).Cast<object>().ToList();

					// Adding the entities that has been removed...
					foreach (var item in kvp.Value) if (!curr.Contains(item)) list.Add(item);
					curr.Clear(); curr = null;

					if (forgetRemoved) // Foget removed childs if such is requested
					{
						foreach (var item in list) kvp.Value.Remove(item);
					}
				}
			}
			return list;
		}

		/// <summary>
		/// Returns a list containing the dependencies of the given entity whose mode match the
		/// one given.
		/// </summary>
		internal static List<object> GetDependencies(this MetaEntity meta, IUberMap map, MemberDependencyMode mode)
		{
			var list = new List<object>();
			var entity = meta.Entity; if (entity != null && map != null && !map.IsDisposed)
			{
				foreach (var member in (IEnumerable<IUberMember>)map.Members)
				{
					if (member.DependencyMode != mode) continue;
					if (!member.ElementInfo.CanRead) continue;

					var source = member.ElementInfo.GetValue(entity);
					if (source == null) continue;

					var type = source.GetType(); if (!type.IsListAlike())
					{
						if (type.IsClass) list.Add(source);
					}
					else
					{
						type = type.ListAlikeMemberType(); if (type.IsClass)
						{
							var iter = source as IEnumerable;
							foreach (var item in iter) if (item != null) list.Add(item);
						}
					}
				}
			}
			return list;
		}

		/// <summary>
		/// Validates that the row version captured in the meta-entity is the same as the current
		/// one in the database, throwing an exception if a change is detected.
		/// </summary>
		internal static void ValidateRowVersion(this MetaEntity meta)
		{
			var entity = meta.Entity; if (entity == null) return;
			if (meta.UberMap == null) return;
			if (meta.Record == null) return;

			var vc = meta.UberMap.VersionColumn;
			if (vc.Name == null) return;

			// Getting the most updated record, if any...
			var cmd = meta.UberMap.Link.From(x => meta.UberMap.Table);
			var tag = new DynamicNode.Argument("x");
			var id = meta.UberMap.ExtractId(meta.Record);
			if (id == null) throw new InvalidOperationException(
				"Cannot obtain identity from entity '{0}'".FormatWith(meta));

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

			var rec = (IRecord)cmd.First();
			cmd.Dispose();
			if (rec == null) return;

			// Comparing values....
			string captured = vc.ValueToString == null
				? meta.Record[vc.Name].Sketch()
				: vc.ValueToString(meta.Record[vc.Name]);

			string current = vc.ValueToString == null
				? rec[vc.Name].Sketch()
				: vc.ValueToString(rec[vc.Name]);

			if (string.Compare(captured, current) != 0) throw new ChangedException(
				"Captured version '{0}' for entity '{1}' differs from the database's current one '{2}'."
				.FormatWith(captured, meta, current));
		}
	}
}
