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
	internal class UberOperationCollection
	{
		List<IUberOperation> _Items = new List<IUberOperation>();

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		internal UberOperationCollection() { }

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the string representation of this instance.</returns>
		public override string ToString()
		{
			return string.Format("{0}({1})", GetType().EasyName(), Count);
		}

		/// <summary>
		/// The items contained in this collection
		/// </summary>
		internal IEnumerable<IUberOperation> Items
		{
			get { return _Items; }
		}

		/// <summary>
		/// The number of elements in this collection.
		/// </summary>
		public int Count
		{
			get { return _Items.Count; }
		}

		/// <summary>
		/// Adds the given element into this collection.
		/// </summary>
		internal void Add(IUberOperation item)
		{
			_Items.Add(item);
		}

		/// <summary>
		/// Removes the given element from this collection.
		/// </summary>
		internal bool Remove(IUberOperation item)
		{
			return _Items.Remove(item);
		}

		/// <summary>
		/// Clears this collection by removing all the elements it contains.
		/// </summary>
		internal void Clear()
		{
			_Items.Clear();
		}

		/// <summary>
		/// Returns a new array containing the elements of this collection.
		/// </summary>
		internal IUberOperation[] ToArray()
		{
			return _Items.ToArray();
		}

		/// <summary>
		/// Returns whether this collection contains the given item.
		/// </summary>
		internal bool Contains(IUberOperation op)
		{
			return op == null ? false : _Items.Contains(op);
		}

		/// <summary>
		/// Returns the first instance associated with the given entity.
		/// </summary>
		internal IUberOperation FindEntity(object entity)
		{
			return _Items.Find(x => object.ReferenceEquals(x.Entity, entity));
		}

		/// <summary>
		/// Returns the first instance associated with the given entity.
		/// </summary>
		internal List<IUberOperation> FindAllEntity(object entity)
		{
			return _Items.FindAll(x => object.ReferenceEquals(x.Entity, entity));
		}

		/// <summary>
		/// Returns the first instance associated with the given meta entity.
		/// </summary>
		internal IUberOperation FindMeta(MetaEntity meta)
		{
			return _Items.Find(x => object.ReferenceEquals(x.MetaEntity, meta));
		}

		/// <summary>
		/// Returns the first instance associated with the given meta entity.
		/// </summary>
		internal IUberOperation FindLastMeta(MetaEntity meta)
		{
			return _Items.FindLast(x => object.ReferenceEquals(x.MetaEntity, meta));
		}

		/// <summary>
		/// Returns the first instance associated with the given meta entity.
		/// </summary>
		internal List<IUberOperation> FindAllMeta(MetaEntity meta)
		{
			return _Items.FindAll(x => object.ReferenceEquals(x.MetaEntity, meta));
		}
	}
}
