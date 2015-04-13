// ======================================================== UberOperationList.cs
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
	/// Represents the list of pending operations held by a repository.
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
		/// Gets the operation stored at the given index.
		/// </summary>
		internal IUberOperation this[int index]
		{
			get { return _Items[index]; }
		}

		/// <summary>
		/// Gets the index of the first registered operation associated with the given entity.
		/// </summary>
		internal int IndexOf(MetaEntity meta)
		{
			for (int i = 0; i < _Items.Count; i++)
				if (object.ReferenceEquals(_Items[i].MetaEntity, meta)) return i;

			return -1;
		}

		/// <summary>
		/// Gets the index of the last registered operation associated with the given entity.
		/// </summary>
		internal int LastIndexOf(MetaEntity meta)
		{
			int n = -1;

			for (int i = 0; i < _Items.Count; i++)
				if (object.ReferenceEquals(_Items[i].MetaEntity, meta)) n = i;

			return n;
		}

		/// <summary>
		/// Whether this collection contains the given item or not.
		/// </summary>
		internal bool Contains(IUberOperation item)
		{
			return _Items.Contains(item);
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
		/// Clears the items contained in this collection.
		/// </summary>
		internal void Clear()
		{
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
		/// Returns the first element in the collection that matches the given predicate.
		/// </summary>
		internal IUberOperation Find(Predicate<IUberOperation> match)
		{
			return _Items.Find(match);
		}

		/// <summary>
		/// Returns a list with the elements in the collection that match the given predicate.
		/// </summary>
		internal List<IUberOperation> FindAll(Predicate<IUberOperation> match)
		{
			return _Items.FindAll(match);
		}
	}
}
// ======================================================== 
