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
	/// Represents a save (insert or update) operation for its associated entity.
	/// </summary>
	public class DataSave<T> : MetaOperation<T>, IDataSave<T>, IUberOperation where T : class
	{
		IEnumerableCommand _Command = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="map">The map this command will be associated with.</param>
		/// <param name="entity">The entity affected by this operation.</param>
		public DataSave(DataMap<T> map, T entity) : base(map, entity) { }

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		protected override void OnDispose(bool disposing)
		{
			if (_Command != null) _Command.Dispose(); _Command = null;

			base.OnDispose(disposing);
		}

		/// <summary>
		/// Returns a new core command that when executed materializes the operation this instance
		/// refers to, or null if that command cannot be generated for any reasons.
		/// </summary>
		/// <returns>A new core command, or null.</returns>
		public IEnumerableCommand GenerateCoreCommand()
		{
			if (IsDisposed) return null;

			return MetaEntity.Map == null
				? (IEnumerableCommand)Map.GenerateInsertCommand(Entity)
				: (IEnumerableCommand)Map.GenerateUpdateCommand(Entity);
		}
		ICommand ICoreCommandProvider.GenerateCoreCommand()
		{
			return this.GenerateCoreCommand();
		}

		/// <summary>
		/// Invoked to execute this operation.
		/// </summary>
		internal void OnExecute(object origin = null)
		{
			lock (Repository.MasterLock)
			{
				List<object> list = null;
				ChangeEntry change = null;
				ChangeType changeType = MetaEntity.UberMap == null ? ChangeType.Insert : ChangeType.Update;
				IEnumerableCommand cmd = null;
				IRecord rec = null;

				DebugEx.IndentWriteLine("\n- Preparing '{1}({0})'...", MetaEntity, changeType);
				try
				{
					if (changeType == ChangeType.Update)
					{
						list = MetaEntity.GetRemovedChilds();
						foreach (var obj in list)
						{
							if (obj == null) continue;
							if (object.ReferenceEquals(obj, origin)) continue;
							var type = obj.GetType(); if (!type.IsClass && !type.IsInterface) continue;

							var meta = MetaEntity.Locate(obj);
							var map = meta.UberMap ?? Repository.LocateUberMap(type);
							if (map == null) throw new NotFoundException("Cannot find map for type '{0}'.".FormatWith(type.EasyName()));

							var op = map.Delete(obj);
							try { ((IUberOperation)op).OnExecute(origin: Entity); }
							finally { op.Dispose(); }
						}
						list.Clear(); list = null;
					}

					list = MetaEntity.GetDependencies(Map, MemberDependencyMode.Parent);
					foreach (var obj in list)
					{
						if (obj == null) continue;
						if (object.ReferenceEquals(obj, origin)) continue;
						var type = obj.GetType(); if (!type.IsClass && !type.IsInterface) continue;

						var meta = MetaEntity.Locate(obj);
						var map = meta.UberMap ?? Repository.LocateUberMap(type);
						if (map == null) throw new NotFoundException("Cannot find map for type '{0}'.".FormatWith(type.EasyName()));

						if (meta.DoesMetaNeedSave(true, origin: Entity))
						{
							var op = map.Save(obj);
							try { ((IUberOperation)op).OnExecute(origin: Entity); }
							finally { op.Dispose(); }
						}
						else
						{
							change = new ChangeEntry(ChangeType.Refresh, map, obj);
							Repository.ChangeEntries.Add(change);
						}
					}

					if (changeType == ChangeType.Insert)
					{
						cmd = Map.GenerateInsertCommand(Entity);
						if (cmd == null) throw new CannotCreateException("Cannot create an insert command for '{0}'.".FormatWith(MetaEntity));

						DebugEx.IndentWriteLine("\n- Executing '{0}'...", cmd);
						try
						{
							rec = (IRecord)cmd.First();
							if (rec == null) throw new CannotExecuteException("Cannot execute '{0}'.".FormatWith(cmd));
						}
						finally { DebugEx.Unindent(); }

						MetaEntity.Record = rec;
						Map.LoadEntity(rec, Entity);
						MetaEntity.UberMap = Map; if (Map.TrackEntities) Map.MetaEntities.Add(MetaEntity);

						change = new ChangeEntry(ChangeType.Insert, Map, Entity);
						Repository.ChangeEntries.Add(change);
					}
					if (changeType == ChangeType.Update)
					{
						cmd = Map.GenerateUpdateCommand(Entity); if (cmd != null)
						{
							DebugEx.IndentWriteLine("\n- Executing '{0}'...", cmd);
							try
							{
								MetaEntity.ValidateRowVersion();
								rec = (IRecord)cmd.First();
								if (rec == null) throw new CannotExecuteException("Cannot execute '{0}'.".FormatWith(cmd));
							}
							finally { DebugEx.Unindent(); }

							MetaEntity.Record = rec;
							Map.LoadEntity(rec, Entity);

							change = new ChangeEntry(ChangeType.Update, Map, Entity);
							Repository.ChangeEntries.Add(change);
						}
						else
						{
							change = new ChangeEntry(ChangeType.Refresh, Map, Entity);
							Repository.ChangeEntries.Add(change);
						}
					}

					list = MetaEntity.GetDependencies(Map, MemberDependencyMode.Child);
					foreach (var obj in list)
					{
						if (obj == null) continue;
						if (object.ReferenceEquals(obj, origin)) continue;
						var type = obj.GetType(); if (!type.IsClass && !type.IsInterface) continue;

						var meta = MetaEntity.Locate(obj);
						var map = meta.UberMap ?? Repository.LocateUberMap(type);
						if (map == null) throw new NotFoundException("Cannot find map for type '{0}'.".FormatWith(type.EasyName()));

						if (meta.DoesMetaNeedSave(cascade: true))
						{
							var op = map.Save(obj);
							try { ((IUberOperation)op).OnExecute(origin: Entity); }
							finally { op.Dispose(); }
						}
						else
						{
							change = new ChangeEntry(ChangeType.Refresh, map, obj);
							Repository.ChangeEntries.Add(change);
						}
					}
					list.Clear(); list = null;
				}
				finally
				{
					if (cmd != null) cmd.Dispose(); cmd = null;
					if (list != null) list.Clear(); list = null;
					DebugEx.Unindent();
				}
			}
		}
		void IUberOperation.OnExecute(object origin)
		{
			this.OnExecute(origin);
		}
	}

	// ====================================================
	internal static partial class Uber
	{
		/// <summary>
		/// Returns whether this child entity requires a save operation.
		/// </summary>
		internal static bool DoesMetaNeedSave(this MetaEntity child, bool cascade, object origin = null)
		{
			if (child.Map == null) return true;
			if (child.HasRecordChanges()) return true;

			var list = child.GetRemovedChilds();
			var need = list.Count != 0;
			list.Clear(); list = null;
			if (need) return true;

			if (cascade)
			{
				list = child.GetDependencies(child.UberMap, MemberDependencyMode.Child);
				foreach (var obj in list)
				{
					if (obj == null) continue;
					var type = obj.GetType(); if (!type.IsClass && !type.IsInterface) continue;
					if (object.ReferenceEquals(origin, obj)) continue;

					var meta = MetaEntity.Locate(obj);
					var temp = meta.UberMap ?? child.UberMap.Repository.LocateUberMap(type);
					if (temp == null) throw new NotFoundException("Cannot find map for type '{0}'.".FormatWith(type.EasyName()));

					if (meta.DoesMetaNeedSave(cascade: false)) { need = true; break; }
				}
				list.Clear(); list = null;
				if (need) return true;
			}

			return false;
		}
	}
}
