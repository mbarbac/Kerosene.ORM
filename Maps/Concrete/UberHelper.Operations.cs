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
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Internal helpers and extensions.
	/// </summary>
	internal static partial class UberHelper
	{
		/// <summary>
		/// Returns either an ad-hoc schema containing the changes detected from the last record
		/// captured, or null if no changes were detected.
		/// </summary>
		internal static IRecord DetectRecordChanges(this MetaEntity meta)
		{
			object obj = meta.Entity; if (obj == null) return null;
			if (meta.Map == null) return null;
			if (meta.Record == null) return null;

			var current = new Core.Concrete.Record(meta.Map.Schema);
			meta.Map.WriteRecord(obj, current);

			var changes = current.Changes(meta.Record);
			current.Dispose();
			return changes;
		}

		/// <summary>
		/// Returns whether there are changes detected from the last record stored in the meta
		/// entity.
		/// </summary>
		internal static bool HasRecordChanges(this MetaEntity meta)
		{
			var changes = meta.DetectRecordChanges();
			bool r = changes != null;

			if (changes != null) changes.Dispose(disposeSchema: true);
			return r;
		}

		/// <summary>
		/// Returns a list with the child dependencies that have been removed since the last time
		/// thos dependencies were captured.
		/// </summary>
		internal static List<object> DetectRemovedChilds(this MetaEntity meta)
		{
			var list = new List<object>();
			var obj = meta.Entity; if (obj == null) return list;
			var type = obj.GetType();

			foreach (var kvp in meta.MemberChilds)
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
		/// Returns an indication on whether the state of given entity requires it to be updated
		/// or not.
		/// </summary>
		internal static bool UpdateNeeded(this MetaEntity meta, bool verifyDependencies = true)
		{
			if (meta.HasRecordChanges()) return true;

			var list = meta.DetectRemovedChilds();
			var need = list.Count != 0;
			list.Clear(); list = null;
			if (need) return true;

			if (verifyDependencies)
			{
				list = meta.DetectRemovedChilds();
				need = list.Count != 0;
				list.Clear(); list = null;
				if (need) return true;

				list = meta.GetDependencies(MemberDependencyMode.Child); foreach (var obj in list)
				{
					if (obj == null) continue;
					var objType = obj.GetType(); if (!objType.IsClass) continue;
					var objMeta = MetaEntity.Locate(obj);

					if (objMeta.State == MetaState.Detached) { need = true; break; }
					if (objMeta.State == MetaState.Ready)
					{
						if (objMeta.UpdateNeeded(verifyDependencies: false)) { need = true; break; }
					}
				}
				list.Clear(); list = null;
				if (need) return true;
			}

			return false;
		}

		/// <summary>
		/// Returns a list containing the dependencies of the given entity whose mode match the
		/// mode given.
		/// </summary>
		internal static List<object> GetDependencies(this MetaEntity meta, MemberDependencyMode mode)
		{
			var list = new List<object>();
			var obj = meta.Entity; if (obj == null) return list;

			foreach (var member in ((IEnumerable<IUberMember>)meta.Map.Members))
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
		/// Generates an update command for the given entity, or returns null if such command
		/// cannot be generated.
		/// </summary>
		internal static IUpdateCommand GenerateUpdateCommand(this IUberMap map, object entity)
		{
			if (entity == null) return null;
			if (map == null || map.IsDisposed) return null;

			IUpdateCommand cmd = null;

			var meta = MetaEntity.Locate(entity, create: true);
			var changes = meta.DetectRecordChanges();
			
			var num = changes == null ? 0 : changes.Schema.Count(x => !x.IsReadOnlyColumn);
			if (num != 0)
			{
				cmd = map.Link.Engine.CreateUpdateCommand(map.Link, x => map.Table);

				var tag = new DynamicNode.Argument("x");
				var rec = new Core.Concrete.Record(map.Schema); map.WriteRecord(entity, rec);
				var id = map.ExtractId(rec);

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

				id.Dispose();
				rec.Dispose();
				tag.Dispose();
			}

			if (changes != null) changes.Dispose(disposeSchema: true);
			return cmd;
		}

		/// <summary>
		/// Generates a delete command for the given entity, or returns null if such command
		/// cannot be generated.
		/// </summary>
		internal static IDeleteCommand GenerateDeleteCommand(this IUberMap map, object entity)
		{
			if (entity == null) return null;
			if (map == null || map.IsDisposed) return null;

			var cmd = map.Link.Engine.CreateDeleteCommand(map.Link, x => map.Table);
			if (map.Discriminator != null) cmd.Where(map.Discriminator);

			var tag = new DynamicNode.Argument("x");
			var rec = new Core.Concrete.Record(map.Schema); map.WriteRecord(entity, rec);
			var id = map.ExtractId(rec);

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
			rec.Dispose();

			return cmd;
		}

		/// <summary>
		/// Generates an insert command for the given entity, or returns null if such command
		/// cannot be generated.
		/// </summary>
		internal static IInsertCommand GenerateInsertCommand(this IUberMap map, object entity)
		{
			if (entity == null) return null;
			if (map == null || map.IsDisposed) return null;

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
		/// Validates that the row version captured in the meta-entity is the same as the current
		/// one in the database, throwing an exception if a change is detected.
		/// </summary>
		internal static void ValidateRowVersion(this MetaEntity meta)
		{
			var obj = meta.Entity; if (obj == null) return;
			if (meta.Map == null) return;
			if (meta.Record == null) return;

			var vc = meta.Map.VersionColumn;
			if (vc.Name == null) return;

			// Getting the most updated record, if any...
			var cmd = meta.Map.Link.From(x => meta.Map.Table);
			var tag = new DynamicNode.Argument("x");
			var id = meta.Map.ExtractId(meta.Record);

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
					"Captured version for entity '{0}': '{1}' if not the same as current one in the database: '{2}'."
					.FormatWith(meta, captured, current));
		}
	}
}
// ======================================================== 
