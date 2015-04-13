// ======================================================== Schema.cs
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
	/// Represents the schema that describes the metadata and structure of the records returned
	/// from the execution of an enumerable command.
	/// </summary>
	[Serializable]
	public partial class Schema : ISchema
	{
		bool _IsDisposed = false;
#if OLD_702
		List<ISchemaEntry> _Members = new List<ISchemaEntry>();
#else
		EntryList _Members = null;
#endif
		bool _CaseSensitiveNames = Core.Schema.DEFAULT_CASESENSITIVE_NAMES;
		IElementAliasCollection _Aliases = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="caseSensitiveNames">Whether the table and column names in this collection
		/// are case sensitive or not.</param>
		public Schema(
			bool caseSensitiveNames = Core.Schema.DEFAULT_CASESENSITIVE_NAMES)
		{
			_CaseSensitiveNames = caseSensitiveNames;
			if ((_Aliases = CreateAliasCollection()) == null)
				throw new CannotCreateException("Cannot create a collection of aliases.");
#if OLD_702
			List<ISchemaEntry> _Members = new List<ISchemaEntry>();
#else
			_Members = new EntryList(caseSensitiveNames);
#endif
		}

		/// <summary>
		/// Call-back method invoked to create the collection of aliases of this instance.
		/// </summary>
		protected virtual IElementAliasCollection CreateAliasCollection()
		{
			return new ElementAliasCollection(CaseSensitiveNames);
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

		~Schema()
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
					var list = _Members.ToArray();
					foreach (var member in list) member.Dispose();
					list = null;
				}

				if (_Aliases != null && !_Aliases.IsDisposed) _Aliases.Dispose();
			}
			_Members = null;
			_Aliases = null;

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
				bool first = true; foreach (var entry in _Members)
				{
					if (first) first = false; else sb.Append(", ");
					sb.Append(entry);
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
			info.AddExtended("Aliases", _Aliases);

#if OLD_702
			info.AddExtended("List", _Members);
#else
			int count = 0; foreach (var member in _Members)
			{
				info.AddExtended("Member" + count, member);
				count++;
			}
			info.AddValue("MembersCount", count);
#endif 
		}

		/// <summary>
		/// Protected initializer required for custom serialization.
		/// </summary>
		protected Schema(SerializationInfo info, StreamingContext context)
		{
			_CaseSensitiveNames = info.GetBoolean("CaseSensitiveNames");
			_Aliases = info.GetExtended<IElementAliasCollection>("Aliases");

#if OLD_702
			_Members = info.GetExtended<List<ISchemaEntry>>("List");
#else
			_Members = new EntryList(_CaseSensitiveNames);
			int count = (int)info.GetValue("MembersCount", typeof(int));
			for (int i = 0; i < count; i++)
			{
				var member = info.GetExtended<ISchemaEntry>("Member" + i);
				_Members.Add(member);
			}
#endif
		}

		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		public Schema Clone()
		{
			var cloned = new Schema(CaseSensitiveNames);
			OnClone(cloned); return cloned;
		}
		ISchema ISchema.Clone()
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
			var temp = cloned as Schema;
			if (cloned == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' one."
				.FormatWith(cloned.Sketch(), typeof(Schema).EasyName()));

			temp.Aliases.AddRange(_Aliases, cloneNotOrphans: true);
			temp.AddRange(_Members, cloneNotOrphans: true);
		}

		/// <summary>
		/// Returns true if this object can be considered as equivalent to the target one given.
		/// </summary>
		/// <param name="target">The target object this one will be tested for equivalence.</param>
		/// <returns>True if this object can be considered as equivalent to the target one given.</returns>
		public bool EquivalentTo(ISchema target)
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
			var temp = target as ISchema; if (temp == null) return false;
			if (temp.IsDisposed) return false;
			if (IsDisposed) return false;

			if (this.CaseSensitiveNames != temp.CaseSensitiveNames) return false;
			if (!this.Aliases.EquivalentTo(temp.Aliases)) return false;

			if (this.Count != temp.Count) return false; foreach (var item in temp)
			{
				var member = FindEntry(item.TableName, item.ColumnName); if (member == null) return false;
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
		/// The collection of aliases used in the context of this instance.
		/// </summary>
		public IElementAliasCollection Aliases
		{
			get { return _Aliases; }
		}

		/// <summary>
		/// Obtains an enumerator for the members of this instance.
		/// </summary>
		/// <returns>A new enumerator for the members of this instance.</returns>
		public IEnumerator<ISchemaEntry> GetEnumerator()
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
		public ISchemaEntry this[int index]
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
		public int IndexOf(ISchemaEntry member)
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
		public bool Contains(ISchemaEntry member)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (member == null) throw new ArgumentNullException("Member cannot be null.");

			return _Members.Contains(member);
		}

		/// <summary>
		/// Returns the member whose table and column name are given, or null if not such member
		/// can be found.
		/// </summary>
		/// <param name="tableName">The table name of the member to find, or null to refer to the
		/// default one in this context.</param>
		/// <param name="columnName">The column name.</param>
		/// <returns>The member found, or null.</returns>
		public ISchemaEntry FindEntry(string table, string column)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			table = Core.SchemaEntry.ValidateTable(table);
			column = Core.SchemaEntry.ValidateColumn(column);

			var alias = (IElementAlias)(table == null ? null : Aliases.FindAlias(table));
			if (alias != null) table = alias.Element;

