// ======================================================== DataDeleteT.cs
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
	/// Represents a delete operation for its associated entity.
	/// </summary>
	public class DataDelete<T> : MetaOperation<T>, IDataDelete<T> where T : class
	{
		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="map">The map this command will be associated with.</param>
		/// <param name="entity">The entity affected by this operation.</param>
		public DataDelete(DataMap<T> map, T entity) : base(map, entity) { }

		/// <summary>
		/// Returns a new core command that when executed materializes the operation this instance
		/// refers to, or null if that command cannot be generated for any reasons.
		/// </summary>
		/// <returns>A new core command, or null.</returns>
		internal IDeleteCommand GenerateCoreCommand()
		{
			return IsDisposed ? null : Map.GenerateDeleteCommand(Entity);
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
						objMap.Delete(obj).Submit(); // Safe to delete even if it is not in the database...
						break;

					case MetaState.Ready:
						objMap.Delete(obj).Submit();
						break;

					case MetaState.ToInsert:
					case MetaState.ToUpdate:
						objMeta.UberOperation.Dispose();
						objMap.Delete(obj).Submit();
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

			#region Parent dependencies processed as appropriate...
			list = MetaEntity.GetDependencies(MemberDependencyMode.Parent); foreach (var obj in list)
			{
				if (obj == null) continue;
				var objType = obj.GetType(); if (!objType.IsClass) continue;
				var objMeta = MetaEntity.Locate(obj);

				objMeta.ToRefresh = true; // Parent dependencies just need to be signalled...
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
					int n = cmd.Execute();
					if (n == 0) { }
				}
			}
			finally { if (cmd != null) cmd.Dispose(); }
		}
	}
}
// ======================================================== 
