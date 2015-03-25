// ======================================================== DataUpdateT.cs
namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Represents an insert operation for its associated entity.
	/// </summary>
	public class DataUpdate<T> : MetaOperation<T>, IDataUpdate<T> where T : class
	{
		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="map">The map this command will be associated with.</param>
		/// <param name="entity">The entity affected by this operation.</param>
		public DataUpdate(DataMap<T> map, T entity) : base(map, entity) { }

		/// <summary>
		/// Returns a new core command that when executed materializes the operation this instance
		/// refers to, or null if that command cannot be generated for any reasons.
		/// </summary>
		/// <returns>A new core command, or null.</returns>
		internal IUpdateCommand GenerateCoreCommand()
		{
			return IsDisposed ? null : Map.GenerateUpdateCommand(Entity);
		}
		ICommand ICoreCommandProvider.GenerateCoreCommand()
		{
			return this.GenerateCoreCommand();
		}

		/// <summary>
		/// Invoked when submitting this operation.
		/// </summary>
		protected override void OnSubmit()
		{
			List<object> list = null;

			#region Parent dependencies processed as appropriate...
			list = MetaEntity.GetDependencies(MemberDependencyMode.Parent); foreach (var obj in list)
			{
				if (obj == null) continue;
				var objType = obj.GetType(); if (!objType.IsClass) continue;
				var objMeta = MetaEntity.Locate(obj);
				var objMap = objMeta.Map ?? Repository.RetrieveMap(objType);

				if (objMap == null) continue;
				if (!object.ReferenceEquals(Repository, objMap.Repository))
					throw new InvalidOperationException(
						"Entity '{0}' is not managed by repository '{1}'."
						.FormatWith(objMeta, Repository));

				switch (objMeta.State)
				{
					case MetaState.Detached:
						objMap.Insert(obj).Submit();
						break;

					case MetaState.Ready:
						if (objMeta.UpdateNeeded()) objMap.Update(obj).Submit();
						objMeta.ToRefresh = true;
						break;

					case MetaState.ToDelete:
						objMeta.UberOperation.Dispose();
						objMeta.ToRefresh = true;
						break;

					default:
						objMeta.ToRefresh = true;
						break;
				}
			}
			list.Clear(); list = null;
			#endregion

			#region Reordering...
			Repository.UberOperations.Remove(this);
			Repository.UberOperations.Add(this);
			#endregion

			#region Delete removed childs...
			list = MetaEntity.DetectRemovedChilds(); foreach (var obj in list)
			{
				if (obj == null) continue;
				var objType = obj.GetType(); if (!objType.IsClass) continue;
				var objMeta = MetaEntity.Locate(obj);
				var objMap = objMeta.Map ?? Repository.RetrieveMap(objType);

				if (objMap == null) continue;
				if (!object.ReferenceEquals(Repository, objMap.Repository))
					throw new InvalidOperationException(
						"Entity '{0}' is not managed by repository '{1}'."
						.FormatWith(objMeta, Repository));

				if (objMeta.UberOperation != null)
				{
					if (!(objMeta.UberOperation is IDataDelete))
					{
						objMeta.UberOperation.Dispose();
						objMap.Delete(obj).Submit();
					}
				}
				else objMap.Delete(obj).Submit();
			}
			list.Clear(); list = null;
			#endregion

			#region Child dependencies processed as appropriate...
			list = MetaEntity.GetDependencies(MemberDependencyMode.Child); foreach (var obj in list)
			{
				if (obj == null) continue;
				var objType = obj.GetType(); if (!objType.IsClass) continue;
				var objMeta = MetaEntity.Locate(obj);
				var objMap = objMeta.Map ?? Repository.RetrieveMap(objType);

				if (objMap == null) continue;
				if (!object.ReferenceEquals(Repository, objMap.Repository))
					throw new InvalidOperationException(
						"Entity '{0}' is not managed by repository '{1}'."
						.FormatWith(objMeta, Repository));

				switch (objMeta.State)
				{
					case MetaState.Detached:
						objMap.Insert(obj).Submit();
						break;

					case MetaState.Ready:
						if (objMeta.UpdateNeeded()) objMap.Update(obj).Submit();
						objMeta.ToRefresh = true;
						break;

					case MetaState.ToDelete:
						objMeta.UberOperation.Dispose();
						objMap.Update(obj).Submit();
						break;

					default:
						objMeta.ToRefresh = true;
						break;
				}
			}
			list.Clear(); list = null;
			#endregion
		}

		/// <summary>
		/// Invoked when all the pending operations are processed to execute this operation
		/// against the underlying database.
		/// </summary>
		protected override void Execute()
		{
			var cmd = GenerateCoreCommand(); try
			{
				if (cmd != null)
				{
					MetaEntity.ValidateRowVersion();

					var rec = (IRecord)cmd.First(); if (rec != null)
					{
						var map = MetaEntity.UberMap;

						map.UberEntities.Remove(MetaEntity);
						MetaEntity.Record = rec;
						MetaEntity.UberMap.LoadEntity(rec, MetaEntity.Entity);
						MetaEntity.ToRefresh = true;
						map.UberEntities.Add(MetaEntity);
					}
					else throw new DatabaseException(
						"Error executing '{0}'."
						.FormatWith(cmd.TraceString()));
				}
			}
			finally { if (cmd != null) cmd.Dispose(); }
		}
	}
}
// ======================================================== 
