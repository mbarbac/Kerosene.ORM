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
	public class DataUpdate<T> : DataSave<T>, IDataUpdate<T> where T : class
	{
		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="map">The map this command will be associated with.</param>
		/// <param name="entity">The entity affected by this operation.</param>
		public DataUpdate(DataMap<T> map, T entity) : base(map, entity) { }

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
	}
}
