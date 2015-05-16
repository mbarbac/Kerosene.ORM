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
	/// Represents an update operation for its associated entity.
	/// </summary>
	public class DataUpdate<T> : MetaOperation<T>, IDataUpdate<T>, IUberOperation where T : class
	{
		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="map">The map this command will be associated with.</param>
		/// <param name="entity">The entity affected by this operation.</param>
		public DataUpdate(DataMap<T> map, T entity)
			: base(map, entity)
		{
		}

		/// <summary>
		/// Returns a new core command that when executed materializes the operation this instance
		/// refers to, or null if that command cannot be generated for any reasons.
		/// </summary>
		/// <returns>A new core command, or null.</returns>
		public IUpdateCommand GenerateCoreCommand()
		{
			return IsDisposed ? null : Map.GenerateUpdateCommand(Entity);
		}
		ICommand ICoreCommandProvider.GenerateCoreCommand()
		{
			return this.GenerateCoreCommand();
		}

		/// <summary>
		/// Invoked to execute the operation this instance refers to.
		/// </summary>
		internal void OnExecute()
		{
			Repository.DoSave(Entity);
		}
		void IUberOperation.OnExecute()
		{
			this.OnExecute();
		}
	}

	// ====================================================
	internal static partial class Uber
	{
		/// <summary>
		/// Returns an ad-hoc record containing the changes experimented by the entity since the
		/// last time its internal record was captured, or null if no changes can be detected.
		/// </summary>
		internal static IRecord GetRecordChanges(this MetaEntity meta)
		{
			object obj = meta.Entity; if (obj == null) return null;
			if (meta.UberMap == null) return null;
			if (meta.Record == null) return null;

			var current = new Core.Concrete.Record(meta.UberMap.Schema);
			meta.UberMap.WriteRecord(obj, current);

			var changes = current.Changes(meta.Record); current.Dispose();
			return changes;
		}

		/// <summary>
		/// Returns whether the entity has experimented any changes since the last time its
		/// internal record was captured, or not.
		/// </summary>
		internal static bool HasRecordChanges(this MetaEntity meta)
		{
			var changes = meta.GetRecordChanges();
			bool r = changes != null;

			if (changes != null) changes.Dispose(disposeSchema: true);
			return r;
		}

		/// <summary>
		/// Generates an update core command for the given entity, or returns null if such
		/// command cannot be generated for whatever reasons.
		/// </summary>
		internal static IUpdateCommand GenerateUpdateCommand(this IUberMap map, object entity)
		{
			if (entity == null) return null;
			if (map == null || map.IsDisposed || !map.IsValidated) return null;

			IUpdateCommand cmd = null;

			MetaEntity meta = MetaEntity.Locate(entity, create: true); if (meta.Record == null)
			{
				var record = new Core.Concrete.Record(map.Schema);
				map.WriteRecord(entity, record);
				meta.Record = record;
			}

			var changes = meta.GetRecordChanges(); if (changes == null) return null;

			var num = changes.Schema.Count(x => !x.IsReadOnlyColumn);
			if (num != 0)
			{
				var id = map.ExtractId(meta.Record); if (id != null)
				{
					cmd = map.Link.Engine.CreateUpdateCommand(map.Link, x => map.Table);
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

					for (int i = 0; i < changes.Count; i++)
					{
						if (changes.Schema[i].IsReadOnlyColumn) continue;

						var node = new DynamicNode.SetMember(tag, changes.Schema[i].ColumnName, changes[i]);
						cmd.Columns(x => node);
						node.Dispose();
					}

					tag.Dispose();
					id.Dispose();
				}
			}
			changes.Dispose(disposeSchema: true);

			return cmd;
		}

		/// <summary>
		/// Returns wheter the given child entity requires an update.
		/// </summary>
		internal static bool DoesChildNeedUpdate(this MetaEntity child, bool cascade)
		{
			if (child.HasRecordChanges()) return true;

			var list = child.GetRemovedChilds();
			var need = list.Count != 0;
			list.Clear(); list = null; if (need) return true;

			if (cascade)
			{
				list = child.GetDependencies(MemberDependencyMode.Child); foreach (var obj in list)
				{
					if (obj == null) continue;
					var meta = MetaEntity.Locate(obj);
					if (meta.DoesChildNeedUpdate(cascade: false)) { need = true; break; }
				}
				list.Clear(); list = null;
				if (need) return true;
			}

			return false;
		}
	}
}
