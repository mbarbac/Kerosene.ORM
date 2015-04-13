// ======================================================== MetaOperation.cs
namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;

	// ==================================================== 
	/// <summary>
	/// Extends the <see cref="IMetaOperation"/> interface.
	/// </summary>
	internal interface IUberOperation : IMetaOperation, IUberCommand
	{
		/// <summary>
		/// Called from the repo's DiscardChanges() method (instead of calling Dispose()) to
		/// avoid re-entrance.
		/// </summary>
		void OnDiscard();

		/// <summary>
		/// The meta entity associated with the entity affected by this operation.
		/// </summary>
		MetaEntity MetaEntity { get; }

		/// <summary>
		/// Invoked when all the pending operations are processed to execute this operation
		/// against the underlying database.
		/// </summary>
		void Execute();
	}

	// ==================================================== 
	/// <summary>
	/// Represents a change operation associated with a given entity that can be submitted
	/// into a given repository for its future execution along with all other pending change
	/// operations that may have annotated into that repository.
	/// </summary>
	public abstract partial class MetaOperation<T> : MetaCommand<T>, IMetaOperation<T>, IUberOperation where T : class
	{
		T _Entity = null;
		MetaEntity _MetaEntity = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="map">The map this command will be associated with.</param>
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
			if (Repository != null && !Repository.IsDisposed)
			{
				bool discard = Repository.UberOperations.Contains(this);
				if (discard) Repository.DiscardChanges();
			}

			_MetaEntity = null;
			_Entity = null;

			base.OnDispose(disposing);
		}

		/// <summary>
		/// Called from the repo's DiscardChanges() method (instead of calling Dispose()) to
		/// avoid re-entrance.
		/// </summary>
		internal void OnDiscard()
		{
			if (Repository != null && !Repository.IsDisposed)
			{
				lock (Repository.UberOperations.SyncRoot) { Repository.UberOperations.Remove(this); }
			}

			_MetaEntity = null;
			_Entity = null;

			base.OnDispose(disposing: true);
		}
		void IUberOperation.OnDiscard()
		{
			this.OnDiscard();
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
		/// The meta entity associated with the entity affected by this operation.
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
	}

	// ==================================================== 
	public abstract partial class MetaOperation<T>
	{
		/// <summary>
		/// Whether this operation has been submitted or not.
		/// </summary>
		public bool IsSubmitted
		{
			get
			{
				return
					Repository == null || Repository.IsDisposed ? false :
					Repository.UberOperations.Contains(this);
			}
		}

		/// <summary>
		/// Submits this operation so that it will be executed, along with all other pending
		/// change operations on its associated repository, when it executes then all against
		/// the underlying database as a single logic unit.
		/// </summary>
		public void Submit()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (IsSubmitted) throw new
				InvalidOperationException("This '{0}' has been already submitted.".FormatWith(this));

			DebugEx.IndentWriteLine("\n- Submitting '{0}'...".FormatWith(this));
			lock (Repository.UberOperations.SyncRoot)
			{
				// The entity has to be attached for it to be annotated with a pending operation...
				if (MetaEntity.Map == null) Map.Attach(Entity);
				if (!object.ReferenceEquals(Map, MetaEntity.Map))
					throw new InvalidOperationException(
						"Entity '{0}' is not managed by this map '{1}'.".FormatWith(MetaEntity, Map));

				// Adding this instance to the ordered collection of pending operations...
				MetaEntity.ToRefresh = true;
				MetaEntity.UberOperation = this;
				Repository.UberOperations.Add(this);

				// Dependencies initial phase...
				if (this is IDataDelete)
				{
					// Deletions need to "clear" their childs before being executed...
					OnSubmit_RemovedChilds();
					OnSubmit_ChildsDependencies();
				}
				else
				{
					// Inserts and Updates need their parents to exist...
					OnSubmit_ParentDependencies();
				}

				// Ordering phase...
				Repository.UberOperations.Remove(this);
				Repository.UberOperations.Add(this);

				// Dependencies final phase...
				if (this is IDataDelete)
				{
					// Deletions signal their parents...
					OnSubmit_ParentDependencies();
				}
				else
				{
					// Inserts and Updates process their childs once they are done...
					OnSubmit_RemovedChilds();
					OnSubmit_ChildsDependencies();
				}
			}
			DebugEx.Unindent();
		}

		/// <summary>
		/// Processes then parent dependencies.
		/// </summary>
		void OnSubmit_ParentDependencies()
		{
			var list = MetaEntity.GetDependencies(MemberDependencyMode.Parent); foreach (var obj in list)
			{
				if (obj == null) continue;
				var type = obj.GetType(); if (!type.IsClass) continue;
				var meta = MetaEntity.Locate(obj);
				var map = meta.Map ?? Repository.LocateMap(type); if (map == null) continue;

				if (this is IDataDelete)
				{
					meta.ToRefresh = true;
				}

				if ((this is IDataUpdate) || (this is IDataInsert))
				{
					if (meta.State == MetaState.Detached) map.Insert(obj).Submit();
					else
					{
						if (!object.ReferenceEquals(map.Repository, this.Repository))
							throw new InvalidOperationException(
								"Entity '{0}' is not managed by this repository '{1}'."
								.FormatWith(meta, this.Repository));

						switch (meta.State)
						{
							case MetaState.Ready:
								if (meta.HasRecordChanges()) map.Update(obj).Submit();
								meta.ToRefresh = true;
								break;

							case MetaState.ToDelete:
								map.Insert(obj).Submit(); // adding a new operation...
								break;

							case MetaState.ToInsert:
							case MetaState.ToUpdate:
								break;
						}
					}
				}
			}
			list.Clear(); list = null;
		}

		/// <summary>
		/// Processes then childs that have been removed.
		/// </summary>
		void OnSubmit_RemovedChilds()
		{
			var list = MetaEntity.GetRemovedChilds(); foreach (var obj in list)
			{
				if (obj == null) continue;
				var type = obj.GetType(); if (!type.IsClass) continue;
				var meta = MetaEntity.Locate(obj);
				var map = meta.Map ?? Repository.LocateMap(type); if (map == null) continue;

				if (meta.State == MetaState.Detached) { } // Explicitly signalled to do nothing...
				else if (meta.State == MetaState.ToDelete) { } // Deletion already scheduled...
				else
				{
					if (!object.ReferenceEquals(map.Repository, this.Repository))
						throw new InvalidOperationException(
							"Entity '{0}' is not managed by this repository '{1}'."
							.FormatWith(meta, this.Repository));

					switch (meta.State)
					{
						case MetaState.Ready:
							map.Delete(obj).Submit();
							break;

						case MetaState.ToUpdate:
						case MetaState.ToInsert:
							map.Delete(obj).Submit(); // adding a new operation...
							break;
					}
				}
			}
			list.Clear(); list = null;
		}

		/// <summary>
		/// Processes then child dependencies.
		/// </summary>
		void OnSubmit_ChildsDependencies()
		{
			var list = MetaEntity.GetDependencies(MemberDependencyMode.Child); foreach (var obj in list)
			{
				if (obj == null) continue;
				var type = obj.GetType(); if (!type.IsClass) continue;
				var meta = MetaEntity.Locate(obj);
				var map = meta.Map ?? Repository.LocateMap(type); if (map == null) continue;

				if (this is IDataDelete)
				{
					if (meta.State == MetaState.Detached) { } // Explicitly signalled to do nothing...
					else if (meta.State == MetaState.ToDelete) { } // Deletion already scheduled...
					else
					{
						if (!object.ReferenceEquals(map.Repository, this.Repository))
							throw new InvalidOperationException(
								"Entity '{0}' is not managed by this repository '{1}'."
								.FormatWith(meta, this.Repository));

						switch (meta.State)
						{
							case MetaState.Ready:
								map.Delete(obj).Submit();
								break;

							case MetaState.ToInsert:
							case MetaState.ToUpdate:
								map.Delete(obj).Submit(); // adding a new operation...
								break;
						}
					}
				}

				if ((this is IDataUpdate) || (this is IDataInsert))
				{
					if (meta.State == MetaState.Detached) map.Insert(obj).Submit();
					else
					{
						if (!object.ReferenceEquals(map.Repository, this.Repository))
							throw new InvalidOperationException(
								"Entity '{0}' is not managed by this repository '{1}'."
								.FormatWith(meta, this.Repository));

						switch (meta.State)
						{
							case MetaState.Ready:
								if (meta.DoesChildNeedUpdate()) map.Update(obj).Submit();
								meta.ToRefresh = true;
								break;

							case MetaState.ToDelete:
							case MetaState.ToInsert:
							case MetaState.ToUpdate:
								break;
						}
					}
				}
			}
			list.Clear(); list = null;
		}
	}

	// ==================================================== 
	internal static partial class UberHelper
	{
		/// <summary>
		/// Returns a list with the child dependencies that have been removed since the last
		/// time those were captured.
		/// </summary>
		internal static List<object> GetRemovedChilds(this MetaEntity meta)
		{
			var list = new List<object>();
			var obj = meta.Entity; if (obj == null) return list;
			var type = obj.GetType();

			foreach (var kvp in meta.ChildDependencies)
			{
				var info = ElementInfo.Create(type, kvp.Key, flags: TypeEx.InstancePublicAndHidden);
				if (!info.CanRead) continue;
				var curr = ((IEnumerable)info.GetValue(obj)).Cast<object>().ToList();

				foreach (var item in kvp.Value) if (!curr.Contains(item)) list.Add(item);
				curr.Clear(); curr = null;
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

			foreach (var member in ((IEnumerable<IUberMember>)meta.UberMap.Members))
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
		/// Returns wheter the given child entity requires an update.
		/// </summary>
		internal static bool DoesChildNeedUpdate(this MetaEntity child, bool cascade = true)
		{
			if (child.HasRecordChanges()) return true;

			var list = child.GetRemovedChilds();
			var need = list.Count != 0; list.Clear(); list = null;
			if (need) return true;

			if (cascade)
			{
				var repo = child.UberMap.Repository;
				list = child.GetDependencies(MemberDependencyMode.Child); foreach (var obj in list)
				{
					if (obj == null) continue;
					var type = obj.GetType(); if (!type.IsClass) continue;
					var meta = MetaEntity.Locate(obj);
					var map = meta.Map ?? repo.LocateMap(type); if (map == null) continue;

					if (meta.State == MetaState.Ready)
					{
						// Childs are tested up to the next level only...
						if (meta.DoesChildNeedUpdate(cascade: false)) { need = true; break; }
					}
					else { need = true; break; } // Detached, ToInsert, ToDelete, ToUpdate
				}
				list.Clear(); list = null;
				if (need) return true;
			}

			return false;
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
			if (id == null) throw new InvalidOperationException("Cannot obtain identity from entity '{0}'".FormatWith(meta));

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
			string captured = vc.ValueToString == null ? meta.Record[vc.Name].Sketch() : vc.ValueToString(meta.Record[vc.Name]);
			string current = vc.ValueToString == null ? rec[vc.Name].Sketch() : vc.ValueToString(rec[vc.Name]);

			if (string.Compare(captured, current) != 0)
				throw new ChangedException(
					"Captured version for entity '{0}': '{1}' if not the same as the current one in the database: '{2}'."
					.FormatWith(meta, captured, current));
		}
	}

	// ==================================================== 
	public abstract partial class MetaOperation<T>
	{
		/// <summary>
		/// Invoked when all the pending operations are processed to execute this operation
		/// against the underlying database.
		/// </summary>
		internal void Execute()
		{
			ICommand cmd = null;
			try
			{
				DebugEx.IndentWriteLine("\n- Executing '{0}'...", this);
				cmd = ((ICoreCommandProvider)this).GenerateCoreCommand();
				if (cmd != null)
				{
					if ((this is IDataDelete) || (this is IDataUpdate))
					{
						MetaEntity.ValidateRowVersion();
					}
					if (this is IDataDelete)
					{
						var temp = (IDeleteCommand)cmd;
						int n = temp.Execute();
					}
					if ((this is IDataUpdate) || (this is IDataInsert))
					{
						IEnumerableCommand temp = null;
						if (this is IDataInsert) temp = (IInsertCommand)cmd;
						if (this is IDataUpdate) temp = (IUpdateCommand)cmd;

						var rec = (IRecord)temp.First();
						if (rec == null) throw new DatabaseException(
							"Execution of command '{0}' returned no record.".FormatWith(cmd));

						Map.UberEntities.Remove(MetaEntity); // Changing the record may change its identity...
						MetaEntity.Record = rec;
						MetaEntity.UberMap.LoadEntity(rec, MetaEntity.Entity);
						MetaEntity.ToRefresh = true;
						Map.UberEntities.Add(MetaEntity); // Uses re-generated identity if needed
					}
				}
			}
			finally
			{
				if (cmd != null) cmd.Dispose(); cmd = null;
				DebugEx.Unindent();
			}
		}
		void IUberOperation.Execute()
		{
			this.Execute();
		}
	}
}
// ======================================================== 
