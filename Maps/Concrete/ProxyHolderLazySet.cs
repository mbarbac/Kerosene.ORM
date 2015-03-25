// ======================================================== ProxyHolderLazySet.cs
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
	/// Represents a collection of lazy properties in a proxy holder.
	/// </summary>
	internal class ProxyHolderLazySet : IEnumerable<LazyProperty>
	{
		object _SyncRoot = new object();
		Dictionary<string, LazyProperty> _Items = new Dictionary<string, LazyProperty>();

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		internal ProxyHolderLazySet() { }

		/// <summary>
		/// An object that can be used to synchronize access to this collection.
		/// </summary>
		internal object SyncRoot
		{
			get { return _SyncRoot; }
		}

		/// <summary>
		/// Disposes this instance, clearing and disposing all the items in this collection.
		/// </summary>
		internal void Dispose()
		{
			if (_Items != null) Clear();
			_Items = null;
		}

		/// <summary>
		/// Returns a new enumerator for this instance.
		/// </summary>
		/// <returns>A new enumerator.</returns>
		public IEnumerator<LazyProperty> GetEnumerator()
		{
			return _Items.Values.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		/// The number of items in this collection.
		/// </summary>
		internal int Count
		{
			get { return _Items.Count; }
		}

		/// <summary>
		/// Adds the given item into this collection.
		/// </summary>
		internal void Add(LazyProperty item)
		{
			_Items.Add(item.Name, item);
		}

		/// <summary>
		/// Removes the given item from this collection.
		/// </summary>
		internal bool Remove(LazyProperty item)
		{
			return _Items.Remove(item.Name);
		}

		/// <summary>
		/// Clears all the items contained in this collection, disposing them all.
		/// </summary>
		internal void Clear()
		{
			var array = ToArray(); foreach (var item in array) item.Dispose();
			Array.Clear(array, 0, array.Length); array = null;
			_Items.Clear();
		}

		/// <summary>
		/// Returns a new array containing the elements of this collection.
		/// </summary>
		internal LazyProperty[] ToArray()
		{
			return _Items.Values.ToArray();
		}

		/// <summary>
		/// Returns the lazy property in this collection whose name matches the one given, or
		/// null if such item cannot be found.
		/// </summary>
		internal LazyProperty FindByName(string name)
		{
			LazyProperty item = null; _Items.TryGetValue(name, out item);
			return item;
		}
	}
}
// ======================================================== 
