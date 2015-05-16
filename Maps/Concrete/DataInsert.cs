using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kerosene.ORM.Maps.Concrete
{
	// ====================================================
	/// <summary>
	/// Represents an insert operation for its associated entity.
	/// </summary>
	public class DataInsert<T> : MetaOperation<T>, IDataInsert<T>, IUberOperation where T : class
	{
		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="map">The map this command will be associated with.</param>
		/// <param name="entity">The entity affected by this operation.</param>
		public DataInsert(DataMap<T> map, T entity)
			: base(map, entity)
		{
		}

		/// <summary>
		/// Returns a new core command that when executed materializes the operation this instance
		/// refers to, or null if that command cannot be generated for any reasons.
		/// </summary>
		/// <returns>A new core command, or null.</returns>
		public IInsertCommand GenerateCoreCommand()
		{
			return IsDisposed ? null : Map.GenerateInsertCommand(Entity);
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
