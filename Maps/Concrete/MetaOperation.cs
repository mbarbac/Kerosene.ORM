using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;

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
		/// Invoked to execute the operation this instance refers to.
		/// </summary>
		void OnExecute();
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
		/// <param name="entity">The entity this instance will be executed against.</param>
		protected MetaOperation(DataMap<T> map, T entity)
			: base(map)
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
				if (Repository != null && !Repository.IsDisposed)
				{
					lock (Repository.MasterLock)
					{
						Repository.UberOperations.Remove(this);

						if (MetaEntity != null && object.ReferenceEquals(this, MetaEntity.UberOperation))
						{
							MetaEntity.UberOperation =
								Repository.UberOperations.FindLastMeta(MetaEntity);
						}
					}
				}
			}

			_MetaEntity = null;
			_Entity = null;

			base.OnDispose(disposing);
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
		public MetaEntity MetaEntity
		{
			get { return _MetaEntity; }
		}

		/// <summary>
		/// Invoked to obtain additional info when tracing an empty command.
		/// </summary>
		protected override string OnTraceCommandEmpty()
		{
			return MetaEntity == null ? string.Empty : MetaEntity.ToString();
		}

		/// <summary>
		/// Whether this operation has been submitted or not.
		/// </summary>
		public bool IsSubmitted
		{
			get { return IsDisposed ? false : Repository.UberOperations.Contains(this); }
		}

		/// <summary>
		/// Submits this operation so that it will be executed, along with all other pending
		/// change operations on its associated repository, when it executes then all against
		/// the underlying database as a single logic unit.
		/// </summary>
		public void Submit()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			lock (Repository.MasterLock)
			{
				if (!Repository.UberOperations.Contains(this)) Repository.UberOperations.Add(this);
				MetaEntity.UberOperation = this;
			}
		}

		/// <summary>
		/// Invoked to execute the operation this instance refers to.
		/// </summary>
		void IUberOperation.OnExecute()
		{
			throw new NotSupportedException(
				"Abstract IOperation::{0}() invoked."
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
		internal static List<object> GetRemovedChilds(this MetaEntity meta)
		{
			var list = new List<object>();
			var obj = meta.Entity; if (obj != null)
			{
				var type = obj.GetType();
				foreach (var kvp in meta.ChildDependencies)
				{
					var info = ElementInfo.Create(type, kvp.Key, flags: TypeEx.FlattenInstancePublicAndHidden);
					if (!info.CanRead) continue;

					var curr = ((IEnumerable)info.GetValue(obj)).Cast<object>().ToList();
					foreach (var item in kvp.Value) if (!curr.Contains(item)) list.Add(item);
					curr.Clear(); curr = null;
				}
			}
			return list;
		}

		/// <summary>
		/// Returns a list containing the dependencies of the given entity whose mode match the
		/// one given.
		/// </summary>
		internal static List<object> GetDependencies(this MetaEntity meta, MemberDependencyMode mode)
		{
			var list = new List<object>();
			var obj = meta.Entity; if (obj == null) return list;
			var map = meta.UberMap; if (map == null || map.IsDisposed) return list;

			foreach (var member in (IEnumerable<IUberMember>)meta.UberMap.Members)
			{
				if (member.DependencyMode != mode) continue;
				if (!member.ElementInfo.CanRead) continue;

				var source = member.ElementInfo.GetValue(obj);
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

			return list;
		}

		/// <summary>
		/// Validates that the row version captured in the meta-entity is the same as the current
		/// one in the database, throwing an exception if a change is detected.
		/// </summary>
		internal static void ValidateRowVersion(this MetaEntity meta)
		{
			var obj = meta.Entity; if (obj == null) return;
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

	// ====================================================
	internal static partial class Uber
	{
		/// <summary>
		/// Executes a delete operation on the given entity.
		/// </summary>
		internal static void DoDelete(this DataRepository repo, object entity, object parent = null)
		{
			var meta = MetaEntity.Locate(entity);
			DebugEx.IndentWriteLine("\n- Delete({0})...", meta);

			var type = entity.GetType();
			var map = meta.UberMap ?? repo.LocateUberMap(type);
			if (map == null) throw new NotFoundException("Map for type '{0}' cannot be found.".FormatWith(type.EasyName()));

			List<object> list = null;
			IScalarCommand cmd = null;
			int n = 0;

			try
			{
				list = meta.GetDependencies(MemberDependencyMode.Child);
				foreach (var obj in list) repo.DoDelete(obj, parent: entity);
				list.Clear(); list = null;

				list = meta.GetRemovedChilds();
				foreach (var obj in list) repo.DoDelete(obj, parent: entity);
				list.Clear(); list = null;

				list = meta.GetDependencies(MemberDependencyMode.Parent);

				var change = new ChangeEntry(ChangeEntryType.ToDelete, entity);
				cmd = map.GenerateDeleteCommand(entity);
				if (cmd != null)
				{
					meta.ValidateRowVersion();
					n = cmd.Execute();
					change.Executed = true;
				}
				map.Detach(entity);
				repo.Changes.Add(change);

				foreach (var obj in list) // Using the list we have saved before...
				{
					if (object.ReferenceEquals(obj, parent)) continue;
					repo.Changes.Add(new ChangeEntry(ChangeEntryType.ToRefresh, obj));
				}
				list.Clear(); list = null;
			}
			finally
			{
				if (list != null) list.Clear(); list = null;
				if (cmd != null) cmd.Dispose(); cmd = null;
				DebugEx.Unindent();
			}
		}

		/// <summary>
		/// Executes a save operation (insert or delete) on the given entity.
		/// </summary>
		internal static void DoSave(this DataRepository repo, object entity, object child = null, object parent = null)
		{
			var meta = MetaEntity.Locate(entity);
			var insert = meta.Map == null;
			DebugEx.IndentWriteLine("\n- {1}({0})...", meta, insert ? "Insert" : "Update");

			var type = entity.GetType();
			var map = meta.UberMap ?? repo.LocateUberMap(type);
			if (map == null) throw new NotFoundException("Map for type '{0}' cannot be found.".FormatWith(type.EasyName()));

			List<object> list = null;
			IEnumerableCommand cmd = null;
			IRecord rec = null;

			try
			{
				list = meta.GetDependencies(MemberDependencyMode.Parent);
				foreach (var obj in list)
				{
					if (object.ReferenceEquals(obj, parent)) continue;
					repo.DoSave(obj, child: entity);
				}
				list.Clear(); list = null;

				var change = new ChangeEntry(insert ? ChangeEntryType.ToInsert : ChangeEntryType.ToUpdate, entity);
				cmd = insert
					? (IEnumerableCommand)map.GenerateInsertCommand(entity)
					: (IEnumerableCommand)map.GenerateUpdateCommand(entity);

				if (cmd != null)
				{
					if (!insert) meta.ValidateRowVersion();
					rec = (IRecord)cmd.First();
					if (rec == null) throw new CannotExecuteException("Failed execution of '{0}'.".FormatWith(cmd));
					change.Executed = true;

					map.LoadEntity(rec, entity);

					rec = new Core.Concrete.Record(map.Schema); // We need the identity columns...
					map.WriteRecord(entity, rec);
					meta.Record = rec;

					if (insert) map.Attach(entity);
					else
					{
						map.MetaEntities.Remove(meta);
						if (map.TrackEntities) map.MetaEntities.Add(meta);
					}
				}
				repo.Changes.Add(change);

				list = meta.GetDependencies(MemberDependencyMode.Child);
				foreach (var obj in list)
				{
					if (object.ReferenceEquals(obj, child)) continue;
					var temp = MetaEntity.Locate(obj);
					if (temp.Map == null || temp.DoesChildNeedUpdate(cascade: true)) repo.DoSave(obj, parent: entity);
				}
				list.Clear(); list = null;

				list = meta.GetRemovedChilds();
				foreach (var obj in list) repo.DoDelete(obj, parent: entity);
				list.Clear(); list = null;
			}
			finally
			{
				if (list != null) list.Clear(); list = null;
				if (cmd != null) cmd.Dispose(); cmd = null;
				DebugEx.Unindent();
			}
		}
	}
}
