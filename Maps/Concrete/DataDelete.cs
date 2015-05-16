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
		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="map">The map this command will be associated with.</param>
		/// <param name="entity">The entity affected by this operation.</param>
		public DataDelete(DataMap<T> map, T entity)
			: base(map, entity)
		{
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
		/// Invoked to execute the operation this instance refers to.
		/// </summary>
		internal void OnExecute()
		{
			Repository.DoDelete(Entity);
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
