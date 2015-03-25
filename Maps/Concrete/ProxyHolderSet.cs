// ======================================================== ProxyHolderSet.cs
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
	/// Represents a collection of proxy holders.
	/// </summary>
	internal class ProxyHolderSet : IEnumerable<ProxyHolder>
	{
		object _SyncRoot = new object();
		Dictionary<string, ProxyHolder> _Items = new Dictionary<string, ProxyHolder>();

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		internal ProxyHolderSet() { }

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
		public IEnumerator<ProxyHolder> GetEnumerator()
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
		internal void Add(ProxyHolder item)
		{
			_Items.Add(item.ExtendedType.Name, item);
		}

		/// <summary>
		/// Removes the given item from this collection.
		/// </summary>
		internal bool Remove(ProxyHolder item)
		{
			return _Items.Remove(item.ExtendedType.Name);
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
		internal ProxyHolder[] ToArray()
		{
			return _Items.Values.ToArray();
		}

		/// <summary>
		/// Returns the item registered for the given extended type, or null if not such holder
		/// can be found.
		/// </summary>
		internal ProxyHolder FindByExtended(Type extended)
		{
			ProxyHolder item = null; _Items.TryGetValue(extended.Name, out item);
			return item;
		}
	}
}
// ======================================================== 
