namespace Kerosene.ORM.Core.Concrete
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Represents the collection of aliases of elements used in a given context.
	/// </summary>
	[Serializable]
	public class ElementAliasCollection : IElementAliasCollection
	{
		bool _IsDisposed = false;
		List<IElementAlias> _Members = new List<IElementAlias>();
		bool _CaseSensitiveNames = Core.ElementAliasCollection.DEFAULT_CASE_SENSITIVE_NAMES;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="caseSensitiveNames">Whether the names of the members of this collection
		/// are case sensitive or not.</param>
		public ElementAliasCollection(
			bool caseSensitiveNames = Core.ElementAliasCollection.DEFAULT_CASE_SENSITIVE_NAMES)
		{
			_CaseSensitiveNames = caseSensitiveNames;
		}

		/// <summary>
		/// Whether this instance has been disposed or not.
		/// </summary>
		public bool IsDisposed
		{
			get { return _IsDisposed; }
		}

		/// <summary>
		/// Disposes this instance.
		/// </summary>
		public void Dispose()
		{
			if (!IsDisposed) { OnDispose(true); GC.SuppressFinalize(this); }
		}

		~ElementAliasCollection()
		{
			if (!IsDisposed) OnDispose(false);
		}

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		protected virtual void OnDispose(bool disposing)
		{
			if (disposing)
			{
				if (_Members != null)
				{
					var list = _Members.ToArray(); foreach (var member in list) member.Dispose();
				}
			}
			_Members = null;

			_IsDisposed = true;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("["); if (_Members != null)
			{
				for (int i = 0; i < _Members.Count; i++)
				{
					if (i != 0) sb.Append(", ");
					sb.Append(_Members[i]);
				}
			}
			sb.Append("]");

			var str = sb.ToString();
			return IsDisposed ? string.Format("disposed::{0}({1})", GetType().EasyName(), str) : str;
		}

		/// <summary>
		/// Call-back method required for custom serialization.
		/// </summary>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			info.AddValue("CaseSensitiveNames", _CaseSensitiveNames);
			info.AddExtended("List", _Members);
		}

		/// <summary>
		/// Protected initializer required for custom serialization.
		/// </summary>
		protected ElementAliasCollection(SerializationInfo info, StreamingContext context)
		{
			_CaseSensitiveNames = info.GetBoolean("CaseSensitiveNames");
			_Members = info.GetExtended<List<IElementAlias>>("List");
		}

		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		public ElementAliasCollection Clone()
		{
			var cloned = new ElementAliasCollection(CaseSensitiveNames);
			OnClone(cloned); return cloned;
		}
		IElementAliasCollection IElementAliasCollection.Clone()
		{
			return this.Clone();
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		/// <summary>
		/// Invoked when cloning this object to set its state at this point of the inheritance
		/// chain.
		/// </summary>
		/// <param name="cloned">The cloned object.</param>
		protected virtual void OnClone(object cloned)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			var temp = cloned as ElementAliasCollection;
			if (cloned == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' one."
				.FormatWith(cloned.Sketch(), typeof(ElementAliasCollection).EasyName()));

			temp.AddRange(_Members, cloneNotOrphans: true);
		}

		/// <summary>
		/// Returns true if this object can be considered as equivalent to the target one given.
		/// </summary>
		/// <param name="target">The target object this one will be tested for equivalence.</param>
		/// <returns>True if this object can be considered as equivalent to the target one given.</returns>
		public bool EquivalentTo(IElementAliasCollection target)
		{
			return OnEquivalentTo(target);
		}

		/// <summary>
		/// Invoked to test equivalence at this point of the inheritance chain.
		/// </summary>
		/// <param name="target">The target this instance will be tested for equivalence against.</param>
		/// <returns>True if at this level on the inheritance chain this instance can be considered
		/// equivalent to the target instance given.</returns>
		protected virtual bool OnEquivalentTo(object target)
		{
			if (object.ReferenceEquals(this, target)) return true;
			var temp = target as IElementAliasCollection; if (temp == null) return false;
			if (temp.IsDisposed) return false;
			if (IsDisposed) return false;

			if (this.CaseSensitiveNames != temp.CaseSensitiveNames) return false;

			if (this.Count != temp.Count) return false; foreach (var item in temp)
			{
				var member = FindAlias(item.Alias); if (member == null) return false;
				if (!member.EquivalentTo(item)) return false;
			}

			return true;
		}

		/// <summary>
		/// Whether the names of the members of this collection are case sensitive or not.
		/// </summary>
		public bool CaseSensitiveNames
		{
			get { return _CaseSensitiveNames; }
		}

		/// <summary>
		/// Obtains an enumerator for the members of this instance.
		/// </summary>
		/// <returns>A new enumerator for the members of this instance.</returns>
		public IEnumerator<IElementAlias> GetEnumerator()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return _Members.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		/// The number of members this instance contains.
		/// </summary>
		public int Count
		{
			get { return _Members == null ? 0 : _Members.Count; }
		}

		/// <summary>
		/// Gets the member stored at the given position.
		/// </summary>
		/// <param name="index">The position at which the member to return is stored.</param>
		/// <returns>The member at the given position.</returns>
		public IElementAlias this[int index]
		{
			get
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				return _Members[index];
			}
		}

		/// <summary>
		/// Returns the index at which the given member is stored, or -1 if it does not belong
		/// to this collection.
		/// </summary>
		/// <param name="member">The member whose index if to be found.</param>
		/// <returns>The index at which the given member is stored, or -1 if it does not belong
		/// to this collection.</returns>
		public int IndexOf(IElementAlias member)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (member == null) throw new ArgumentNullException("member", "Member cannot be null.");
			return _Members.IndexOf(member);
		}

		/// <summary>
		/// Returns whether the given member is in this collection.
		/// </summary>
		/// <param name="member">The member to validate.</param>
		/// <returns>True if the given member is part of this collection, or false otherwise.</returns>
		public bool Contains(IElementAlias member)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (member == null) throw new ArgumentNullException("Member cannot be null.");
			return _Members.Contains(member);
		}

		/// <summary>
		/// Returns the unique entry whose alias is given, or null if such cannot be found.
		/// </summary>
		/// <param name="alias">The alias of the entry to find.</param>
		/// <returns>The member found, or null.</returns>
		public IElementAlias FindAlias(string alias)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			alias = Core.ElementAlias.ValidateAlias(alias);

			return _Members.Find(x => string.Compare(x.Alias, alias, !CaseSensitiveNames) == 0);
		}

		/// <summary>
		/// Returns the collection of entries whose name match the one given.
		/// </summary>
		/// <param name="element">The name of the elements to return.</param>
		/// <returns>The requested collection.</returns>
		public IEnumerable<IElementAlias> FindElement(string element)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			element = Core.ElementAlias.ValidateElement(element);

			return _Members.FindAll(x => string.Compare(x.Element, element, !CaseSensitiveNames) == 0);
		}

		/// <summary>
		/// Adds the given orphan instance into this collection.
		/// </summary>
		/// <param name="member">The orphan instance to add into this collection.</param>
		public void Add(IElementAlias member)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			if (member == null) throw new ArgumentNullException("member", "Member cannot be null.");
			if (member.IsDisposed) throw new ObjectDisposedException(member.ToString());

			if (object.ReferenceEquals(this, member.Owner)) return;
			if (member.Owner != null) throw new NotOrphanException(
				"Cannot add member '{0}' into this '{1}' because it is not orphan."
				.FormatWith(member, this));

			Core.ElementAlias.ValidateElement(member.Element);
			Core.ElementAlias.ValidateAlias(member.Alias);

			var temp = FindAlias(member.Alias);
			if (temp != null) throw new DuplicateException(
				"Cannot add member '{0}' into this '{1}' because its alias is already used."
				.FormatWith(member, this));

			var list = FindElement(member.Alias).ToList();
			if (list.Count != 0) throw new DuplicateException(
				"Cannot add member '{0}' into this '{1}' because its alias is used as an element name."
				.FormatWith(member, this));

			if (member.Element != null)
			{
				temp = FindAlias(member.Element);
				if (temp != null) throw new DuplicateException(
					"Cannot add member '{0}' into this '{1}' because its element is already used as an alias."
					.FormatWith(member, this));
			}

			_Members.Add(member); // To intercept re-entrant operation...
			member.Owner = this;
		}

		/// <summary>
		/// Factory method invoked to create a new orphan member but with the right type for
		/// this collection.
		/// </summary>
		/// <returns>A new orphan member.</returns>
		public virtual IElementAlias CreateOrphanMember()
		{
			return new ElementAlias();
		}

		/// <summary>
		/// Creates and add into this collection a new member using the arguments given.
		/// </summary>
		/// <param name="element">The string representation of the element to be aliased, or null
		/// if it is the default one this context.</param>
		/// <param name="alias">The alias.</param>
		public IElementAlias AddCreate(string element, string alias)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			var member = CreateOrphanMember();
			member.Element = element;
			member.Alias = alias;

			member.Owner = this;
			return member;
		}

		/// <summary>
		/// Creates and adds into this collection a new member with a new alias for the default
		/// element in this context.
		/// </summary>
		/// <param name="alias">The alias.</param>
		public IElementAlias AddCreate(string alias)
		{
			return AddCreate(null, alias);
		}

		/// <summary>
		/// Adds the given range of members into this collection, optionally cloning those that
		/// were not orphan ones.
		/// </summary>
		/// <param name="range">The range of members to add into this collection.</param>
		/// <param name="cloneNotOrphans">True to clone those member in the range that were
		/// not orphan ones, or false to throw an exception if such scenario ocurrs.</param>
		public void AddRange(IEnumerable<IElementAlias> range, bool cloneNotOrphans = true)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (range == null) throw new ArgumentNullException("range", "Range cannot be null.");

			int i = 0; foreach (var member in range)
			{
				if (member == null) throw new ArgumentNullException(
					"Member #{0} of range '{1}' cannot be null.".FormatWith(i, range.Sketch()));

				if (member.Owner == null) Add(member);
				else
				{
					if (!object.ReferenceEquals(this, member.Owner))
					{
						if (!cloneNotOrphans) throw new NotOrphanException(
							"Member #{0} '{1}' of range '{2} is not orphan."
							.FormatWith(i, member, range.Sketch()));

						Add(member.Clone());
					}
				}

				i++;
			}
		}

		/// <summary>
		/// Removes the given parameter from this collection. Returns true if it has been removed
		/// succesfully, or false otherwise.
		/// </summary>
		/// <param name="member">The member to remove.</param>
		/// <returns>True if the member has been removed succesfully, or false otherwise.</returns>
		public bool Remove(IElementAlias member)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (member == null) throw new ArgumentNullException("member", "Member cannot be null.");

			// If r is false intercepts re-entrant operations...
			bool r = _Members.Remove(member); if (r) member.Owner = null;
			return r;
		}

		/// <summary>
		/// Clears this collection by removing all its members and optionally disposing them.
		/// </summary>
		/// <param name="disposeMembers">True to dispose the removed members, false to just
		/// remove them.</param>
		public void Clear(bool disposeMembers = true)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			if (disposeMembers)
			{
				var members = _Members.ToArray(); foreach (var member in members) member.Dispose();
			}

			_Members.Clear();
		}
	}
}
