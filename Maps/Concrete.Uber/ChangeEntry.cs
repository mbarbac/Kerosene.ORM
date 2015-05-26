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
	internal enum ChangeType
	{
		Refresh, Delete, Insert, Update
	}

	// ====================================================
	internal class ChangeEntry
	{
		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		internal ChangeEntry(ChangeType changeType, IUberMap map, object entity)
		{
			UberMap = map;
			ChangeType = changeType;
			Entity = entity;
			MetaEntity = MetaEntity.Locate(entity);
		}

		/// <summary>
		/// Disposes this instance.
		/// </summary>
		internal void Dispose()
		{
			Entity = null;
			MetaEntity = null;
			UberMap = null;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			var str = string.Format("{0}({1})", ChangeType, MetaEntity == null ? string.Empty : MetaEntity.ToString());
			return str;
		}

		/// <summary>
		/// The type of change this entry represents.
		/// </summary>
		internal ChangeType ChangeType { get; private set; }

		/// <summary>
		/// The entity associated with this entry.
		/// </summary>
		internal object Entity { get; private set; }

		/// <summary>
		/// The entity associated with this entry.
		/// </summary>
		internal MetaEntity MetaEntity { get; private set; }

		/// <summary>
		/// The map associated with this entry.
		/// </summary>
		internal IUberMap UberMap { get; private set; }

		/// <summary>
		/// The repository associated with this entry.
		/// </summary>
		internal DataRepository DataRepository
		{
			get { return UberMap == null ? null : UberMap.Repository; }
		}
	}
}
