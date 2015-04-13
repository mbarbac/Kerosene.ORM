// ======================================================== UberHelper.Operations.cs
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
	internal static partial class UberHelper
	{
		/// <summary>
		/// Generates an insert command for the given entity, or returns null if such command
		/// cannot be generated.
		/// </summary>
		internal static IInsertCommand GenerateInsertCommand(this IUberMap map, object entity)
		{
			if (entity == null) return null;
			if (map == null || map.IsDisposed || !map.IsValidated) return null;

			IInsertCommand cmd = null;

			int num = map.Schema.Count(x => !x.IsReadOnlyColumn);
			if (num != 0)
			{
				cmd = map.Link.Engine.CreateInsertCommand(map.Link, x => map.Table);

				var tag = new DynamicNode.Argument("x");
				var rec = new Core.Concrete.Record(map.Schema); map.WriteRecord(entity, rec);

				for (int i = 0; i < rec.Count; i++)
				{
					if (rec.Schema[i].IsReadOnlyColumn) continue;

					var node = new DynamicNode.SetMember(tag, rec.Schema[i].ColumnName, rec[i]);
					cmd.Columns(x => node);
					node.Dispose();
				}

				tag.Dispose();
				rec.Dispose();
			}

			return cmd;
		}

		/// <summary>
		/// Generates a delete command for the given entity, or returns null if such command
		/// cannot be generated.
		/// </summary>
		internal static IDeleteCommand GenerateDeleteCommand(this IUberMap map, object entity)
		{
			if (entity == null) return null;
			if (map == null || map.IsDisposed || !map.IsValidated) return null;

			IDeleteCommand cmd = null;

			var meta = MetaEntity.Locate(entity, create: true);
			if (meta.Record == null)
			{
				meta.Record = new Core.Concrete.Record(map.Schema);
				map.WriteRecord(entity, meta.Record);
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

		/// <summary>
		/// Generates an update command for the given entity, or returns null if such command
		/// cannot be generated.
		/// </summary>
		internal static IUpdateCommand GenerateUpdateCommand(this IUberMap map, object entity)
		{
			if (entity == null) return null;
			if (map == null || map.IsDisposed || !map.IsValidated) return null;

			IUpdateCommand cmd = null;

			var meta = MetaEntity.Locate(entity, create: true);
			var changes = meta.GetRecordChanges(); if (changes == null) return null;

			var num = changes == null ? 0 : changes.Schema.Count(x => !x.IsReadOnlyColumn);
			if (num != 0)
			{
				var id = map.ExtractId(meta.Record);
				if (id != null)
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

			if (changes != null) changes.Dispose(disposeSchema: true);
			return cmd;
		}

		/// <summary>
		/// Returns an ad-hoc record containing the changes experimented by the entity since the
		/// last time its internal record was captured, or null if no changes can be detected.
		/// </summary>
		internal static IRecord GetRecordChanges(this MetaEntity meta)
		{
			object obj = meta.Entity; if (obj == null) return null;
			if (meta.UberMap == null) return null;
			if (meta.Record == null) return null;

			var current = new Core.Concrete.Record(meta.UberMap.Schema); meta.UberMap.WriteRecord(obj, current);
			var changes = current.Changes(meta.Record);

			current.Dispose();
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
	}
}
// ======================================================== 
