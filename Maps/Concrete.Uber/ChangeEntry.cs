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
	internal enum ChangeEntryType
	{
		ToRefresh, ToInsert, ToUpdate, ToDelete
	}

	// ====================================================
	internal class ChangeEntry
	{
		internal ChangeEntry(ChangeEntryType entryType, object entity)
		{
			EntryType = entryType;
			Entity = entity;
			MetaEntity = MetaEntity.Locate(Entity);
			Executed = false;
		}

		internal void Dispose()
		{
			Entity = null;
			MetaEntity = null;
		}

		public override string ToString()
		{
			return string.Format("{0}({1})", EntryType, MetaEntity);
		}

		internal object Entity { get; private set; }

		internal MetaEntity MetaEntity { get; private set; }

		internal ChangeEntryType EntryType { get; private set; }

		internal bool Executed { get; set; }
	}
}
