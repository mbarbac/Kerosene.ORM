// ======================================================== ParameterCollection.cs
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
	/// Represents the collection of generic parameters captured for a command.
	/// </summary>
	[Serializable]
	public class ParameterCollection : IParameterCollection
	{
		bool _IsDisposed = false;
		List<IParameter> _Members = new List<IParameter>();
		bool _CaseSensitiveNames = Core.ParameterCollection.DEFAULT_CASE_SENSITIVE_NAMES;
		string _Prefix = Core.ParameterCollection.DEFAULT_PREFIX;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="caseSensitiveNames">Whether the identifiers of the members in this
		/// collection are case sensitive or not.</param>
		/// <param name="prefix">The prefix to use when automatically building parameter names
		/// for values without names added into this collection.</param>
		public ParameterCollection(
			bool caseSensitiveNames = Core.ParameterCollection.DEFAULT_CASE_SENSITIVE_NAMES,
			string prefix = Core.ParameterCollection.DEFAULT_PREFIX)
		{
			_CaseSensitiveNames = caseSensitiveNames;
			_Prefix = Core.ParameterCollection.ValidatePrefix(prefix);
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

		~ParameterCollection()
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
					var list = new List<IParameter>(_Members);
					foreach (var member in list) if (!member.IsDisposed) member.Dispose();
					list.Clear(); list = null;
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
			info.AddValue("Prefix", _Prefix);
			info.AddExtended("List", _Members);
		}

		/// <summary>
		/// Protected initializer required for custom serialization.
		/// </summary>
		protected ParameterCollection(SerializationInfo info, StreamingContext context)
		{
			_CaseSensitiveNames = info.GetBoolean("CaseSensitiveNames");
			_Prefix = info.GetString("Prefix");
			_Members = info.GetExtended<List<IParameter>>("List");
		}

		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		public ParameterCollection Clone()
		{
			var cloned = new ParameterCollection(CaseSensitiveNames, Prefix);
			OnClone(cloned); return cloned;
		}
		IParameterCollection IParameterCollection.Clone()
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
			var temp = cloned as ParameterCollection;
			if (cloned == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' one."
				.FormatWith(cloned.Sketch(), typeof(ParameterCollection).EasyName()));

			// CaseSensitiveNames: constructor
			// Prefix: constructor
			temp.AddRange(_Members, cloneNotOrphans: true);
		}

		/// <summary>
		/// Returns true if this object can be considered as equivalent to the target one given.
		/// </summary>
		/// <param name="target">The target object this one will be tested for equivalence.</param>
		/// <returns>True if this object can be considered as equivalent to the target one given.</returns>
		public bool EquivalentTo(IParameterCollection target)
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
			var temp = target as IParameterCollection; if (temp == null) return false;
			if (temp.IsDisposed) return false;
			if (IsDisposed) return false;

			if (this.CaseSensitiveNames != temp.CaseSensitiveNames) return false;
			if (string.Compare(Prefix, temp.Prefix, !CaseSensitiveNames) != 0) return false;

			if (this.Count != temp.Count) return false; foreach (var item in temp)
			{
				var member = FindName(item.Name); if (member == null) return false;
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
		/// The default prefix to use to automatically create the name of a new parameter if it
		/// was added into this collection using only its value.
		/// </summary>
		public string Prefix
		{
			get { return _Prefix; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				_Prefix = Core.ParameterCollection.ValidatePrefix(value);
			}
		}

		/// <summary>
		/// Obtains an enumerator for the members of this instance.
		/// </summary>
		/// <returns>A new enumerator for the members of this instance.</returns>
		public IEnumerator<IParameter> GetEnumerator()
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
		public IParameter this[int index]
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
		public int IndexOf(IParameter member)
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
		public bool Contains(IParameter member)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (member == null) throw new ArgumentNullException("Member cannot be null.");
			return _Members.Contains(member);
		}

		/// <summary>
		/// Returns the first member in this instance that matches the conditions given in the
		/// predicate, or null if not such member can be found.
		/// </summary>
		/// <param name="match">The predicate that defines the conditions of the member to find.</param>
		/// <returns>The member found, or null.</returns>
		public IParameter Find(Predicate<IParameter> match)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (match == null) throw new ArgumentNullException("match", "Predicate cannot be null.");
			return _Members.Find(match);
		}

		/// <summary>
		/// Returns the collection of members in this instance that match the conditions given in
		/// the predicate. This collection might be empty if there were no members that match that
		/// conditions.
		/// </summary>
		/// <param name="match">The predicate that defines the conditions of the members to find.</param>
		/// <returns>A collection with the members found.</returns>
		public IEnumerable<IParameter> FindAll(Predicate<IParameter> match)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (match == null) throw new ArgumentNullException("match", "Predicate cannot be null.");
			return _Members.FindAll(match);
		}

		/// <summary>
		/// Gets the member whose name is given, or null if not such member can be found.
		/// </summary>
		/// <param name="name">The name of the member to find.</param>
		/// <returns>The member found, or null.</returns>
		public IParameter FindName(string name)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			name = Core.Parameter.ValidateName(name);

			return _Members.Find(x => string.Compare(x.Name, name, !CaseSensitiveNames) == 0);
		}

		/// <summary>
		/// Factory method invoked to create a new orphan member but with the right type for
		/// this collection.
		/// </summary>
		/// <returns>A new orphan member.</returns>
		public virtual IParameter CreateOrphanMember()
		{
			return new Parameter();
		}

		/// <summary>
		/// Adds the given orphan instance into this collection.
		/// </summary>
		/// <param name="member">The orphan instance to add into this collection.</param>
		public void Add(IParameter member)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			if (member == null) throw new ArgumentNullException("member", "Member cannot be null.");
			if (member.IsDisposed) throw new ObjectDisposedException(member.ToString());

			if (object.ReferenceEquals(this, member.Owner)) return;
			if (member.Owner != null) throw new NotOrphanException(
				"Cannot add member '{0}' into this '{1}' because it is not orphan."
				.FormatWith(member, this));

			Core.Parameter.ValidateName(member.Name);

			var temp = FindName(member.Name);
			if (temp != null) throw new DuplicateException(
				"Cannot add member '{0}' into this '{1}' because its name is already used."
				.FormatWith(member, this));

			_Members.Add(member); // To intercept re-entrant operation...
			member.Owner = this;
		}

		/// <summary>
		/// Creates and add into this collection a new member using the arguments given.
		/// </summary>
		/// <param name="name">The name of the new member to add.</param>
		/// <param name="value">The value the new member will hold.</param>
		public IParameter AddCreate(string name, object value)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			var member = CreateOrphanMember();
			member.Name = name;
			member.Value = value;

			member.Owner = this;
			return member;
		}

		/// <summary>
		/// Creates and adds into this collection a new member to hold the given value, whose
		/// name is automatically built using the default prefix of this collection plus an
		/// ordinal number.
		/// </summary>
		/// <param name="value">The value the new member will hold.</param>
		public IParameter AddCreate(object value)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			int count = Count; while (count < int.MaxValue)
			{
				var name = string.Format("{0}{1}", Prefix, count);
				var member = FindName(name);

				if (member == null) return AddCreate(name, value);
				count++;
			}
			throw new ArgumentOutOfRangeException("Max number of members reached.");
		}

		/// <summary>
		/// Adds the given range of members into this collection, optionally cloning those that
		/// were not orphan ones.
		/// </summary>
		/// <param name="range">The range of members to add into this collection.</param>
		/// <param name="cloneNotOrphans">True to clone those member in the range that were
		/// not orphan ones, or false to throw an exception if such scenario ocurrs.</param>
		public void AddRange(IEnumerable<IParameter> range, bool cloneNotOrphans = true)
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
		public bool Remove(IParameter member)
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
				var members = new List<IParameter>(_Members);
				foreach (var member in members) if (!member.IsDisposed) member.Dispose();
				members.Clear(); members = null;
			}

			_Members.Clear();
		}
	}
}
// ======================================================== 
