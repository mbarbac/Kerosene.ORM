using Kerosene.Tools;
using System;
using System.Collections;
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Kerosene.ORM.Core.Concrete
{
	// ==================================================== 
	/// <summary>
	/// Represents a record on the database, typically obtained by the execution of an enumarable
	/// command, that provides both indexed and dynamic ways to access its contents.
	/// </summary>
	[Serializable]
	public class Record : DynamicObject, IRecord
	{
		bool _IsDisposed = false;
		object[] _Values = null;
		ISchema _Schema = null;
		bool _SerializeSchema = Core.Record.DEFAULT_SERIALIZE_SCHEMA;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="count">The number of columns this record will hold.</param>
		public Record(int count)
		{
			if (count < 1) throw new ArgumentException("Number of entries '{0}' must be one at least.".FormatWith(count));
			_Values = new object[count]; Array.Clear(_Values, 0, count);
		}

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="schema">The schema this record will be associated with.</param>
		public Record(ISchema schema)
		{
			if (schema == null) throw new ArgumentNullException("schema", "Schema cannot be null.");
			if (schema.IsDisposed) throw new ObjectDisposedException(schema.ToString());

			var count = schema.Count;
			if (count < 1) throw new EmptyException("Schema is empty");
			_Values = new object[count]; Array.Clear(_Values, 0, count);
			_Schema = schema;
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
			Dispose(disposeSchema: false);
		}

		/// <summary>
		/// Disposes this instance and optionally disposes the schema it is associated with,
		/// if any.
		/// </summary>
		/// <param name="disposeSchema">True to also dispose the schema this instance is
		/// associated with.</param>
		public void Dispose(bool disposeSchema)
		{
			if (!IsDisposed) { OnDispose(true, disposeSchema); GC.SuppressFinalize(this); }
		}

		/// <summary></summary>
		~Record()
		{
			if (!IsDisposed) OnDispose(false, disposeSchema: false);
		}

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		/// <param name="disposeSchema">True to also dispose the schema this instance is
		/// associated with.</param>
		protected virtual void OnDispose(bool disposing, bool disposeSchema)
		{
			if (disposing)
			{
				if (disposeSchema && _Schema != null && !_Schema.IsDisposed) _Schema.Dispose();
				if (_Values != null) Array.Clear(_Values, 0, _Values.Length);
			}
			_Values = null;
			_Schema = null;

			_IsDisposed = true;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("["); if (_Values != null)
			{
				for (int i = 0; i < _Values.Length; i++)
				{
					if (i != 0) sb.Append(", ");

					var value = _Values[i].Sketch();
					var name = string.Format("#{0}", i);
					if (_Schema != null && !_Schema.IsDisposed && i < _Schema.Count)
						name = Core.SchemaEntry.NormalizedName(_Schema[i].TableName, _Schema[i].ColumnName);

					sb.AppendFormat("{0} = '{1}'", name, value);
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

			info.AddExtended("Values", _Values);
			info.AddValue("SerializeSchema", _SerializeSchema);
			if (SerializeSchema) info.AddExtended("Schema", _Schema);
		}

		/// <summary>
		/// Protected initializer required for custom serialization.
		/// </summary>
		protected Record(SerializationInfo info, StreamingContext context)
		{
			_Values = info.GetExtended<object[]>("Values");
			_SerializeSchema = info.GetBoolean("SerializeSchema");
			if (_SerializeSchema) Schema = (ISchema)info.GetExtended("Schema");
		}

		/// <summary>
		/// Returns a new instance that otherwise is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		public Record Clone()
		{
			return Clone(cloneSchema: false);
		}
		IRecord IRecord.Clone()
		{
			return this.Clone();
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		/// <summary>
		/// Returns a new instance that otherwise is a copy of the original one, and
		/// optionally clones also the original schema it was associated with, if any.
		/// </summary>
		/// <param name="cloneSchema">True to also clone the schema this instance is associated
		/// with.</param>
		/// <returns>A new instance.</returns>
		public Record Clone(bool cloneSchema)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			var cloned = _Schema == null
				? new Record(_Values.Length)
				: new Record(cloneSchema ? _Schema.Clone() : _Schema);

			OnClone(cloned); return cloned;
		}
		IRecord IRecord.Clone(bool cloneSchema)
		{
			return this.Clone(cloneSchema);
		}

		/// <summary>
		/// Invoked when cloning this object to set its state at this point of the inheritance
		/// chain.
		/// </summary>
		/// <param name="cloned">The cloned object.</param>
		protected virtual void OnClone(object cloned)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			var temp = cloned as Record;
			if (temp == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' one."
				.FormatWith(cloned.Sketch(), typeof(Record).EasyName()));

			temp.SerializeSchema = this.SerializeSchema;
			for (int i = 0; i < _Values.Length; i++) temp[i] = this[i].TryClone();
		}

		/// <summary>
		/// Returns true if this object can be considered as equivalent to the target one given.
		/// </summary>
		/// <param name="target">The target object this one will be tested for equivalence.</param>
		/// <returns>True if this object can be considered as equivalent to the target one given.</returns>
		public bool EquivalentTo(IRecord target)
		{
			return OnEquivalentTo(target, onlyValues: false);
		}

		/// <summary>
		/// Returns true if the state of this instance can be considered as equivalent to the
		/// target object given, or false otherwise. Optionally the comparison can be carried
		/// considering only the values and not equivalence of the respective schemas.
		/// </summary>
		/// <param name="target">The target object to test for equivalence against.</param>
		/// <param name="onlyValues">True (by default) to perform the comparison only on the
		/// values and not on their respective schemas.</param>
		/// <returns>True if the state of this instance can be considered as equivalent to the
		/// target object given, or false otherwise</returns>
		public bool EquivalentTo(IRecord target, bool onlyValues)
		{
			return OnEquivalentTo(target, onlyValues);
		}

		/// <summary>
		/// Invoked to test equivalence at this point of the inheritance chain.
		/// </summary>
		/// <param name="target">The target this instance will be tested for equivalence against.</param>
		/// <param name="onlyValues">True to perform the comparison only on the values and not
		/// on their respective schemas.</param>
		/// <returns>True if at this level on the inheritance chain this instance can be considered
		/// equivalent to the target instance given.</returns>
		protected virtual bool OnEquivalentTo(object target, bool onlyValues)
		{
			if (object.ReferenceEquals(this, target)) return true;
			var temp = target as IRecord; if (temp == null) return false;
			if (temp.IsDisposed) return false;
			if (IsDisposed) return false;

			if (temp.Count != this.Count) return false;
			for (int i = 0; i < Count; i++)
				if (!this[i].IsEquivalentTo(temp[i])) return false;

			if (!onlyValues)
			{
				if (this.Schema == null) { if (temp.Schema != null) return false; }
				else { if (!this.Schema.EquivalentTo(temp.Schema)) return false; }
			}

			// SerializeSchema: not considered on purpose!

			return true;
		}

		/// <summary>
		/// The schema that describes the structure and metadata of the contents in this record.
		/// <para>The setter fails if the value is not null and this instance already has a
		/// schema associated with it.</para>
		/// </summary>
		/// <remarks>The value held by this property can be null if this instance is disposed,
		/// or when there is no schema associated to it, which can happen in some border case
		/// scenarios (as for instance while it is being deserialized, among others).</remarks>
		public ISchema Schema
		{
			get { return _Schema; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());

				if (value == null) _Schema = null;
				else
				{
					if (value.IsDisposed) throw new ObjectDisposedException(value.ToString());
					if (value.Count == 0) throw new EmptyException("Schema cannot be empty.");

					if (_Schema != null) throw new InvalidOperationException(
						"This record '{0}' is already associated with a schema.".FormatWith(this));

					if (_Values.Length != value.Count) throw new InvalidOperationException(
						"Lenght of this record '{0}' is not the same as the number of entries in schema '{1}'."
						.FormatWith(this, value));

					_Schema = value;
				}
			}
		}

		/// <summary>
		/// Whether the schema this instance may has associated with it is serialized along with
		/// this record or not. A value of 'false' can be used when serializing many records
		/// associated with the same schema, for performance reasons, and in this case it is
		/// assumed that the schema reference is set by the receiving environment afterwards.
		/// </summary>
		public bool SerializeSchema
		{
			get { return _SerializeSchema; }
			set { _SerializeSchema = value; }
		}

		/// <summary>
		/// Obtains an enumerator for the members of this instance.
		/// </summary>
		/// <returns>A new enumerator for the members of this instance.</returns>
		public IEnumerator GetEnumerator()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (_Values == null) throw new EmptyException("This record is not initialized.");

			return _Values.GetEnumerator();
		}

		/// <summary>
		/// The number of table-column entries in this record.
		/// </summary>
		public int Count
		{
			get { return _Values == null ? 0 : _Values.Length; }
		}

		/// <summary>
		/// Gets or sets the value held by table-column entry whose index is given.
		/// </summary>
		/// <param name="index">The index of the table-column entry.</param>
		/// <returns>The value held by the entry whose index is given.</returns>
		public object this[int index]
		{
			get
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				return _Values[index];
			}
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				_Values[index] = value;
			}
		}

		/// <summary>
		/// Gets or sets the value stored at the entry whose table and column names are given.
		/// </summary>
		/// <param name="tableName">The table name, or null if it refers to the default one in
		/// this context.</param>
		/// <param name="columnName">The column name.</param>
		/// <returns>The value held at the given entry.</returns>
		public object this[string tableName, string columnName]
		{
			get
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (_Schema == null) throw new InvalidOperationException("This '{0}' is not associated with any schema.".FormatWith(this));
				tableName = Core.SchemaEntry.ValidateTable(tableName);
				columnName = Core.SchemaEntry.ValidateColumn(columnName);

				var entry = _Schema.FindEntry(tableName, columnName);
				if (entry == null) throw new NotFoundException(
					"Entry '{0}' not found in this '{1}'".FormatWith(Core.SchemaEntry.NormalizedName(tableName, columnName), this));

				var index = _Schema.IndexOf(entry);
				return _Values[index];
			}
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (_Schema == null) throw new InvalidOperationException("This '{0}' is not associated with any schema.".FormatWith(this));
				tableName = Core.SchemaEntry.ValidateTable(tableName);
				columnName = Core.SchemaEntry.ValidateColumn(columnName);

				var entry = _Schema.FindEntry(tableName, columnName);
				if (entry == null) throw new NotFoundException(
					"Entry '{0}' not found in this '{1}'".FormatWith(Core.SchemaEntry.NormalizedName(tableName, columnName), this));

				var index = _Schema.IndexOf(entry);
				_Values[index] = value;
			}
		}

		/// <summary>
		/// Gets or sets the value stored at the entry whose unique column name is given.
		/// <para>If the schema of the record contains several columns with the same column
		/// name an exception is thrown.</para>
		/// </summary>
		/// <param name="columnName">The column name.</param>
		/// <returns>The value held at the given entry.</returns>
		public object this[string columnName]
		{
			get
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (_Schema == null) throw new InvalidOperationException("This '{0}' is not associated with any schema.".FormatWith(this));
				columnName = Core.SchemaEntry.ValidateColumn(columnName);

				var entry = _Schema.FindEntry(columnName, raise: true);
				if (entry == null) throw new NotFoundException(
					"Entry '{0}' not found in this '{1}'".FormatWith(columnName, this));

				var index = _Schema.IndexOf(entry);
				return _Values[index];
			}
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (_Schema == null) throw new InvalidOperationException("This '{0}' is not associated with any schema.".FormatWith(this));
				columnName = Core.SchemaEntry.ValidateColumn(columnName);

				var entry = _Schema.FindEntry(columnName, raise: true);
				if (entry == null) throw new NotFoundException(
					"Entry '{0}' not found in this '{1}'".FormatWith(columnName, this));

				var index = _Schema.IndexOf(entry);
				_Values[index] = value;
			}
		}

		/// <summary>
		/// Gets or sets the value stored at the entry whose table and colum name are obtained
		/// parsing the given dynamic lambda expression, using either the 'x => x.Table.Column'
		/// or 'x => x.Column' forms, or null if no such member can be found. In the later case,
		/// if the collection contains several members with the same column name, even if they
		/// belong to different tables, an exception is thrown.
		/// </summary>
		/// <param name="spec">A dynamic lambda expressin that resolves into the specification
		/// of the entry to find.</param>
		/// <returns>The member found, or null.</returns>
		public object this[Func<dynamic, object> spec]
		{
			get
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (_Schema == null) throw new InvalidOperationException("This '{0}' is not associated with any schema.".FormatWith(this));
				if (spec == null) throw new ArgumentNullException("spec", "Entry specification cannot be null.");

				var entry = _Schema.FindEntry(spec);
				if (entry == null) throw new NotFoundException(
					"Entry '{0}' not found in this '{1}'".FormatWith(DynamicInfo.ParseName(spec), this));

				var index = _Schema.IndexOf(entry);
				return _Values[index];
			}
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (_Schema == null) throw new InvalidOperationException("This '{0}' is not associated with any schema.".FormatWith(this));
				if (spec == null) throw new ArgumentNullException("spec", "Entry specification cannot be null.");

				var entry = _Schema.FindEntry(spec);
				if (entry == null) throw new NotFoundException(
					"Entry '{0}' not found in this '{1}'".FormatWith(DynamicInfo.ParseName(spec), this));

				var index = _Schema.IndexOf(entry);
				_Values[index] = value;
			}
		}

		/// <summary>
		/// Clears all the values held by this instance.
		/// </summary>
		public void Clear()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			Array.Clear(_Values, 0, _Values.Length);
		}

		/// <summary>
		/// Invoked when a get memebr operation is resolved dynamically.
		/// </summary>
		/// <param name="binder">Provides information about the operation to execute.</param>
		/// <param name="result">Holder for the result of the operation.</param>
		/// <returns>True if the operation executed successfully, false otherwise.</returns>
		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (_Schema == null) throw new InvalidOperationException("This '{0}' is not associated with any schema.".FormatWith(this));

			var list = _Schema.FindTable(binder.Name).ToList(); if (list.Count != 0)
			{
				result = new RecordResolver(this, binder.Name); list.Clear(); list = null;
				return true;
			}

			list = _Schema.FindColumn(binder.Name).ToList(); if (list.Count == 1)
			{
				result = this[binder.Name]; list.Clear(); list = null;
				return true;
			}
			if (list.Count == 0) throw new NotFoundException("Column '{0}' not found.".FormatWith(binder.Name));
			throw new DuplicateException("Dynamic name '{0}' found in several columns in '{1}'.".FormatWith(binder.Name, list.Sketch()));
		}

		/// <summary>
		/// Invoked when a set member operation is resolved dynamically.
		/// </summary>
		/// <param name="binder">Provides information about the operation to execute.</param>
		/// <param name="value">The value to set into the member.</param>
		/// <returns>True if the operation executed successfully, false otherwise.</returns>
		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (_Values == null) throw new EmptyException("This record is not initialized.");
			if (_Schema == null) throw new EmptyException("The schema of record '{0}' is null.".FormatWith(this));

			var list = _Schema.FindColumn(binder.Name).ToList(); if (list.Count == 1)
			{
				var index = _Schema.IndexOf(list[0]); list.Clear(); list = null;
				this[index] = value;
				return true;
			}

			if (list.Count == 0) throw new NotFoundException("Column '{0}' not found.".FormatWith(binder.Name));
			throw new DuplicateException("Dynamic name '{0}' found in several columns in '{1}'.".FormatWith(binder.Name, list.Sketch()));
		}
	}

	// ==================================================== 
	internal class RecordResolver : DynamicObject
	{
		IRecord _Record = null;
		string _Table = null;

		internal RecordResolver(IRecord record, string table)
		{
			_Record = record;
			_Table = table;
		}

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			result = _Record[_Table, binder.Name];
			_Record = null;
			return true;
		}

		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			_Record[_Table, binder.Name] = value;
			_Record = null;
			return true;
		}
	}
}
