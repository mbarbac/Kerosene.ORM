// ======================================================== LazyPropertySet.cs
namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents the collection of lazy properties maintained by a lazy type holder.
	/// </summary>
	internal class LazyPropertySet : IEnumerable<LazyProperty>
	{
		/// <summary>
		/// Generates the key to use with a collection member.
		/// </summary>
		internal static string GetKey(LazyProperty item)
		{
			return item.Name;
		}

		object _SyncRoot = new object();
		Dictionary<string, LazyProperty> _Items = new Dictionary<string, LazyProperty>();

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		internal LazyPropertySet() { }

		/// <summary>
		/// An object that can be used to synchronize access to this collection.
		/// </summary>
		internal object SyncRoot
		{
			get { return _SyncRoot; }
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
			_Items.Add(GetKey(item), item);
		}

		/// <summary>
		/// Removes the given item from this collection.
		/// </summary>
		internal bool Remove(LazyProperty item)
		{
			return _Items.Remove(GetKey(item));
		}

		/// <summary>
		/// Clears the items contained in this collection.
		/// </summary>
		internal void Clear()
		{
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
		internal LazyProperty FindByLazyName(string name)
		{
			LazyProperty item = null; _Items.TryGetValue(name, out item);
			return item;
		}
	}
}
// ======================================================== 
