// ======================================================== UberOperationList.cs
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
	/// Represents a collection of operations annotated in a given repository.
	/// <para>This collection preserves the order at which its items are added or removed.</para>
	/// </summary>
	internal class UberOperationList : IEnumerable<IUberOperation>
	{
		object _SyncRoot = new object();
		List<IUberOperation> _Items = new List<IUberOperation>();

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		internal UberOperationList() { }

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
		public IEnumerator<IUberOperation> GetEnumerator()
		{
			return _Items.GetEnumerator();
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
		internal void Add(IUberOperation item)
		{
			_Items.Add(item);
		}

		/// <summary>
		/// Removes the given item from this collection.
		/// </summary>
		internal bool Remove(IUberOperation item)
		{
			return _Items.Remove(item);
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
		internal IUberOperation[] ToArray()
		{
			return _Items.ToArray();
		}

		/// <summary>
		/// Returns whether the given item is contained in this collection or not.
		/// </summary>
		internal bool Contains(IUberOperation item)
		{
			return _Items.Contains(item);
		}
	}
}
// ======================================================== 
