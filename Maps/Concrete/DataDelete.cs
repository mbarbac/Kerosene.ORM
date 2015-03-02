// ======================================================== DataDelete.cs
namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.ORM.Core;
	using Kerosene.ORM.Core.Concrete;
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
	public class DataDelete<T> : MetaOperation<T>, IDataDelete<T>, ICoreCommandProvider where T : class
	{
		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="map">The map this command will be associated with.</param>
		/// <param name="entity">The entity affected by this operation.</param>
		public DataDelete(DataMap<T> map, T entity) : base(map, entity) { }

		/// <summary>
		/// Generates a new command that when executed implements the operation this instance
		/// refers to, or null if the state of this instance impedes such core command to be
		/// create
		/// </summary>
		/// <returns>A new command, or null.</returns>
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

			// Removed childs are deleted...
			list = MetaEntity.DetectRemovedChilds(); foreach (var obj in list)
			{
				if (obj == null) continue;
				var objType = obj.GetType(); if (!objType.IsClass) continue;
				var objMeta = MetaEntity.Locate(obj);
				var objMap = objMeta.Map ?? Repository.RetrieveUberMap(objType);

				if (objMap == null) continue;
				if (!object.ReferenceEquals(Repository, objMap.Repository))
					throw new InvalidOperationException(
						"Entity '{0}' is not managed by repository '{1}'."
						.FormatWith(obj.Sketch(), Repository));

				if (objMeta.Operation != null)
				{
					if (!(objMeta.Operation is IDataDelete))
					{
						objMeta.Operation.Dispose();
						objMap.Delete(obj).Submit();
					}
				}
				else objMap.Delete(obj).Submit();
			}
			list.Clear(); list = null;

			// Child dependencies are deleted...
			list = MetaEntity.GetDependencies(MemberDependencyMode.Child); foreach (var obj in list)
			{
				if (obj == null) continue;
				var objType = obj.GetType(); if (!objType.IsClass) continue;
				var objMeta = MetaEntity.Locate(obj);
				var objMap = objMeta.Map ?? Repository.RetrieveUberMap(objType);

				if (objMap == null) continue;
				if (!object.ReferenceEquals(Repository, objMap.Repository))
					throw new InvalidOperationException(
						"Entity '{0}' is not managed by repository '{1}'."
						.FormatWith(obj.Sketch(), Repository));

				switch (objMeta.State)
				{
					case MetaState.Detached:
						// Is safe to delete even if it does not exist in the DB...
						objMap.Delete(obj).Submit();
						break;

					case MetaState.Ready:
						objMap.Delete(obj).Submit();
						break;

					case MetaState.ToInsert:
					case MetaState.ToUpdate:
						objMeta.Operation.Dispose();
						objMap.Delete(obj).Submit();
						break;
				}
			}
			list.Clear(); list = null;

			// Reordering...
			Repository.UberOperations.Remove(this);
			Repository.UberOperations.Add(this);

			// Parent dependencies are just signalled...
			list = MetaEntity.GetDependencies(MemberDependencyMode.Parent); foreach (var obj in list)
			{
				if (obj == null) continue;
				var objType = obj.GetType(); if (!objType.IsClass) continue;
				var objMeta = MetaEntity.Locate(obj);

				objMeta.ToRefresh = true;
			}
			list.Clear(); list = null;
		}

		/// <summary>
		/// Invoked when executing this operation against the database.
		/// </summary>
		protected override void OnExecute()
		{
			var cmd = GenerateCoreCommand(); try
			{
				if (cmd != null)
				{
					MetaEntity.ValidateRowVersion();
					cmd.Execute();
				}
			}
			finally { if (cmd != null) cmd.Dispose(); }
		}
	}
}
// ======================================================== 
