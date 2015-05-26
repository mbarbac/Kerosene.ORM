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
	internal class UberOperationList : IEnumerable<IUberOperation>
	{
		DataRepository _Master = null;
		List<IUberOperation> _Items = new List<IUberOperation>();

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		internal UberOperationList(DataRepository repo)
		{
			_Master = repo;
		}

		/// <summary>
		/// Disposes this instance.
		/// </summary>
		internal void Dispose()
		{
			if (_Items != null) Clear(); _Items = null;
			_Master = null;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the string representation of this instance.</returns>
		public override string ToString()
		{
			return string.Format("{0}({1})", GetType().EasyName(), Count);
		}

		/// <summary>
		/// Obtains an enumerator for the members of this instance.
		/// </summary>
		public IEnumerator<IUberOperation> GetEnumerator()
		{
			return _Items.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		/// The number of elements in this collection.
		/// </summary>
		public int Count
		{
			get { return _Items == null ? 0 : _Items.Count; }
		}

		/// <summary>
		/// Adds the given element into this collection.
		/// </summary>
		internal void Add(IUberOperation item)
		{
			if (_Items.Contains(item)) throw new DuplicateException("Operation '{0}' is already registered.".FormatWith(item));
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
		/// Returns the first operation associated with the given entity, or null.
		/// </summary>
		internal IUberOperation FindEntity(object entity)
		{
			return _Items.Find(x => object.ReferenceEquals(x.Entity, entity));
		}

		/// <summary>
		/// Returns the last operation associated with the given entity, or null.
		/// </summary>
		internal IUberOperation FindLastEntity(object entity)
		{
			return _Items.FindLast(x => object.ReferenceEquals(x.Entity, entity));
		}

		/// <summary>
		/// Returns all the operations associated with the given entity, or null.
		/// </summary>
		internal List<IUberOperation> FindAllEntity(object entity)
		{
			return _Items.FindAll(x => object.ReferenceEquals(x.Entity, entity));
		}

		/// <summary>
		/// Returns the first operation associated with the given entity, or null.
		/// </summary>
		internal IUberOperation FindMeta(MetaEntity meta)
		{
			return _Items.Find(x => object.ReferenceEquals(x.MetaEntity, meta));
		}

		/// <summary>
		/// Returns the last operation associated with the given entity, or null.
		/// </summary>
		internal IUberOperation FindLastMeta(MetaEntity meta)
		{
			return _Items.FindLast(x => object.ReferenceEquals(x.MetaEntity, meta));
		}

		/// <summary>
		/// Returns all the operations associated with the given entity, or null.
		/// </summary>
		internal List<IUberOperation> FindAllMeta(MetaEntity meta)
		{
			return _Items.FindAll(x => object.ReferenceEquals(x.MetaEntity, meta));
		}
	}
}
