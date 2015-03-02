// ======================================================== DataInsert.cs
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
	/// Represents an insert operation for its associated entity.
	/// </summary>
	public class DataInsert<T> : MetaOperation<T>, IDataInsert<T>, ICoreCommandProvider where T : class
	{
		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="map">The map this command will be associated with.</param>
		/// <param name="entity">The entity affected by this operation.</param>
		public DataInsert(DataMap<T> map, T entity) : base(map, entity) { }

		/// <summary>
		/// Generates a new command that when executed implements the operation this instance
		/// refers to, or null if the state of this instance impedes such core command to be
		/// create
		/// </summary>
		/// <returns>A new command, or null.</returns>
		internal IInsertCommand GenerateCoreCommand()
		{
			return IsDisposed ? null : Map.GenerateInsertCommand(Entity);
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

			// Parent dependencies are inserted or updated as appropriate...
			list = MetaEntity.GetDependencies(MemberDependencyMode.Parent); foreach (var obj in list)
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
						objMap.Insert(obj).Submit();
						break;

					case MetaState.Ready:
						if (objMeta.UpdateNeeded()) objMap.Update(obj).Submit();
						objMeta.ToRefresh = true;
						break;

					case MetaState.ToDelete:
						objMeta.Operation.Dispose();
						objMeta.ToRefresh = true;
						break;

					default:
						objMeta.ToRefresh = true;
						break;
				}
			}
			list.Clear(); list = null;

			// Reordering...
			Repository.UberOperations.Remove(this);
			Repository.UberOperations.Add(this);

			// Child dependencies are inserted or updated as appropriate...
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
						objMap.Insert(obj).Submit();
						break;

					case MetaState.Ready:
						if (objMeta.UpdateNeeded()) objMap.Update(obj).Submit();
						objMeta.ToRefresh = true;
						break;

					default:
						objMeta.ToRefresh = true;
						break;
				}
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
					var rec = (IRecord)cmd.First(); if (rec != null)
					{
						MetaEntity.SetRecord(rec, disposeOld: true);
						MetaEntity.Map.LoadEntity(rec, MetaEntity.Entity);
						MetaEntity.ToRefresh = true;
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
