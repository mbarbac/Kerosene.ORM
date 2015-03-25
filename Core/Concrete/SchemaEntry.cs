// ======================================================== SchemaEntry.cs
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
	/// Represents the metadata of a given table-column entry on a given a given schema.
	/// </summary>
	[Serializable]
	public class SchemaEntry : ISchemaEntry
	{
		bool _IsDisposed = false;
		ISchema _Owner = null;
		Dictionary<string, object> _Metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		public SchemaEntry() { }

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

		~SchemaEntry()
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
				if (_Owner != null)
				{
					var temp = _Owner; _Owner = null;
					if (temp != null && !temp.IsDisposed) temp.Remove(this);
				}
				if (_Metadata != null) _Metadata.Clear();
			}
			_Owner = null;
			_Metadata = null;

			_IsDisposed = true;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <param name="extended">True to obtain the extended representation instead of the
		/// standard one.</param>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public string ToString(bool extended)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(Core.SchemaEntry.NormalizedName(TableName, ColumnName));

			if (!IsDisposed && extended)
			{
				int count = Count;
				if (_Metadata.ContainsKey(TableNameTag)) count--;
				if (_Metadata.ContainsKey(ColumnNameTag)) count--;

				if (count > 0)
				{
					sb.Append(" {"); bool first = true; foreach (var kvp in _Metadata)
					{
						if (string.Compare(kvp.Key, TableNameTag, ignoreCase: true) == 0) continue;
						if (string.Compare(kvp.Key, ColumnNameTag, ignoreCase: true) == 0) continue;

						if (first) first = false; else sb.Append(", ");
						sb.AppendFormat("{0} = '{1}'", kvp.Key, kvp.Value.Sketch());
					}
					sb.Append("}");
				}
			}

			var str = sb.ToString();
			return IsDisposed ? string.Format("disposed::{0}({1})", GetType().EasyName(), str) : str;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			return ToString(extended: false);
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
		}

		/// <summary>
		/// Protected initializer required for custom serialization.
		/// </summary>
		protected SchemaEntry(SerializationInfo info, StreamingContext context)
		{
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
			if (cloned == null) throw new InvalidCastException(
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
		/// Returns true if the state of this object can be considered as equivalent to the target
		/// one, based upon any arbitrary criteria implemented in this method.
		/// </summary>
		/// <param name="target">The target instance this one will be tested for equivalence against.</param>
		/// <returns>True if the state of this instance can be considered as equivalent to the
		/// target one, or false otherwise.</returns>
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

			bool thiscs = this.Owner == null ? Core.Schema.DEFAULT_CASE_SENSITIVE_NAMES : this.Owner.CaseSensitiveNames;
			bool othercs = temp.Owner == null ? Core.Schema.DEFAULT_CASE_SENSITIVE_NAMES : temp.Owner.CaseSensitiveNames;
			if (thiscs != othercs) return false;

			if (this.Count != temp.Count) return false;

			if (string.Compare(TableName, temp.TableName, !thiscs) != 0) return false;
			if (string.Compare(ColumnName, temp.ColumnName, !thiscs) != 0) return false;
			if (IsPrimaryKeyColumn != temp.IsPrimaryKeyColumn) return false;
			if (IsUniqueValuedColumn != temp.IsUniqueValuedColumn) return false;
			if (IsReadOnlyColumn != temp.IsReadOnlyColumn) return false;

			foreach (var kvp in _Metadata)
			{
				if (string.Compare(kvp.Key, TableNameTag, true) == 0) continue;
				if (string.Compare(kvp.Key, ColumnNameTag, true) == 0) continue;
				if (string.Compare(kvp.Key, IsPrimaryKeyColumnTag, true) == 0) continue;
				if (string.Compare(kvp.Key, IsUniqueValuedColumnTag, true) == 0) continue;
				if (string.Compare(kvp.Key, IsReadOnlyColumnTag, true) == 0) continue;

				if (!temp.Contains(kvp.Key)) return false;
				if (!this[kvp.Key].IsEquivalentTo(temp[kvp.Key])) return false;
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
			get { return (IsDisposed ? null : (string)this[TableNameTag]); }
			set { this[TableNameTag] = value; }
		}

		/// <summary>
		/// The column name this entry refers to.
		/// </summary>
		public string ColumnName
		{
			get { return (IsDisposed ? null : (string)this[ColumnNameTag]); }
			set { this[ColumnNameTag] = value; }
		}

		/// <summary>
		/// Whether the column this instance refers to is a primary key one or not.
		/// </summary>
		public bool IsPrimaryKeyColumn
		{
			get { return (IsDisposed ? false : (bool)this[IsPrimaryKeyColumnTag]); }
			set { this[IsPrimaryKeyColumnTag] = value; }
		}

		/// <summary>
		/// Whether the column this instance refers to is an unique valued one or not.
		/// </summary>
		public bool IsUniqueValuedColumn
		{
			get { return (IsDisposed ? false : (bool)this[IsUniqueValuedColumnTag]); }
			set { this[IsUniqueValuedColumnTag] = value; }
		}

		/// <summary>
		/// Whether the column this instance refers to is read only one or not.
		/// </summary>
		public bool IsReadOnlyColumn
		{
			get { return (IsDisposed ? false : (bool)this[IsReadOnlyColumnTag]); }
			set { this[IsReadOnlyColumnTag] = value; }
		}

		/// <summary>
		/// The current metadata this instance carries (including the standard properties), as
		/// a collection of name-value pairs, where their names are case insensitive.
		/// </summary>
		public IEnumerable<KeyValuePair<string, object>> Metadata
		{
			get
			{
				yield return new KeyValuePair<string, object>(TableNameTag, TableName);
				yield return new KeyValuePair<string, object>(ColumnNameTag, ColumnName);
				yield return new KeyValuePair<string, object>(IsPrimaryKeyColumnTag, IsPrimaryKeyColumn);
				yield return new KeyValuePair<string, object>(IsUniqueValuedColumnTag, IsUniqueValuedColumn);
				yield return new KeyValuePair<string, object>(IsReadOnlyColumnTag, IsReadOnlyColumn);

				if (_Metadata != null)
				{
					foreach (var kvp in _Metadata)
					{
						if (string.Compare(kvp.Key, TableNameTag, true) == 0) continue;
						if (string.Compare(kvp.Key, ColumnNameTag, true) == 0) continue;
						if (string.Compare(kvp.Key, IsPrimaryKeyColumnTag, true) == 0) continue;
						if (string.Compare(kvp.Key, IsUniqueValuedColumnTag, true) == 0) continue;
						if (string.Compare(kvp.Key, IsReadOnlyColumnTag, true) == 0) continue;

						yield return kvp;
					}
				}
			}
		}

		/// <summary>
		/// The number of metadata entries this instance contains, always taking into consideratin
		/// the standard ones.
		/// </summary>
		public int Count
		{
			get { return _Metadata == null ? 0 : Metadata.Count(); }
		}

		/// <summary>
		/// Returns whether a metadata entry with the given case insensitive name exists is in
		/// this collection.
		/// </summary>
		/// <param name="metadata">The metadata name to validate.</param>
		/// <returns>True if a metadata entry with the given name is part of this collection, or
		/// false otherwise.</returns>
		public bool Contains(string metadata)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			bool r = metadata == null ? false : _Metadata.ContainsKey(metadata);
			return r;
		}

		/// <summary>
		/// Gets or sets the value of the metadata entry whose case insensitive name is given.
		/// <para>- The getter throws an exception if the requested entry does not exist.</para>
		/// <para>- The setter adds a new entry if the requested one did not exist.</para>
		/// </summary>
		/// <param name="metadata">The name of the metadata entry.</param>
		/// <returns>The member at the given position.</returns>
		public object this[string metadata]
		{
			get
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				metadata = metadata ?? string.Empty;

				object value = null;
				if (_Metadata.TryGetValue(metadata, out value)) return value;

				if (string.Compare(metadata, TableNameTag, true) == 0) return null;
				if (string.Compare(metadata, ColumnNameTag, true) == 0) return null;
				if (string.Compare(metadata, IsPrimaryKeyColumnTag, true) == 0) return false;
				if (string.Compare(metadata, IsUniqueValuedColumnTag, true) == 0) return false;
				if (string.Compare(metadata, IsReadOnlyColumnTag, true) == 0) return false;

				throw new NotFoundException("Metadata '{0}' not found in this '{1}'"
					.FormatWith(metadata, this.ToString(extended: true)));
			}
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (_Owner != null) throw new NotOrphanException("This '{0}' is not orphan.".FormatWith(this));
				metadata = metadata.Validated("Metadata Name");

				if (string.Compare(metadata, TableNameTag, true) == 0) { _Metadata[metadata] = Core.SchemaEntry.ValidateTable((string)value); return; }
				if (string.Compare(metadata, ColumnNameTag, true) == 0) { _Metadata[metadata] = Core.SchemaEntry.ValidateColumn((string)value); return; }
				if (string.Compare(metadata, IsPrimaryKeyColumnTag, true) == 0) { _Metadata[metadata] = (bool)(value == null ? false : value); return; }
				if (string.Compare(metadata, IsUniqueValuedColumnTag, true) == 0) { _Metadata[metadata] = (bool)(value == null ? false : value); return; }
				if (string.Compare(metadata, IsReadOnlyColumnTag, true) == 0) { _Metadata[metadata] = (bool)(value == null ? false : value); return; }

				_Metadata[metadata] = value;
			}
		}

		/// <summary>
		/// Removes from this collection the metadata entry whose case insensitive name is given.
		/// Returns true if it has been removed succesfully, or false otherwise.
		/// </summary>
		/// <param name="metadata">The name of the metadata entry.</param>
		/// <returns>True if the metadata entry has been removed succesfully, or false otherwise.</returns>
		public bool Remove(string metadata)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (_Owner != null) throw new NotOrphanException("This '{0}' is not orphan.".FormatWith(this));
			metadata = metadata.Validated("Metadata Name");

			return _Metadata.Remove(metadata);
		}

		/// <summary>
		/// Clears all the metadata entries this instance may carry.
		/// <para>- Note that, as a side effect, the standard properties will return their default
		/// values.</para>
		/// </summary>
		public void Clear()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (_Owner != null) throw new NotOrphanException("This '{0}' is not orphan.".FormatWith(this));

			_Metadata.Clear();
		}

		/// <summary>
		/// The tag to identify the 'table name' entry in a metadata collection.
		/// </summary>
		public string TableNameTag
		{
			get { return _TableNameTag; }
			set { _TableNameTag = value.Validated("Current Table Name Tag"); }
		}
		string _TableNameTag = Core.SchemaEntry.DEFAULT_TABLE_NAME_TAG;

		/// <summary>
		/// The tag to identify the 'column name' entry in a metadata collection.
		/// </summary>
		public string ColumnNameTag
		{
			get { return _ColumnNameTag; }
			set { _ColumnNameTag = value.Validated("Current Column Name Tag"); }
		}
		string _ColumnNameTag = Core.SchemaEntry.DEFAULT_COLUMN_NAME_TAG;

		/// <summary>
		/// The tag to identify the 'is primary key' entry in a metadata collection.
		/// </summary>
		public string IsPrimaryKeyColumnTag
		{
			get { return _IsPrimaryKeyColumnTag; }
			set { _IsPrimaryKeyColumnTag = value.Validated("Current Primary Key Column Tag"); }
		}
		string _IsPrimaryKeyColumnTag = Core.SchemaEntry.DEFAULT_IS_PRIMARY_KEY_COLUMN_TAG;

		/// <summary>
		/// The tag to identify the 'is unique valued' entry in a metadata collection.
		/// </summary>
		public string IsUniqueValuedColumnTag
		{
			get { return _IsUniqueValuedColumnTag; }
			set { _IsUniqueValuedColumnTag = value.Validated("Current Unique Valued Column Tag"); }
		}
		string _IsUniqueValuedColumnTag = Core.SchemaEntry.DEFAULT_IS_UNIQUE_VALUED_COLUMN_TAG;

		/// <summary>
		/// The tag to identify the 'is read only' entry in a metadata collection.
		/// </summary>
		public string IsReadOnlyColumnTag
		{
			get { return _IsReadOnlyColumnTag; }
			set { _IsReadOnlyColumnTag = value.Validated("Current Read Only Column Tag"); }
		}
		string _IsReadOnlyColumnTag = Core.SchemaEntry.DEFAULT_IS_READ_ONLY_COLUMN_TAG;
	}
}
// ======================================================== 
