// ======================================================== MapMemberColumnCollection.cs
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
	/// Extends the <see cref="IMapMemberColumnCollection"/> interface.
	/// </summary>
	internal interface IUberMemberColumnCollection
		: IMapMemberColumnCollection, IEnumerable<IUberMemberColumn>
	{
		/// <summary>
		/// The member this instance is associated with.
		/// </summary>
		new IUberMember Member { get; }

		/// <summary>
		/// The map reference held by the associated member, if any.
		/// </summary>
		IUberMap Map { get; }
	}

	// ==================================================== 
	/// <summary>
	/// Represents the collection of columns in the primary table that have been explicitly
	/// associated with a member of the map.
	/// </summary>
	public class MapMemberColumnCollection<T>
		: IMapMemberColumnCollection<T>, IEnumerable<MapMemberColumn<T>>
		, IUberMemberColumnCollection where T : class
	{
		MapMember<T> _Member = null;
		List<MapMemberColumn<T>> _List = new List<MapMemberColumn<T>>();

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		internal protected MapMemberColumnCollection(MapMember<T> member)
		{
			if (member == null) throw new ArgumentNullException("member", "Meta Member cannot be null.");
			if (member.IsDisposed) throw new ObjectDisposedException(member.ToString());
			if (member.Map.IsDisposed) throw new ObjectDisposedException(member.Map.ToString());

			_Member = member;
		}

		/// <summary>
		/// Whether this instance has been disposed or not.
		/// </summary>
		internal bool IsDisposed
		{
			get { return (Member == null || Member.Map == null); }
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
			_Member = null;
		}

		~MapMemberColumnCollection()
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
		internal protected virtual MapMemberColumnCollection<T> Clone(MapMember<T> member)
		{
			var temp = new MapMemberColumnCollection<T>(member);
			foreach (var entry in _List)
			{
				temp._List.Add(entry.Clone(member));
			}
			return temp;
		}

		/// <summary>
		/// The member this instance is associated with.
		/// </summary>
		public MapMember<T> Member
		{
			get { return _Member; }
		}
		IMapMember<T> IMapMemberColumnCollection<T>.Member
		{
			get { return this.Member; }
		}
		IMapMember IMapMemberColumnCollection.Member
		{
			get { return this.Member; }
		}
		IUberMember IUberMemberColumnCollection.Member
		{
			get { return this.Member; }
		}

		/// <summary>
		/// The map reference held by the associated member, if any.
		/// </summary>
		internal IUberMap Map
		{
			get { return Member == null ? null : Member.Map; }
		}
		IUberMap IUberMemberColumnCollection.Map
		{
			get { return this.Map; }
		}

		/// <summary>
		/// Returns a new enumerator for this instance.
		/// </summary>
		/// <returns>A new enumerator.</returns>
		public IEnumerator<MapMemberColumn<T>> GetEnumerator()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return _List.GetEnumerator();
		}
		IEnumerator<IMapMemberColumn<T>> IEnumerable<IMapMemberColumn<T>>.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		IEnumerator<IMapMemberColumn> IEnumerable<IMapMemberColumn>.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		IEnumerator<IUberMemberColumn> IEnumerable<IUberMemberColumn>.GetEnumerator()
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
		public MapMemberColumn<T> Add(Func<dynamic, object> name)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (Member.IsDisposed) throw new ObjectDisposedException(Member.ToString());
			if (Member.Map.IsDisposed) throw new ObjectDisposedException(Member.Map.ToString());

			if (name == null) throw new ArgumentNullException("name", "Name specification cannot be null.");
			var temp = DynamicInfo.ParseName(name);

			var sensitive = Member.Map.Repository.Link.Engine.CaseSensitiveNames;
			var entry = _List.Find(x => string.Compare(x.Name, temp, !sensitive) == 0);
			if (entry != null) throw new DuplicateException(
				"Entry for name '{0}' is already registered in this '{1}'.".FormatWith(temp, this));

			entry = new MapMemberColumn<T>(Member, temp); _List.Add(entry);
			return entry;
		}
		IMapMemberColumn<T> IMapMemberColumnCollection<T>.Add(Func<dynamic, object> name)
		{
			return this.Add(name);
		}
		IMapMemberColumn IMapMemberColumnCollection.Add(Func<dynamic, object> name)
		{
			return this.Add(name);
		}

		/// <summary>
		/// Removes the given entry from this collection. Returns true if the member has been
		/// removed, or false otherwise.
		/// </summary>
		/// <param name="entry">The entry to remove.</param>
		/// <returns>True if the entry has been removed, or false otherwise.</returns>
		public bool Remove(MapMemberColumn<T> entry)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (Member.IsDisposed) throw new ObjectDisposedException(Member.ToString());
			if (Member.Map.IsDisposed) throw new ObjectDisposedException(Member.Map.ToString());

			if (entry == null) return false;

			bool r = _List.Remove(entry); if (r) entry.Dispose();
			return r;
		}
		bool IMapMemberColumnCollection<T>.Remove(IMapMemberColumn<T> entry)
		{
			var temp = entry as MapMemberColumn<T>;
			return temp == null ? false : this.Remove(temp);
		}
		bool IMapMemberColumnCollection.Remove(IMapMemberColumn entry)
		{
			var temp = entry as MapMemberColumn<T>;
			return temp == null ? false : this.Remove(temp);
		}

		/// <summary>
		/// Returns the first element in the collection that matches the given predicate.
		/// </summary>
		internal MapMemberColumn<T> Find(Predicate<MapMemberColumn<T>> match)
		{
			return _List.Find(match);
		}

		/// <summary>
		/// Returns a list with the elements in the collection that match the given predicate.
		/// </summary>
		internal List<MapMemberColumn<T>> FindAll(Predicate<MapMemberColumn<T>> match)
		{
			return _List.FindAll(match);
		}
	}
}
// ======================================================== 
