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
	internal class ProxyHolderCollection
	{
		Dictionary<Type, ProxyHolder> _Items = new Dictionary<Type, ProxyHolder>();

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		internal ProxyHolderCollection() { }

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
		internal IEnumerable<ProxyHolder> Items
		{
			get { return _Items.Values; }
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
		internal void Add(ProxyHolder item)
		{
			_Items.Add(item.ProxyType, item);
		}

		/// <summary>
		/// Removes the given element from this collection.
		/// </summary>
		internal bool Remove(ProxyHolder item)
		{
			return _Items.Remove(item.ProxyType);
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
		public ProxyHolder[] ToArray()
		{
			return _Items.Values.ToArray();
		}

		/// <summary>
		/// Returns the member stored for the given extended (proxy) type.
		/// </summary>
		internal ProxyHolder Find(Type ExtendedType)
		{
			ProxyHolder item = null; _Items.TryGetValue(ExtendedType, out item);
			return item;
		}
	}
}
