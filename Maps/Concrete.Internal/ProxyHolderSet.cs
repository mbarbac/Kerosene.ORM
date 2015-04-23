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
	/// Represents the collection of proxy holders.
	/// </summary>
	internal class ProxyHolderSet : IEnumerable<ProxyHolder>
	{
		/// <summary>
		/// Generates the key to use with a collection member.
		/// </summary>
		internal static string GetKey(Type extendedType)
		{
			return extendedType.Name;
		}

		/// <summary>
		/// Generates the key to use with a collection member.
		/// </summary>
		internal static string GetKey(ProxyHolder item)
		{
			return GetKey(item.ExtendedType);
		}

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
			_Items.Add(GetKey(item), item);
		}

		/// <summary>
		/// Removes the given item from this collection.
		/// </summary>
		internal bool Remove(ProxyHolder item)
		{
			return _Items.Remove(GetKey(item));
		}

		/// <summary>
		/// Returns a new array containing the elements of this collection.
		/// </summary>
		internal ProxyHolder[] ToArray()
		{
			return _Items.Values.ToArray();
		}

		/// <summary>
		/// Returns the registered map associated with the given type, or null if such map cannot
		/// be found.
		/// </summary>
		internal ProxyHolder FindByExtendedType(Type extendedType)
		{
			ProxyHolder item = null; _Items.TryGetValue(GetKey(extendedType), out item);
			return item;
		}

		/// <summary>
		/// Returns the first element in the collection that matches the given predicate.
		/// </summary>
		internal ProxyHolder Find(Func<ProxyHolder, bool> match)
		{
			var item = _Items.Values.FirstOrDefault(match);
			return item;
		}

		/// <summary>
		/// Returns a list with the elements in the collection that match the given predicate.
		/// </summary>
		internal List<ProxyHolder> FindAll(Func<ProxyHolder, bool> match)
		{
			var list = _Items.Values.Where(match).ToList();
			return list;
		}
	}
}
