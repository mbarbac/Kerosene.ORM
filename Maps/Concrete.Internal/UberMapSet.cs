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
	/// Represents a collection of maps registered in a given repository.
	/// </summary>
	internal class UberMapSet : IEnumerable<IUberMap>
	{
		/// <summary>
		/// Generates the key to use with a collection member.
		/// </summary>
		internal static string GetKey(Type type)
		{
			return type.AssemblyQualifiedName;
		}

		/// <summary>
		/// Generates the key to use with a collection member.
		/// </summary>
		internal static string GetKey(IUberMap item)
		{
			return GetKey(item.EntityType);
		}

		object _SyncRoot = new object();
		Dictionary<string, IUberMap> _Items = new Dictionary<string, IUberMap>();

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		internal UberMapSet() { }

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
		public IEnumerator<IUberMap> GetEnumerator()
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
		internal void Add(IUberMap item)
		{
			_Items.Add(GetKey(item), item);
		}

		/// <summary>
		/// Removes the given item from this collection.
		/// </summary>
		internal bool Remove(IUberMap item)
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
		internal IUberMap[] ToArray()
		{
			return _Items.Values.ToArray();
		}

		/// <summary>
		/// Returns the registered map associated with the given type, or null if such map cannot
		/// be found.
		/// </summary>
		internal IUberMap FindByType(Type type)
		{
			IUberMap item = null; _Items.TryGetValue(GetKey(type), out item);
			return item;
		}

		/// <summary>
		/// Returns the first element in the collection that matches the given predicate.
		/// </summary>
		internal IUberMap Find(Func<IUberMap, bool> match)
		{
			var item = _Items.Values.FirstOrDefault(match);
			return item;
		}

		/// <summary>
		/// Returns a list with the elements in the collection that match the given predicate.
		/// </summary>
		internal List<IUberMap> FindAll(Func<IUberMap, bool> match)
		{
			var list = _Items.Values.Where(match).ToList();
			return list;
		}
	}
}