#if OLD_702
			var name = Core.SchemaEntry.NormalizedName(table, column);
			return _Members.Find(x =>
				string.Compare(Core.SchemaEntry.NormalizedName(x.TableName, x.ColumnName), name, !CaseSensitiveNames) == 0);
#else
			return _Members.FindEntry(table, column);
#endif
		}

		/// <summary>
		/// Returns the unique member whose column name is given, or null if no such member can
		/// be found. If the collection contains several members with the same column name, even
		/// if they belong to different tables, an exception is thrown by default.
		/// </summary>
		/// <param name="columnName">The column name.</param>
		/// <param name="raise">True to raise an exception if several columns are found sharing
		/// the same name. If false then null is returned in that case.</param>
		/// <returns>The member found, or null.</returns>
		public ISchemaEntry FindEntry(string column, bool raise = true)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			column = Core.SchemaEntry.ValidateColumn(column);

#if OLD_702
			var list = _Members.FindAll(x => string.Compare(x.ColumnName, column, !CaseSensitiveNames) == 0);
#else
			var list = _Members.FindColumn(column);
#endif
			if (list.Count == 0) return null;
			if (list.Count == 1) return list[0];

			list.Clear(); list = null;
			if (raise) throw new DuplicateException(
					"Column name '{0}' found in several entries: '{1}'.".FormatWith(column, list.Sketch()));

			return null;
		}

		/// <summary>
		/// Gets the member whose table and colum name are obtained parsing the given dynamic
		/// lambda expression, using either the 'x => x.Table.Column' or 'x => x.Column' forms,
		/// or null if no such member can be found. In the later case, if the collection contains
		/// several members with the same column name, even if they belong to different tables, an
		/// exception is thrown.
		/// </summary>
		/// <param name="spec">A dynamic lambda expressin that resolves into the specification
		/// of the entry to find.</param>
		/// <returns>The member found, or null.</returns>
		public ISchemaEntry FindEntry(Func<dynamic, object> spec)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (spec == null) throw new ArgumentNullException("spec", "Entry specification cannot be null.");

			var name = DynamicInfo.ParseName(spec);
			var parts = name.Split('.');

			if (parts.Length == 1) return FindEntry(parts[0]);
			if (parts.Length == 1) return FindEntry(parts[0], parts[1]);

			throw new FormatException("Invalid specification '{0}'.".FormatWith(name));
		}

		/// <summary>
		/// Gets the collection of entries where the given table name is used.
		/// </summary>
		/// <param name="table">The table name of the member to find, or null to refer to the
		/// default one in this context.</param>
		/// <returns>The requested collection of entries.</returns>
		public IEnumerable<ISchemaEntry> FindTable(string table)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			table = Core.SchemaEntry.ValidateTable(table);

			var alias = (IElementAlias)(table == null ? null : Aliases.FindAlias(table));
			if (alias != null) table = alias.Element;

#if OLD_702
			return _Members.FindAll(x => string.Compare(x.TableName, table, !CaseSensitiveNames) == 0);
#else
			return _Members.FindTable(table);
#endif
		}

		/// <summary>
		/// Gets the collection of entries where the given column name is used.
		/// </summary>
		///	<param name="column">The column name.</param>
		/// <returns>The requested collection of entries.</returns>
		public IEnumerable<ISchemaEntry> FindColumn(string column)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			column = Core.SchemaEntry.ValidateColumn(column);

