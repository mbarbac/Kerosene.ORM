// ======================================================== DataUpdate.cs
namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents an update operation for its associated entity.
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
	}
}
// ======================================================== 
