// ======================================================== UberMapSet.cs
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
	/// Represents a collection of maps registered in a given repository.
	/// </summary>
	internal class UberMapSet : IEnumerable<IUberMap>
	{
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
			_Items.Add(item.EntityType.FullName, item);
		}

		/// <summary>
		/// Removes the given item from this collection.
		/// </summary>
		internal bool Remove(IUberMap item)
		{
			return _Items.Remove(item.EntityType.FullName);
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
		internal IUberMap[] ToArray()
		{
			return _Items.Values.ToArray();
		}

		/// <summary>
		/// Returns the registered map associated with the given type, or null if such map cannot
		/// be found.
		/// </summary>
		internal IUberMap Find(Type type)
		{
			IUberMap map = null; _Items.TryGetValue(type.FullName, out map);
			return map;
		}
	}
}
// ======================================================== 
