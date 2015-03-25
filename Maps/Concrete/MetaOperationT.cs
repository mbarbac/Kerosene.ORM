// ======================================================== MetaOperationT.cs
namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Extends the <see cref="IMetaOperation"/> interface.
	/// </summary>
	internal interface IUberOperation : IUberCommand, IMetaOperation
	{
		/// <summary>
		/// The meta entity of the associated entity, if any.
		/// </summary>
		MetaEntity MetaEntity { get; }

		/// <summary>
		/// Invoked when all the pending operations are processed to execute this operation
		/// against the underlying database.
		/// </summary>
		void Execute();
	}

	// ==================================================== 
	/// <summary>
	/// Represents a change operation associated with a given entity.
	/// </summary>
	public abstract class MetaOperation<T> : MetaCommand<T>, IMetaOperation<T>, IUberOperation where T : class
	{
		T _Entity = null;
		MetaEntity _MetaEntity = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="map">The map this command will be associated with.</param>
		/// <param name="entity">The entity affected by this operation.</param>
		protected MetaOperation(DataMap<T> map, T entity)
			: base(map)
		{
			if (entity == null) throw new ArgumentNullException("entity", "Entity cannot be null.");
			_Entity = entity;
			_MetaEntity = MetaEntity.Locate(_Entity);
		}

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		protected override void OnDispose(bool disposing)
		{
			if (disposing)
			{
				if (Repository != null && !Repository.IsDisposed)
				{
					lock (Repository.UberOperations.SyncRoot)
					{
						Repository.UberOperations.Remove(this);
					}
				}
				if (MetaEntity != null)
				{
					if (object.ReferenceEquals(MetaEntity.UberOperation, this))
						MetaEntity.UberOperation = null;
				}
			}

			_MetaEntity = null;
			_Entity = null;

			base.OnDispose(disposing);
		}

		/// <summary>
		/// Invoked to obtain additional info when tracing an empty command.
		/// </summary>
		protected override string OnTraceStringCommandEmpty()
		{
			return MetaEntity == null ? string.Empty : MetaEntity.ToString();
		}

		/// <summary>
		/// The entity affected by this operation.
		/// </summary>
		public T Entity
		{
			get { return _Entity; }
		}
		object IMetaOperation.Entity
		{
			get { return this.Entity; }
		}

		/// <summary>
		/// The meta entity of the associated entity, if any.
		/// </summary>
		internal MetaEntity MetaEntity
		{
			get { return _MetaEntity; }
		}
		MetaEntity IUberOperation.MetaEntity
		{
			get { return this.MetaEntity; }
		}

		/// <summary>
		/// Whether this operation has been submitted or not.
		/// </summary>
		public bool IsSubmitted
		{
			get
			{
				if (Repository != null && !Repository.IsDisposed)
				{
					return Repository.UberOperations.Contains(this);
				}
				return false;
			}
		}

		/// <summary>
		/// Submits this operation so that it will be executed, along with all other pending
		/// change operations on its associated repository, when it executes then all against
		/// the underlying database as a single logic unit.
		/// </summary>
		public void Submit()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			lock (Repository.UberOperations.SyncRoot)
			{
				if (IsSubmitted) return;
				if (object.ReferenceEquals(MetaEntity.UberOperation, this)) return;

				if (MetaEntity.UberOperation != null) throw new InvalidOperationException(
					"Entity '{0}' already has other change operation '{1}' annotated into it."
					.FormatWith(Entity, MetaEntity.UberOperation));

				if (MetaEntity.UberMap == null) Map.Attach(Entity);
				else
					if (!object.ReferenceEquals(MetaEntity.UberMap, Map))
						throw new InvalidOperationException(
							"Entity '{0}' is not managed by map '{1}'."
							.FormatWith(MetaEntity, Map));

				DebugEx.IndentWriteLine("\n- Submitting '{0}'...".FormatWith(this));

				Repository.UberOperations.Add(this);
				MetaEntity.UberOperation = this;
				MetaEntity.ToRefresh = true;
				OnSubmit();

				DebugEx.Unindent();
			}
		}

		/// <summary>
		/// Invoked when submitting this operation.
		/// </summary>
		protected abstract void OnSubmit();

		/// <summary>
		/// Invoked when all the pending operations are processed to execute this operation
		/// against the underlying database.
		/// </summary>
		protected abstract void Execute();
		void IUberOperation.Execute()
		{
			this.Execute();
		}
	}
}
// ======================================================== 
