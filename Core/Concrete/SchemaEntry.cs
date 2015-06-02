using Kerosene.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Kerosene.ORM.Core.Concrete
{
	// ==================================================== 
	/// <summary>
	/// Represents the metadata associated with a given table and column combination on a given
	/// schema.
	/// </summary>
	[Serializable]
	public class SchemaEntry : ISchemaEntry
	{
		bool _IsDisposed = false;
		ISchema _Owner = null;
		Dictionary<string, object> _Metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		string _TableNameTag = Core.SchemaEntry.DEFAULT_TABLE_NAME_TAG;
		string _ColumnNameTag = Core.SchemaEntry.DEFAULT_COLUMN_NAME_TAG;
		string _IsPrimaryKeyColumnTag = Core.SchemaEntry.DEFAULT_IS_PRIMARY_KEY_COLUMN_TAG;
		string _IsUniqueValuedColumnTag = Core.SchemaEntry.DEFAULT_IS_UNIQUE_VALUED_COLUMN_TAG;
		string _IsReadOnlyColumnTag = Core.SchemaEntry.DEFAULT_IS_READ_ONLY_COLUMN_TAG;
		SchemaEntryMetadata _EntryMetadata = null;
		SchemaEntryTags _EntryTags = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		public SchemaEntry()
		{
			_EntryMetadata = new SchemaEntryMetadata() { _Entry = this };
			_EntryTags = new SchemaEntryTags() { _Entry = this };
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

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		protected virtual void OnDispose(bool disposing)
		{
			if (disposing)
			{
				try
				{
					if (_Owner != null)
					{
						var temp = _Owner; _Owner = null;
						if (temp != null && !temp.IsDisposed) temp.Remove(this);
					}

					if (_Metadata != null) _Metadata.Clear();

					if (_EntryMetadata != null) _EntryMetadata._Entry = null;
					if (_EntryTags != null) _EntryTags._Entry = null;
				}
				catch { }
			}

			_Owner = null;
			_Metadata = null;
			_EntryMetadata = null;
			_EntryTags = null;

			_IsDisposed = true;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			if (IsDisposed) return string.Format("disposed::{0}", GetType().EasyName());

			StringBuilder sb = new StringBuilder();
			sb.Append("[");
			sb.Append(Core.SchemaEntry.NormalizedName(TableName, ColumnName));
			if (Metadata.Contains(Tags.IsPrimaryKeyColumnTag) && IsPrimaryKeyColumn) sb.AppendFormat(", PrimaryKey:{0}", IsPrimaryKeyColumn);
			if (Metadata.Contains(Tags.IsUniqueValuedColumnTag) && IsUniqueValuedColumn) sb.AppendFormat(", UniqueValued:{0}", IsUniqueValuedColumn);
			if (Metadata.Contains(Tags.IsReadOnlyColumnTag) && IsReadOnlyColumn) sb.AppendFormat(", ReadOnly:{0}", IsReadOnlyColumn);
			sb.Append("]");
			return sb.ToString();
		}

		/// <summary>
		/// Call-back method required for custom serialization.
		/// </summary>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			// As serialization is quite fragile we will only consider types that are known to the CLR...
			int count = 0; foreach (var kvp in _Metadata)
			{
				var type = kvp.Value == null ? null : kvp.Value.GetType();
				var valid = false;

				if (type == null) valid = true;
				else if (type == typeof(bool)) valid = true;
				else if (type == typeof(bool?)) valid = true;
				else if (type == typeof(byte)) valid = true;
				else if (type == typeof(byte?)) valid = true;
				else if (type == typeof(char)) valid = true;
				else if (type == typeof(char?)) valid = true;
				else if (type == typeof(short)) valid = true;
				else if (type == typeof(ushort)) valid = true;
				else if (type == typeof(int)) valid = true;
				else if (type == typeof(uint)) valid = true;
				else if (type == typeof(long)) valid = true;
				else if (type == typeof(ulong)) valid = true;
				else if (type == typeof(float)) valid = true;
				else if (type == typeof(double)) valid = true;
				else if (type == typeof(string)) valid = true;
				else if (type == typeof(DateTime)) valid = true;
				else if (type.Name == "RuntimeType") valid = true;

				if (!valid) continue;

				if (type != null && type.Name == "RuntimeType")
				{
					info.AddValue("MetaName" + count, kvp.Key);
					info.AddValue("MetaType" + count, type.Name);
					info.AddValue("MetaValue" + count, ((Type)kvp.Value).FullName);
				}
				else
				{
					info.AddValue("MetaName" + count, kvp.Key);
					info.AddValue("MetaType" + count, type == null ? "NULL" : type.AssemblyQualifiedName);
					if (kvp.Value != null) info.AddValue("MetaValue" + count, kvp.Value);
				}

				count++;
			}

			info.AddValue("MetaCount", count);

			info.AddValue("TableNameTag", _TableNameTag);
			info.AddValue("ColumnNameTag", _ColumnNameTag);
			info.AddValue("IsPrimaryKeyColumnTag", _IsPrimaryKeyColumnTag);
			info.AddValue("IsUniqueValuedColumnTag", _IsUniqueValuedColumnTag);
			info.AddValue("_IsReadOnlyColumnTag", _IsReadOnlyColumnTag);
		}

		/// <summary>
		/// Protected initializer required for custom serialization.
		/// </summary>
		protected SchemaEntry(SerializationInfo info, StreamingContext context)
		{
			_EntryMetadata = new SchemaEntryMetadata() { _Entry = this };
			_EntryTags = new SchemaEntryTags() { _Entry = this };

			int count = (int)info.GetValue("MetaCount", typeof(int));

			for (int i = 0; i < count; i++)
			{
				string name = info.GetString("MetaName" + i);
				string meta = info.GetString("MetaType" + i);
				object value = null;

				if (meta == "RuntimeType")
				{
					string temp = info.GetString("MetaValue" + i);
					Type type = Type.GetType(temp);
					value = type;
				}
				else if (meta == "NULL")
				{
					value = null;
				}
				else
				{
					Type type = Type.GetType(meta);
					value = info.GetValue("MetaValue" + i, type);
				}

				_Metadata[name] = value;
			}

			_TableNameTag = info.GetString("TableNameTag");
			_ColumnNameTag = info.GetString("ColumnNameTag");
			_IsPrimaryKeyColumnTag = info.GetString("IsPrimaryKeyColumnTag");
			_IsUniqueValuedColumnTag = info.GetString("IsUniqueValuedColumnTag");
			_IsReadOnlyColumnTag = info.GetString("_IsReadOnlyColumnTag");
		}

		/// <summary>
		/// Returns a new instance that otherwise is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		public SchemaEntry Clone()
		{
			var cloned = new SchemaEntry();
			OnClone(cloned); return cloned;
		}
		ISchemaEntry ISchemaEntry.Clone()
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
			var temp = cloned as SchemaEntry;
			if (temp == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' one."
				.FormatWith(cloned.Sketch(), typeof(SchemaEntry).EasyName()));

			foreach (var kvp in _Metadata)
				temp._Metadata.Add(
					kvp.Key,
					kvp.Value is ICloneable
						? ((ICloneable)kvp.Value).Clone()
						: kvp.Value);
		}

		/// <summary>
		/// Returns true if this object can be considered as equivalent to the target one given.
		/// </summary>
		/// <param name="target">The target object this one will be tested for equivalence.</param>
		/// <returns>True if this object can be considered as equivalent to the target one given.</returns>
		public bool EquivalentTo(ISchemaEntry target)
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
			var temp = target as ISchemaEntry; if (temp == null) return false;
			if (temp.IsDisposed) return false;
			if (IsDisposed) return false;

			bool thiscs = this.Owner == null ? Core.Schema.DEFAULT_CASESENSITIVE_NAMES : this.Owner.CaseSensitiveNames;
			bool othercs = temp.Owner == null ? Core.Schema.DEFAULT_CASESENSITIVE_NAMES : temp.Owner.CaseSensitiveNames;
			if (thiscs != othercs) return false;

			if (this.Metadata.Count != temp.Metadata.Count) return false;

			if (string.Compare(TableName, temp.TableName, !thiscs) != 0) return false;
			if (string.Compare(ColumnName, temp.ColumnName, !thiscs) != 0) return false;
			if (IsPrimaryKeyColumn != temp.IsPrimaryKeyColumn) return false;
			if (IsUniqueValuedColumn != temp.IsUniqueValuedColumn) return false;
			if (IsReadOnlyColumn != temp.IsReadOnlyColumn) return false;

			foreach (var kvp in _Metadata)
			{
				if (string.Compare(kvp.Key, Tags.TableNameTag, true) == 0) continue;
				if (string.Compare(kvp.Key, Tags.ColumnNameTag, true) == 0) continue;
				if (string.Compare(kvp.Key, Tags.IsPrimaryKeyColumnTag, true) == 0) continue;
				if (string.Compare(kvp.Key, Tags.IsUniqueValuedColumnTag, true) == 0) continue;
				if (string.Compare(kvp.Key, Tags.IsReadOnlyColumnTag, true) == 0) continue;

				if (!temp.Metadata.Contains(kvp.Key)) return false;
				if (!this.Metadata[kvp.Key].IsEquivalentTo(temp.Metadata[kvp.Key])) return false;
			}

			return true;
		}

		/// <summary>
		/// The collection this instance belongs to, if any.
		/// </summary>
		public ISchema Owner
		{
			get { return _Owner; }
			set
			{
				if (value == null)
				{
					var temp = _Owner; _Owner = null;
					if (temp != null && !temp.IsDisposed) temp.Remove(this);
				}
				else
				{
					if (IsDisposed) throw new ObjectDisposedException(this.ToString());

					if (object.ReferenceEquals(value, _Owner)) return;
					if (_Owner != null) throw new NotOrphanException(
						"This '{0}' is not an orphan one.".FormatWith(this));

					// To intercept the re-entrant operation...
					if (!value.Contains(this)) value.Add(this);
					_Owner = value;
				}
			}
		}

		/// <summary>
		/// The table this entry refers to, or null if it is the default one in a given context.
		/// </summary>
		public string TableName
		{
			get { return (IsDisposed ? null : (string)Metadata[Tags.TableNameTag]); }
			set { Metadata[Tags.TableNameTag] = Core.SchemaEntry.ValidateTable(value); }
		}

		/// <summary>
		/// The column name this entry refers to.
		/// </summary>
		public string ColumnName
		{
			get { return (IsDisposed ? null : (string)Metadata[Tags.ColumnNameTag]); }
			set { Metadata[Tags.ColumnNameTag] = Core.SchemaEntry.ValidateColumn(value); }
		}

		/// <summary>
		/// Whether the column this instance refers to is a primary key one or not.
		/// </summary>
		public bool IsPrimaryKeyColumn
		{
			get { return (IsDisposed ? false : (bool)Metadata[Tags.IsPrimaryKeyColumnTag]); }
			set { Metadata[Tags.IsPrimaryKeyColumnTag] = value; }
		}

		/// <summary>
		/// Whether the column this instance refers to is an unique valued one or not.
		/// </summary>
		public bool IsUniqueValuedColumn
		{
			get { return (IsDisposed ? false : (bool)Metadata[Tags.IsUniqueValuedColumnTag]); }
			set { Metadata[Tags.IsUniqueValuedColumnTag] = value; }
		}

		/// <summary>
		/// Whether the column this instance refers to is read only one or not.
		/// </summary>
		public bool IsReadOnlyColumn
		{
			get { return (IsDisposed ? false : (bool)Metadata[Tags.IsReadOnlyColumnTag]); }
			set { Metadata[Tags.IsReadOnlyColumnTag] = value; }
		}

		/// <summary>
		/// The actual metadata this entry carries.
		/// </summary>
		public ISchemaEntryMetadata Metadata
		{
			get { return _EntryMetadata; }
		}

		/// <summary>
		/// The tags used to identity the standard properties in the metadata collection.
		/// </summary>
		public ISchemaEntryTags Tags
		{
			get { return _EntryTags; }
		}

		// ================================================ 
		internal class SchemaEntryMetadata : ISchemaEntryMetadata
		{
			internal SchemaEntry _Entry = null;

			internal SchemaEntryMetadata() { }

			internal IEnumerable<KeyValuePair<string, object>> Pairs
			{
				get
				{
					yield return new KeyValuePair<string, object>(_Entry.Tags.TableNameTag, _Entry.TableName);
					yield return new KeyValuePair<string, object>(_Entry.Tags.ColumnNameTag, _Entry.ColumnName);
					yield return new KeyValuePair<string, object>(_Entry.Tags.IsPrimaryKeyColumnTag, _Entry.IsPrimaryKeyColumn);
					yield return new KeyValuePair<string, object>(_Entry.Tags.IsUniqueValuedColumnTag, _Entry.IsUniqueValuedColumn);
					yield return new KeyValuePair<string, object>(_Entry.Tags.IsReadOnlyColumnTag, _Entry.IsReadOnlyColumn);

					foreach (var kvp in _Entry._Metadata)
					{
						if (string.Compare(kvp.Key, _Entry.Tags.TableNameTag, true) == 0) continue;
						if (string.Compare(kvp.Key, _Entry.Tags.ColumnNameTag, true) == 0) continue;
						if (string.Compare(kvp.Key, _Entry.Tags.IsPrimaryKeyColumnTag, true) == 0) continue;
						if (string.Compare(kvp.Key, _Entry.Tags.IsUniqueValuedColumnTag, true) == 0) continue;
						if (string.Compare(kvp.Key, _Entry.Tags.IsReadOnlyColumnTag, true) == 0) continue;

						yield return kvp;
					}
				}
			}

			public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
			{
				return Pairs.GetEnumerator();
			}
			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}

			public int Count
			{
				get { return Pairs.Count(); }
			}

			public bool Contains(string name)
			{
				name = name.Validated("Metadata Name");

				if (string.Compare(name, _Entry.Tags.TableNameTag, true) == 0) return true;
				if (string.Compare(name, _Entry.Tags.ColumnNameTag, true) == 0) return true;
				if (string.Compare(name, _Entry.Tags.IsPrimaryKeyColumnTag, true) == 0) return true;
				if (string.Compare(name, _Entry.Tags.IsUniqueValuedColumnTag, true) == 0) return true;
				if (string.Compare(name, _Entry.Tags.IsReadOnlyColumnTag, true) == 0) return true;

				return _Entry._Metadata.ContainsKey(name);
			}

			public object this[string name]
			{
				get
				{
					name = name.Validated("Metadata Name");

					object value = null;
					if (_Entry._Metadata.TryGetValue(name, out value)) return value;

					if (string.Compare(name, _Entry.Tags.TableNameTag, true) == 0) return null;
					if (string.Compare(name, _Entry.Tags.ColumnNameTag, true) == 0) return null;
					if (string.Compare(name, _Entry.Tags.IsPrimaryKeyColumnTag, true) == 0) return false;
					if (string.Compare(name, _Entry.Tags.IsUniqueValuedColumnTag, true) == 0) return false;
					if (string.Compare(name, _Entry.Tags.IsReadOnlyColumnTag, true) == 0) return false;

					throw new NotFoundException(
						"Metadata '{0}' not found in '{1}'".FormatWith(name, _Entry));
				}
				set
				{
					name = name.Validated("Metadata Name");

					if (string.Compare(name, _Entry.Tags.TableNameTag, true) == 0)
					{
						if (_Entry.Owner != null) throw new NotOrphanException("This '{0}' is not orphan.".FormatWith(_Entry));
						_Entry._Metadata[name] = Core.SchemaEntry.ValidateTable((string)value);
						return;
					}
					if (string.Compare(name, _Entry.Tags.ColumnNameTag, true) == 0)
					{
						if (_Entry.Owner != null) throw new NotOrphanException("This '{0}' is not orphan.".FormatWith(_Entry));
						_Entry._Metadata[name] = Core.SchemaEntry.ValidateColumn((string)value);
						return;
					}
					if (string.Compare(name, _Entry.Tags.IsPrimaryKeyColumnTag, true) == 0)
					{
						_Entry._Metadata[name] = (bool)(value == null ? false : value);
						return;
					}
					if (string.Compare(name, _Entry.Tags.IsUniqueValuedColumnTag, true) == 0)
					{
						_Entry._Metadata[name] = (bool)(value == null ? false : value);
						return;
					}
					if (string.Compare(name, _Entry.Tags.IsReadOnlyColumnTag, true) == 0)
					{
						_Entry._Metadata[name] = (bool)(value == null ? false : value);
						return;
					}

					_Entry._Metadata[name] = value;
				}
			}

			public void Add(string name, object value)
			{
				if (Contains(name)) throw new DuplicateException(
					 "Metadata entry '{0}' already exists in '{1}'."
					 .FormatWith(name, _Entry));

				_Entry._Metadata[name] = value;
			}

			public bool Remove(string name)
			{
				name = name.Validated("Metadata Name");
				if (_Entry.Owner != null) throw new NotOrphanException("This '{0}' is not orphan.".FormatWith(_Entry));

				return _Entry._Metadata.Remove(name);
			}

			public void Clear()
			{
				if (_Entry.Owner != null) throw new NotOrphanException("This '{0}' is not orphan.".FormatWith(_Entry));
				_Entry._Metadata.Clear();
			}
		}

		// ================================================ 
		internal class SchemaEntryTags : ISchemaEntryTags
		{
			internal SchemaEntry _Entry = null;

			internal SchemaEntryTags() { }

			/// <summary>
			/// The tag to identify the 'table name' entry in a metadata collection.
			/// </summary>
			public string TableNameTag
			{
				get { return _Entry._TableNameTag; }
				set { _Entry._TableNameTag = value.Validated("Current Table Name Tag"); }
			}

			/// <summary>
			/// The tag to identify the 'column name' entry in a metadata collection.
			/// </summary>
			public string ColumnNameTag
			{
				get { return _Entry._ColumnNameTag; }
				set { _Entry._ColumnNameTag = value.Validated("Current Column Name Tag"); }
			}

			/// <summary>
			/// The tag to identify the 'is primary key' entry in a metadata collection.
			/// </summary>
			public string IsPrimaryKeyColumnTag
			{
				get { return _Entry._IsPrimaryKeyColumnTag; }
				set { _Entry._IsPrimaryKeyColumnTag = value.Validated("Current Primary Key Column Tag"); }
			}

			/// <summary>
			/// The tag to identify the 'is unique valued' entry in a metadata collection.
			/// </summary>
			public string IsUniqueValuedColumnTag
			{
				get { return _Entry._IsUniqueValuedColumnTag; }
				set { _Entry._IsUniqueValuedColumnTag = value.Validated("Current Unique Valued Column Tag"); }
			}

			/// <summary>
			/// The tag to identify the 'is read only' entry in a metadata collection.
			/// </summary>
			public string IsReadOnlyColumnTag
			{
				get { return _Entry._IsReadOnlyColumnTag; }
				set { _Entry._IsReadOnlyColumnTag = value.Validated("Current Read Only Column Tag"); }
			}
		}
	}
}
