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
	internal class LazyPropertyCollection : IEnumerable<LazyProperty>
	{
		Dictionary<string, LazyProperty> _Items = new Dictionary<string, LazyProperty>();

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		internal LazyPropertyCollection() { }

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
		public IEnumerator<LazyProperty> GetEnumerator()
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
			get { return _Items.Count; }
		}

		/// <summary>
		/// Adds the given element into this collection.
		/// </summary>
		internal void Add(LazyProperty item)
		{
			_Items.Add(item.Name, item);
		}

		/// <summary>
		/// Removes the given element from this collection.
		/// </summary>
		internal bool Remove(LazyProperty item)
		{
			return _Items.Remove(item.Name);
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
		public LazyProperty[] ToArray()
		{
			return _Items.Values.ToArray();
		}

		/// <summary>
		/// Returns the member stored for the given name.
		/// </summary>
		public LazyProperty Find(string name)
		{
			LazyProperty item = null; _Items.TryGetValue(name, out item);
			return item;
		}
	}
}
