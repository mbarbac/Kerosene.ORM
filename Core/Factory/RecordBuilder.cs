// ======================================================== RecordBuilder.cs
namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Dynamic;
	using System.Linq;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Represents an object able to create new records using the collection of entries and
	/// values annotated dynamically into it.
	/// </summary>
	public class RecordBuilder : DynamicObject, IDisposableEx, IElementAliasProvider
	{
		bool _IsDisposed = false;
		bool _CaseSensitiveNames = Schema.DEFAULT_CASE_SENSITIVE_NAMES;
		ISchema _Schema = null;
		List<object> _Values = new List<object>();

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="caseSensitiveNames">Whether the table and column names of the members
		/// of this collection are case sensitive or not.</param>
		public RecordBuilder(
			bool caseSensitiveNames = Schema.DEFAULT_CASE_SENSITIVE_NAMES)
		{
			_CaseSensitiveNames = caseSensitiveNames;

			if ((_Schema = CreateEmptySchema()) == null)
				throw new CannotCreateException("Cannot create an empty schema for this instance.");
		}

		/// <summary>
		/// Factory method invoked to create an empty schema for this instance.
		/// </summary>
		protected virtual ISchema CreateEmptySchema()
		{
			return new Concrete.Schema(CaseSensitiveNames);
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

		~RecordBuilder()
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
				if (_Schema != null && !_Schema.IsDisposed) _Schema.Dispose();
				if (_Values != null) _Values.Clear();
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
				for (int i = 0; i < _Values.Count; i++)
				{
					if (i != 0) sb.Append(", ");

					var value = _Values[i].Sketch();
					var name = SchemaEntry.NormalizedName(_Schema[i].TableName, _Schema[i].ColumnName);

					sb.AppendFormat("{0} = '{1}'", name, value);
				}
			}

			sb.Append("]");

			var str = sb.ToString();
			return IsDisposed ? string.Format("disposed::{0}({1})", GetType().EasyName(), str) : str;
		}

		/// <summary>
		/// Whether the table and column names of the record entries to create are case sensitive
		/// or not.
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
			get { return _Schema == null ? null : _Schema.Aliases; }
		}

		/// <summary>
		/// The number of entries in this builder.
		/// </summary>
		public int Count
		{
			get { return _Values == null ? 0 : _Values.Count; }
		}

		/// <summary>
		/// Gets or sets the value held by column whose index is given.
		/// </summary>
		/// <param name="index">The index of the affected column.</param>
		/// <returns>The value held by the column whose index is given.</returns>
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
		/// Gets or sets the value held by the column whose table and column names are given.
		/// <para>The setter creates dynamically an entry for the given column specification.</para>
		/// </summary>
		/// <param name="table">The table name of the entry to find, or null to refer to the
		/// default one in this context.</param>
		/// <param name="column">The column name.</param>
		/// <returns>The value held by the requested entry.</returns>
		public object this[string table, string column]
		{
			get
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				table = SchemaEntry.ValidateTable(table);
				column = SchemaEntry.ValidateColumn(column);

				var entry = _Schema.FindEntry(table, column);
				if (entry == null) throw new NotFoundException(
					"Entry '{0}' not found in this '{1}'".FormatWith(SchemaEntry.NormalizedName(table, column), this));

				var index = _Schema.IndexOf(entry);
				return _Values[index];
			}
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				table = SchemaEntry.ValidateTable(table);
				column = SchemaEntry.ValidateColumn(column);

				var entry = _Schema.FindEntry(table, column); if (entry == null)
				{
					_Schema.AddCreate(table, column);
					_Values.Add(value);
				}
				else
				{
					var index = _Schema.IndexOf(entry);
					_Values[index] = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets the value held by the unique column whose column name is given. If
		/// several entries are found sharing the same column name for different tables then
		/// an exception is thrown.
		/// <para>The setter creates dynamically an entry for the given column specification.</para>
		/// </summary>
		/// <param name="column">The column name.</param>
		/// <returns>The value held by the requested entry.</returns>
		public object this[string column]
		{
			get
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				column = SchemaEntry.ValidateColumn(column);

				var entry = _Schema.FindEntry(column);
				if (entry == null) throw new NotFoundException(
					"Entry '{0}' not found in this '{1}'".FormatWith(column, this));

				var index = _Schema.IndexOf(entry);
				return _Values[index];
			}
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				column = SchemaEntry.ValidateColumn(column);

				var entry = _Schema.FindEntry(column);
				if (entry == null)
				{
					_Schema.AddCreate(column);
					_Values.Add(value);
				}
				else
				{
					var index = _Schema.IndexOf(entry);
					_Values[index] = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets the value held by the column whose table and colum names are obtained
		/// parsing the given dynamic lambda expression, using either the 'x => x.Table.Column'
		/// or 'x => x.Column' forms. In the later case, if several members are found sharing the
		/// same column name for different tables then an exception is thrown.
		/// <para>The setter creates dynamically an entry for the given column specification.</para>
		/// </summary>
		/// <param name="column">The column name.</param>
		/// <returns>The value held by the requested entry.</returns>
		public object this[Func<dynamic, object> spec]
		{
			get
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
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
				if (spec == null) throw new ArgumentNullException("spec", "Entry specification cannot be null.");

				var entry = _Schema.FindEntry(spec);
				if (entry == null)
				{
					var name = DynamicInfo.ParseName(spec);
					var parts = name.Split('.');

					if (parts.Length == 1) _Schema.AddCreate(parts[0]);
					else if (parts.Length == 2) _Schema.AddCreate(parts[0], parts[1]);
					else throw new FormatException("Invalid specification '{0}'.".FormatWith(name));

					_Values.Add(value);
				}
				else
				{
					var index = _Schema.IndexOf(entry);
					_Values[index] = value;
				}
			}
		}

		/// <summary>
		/// Adds into this builder a new entry using the given one, optionally cloning it, along
		/// with the value to be held by its associated column.
		/// </summary>
		/// <param name="entry">The schema entry.</param>
		/// <param name="value">The value of the column.</param>
		/// <param name="cloneEntry">True to clone the given entry</param>
		public void AddEntry(ISchemaEntry entry, object value, bool cloneEntry = true)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (entry == null) throw new ArgumentNullException("entry", "Entry cannot be null.");
			if (entry.IsDisposed) throw new ObjectDisposedException(entry.ToString());

			if (cloneEntry) entry = entry.Clone();

			_Schema.Add(entry);
			_Values.Add(value);
		}

		/// <summary>
		/// Clears this builder.
		/// </summary>
		public void Clear()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			_Schema.Clear();
			_Values.Clear();
		}

		/// <summary>
		/// Factory method to create an empty record to return.
		/// </summary>
		protected virtual IRecord CreateEmptyRecord(ISchema schema)
		{
			return new Concrete.Record(schema.Clone());
		}

		/// <summary>
		/// Creates and returns a new record based upon the entries and values recorded into
		/// this instance. Both its schema and values are clones of the original ones.
		/// </summary>
		/// <returns>A new record.</returns>
		public IRecord Create()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (_Schema.Count == 0) throw new InvalidOperationException("There are no entries in this builder.");

			var record = CreateEmptyRecord(_Schema);
			for (int i = 0; i < _Schema.Count; i++) record[i] = _Values[i].TryClone();

			return record;
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

			var list = _Schema.FindTable(binder.Name).ToList(); if (list.Count != 0)
			{
				result = new RecordBuilderResolver(this, binder.Name); list.Clear(); list = null;
				return true;
			}

			list = _Schema.FindColumn(binder.Name).ToList(); if (list.Count == 1)
			{
				result = this[binder.Name]; list.Clear(); list = null;
				return true;
			}

			if (list.Count == 0)
			{
				result = new RecordBuilderResolver(this, binder.Name);
				return true;
			}

			throw new DuplicateException(
				"Column '{0}' found in several columns in '{1}'.".FormatWith(binder.Name, this));
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

			var list = _Schema.FindColumn(binder.Name).ToList(); if (list.Count == 1)
			{
				var index = _Schema.IndexOf(list[0]); list.Clear(); list = null;
				this[index] = value;
				return true;
			}
			if (list.Count == 0)
			{
				this[binder.Name] = value;
				return true;
			}

			throw new DuplicateException(
				"Column '{0}' found in several columns in '{1}'.".FormatWith(binder.Name, this));
		}
	}

	// ====================================================
	internal class RecordBuilderResolver : DynamicObject
	{
		RecordBuilder _Builder = null;
		string _Table = null;

		internal RecordBuilderResolver(RecordBuilder builder, string table)
		{
			_Builder = builder;
			_Table = table;
		}

		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			_Builder[_Table, binder.Name] = value;
			_Builder = null;
			return true;
		}
	}
}
// ======================================================== 
