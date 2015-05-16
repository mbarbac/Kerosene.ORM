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
	internal interface IUberMemberCollection : IMapMemberCollection, IEnumerable<IUberMember>
	{
		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		new IUberMap Map { get; }

		/// <summary>
		/// Returns the first member that matches the given predicate, or null.
		/// </summary>
		/// <param name="match">The predicate for the the item to find.</param>
		/// <returns>The member found, or null.</returns>
		IUberMember Find(Predicate<IUberMember> match);

		/// <summary>
		/// Returns the first member that matches the given predicate, or null.
		/// </summary>
		/// <param name="match">The predicate for the the item to find.</param>
		/// <returns>The member found, or null.</returns>
		List<IUberMember> FindAll(Predicate<IUberMember> match);
	}

	// ====================================================
	/// <summary>
	/// Represents the collection of members of the type for which either a dependency and/or
	/// a completion method has been defined.
	/// </summary>
	public class MapMemberCollection<T> : IMapMemberCollection<T>, IEnumerable<MapMember<T>>, IUberMemberCollection where T : class
	{
		DataMap<T> _Map = null;
		internal List<MapMember<T>> _List = new List<MapMember<T>>();

		/// <summary>
		/// Intializes a new instance.
		/// </summary>
		internal MapMemberCollection(DataMap<T> map)
		{
			if (map == null) throw new ArgumentNullException("map", "Meta Map cannot be null.");
			if (map.IsDisposed) throw new ObjectDisposedException(map.ToString());
			_Map = map;
		}

		/// <summary>
		/// Whether this instance has been disposed or not.
		/// </summary>
		internal bool IsDisposed
		{
			get { return Map == null; }
		}

		/// <summary>
		/// Invoked when disposing this instance.
		/// </summary>
		internal protected virtual void OnDispose()
		{
			if (_List != null)
			{
				var list = _List.ToArray(); foreach (var item in list) item.OnDispose();
				Array.Clear(list, 0, list.Length);
				_List.Clear();
			}
			_List = null;
			_Map = null;
		}

		/// <summary></summary>
		~MapMemberCollection()
		{
			if (!IsDisposed) OnDispose();
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("["); if (_List != null)
			{
				bool first = true; foreach (var entry in _List)
				{
					if (first) first = false; else sb.Append(", ");
					sb.Append(entry);
				}
			}
			sb.Append("]");

			var str = sb.ToString();
			return IsDisposed ? "disposed::{0}({1})".FormatWith(GetType().EasyName(), str) : str;
		}

		/// <summary>
		/// Invoked when the associated map is validated.
		/// </summary>
		internal protected virtual void OnValidate()
		{
			foreach (var item in _List) item.OnValidate();
		}

		/// <summary>
		/// Returns a new instance that is a copy of the original one, but associated with
		/// the given target.
		/// </summary>
		/// <param name="target">The target the new instance with be associated with.</param>
		/// <returns>A new instance.</returns>
		internal MapMemberCollection<T> Clone(DataMap<T> target)
		{
			var cloned = new MapMemberCollection<T>(target); OnClone(cloned);
			return cloned;
		}

		/// <summary>
		/// Invoked when cloning this object to set its state at this point of the inheritance
		/// chain.
		/// </summary>
		/// <param name="cloned">The cloned object.</param>
		/// <param name="cloneList">True to clone the source items that were not orphans.</param>
		protected virtual void OnClone(object cloned, bool cloneList = true)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (cloned == null) throw new ArgumentNullException("cloned", "Cloned object cannot be null.");

			var temp = cloned as MapMemberCollection<T>;
			if (temp == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' instance."
				.FormatWith(cloned.Sketch(), typeof(MapMemberCollection<T>).EasyName()));

			if (cloneList)
				foreach (var item in _List) temp._List.Add(item.Clone(temp.Map));
		}

		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		public DataMap<T> Map
		{
			get { return _Map; }
		}
		IDataMap<T> IMapMemberCollection<T>.Map
		{
			get { return this.Map; }
		}
		IDataMap IMapMemberCollection.Map
		{
			get { return this.Map; }
		}
		IUberMap IUberMemberCollection.Map
		{
			get { return this.Map; }
		}

		/// <summary>
		/// Returns a new enumerator for this instance.
		/// </summary>
		/// <returns>A new enumerator.</returns>
		public IEnumerator<MapMember<T>> GetEnumerator()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return _List.GetEnumerator();
		}
		IEnumerator<IMapMember<T>> IEnumerable<IMapMember<T>>.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		IEnumerator<IMapMember> IEnumerable<IMapMember>.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		IEnumerator<IUberMember> IEnumerable<IUberMember>.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		/// The number of entries in this collection.
		/// </summary>
		public int Count
		{
			get { return _List == null ? 0 : _List.Count; }
		}

		/// <summary>
		/// Adds into this collection a new entry whose name is specified or, optionally, returns
		/// the existing column for that name.
		/// </summary>
		/// <param name="name">A dynamic lambda expression that resolves into the name of the
		/// entry to add.</param>
		/// <param name="raise">If true and the name matches with the name of an existing column
		/// then throw an exception. False to return the existing column.</param>
		/// <returns>The entry added into or found in this collection.</returns>
		public MapMember<T> Add(Func<dynamic, object> name, bool raise = true)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (Map.IsDisposed) throw new ObjectDisposedException(Map.ToString());
			if (Map.IsValidated) throw new InvalidOperationException("Map '{0}' is validated.".FormatWith(Map));

			if (name == null) throw new ArgumentNullException("name", "Name specification cannot be null.");
			var temp = DynamicInfo.ParseName(name);

			var sensitive = Map.Repository.Link.Engine.CaseSensitiveNames;
			var entry = _List.Find(x => string.Compare(x.Name, temp, !sensitive) == 0);
			if (entry != null)
			{
				if (!raise) return entry;
				throw new DuplicateException(
					"Entry for name '{0}' is already registered in this '{1}'.".FormatWith(temp, this));
			}

			entry = CreateChild(Map, temp); _List.Add(entry);
			return entry;
		}
		IMapMember<T> IMapMemberCollection<T>.Add(Func<dynamic, object> name, bool raise)
		{
			return this.Add(name);
		}
		IMapMember IMapMemberCollection.Add(Func<dynamic, object> name, bool raise)
		{
			return this.Add(name);
		}

		/// <summary>
		/// Used to create a new instance to add into the list.
		/// </summary>
		protected virtual MapMember<T> CreateChild(DataMap<T> map, string name)
		{
			return new MapMember<T>(map, name);
		}

		/// <summary>
		/// Removes the given entry from this collection. Returns true if the member has been
		/// removed, or false otherwise.
		/// </summary>
		/// <param name="entry">The entry to remove.</param>
		/// <returns>True if the entry has been removed, or false otherwise.</returns>
		public bool Remove(MapMember<T> entry)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (Map.IsDisposed) throw new ObjectDisposedException(Map.ToString());
			if (Map.IsValidated) throw new InvalidOperationException("Map '{0}' is validated.".FormatWith(Map));

			bool r = entry == null ? false : _List.Remove(entry);
			if (r) entry.OnDispose();
			return r;
		}
		bool IMapMemberCollection<T>.Remove(IMapMember<T> entry)
		{
			var temp = entry as MapMember<T>;
			return temp == null ? false : this.Remove(temp);
		}
		bool IMapMemberCollection.Remove(IMapMember entry)
		{
			var temp = entry as MapMember<T>;
			return temp == null ? false : this.Remove(temp);
		}

		/// <summary>
		/// Clears and disposes all the entries in this collection.
		/// </summary>
		public void Clear()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (Map.IsDisposed) throw new ObjectDisposedException(Map.ToString());
			if (Map.IsValidated) throw new InvalidOperationException("Map '{0}' is validated.".FormatWith(Map));

			foreach (var item in _List) item.OnDispose();
			_List.Clear();
		}

		/// <summary>
		/// Returns the first member that matches the given predicate, or null.
		/// </summary>
		/// <param name="match">The predicate for the the item to find.</param>
		/// <returns>The member found, or null.</returns>
		public MapMember<T> Find(Predicate<MapMember<T>> match)
		{
			return _List.Find(match);
		}
		IUberMember IUberMemberCollection.Find(Predicate<IUberMember> match)
		{
			return this.Find(match);
		}

		/// <summary>
		/// Returns the first member that matches the given predicate, or null.
		/// </summary>
		/// <param name="match">The predicate for the the item to find.</param>
		/// <returns>The member found, or null.</returns>
		public List<MapMember<T>> FindAll(Predicate<MapMember<T>> match)
		{
			return _List.FindAll(match);
		}
		List<IUberMember> IUberMemberCollection.FindAll(Predicate<IUberMember> match)
		{
			return this.FindAll(match).Cast<IUberMember>().ToList();
		}
	}
}
