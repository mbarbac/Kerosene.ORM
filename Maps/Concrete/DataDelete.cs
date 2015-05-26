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
	/// <summary>
	/// Represents a delete operation for its associated entity.
	/// </summary>
	public class DataDelete<T> : MetaOperation<T>, IDataDelete<T>, IUberOperation where T : class
	{
		IDeleteCommand _Command = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="map">The map this command will be associated with.</param>
		/// <param name="entity">The entity affected by this operation.</param>
		public DataDelete(DataMap<T> map, T entity) : base(map, entity) { }

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		protected override void OnDispose(bool disposing)
		{
			if (_Command != null) _Command.Dispose(); _Command = null;

			base.OnDispose(disposing);
		}

		/// <summary>
		/// Returns a new core command that when executed materializes the operation this instance
		/// refers to, or null if that command cannot be generated for any reasons.
		/// </summary>
		/// <returns>A new core command, or null.</returns>
		public IDeleteCommand GenerateCoreCommand()
		{
			return IsDisposed ? null : Map.GenerateDeleteCommand(Entity);
		}
		ICommand ICoreCommandProvider.GenerateCoreCommand()
		{
			return this.GenerateCoreCommand();
		}

		/// <summary>
		/// Submits this operation so that it will be executed, along with all other pending
		/// change operations on its associated repository, when it executes then all against
		/// the underlying database as a single logic unit.
		/// </summary>
		public override void Submit()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (MetaEntity.UberMap == null) throw new OrphanException(
				"Entity '{0}' is not attached to any map.".FormatWith(MetaEntity));

			base.Submit();
		}

		/// <summary>
		/// Invoked to execute this operation.
		/// </summary>
		internal void OnExecute(object origin = null)
		{
			lock (Repository.MasterLock)
			{
				List<object> list = null;
				ChangeEntry change = null;
				IDeleteCommand cmd = null;

				DebugEx.IndentWriteLine("\n- Preparing 'Delete({0})'...", MetaEntity);
				try
				{
					list = MetaEntity.GetRemovedChilds();
					foreach (var obj in list)
					{
						if (obj == null) continue;
						if (object.ReferenceEquals(obj, origin)) continue;
						var type = obj.GetType(); if (!type.IsClass && !type.IsInterface) continue;

						var meta = MetaEntity.Locate(obj);
						var map = meta.UberMap ?? Repository.LocateUberMap(type);
						if (map == null) throw new NotFoundException("Cannot find map for type '{0}'.".FormatWith(type.EasyName()));

						var op = map.Delete(obj);
						try { ((IUberOperation)op).OnExecute(origin: Entity); }
						finally { op.Dispose(); }
					}
					list.Clear(); list = null;

					list = MetaEntity.GetDependencies(Map, MemberDependencyMode.Child);
					foreach (var obj in list)
					{
						if (obj == null) continue;
						if (object.ReferenceEquals(obj, origin)) continue;
						var type = obj.GetType(); if (!type.IsClass && !type.IsInterface) continue;

						var meta = MetaEntity.Locate(obj);
						var map = meta.UberMap ?? Repository.LocateUberMap(type);
						if (map == null) throw new NotFoundException("Cannot find map for type '{0}'.".FormatWith(type.EasyName()));

						var op = map.Delete(obj);
						try { ((IUberOperation)op).OnExecute(origin: Entity); }
						finally { op.Dispose(); }
					}
					list.Clear(); list = null;

					cmd = Map.GenerateDeleteCommand(Entity);
					if (cmd != null)
					{
						DebugEx.IndentWriteLine("\n- Executing '{0}'...", cmd);
						try
						{
							MetaEntity.ValidateRowVersion();
							int n = cmd.Execute();
						}
						finally { DebugEx.Unindent(); }
					}
					Map.Detach(Entity);

					change = new ChangeEntry(ChangeType.Delete, Map, Entity);
					Repository.ChangeEntries.Add(change);

					list = MetaEntity.GetDependencies(Map, MemberDependencyMode.Parent);
					foreach (var obj in list)
					{
						if (obj == null) continue;
						if (object.ReferenceEquals(obj, origin)) continue;
						var type = obj.GetType(); if (!type.IsClass && !type.IsInterface) continue;

						var meta = MetaEntity.Locate(obj);
						var map = meta.UberMap ?? Repository.LocateUberMap(type);
						if (map == null) throw new NotFoundException("Cannot find map for type '{0}'.".FormatWith(type.EasyName()));

						change = new ChangeEntry(ChangeType.Refresh, map, obj);
						Repository.ChangeEntries.Add(change);
					}
				}
				finally
				{
					if (cmd != null) cmd.Dispose(); cmd = null;
					if (list != null) list.Clear(); list = null;
					DebugEx.Unindent();
				}
			}
		}
		void IUberOperation.OnExecute(object origin)
		{
			this.OnExecute(origin);
		}
	}

	// ====================================================
	internal static partial class Uber
	{
		/// <summary>
		/// Generates a delete core command for the given entity, or returns null if such
		/// command cannot be generated for whatever reasons.
		/// </summary>
		internal static IDeleteCommand GenerateDeleteCommand(this IUberMap map, object entity)
		{
			if (entity == null) return null;
			if (map == null || map.IsDisposed || !map.IsValidated) return null;

			IDeleteCommand cmd = null;

			MetaEntity meta = MetaEntity.Locate(entity, create: true); if (meta.Record == null)
			{
				var record = new Core.Concrete.Record(map.Schema);
				map.WriteRecord(entity, record);
				meta.Record = record;
			}

			var id = map.ExtractId(meta.Record);
			if (id != null)
			{
				cmd = map.Link.Engine.CreateDeleteCommand(map.Link, x => map.Table);
				if (map.Discriminator != null) cmd.Where(map.Discriminator);

				var tag = new DynamicNode.Argument("x");
				for (int i = 0; i < id.Count; i++)
				{
					var left = new DynamicNode.GetMember(tag, id.Schema[i].ColumnName);
					var bin = new DynamicNode.Binary(left, ExpressionType.Equal, id[i]);
					cmd.Where(x => bin);
					left.Dispose();
					bin.Dispose();
				}
				tag.Dispose();
				id.Dispose();
			}

			return cmd;
		}
	}
}
