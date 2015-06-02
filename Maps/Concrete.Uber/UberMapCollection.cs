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
	internal class UberMapCollection : IEnumerable<IUberMap>
	{
		DataRepository _Master = null;
		Dictionary<Type, IUberMap> _Items = new Dictionary<Type, IUberMap>();

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		internal UberMapCollection(DataRepository repo)
		{
			_Master = repo;
		}

		/// <summary>
		/// Disposes this instance.
		/// </summary>
		internal void Dispose()
		{
			try { if (_Items != null) Clear(); }
			catch { }
			
			_Items = null;
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
		public IEnumerator<IUberMap> GetEnumerator()
		{
			return _Items.Values.GetEnumerator();
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
		internal void Add(IUberMap item)
		{
			if (_Items.ContainsKey(item.EntityType)) throw new DuplicateException("Type '{0}' is already registered.".FormatWith(item.EntityType.EasyName()));
			_Items.Add(item.EntityType, item);
		}

		/// <summary>
		/// Removes the given element from this collection.
		/// </summary>
		internal bool Remove(IUberMap item)
		{
			return _Items.Remove(item.EntityType);
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
		internal IUberMap[] ToArray()
		{
			return _Items.Values.ToArray();
		}

		/// <summary>
		/// Returns the member stored for managing entities of the given type.
		/// </summary>
		internal IUberMap Find(Type entityType)
		{
			IUberMap item = null; _Items.TryGetValue(entityType, out item);
			return item;
		}
	}
}
