// ======================================================== MapColumnCollection.cs
namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Extends the <see cref="IMapColumnCollection"/> interface.
	/// </summary>
	internal interface IUberColumnCollection : IMapColumnCollection, IEnumerable<IUberColumn>
	{
		/// <summary>
		/// The map this instance is associated with, if any.
		/// </summary>
		new IUberMap Map { get; }
	}

	// ==================================================== 
	/// <summary>
	/// Represents the collection of columns in the primary table that have been  associated
	/// with the map.
	/// </summary>
	public class MapColumnCollection<T>
		: IMapColumnCollection<T>, IEnumerable<MapColumn<T>>
		, IUberColumnCollection where T : class
	{
		DataMap<T> _Map = null;
		List<MapColumn<T>> _List = new List<MapColumn<T>>();

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		internal protected MapColumnCollection(DataMap<T> map)
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
		/// Disposes this instance.
		/// </summary>
		internal virtual void Dispose()
		{
			if (_List != null)
			{
				var list = _List.ToArray(); foreach (var entry in list) entry.Dispose();
				_List.Clear(); _List = null;
			}
			_Map = null;
		}

		~MapColumnCollection()
		{
			if (!IsDisposed) Dispose();
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
		/// Clones this instance.
		/// </summary>
		internal protected virtual MapColumnCollection<T> Clone(DataMap<T> map)
		{
			var temp = new MapColumnCollection<T>(map);
			foreach (var entry in _List)
			{
				// Auto-discovered columns are not clones to permit receiving prog to define
				// their owns...
				if (!entry.AutoDiscovered) temp._List.Add(entry.Clone(map));
			}
			return temp;
		}

		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		public DataMap<T> Map
		{
			get { return _Map; }
		}
		IDataMap<T> IMapColumnCollection<T>.Map
		{
			get { return this.Map; }
		}
		IDataMap IMapColumnCollection.Map
		{
			get { return this.Map; }
		}
		IUberMap IUberColumnCollection.Map
		{
			get { return this.Map; }
		}

		/// <summary>
		/// Returns a new enumerator for this instance.
		/// </summary>
		/// <returns>A new enumerator.</returns>
		public IEnumerator<MapColumn<T>> GetEnumerator()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return _List.GetEnumerator();
		}
		IEnumerator<IMapColumn<T>> IEnumerable<IMapColumn<T>>.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		IEnumerator<IMapColumn> IEnumerable<IMapColumn>.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		IEnumerator<IUberColumn> IEnumerable<IUberColumn>.GetEnumerator()
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
		/// Adds into this collection a new entry for the member whose name is specified.
		/// </summary>
		/// <param name="name">A dynamic lambda expression that resolves into the name of the
		/// column in the master table of the map.</param>
		/// <returns>The new entry added into this collection.</returns>
		public MapColumn<T> Add(Func<dynamic, object> name)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (Map.IsDisposed) throw new ObjectDisposedException(Map.ToString());
			if (Map.IsValidated) throw new InvalidOperationException("Map '{0}' is validated.".FormatWith(Map));

			if (name == null) throw new ArgumentNullException("name", "Name specification cannot be null.");
			var temp = DynamicInfo.ParseName(name);

			var sensitive = Map.Repository.Link.Engine.CaseSensitiveNames;
			var entry = _List.Find(x => string.Compare(x.Name, temp, !sensitive) == 0);
			if (entry != null) throw new DuplicateException(
				"Entry for name '{0}' is already registered in this '{1}'.".FormatWith(temp, this));

			entry = new MapColumn<T>(Map, temp); _List.Add(entry);
			return entry;
		}
		IMapColumn<T> IMapColumnCollection<T>.Add(Func<dynamic, object> name)
		{
			return this.Add(name);
		}
		IMapColumn IMapColumnCollection.Add(Func<dynamic, object> name)
		{
			return this.Add(name);
		}

		/// <summary>
		/// Removes the given entry from this collection. Returns true if the member has been
		/// removed, or false otherwise.
		/// </summary>
		/// <param name="entry">The entry to remove.</param>
		/// <returns>True if the entry has been removed, or false otherwise.</returns>
		public bool Remove(MapColumn<T> entry)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (Map.IsDisposed) throw new ObjectDisposedException(Map.ToString());
			if (Map.IsValidated) throw new InvalidOperationException("Map '{0}' is validated.".FormatWith(Map));

			if (entry == null) return false;

			bool r = _List.Remove(entry); if (r) entry.Dispose();
			return r;
		}
		bool IMapColumnCollection<T>.Remove(IMapColumn<T> entry)
		{
			var temp = entry as MapColumn<T>;
			return temp == null ? false : this.Remove(temp);
		}
		bool IMapColumnCollection.Remove(IMapColumn entry)
		{
			var temp = entry as MapColumn<T>;
			return temp == null ? false : this.Remove(temp);
		}

		/// <summary>
		/// Returns the first element in the collection that matches the given predicate.
		/// </summary>
		internal MapColumn<T> Find(Predicate<MapColumn<T>> match)
		{
			return _List.Find(match);
		}

		/// <summary>
		/// Returns a list with the elements in the collection that match the given predicate.
		/// </summary>
		internal List<MapColumn<T>> FindAll(Predicate<MapColumn<T>> match)
		{
			return _List.FindAll(match);
		}
	}
}
// ======================================================== 