#if OLD_702
			return _Members.FindAll(x => string.Compare(x.ColumnName, column, !CaseSensitiveNames) == 0);
#else
			return _Members.FindColumn(column);
#endif
		}

		/// <summary>
		/// Gets the collection of entries that refer to primary key columns.
		/// </summary>
		/// <returns>The requested collection.</returns>
		public IEnumerable<ISchemaEntry> PrimaryKeyColumns()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

#if OLD_702
			return _Members.FindAll(x => x.IsPrimaryKeyColumn);
#else
			return _Members.Where(x => x.IsPrimaryKeyColumn);
#endif
		}

		/// <summary>
		/// Gets the collection of entries that refer to unique valued columns.
		/// </summary>
		/// <returns>The requested collection.</returns>
		public IEnumerable<ISchemaEntry> UniqueValuedColumns()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

#if OLD_702
			return _Members.FindAll(x => x.IsUniqueValuedColumn);
#else
			return _Members.Where(x => x.IsUniqueValuedColumn);
#endif
		}

		/// <summary>
		/// Adds the given orphan instance into this collection.
		/// </summary>
		/// <param name="member">The orphan instance to add into this collection.</param>
		public void Add(ISchemaEntry member)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			if (member == null) throw new ArgumentNullException("member", "Member cannot be null.");
			if (member.IsDisposed) throw new ObjectDisposedException(member.ToString());

			if (object.ReferenceEquals(this, member.Owner)) return;
			if (member.Owner != null) throw new NotOrphanException(
				"Cannot add member '{0}' into this '{1}' because it is not orphan."
				.FormatWith(member, this));

			Core.SchemaEntry.ValidateTable(member.TableName);
			Core.SchemaEntry.ValidateColumn(member.ColumnName);

			var alias = (IElementAlias)(member.TableName == null ? null : Aliases.FindAlias(member.TableName));
			if (alias != null) member.TableName = alias.Element;

			var temp = FindEntry(member.TableName, member.ColumnName);
			if (temp != null) throw new DuplicateException(
				"Cannot add member '{0}' into this '{1}' because its name is already used."
				.FormatWith(member, this));

			var list = FindColumn(member.ColumnName).ToList(); if (list.Count != 0)
			{
				if (member.TableName == null) throw new DuplicateException(
					"Cannot add member '{0}' into this '{1}' because its column is already used in a non-default table."
					.FormatWith(member, this));

				temp = list.Find(x => x.TableName == null);
				if (temp != null) throw new DuplicateException(
					"Cannot add member '{0}' into this '{1}' because its name is already used in the default table."
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
		public ISchemaEntry CreateOrphanMember()
		{
			return new SchemaEntry();
		}

		/// <summary>
		/// Creates and add into this collection a new member using the arguments given.
		/// </summary>
		/// <param name="table">The table name of the member to find, or null to refer to the
		/// default one in this context.</param>
		/// <param name="column">The column name.</param>
		public ISchemaEntry AddCreate(string table, string column)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			var member = CreateOrphanMember();
			member.TableName = table;
			member.ColumnName = column;

			member.Owner = this;
			return member;
		}

		/// <summary>
		/// Creates and add into this collection a new member with the given column name for the
		/// default table.
		/// </summary>
		/// <param name="column">The column name.</param>
		public ISchemaEntry AddCreate(string column)
		{
			return AddCreate(null, column);
		}

		/// <summary>
		/// Adds the given range of members into this collection, optionally cloning those that
		/// were not orphan ones.
		/// </summary>
		/// <param name="range">The range of members to add into this collection.</param>
		/// <param name="cloneNotOrphans">True to clone those member in the range that were
		/// not orphan ones, or false to throw an exception if such scenario ocurrs.</param>
		public void AddRange(IEnumerable<ISchemaEntry> range, bool cloneNotOrphans = true)
		{
			if (IsDisposed) throw new ObjectDisposedException(ToString());
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
		public bool Remove(ISchemaEntry member)
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
				var list = _Members.ToArray(); foreach (var member in list) member.Dispose();
				Array.Clear(list, 0, list.Length);
			}

			_Members.Clear();
		}
	}
}
// ======================================================== 
