// ======================================================== MapMemberTCollection.cs
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
	/// Extends the <see cref="IMapMemberCollection"/> interface.
	/// </summary>
	internal interface IUberMemberCollection : IMapMemberCollection, IEnumerable<IUberMember>
	{
		/// <summary>
		/// The map this instance is associated with, if any.
		/// </summary>
		new IUberMap Map { get; }
	}

	// ==================================================== 
	/// <summary>
	/// Represents the collection of members that have been explicitly defined for its
	/// associated map.
	/// </summary>
	public class MapMemberCollection<T>
		: IMapMemberCollection<T>, IUberMemberCollection
		, IEnumerable<MapMember<T>> where T : class
	{
		DataMap<T> _Map = null;
		List<MapMember<T>> _List = new List<MapMember<T>>();

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		internal protected MapMemberCollection(DataMap<T> map)
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

		~MapMemberCollection()
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
		internal virtual MapMemberCollection<T> Clone(DataMap<T> map)
		{
			var temp = new MapMemberCollection<T>(map);
			foreach (var entry in _List)
			{
				temp._List.Add(entry.Clone(map));
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
		/// Adds into this collection a new entry for the member whose name is specified.
		/// </summary>
		/// <param name="name">A dynamic lambda expression that resolves into the name of the
		/// column in the master table of the map.</param>
		/// <returns>The new entry added into this collection.</returns>
		public MapMember<T> Add(Func<dynamic, object> name)
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

			entry = new MapMember<T>(Map, temp); _List.Add(entry);
			return entry;
		}
		IMapMember<T> IMapMemberCollection<T>.Add(Func<dynamic, object> name)
		{
			return this.Add(name);
		}
		IMapMember IMapMemberCollection.Add(Func<dynamic, object> name)
		{
			return this.Add(name);
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

			if (entry == null) return false;

			bool r = _List.Remove(entry); if (r) entry.Dispose();
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
	}
}
// ======================================================== 
