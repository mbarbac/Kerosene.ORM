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
	/// Represents an insert operation for its associated entity.
	/// </summary>
	public class DataInsert<T> : DataSave<T>, IDataInsert<T> where T : class
	{
		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="map">The map this command will be associated with.</param>
		/// <param name="entity">The entity affected by this operation.</param>
		public DataInsert(DataMap<T> map, T entity) : base(map, entity) { }

		/// <summary>
		/// Submits this operation so that it will be executed, along with all other pending
		/// change operations on its associated repository, when it executes then all against
		/// the underlying database as a single logic unit.
		/// </summary>
		public override void Submit()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (MetaEntity.UberMap != null) throw new NotOrphanException(
				"Entity '{0}' is already attached to map '{1}'.".FormatWith(MetaEntity, MetaEntity.UberMap));

			base.Submit();
		}
	}

	// ====================================================
	internal static partial class Uber
	{
		/// <summary>
		/// Generates an insert core command for the given entity, or returns null if such
		/// command cannot be generated for whatever reasons.
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
	}
}
