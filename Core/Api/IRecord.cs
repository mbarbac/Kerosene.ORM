// ======================================================== IRecord.cs
namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Dynamic;
	using System.IO;
	using System.Linq.Expressions;
	using System.Runtime.Serialization;
	using System.Runtime.Serialization.Formatters.Binary;

	// ==================================================== 
	/// <summary>
	/// Represents a record on the database, typically obtained by the execution of an enumarable
	/// command, that provides both indexed and dynamic ways to access its contents.
	/// </summary>
	public interface IRecord
		: IDynamicMetaObjectProvider, IDisposableEx, ICloneable, ISerializable
		, IEnumerable, IEquivalent<IRecord>
	{
		/// <summary>
		/// Disposes this instance and optionally disposes the schema it is associated with,
		/// if any.
		/// </summary>
		/// <param name="disposeSchema">True to also dispose the schema this instance is
		/// associated with.</param>
		void Dispose(bool disposeSchema);

		/// <summary>
		/// Returns a new instance that otherwise is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		new IRecord Clone();

		/// <summary>
		/// Returns a new instance that otherwise is a copy of the original one, and
		/// optionally clones also the original schema it was associated with, if any.
		/// </summary>
		/// <param name="cloneSchema">True to also clone the schema this instance is associated
		/// with.</param>
		/// <returns>A new instance.</returns>
		IRecord Clone(bool cloneSchema);

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
		bool EquivalentTo(IRecord target, bool onlyValues);

		/// <summary>
		/// The schema that describes the structure and metadata of the contents in this record.
		/// <para>The setter fails if the value is not null and this instance already has a
		/// schema associated with it.</para>
		/// </summary>
		/// <remarks>The value held by this property can be null if this instance is disposed,
		/// or when there is no schema associated to it, which can happen in some border case
		/// scenarios (as for instance while it is being deserialized, among others).</remarks>
		ISchema Schema { get; set; }

		/// <summary>
		/// Whether the schema this instance may has associated with it is serialized along with
		/// this record or not. A value of 'false' can be used when serializing many records
		/// associated with the same schema, for performance reasons, and in this case it is
		/// assumed that the schema reference is set by the receiving environment afterwards.
		/// </summary>
		bool SerializeSchema { get; set; }

		/// <summary>
		/// The number of table-column entries in this record.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Gets or sets the value held by table-column entry whose index is given.
		/// </summary>
		/// <param name="index">The index of the table-column entry.</param>
		/// <returns>The value held by the entry whose index is given.</returns>
		object this[int index] { get; set; }

		/// <summary>
		/// Gets or sets the value stored at the entry whose table and column names are given.
		/// </summary>
		/// <param name="tableName">The table name, or null if it refers to the default one in
		/// this context.</param>
		/// <param name="columnName">The column name.</param>
		/// <returns>The value held at the given entry.</returns>
		object this[string tableName, string columnName] { get; set; }

		/// <summary>
		/// Gets or sets the value stored at the entry whose unique column name is given.
		/// <para>If the schema of the record contains several columns with the same column
		/// name an exception is thrown.</para>
		/// </summary>
		/// <param name="columnName">The column name.</param>
		/// <returns>The value held at the given entry.</returns>
		object this[string columnName] { get; set; }

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
		object this[Func<dynamic, object> spec] { get; set; }

		/// <summary>
		/// Clears all the values held by this instance.
		/// </summary>
		void Clear();
	}

	// ==================================================== 
	/// <summary>
	/// Helpers and extensions for working with <see cref="IRecord"/> instances.
	/// </summary>
	public static class Record
	{
		/// <summary>
		/// Whether, by default, the schema of a record is serialized along with it or not.
		/// </summary>
		public const bool DEFAULT_SERIALIZE_SCHEMA = false;

		/// <summary>
		/// Conditionally executes the given action with the value held by the record in the
		/// entry whose table and column names are given, if such entry exists. Returns true in
		/// this case, or false if the entry was not found.
		/// </summary>
		/// <param name="record">The record.</param>
		/// <param name="table">The table name, or null to refer to the default table.</param>
		/// <param name="column">The column name.</param>
		/// <param name="action">The action to execute.</param>
		/// <returns>True if the entry was found, false otherwise.</returns>
		public static bool TryGet(this IRecord record, string table, string column, Action<object> action)
		{
			if (record == null) throw new NullReferenceException("Record cannot be null.");
			if (record.IsDisposed) throw new ObjectDisposedException(record.ToString());
			if (record.Schema == null) throw new InvalidOperationException("This record '{0}' has no schema associated with it.".FormatWith(record));
			if (action == null) throw new ArgumentNullException("action", "Delegate cannot be null.");

			var entry = record.Schema.FindEntry(table, column);
			if (entry == null) return false;

			var index = record.Schema.IndexOf(entry);
			var value = record[index];
			action(value);

			return true;
		}

		/// <summary>
		/// Conditionally executes the given action with the value held by the record in the
		/// unique entry whose column name is given, if such entry exists. Returns true in this
		/// case, or false if the entry was not found. If several entries are found sharing the
		/// same column name then an exception is thrown.
		/// </summary>
		/// <param name="record">The record.</param>
		/// <param name="column">The column name.</param>
		/// <param name="action">The action to execute.</param>
		/// <returns>True if the entry was found, false otherwise.</returns>
		public static bool TryGet(this IRecord record, string column, Action<object> action)
		{
			return record.TryGet(null, column, action);
		}

		/// <summary>
		/// Conditionally executes the given action with the value held by the record in the
		/// entry whose table and column names are obtained parsing the given dynamic lambda
		/// expression, using either the 'x => x.Table.Column' or 'x => x.Column' forms, if such
		/// entry exists. Returns true in this case, or false if the entry was not found.
		/// </summary>
		/// <param name="record">The record.</param>
		/// <param name="spec">A dynamic lambda expressin that resolves into the specification
		/// of the entry to find.</param>
		/// <param name="action">The action to execute.</param>
		/// <returns>True if the entry was found, false otherwise.</returns>
		public static bool TryGet(this IRecord record, Func<dynamic, object> spec, Action<object> action)
		{
			if (record == null) throw new NullReferenceException("Record cannot be null.");
			if (record.IsDisposed) throw new ObjectDisposedException(record.ToString());
			if (record.Schema == null) throw new InvalidOperationException("This record '{0}' has no schema associated with it.".FormatWith(record));
			if (action == null) throw new ArgumentNullException("action", "Delegate cannot be null.");

			var entry = record.Schema.FindEntry(spec);
			if (entry == null) return false;

			var index = record.Schema.IndexOf(entry);
			var value = record[index];
			action(value);

			return true;
		}

		/// <summary>
		/// Conditionally sets the value of the entry in the record whose table and column names
		/// are given, if such entry exists. Returns true in this case, or false if the entry was
		/// not found.
		/// </summary>
		/// <param name="record">The record.</param>
		/// <param name="table">The table name, or null to refer to the default table.</param>
		/// <param name="column">The column name.</param>
		/// <param name="func">The delegate to execute to obtain the value to set into the column.</param>
		/// <returns>True if the entry was found, false otherwise.</returns>
		public static bool TrySet(this IRecord record, string table, string column, Func<object> func)
		{
			if (record == null) throw new NullReferenceException("Record cannot be null.");
			if (record.IsDisposed) throw new ObjectDisposedException(record.ToString());
			if (record.Schema == null) throw new InvalidOperationException("This record '{0}' has no schema associated with it.".FormatWith(record));
			if (func == null) throw new ArgumentNullException("func", "Delegate cannot be null.");

			var entry = record.Schema.FindEntry(table, column);
			if (entry == null) return false;

			var index = record.Schema.IndexOf(entry);
			var value = func();
			record[index] = value;

			return true;
		}

		/// <summary>
		/// Conditionally sets the value of the entry in the record whose table and column names
		/// are given, if such entry exists. Returns true in this case, or false if the entry was
		/// not found. If several entries are found sharing the same column name then an exception
		/// is thrown.
		/// </summary>
		/// <param name="record">The record.</param>
		/// <param name="column">The column name.</param>
		/// <param name="func">The delegate to execute to obtain the value to set into the column.</param>
		/// <returns>True if the entry was found, false otherwise.</returns>
		public static bool TrySet(this IRecord record, string column, Func<object> func)
		{
			return record.TrySet(null, column, func);
		}

		/// <summary>
		/// Conditionally sets the value of the entry in the record whose table and column names
		/// are obtained parsing the given dynamic lambda expression, using either the
		/// 'x => x.Table.Column' or 'x => x.Column' forms, if such entry exists. Returns true in
		/// this case, or false if the entry was not found.
		/// </summary>
		/// <param name="record">The record.</param>
		/// <param name="spec">A dynamic lambda expressin that resolves into the specification
		/// of the entry to find.</param>
		/// <param name="func">The delegate to execute to obtain the value to set into the column.</param>
		/// <returns>True if the entry was found, false otherwise.</returns>
		public static bool TrySet(this IRecord record, Func<dynamic, object> spec, Func<object> func)
		{
			if (record == null) throw new NullReferenceException("Record cannot be null.");
			if (record.IsDisposed) throw new ObjectDisposedException(record.ToString());
			if (record.Schema == null) throw new InvalidOperationException("This record '{0}' has no schema associated with it.".FormatWith(record));
			if (func == null) throw new ArgumentNullException("func", "Delegate cannot be null.");

			var entry = record.Schema.FindEntry(spec);
			if (entry == null) return false;

			var index = record.Schema.IndexOf(entry);
			var value = func();
			record[index] = value;

			return true;
		}

		/// <summary>
		/// Provides an estimation of the size of the record in bytes, based upon the memory
		/// size to use to hold the values of the types of each column.
		/// </summary>
		/// <param name="record">This record.</param>
		/// <returns>An estimation of the size of the record in bytes.</returns>
		public static long Size(this IRecord record)
		{
			if (record == null) throw new NullReferenceException("Record cannot be null.");
			long size = 0;

			for (int i = 0, count = record.Count; i < count; i++)
			{
				var value = record[i];
				var type = value == null ? null : value.GetType();

				if (type == typeof(byte)) { size += sizeof(byte); continue; }
				if (type == typeof(byte?)) { size += sizeof(byte); continue; }

				if (type == typeof(bool)) { size += sizeof(bool); continue; }
				if (type == typeof(bool?)) { size += sizeof(bool); continue; }

				if (type == typeof(char)) { size += sizeof(char); continue; }
				if (type == typeof(char?)) { size += sizeof(char); continue; }

				if (type == typeof(Int16)) { size += sizeof(Int16); continue; }
				if (type == typeof(Int16?)) { size += sizeof(Int16); continue; }
				if (type == typeof(short)) { size += sizeof(short); continue; }
				if (type == typeof(short?)) { size += sizeof(short); continue; }

				if (type == typeof(UInt16)) { size += sizeof(UInt16); continue; }
				if (type == typeof(UInt16?)) { size += sizeof(UInt16); continue; }
				if (type == typeof(ushort)) { size += sizeof(ushort); continue; }
				if (type == typeof(ushort?)) { size += sizeof(ushort); continue; }

				if (type == typeof(Int32)) { size += sizeof(Int32); continue; }
				if (type == typeof(Int32?)) { size += sizeof(Int32); continue; }
				if (type == typeof(int)) { size += sizeof(int); continue; }
				if (type == typeof(int?)) { size += sizeof(int); continue; }

				if (type == typeof(UInt32)) { size += sizeof(UInt32); continue; }
				if (type == typeof(UInt32?)) { size += sizeof(UInt32); continue; }
				if (type == typeof(uint)) { size += sizeof(uint); continue; }
				if (type == typeof(uint?)) { size += sizeof(uint); continue; }

				if (type == typeof(Int64)) { size += sizeof(Int64); continue; }
				if (type == typeof(Int64?)) { size += sizeof(Int64); continue; }
				if (type == typeof(long)) { size += sizeof(long); continue; }
				if (type == typeof(long?)) { size += sizeof(long); continue; }

				if (type == typeof(UInt64)) { size += sizeof(UInt64); continue; }
				if (type == typeof(UInt64?)) { size += sizeof(UInt64); continue; }
				if (type == typeof(ulong)) { size += sizeof(long); continue; }
				if (type == typeof(ulong?)) { size += sizeof(long); continue; }

				if (type == typeof(Single)) { size += sizeof(Single); continue; }
				if (type == typeof(Single?)) { size += sizeof(Single); continue; }
				if (type == typeof(float)) { size += sizeof(float); continue; }
				if (type == typeof(float?)) { size += sizeof(float); continue; }

				if (type == typeof(Double)) { size += sizeof(Double); continue; }
				if (type == typeof(Double?)) { size += sizeof(Double); continue; }
				if (type == typeof(double)) { size += sizeof(double); continue; }
				if (type == typeof(double?)) { size += sizeof(double); continue; }

				if (type == typeof(CalendarDate)) { size += 3 * sizeof(int); continue; }
				if (type == typeof(ClockTime)) { size += 4 * sizeof(int); continue; }

				if (type == typeof(string)) { size += ((string)value).Length * sizeof(char); continue; }

				// What follows is a last resort mechanism, slow and possibly inaccurate...
				try
				{
					using (Stream stream = new MemoryStream())
					{
						BinaryFormatter formatter = new BinaryFormatter();
						formatter.Serialize(stream, value.Sketch());
						size += stream.Length;
					}
				}
				catch { }
			}

			return size;
		}

		/// <summary>
		/// Creates a new record parsing the collection of dynamic lambda expressions provided,
		/// each with the 'x => x.Table.Column = Value' or 'x => x.Column = Value' forms.
		/// <para>The new record carries its own ad-hoc schema and clones of the values given.</para>
		/// </summary>
		/// <param name="caseSensitiveNames">Whether the table and column names of the schema of
		/// the new record are case sensitive or not.</param>
		/// <param name="specs">The collectoin of dynamic lambda expressions that specify the
		/// contents and schema of the new record.</param>
		/// <returns>A new record.</returns>
		public static IRecord Create(bool caseSensitiveNames, params Func<dynamic, object>[] specs)
		{
			if (specs == null) throw new ArgumentNullException("specs", "List of specifications cannot be null.");
			if (specs.Length == 0) throw new ArgumentException("List of specifications cannot be empty.");

			var builder = new RecordBuilder(caseSensitiveNames);
			for (int i = 0; i < specs.Length; i++)
			{
				var spec = specs[i];
				if (spec == null) throw new ArgumentNullException("Specification #{0} cannot be null.".FormatWith(i));

				var parser = DynamicParser.Parse(spec);
				var result = parser.Result;
				if (result == null) throw new ArgumentNullException("Specification #{0}: '{1}' cannot resolve to null.".FormatWith(i, parser));

				if (result is DynamicNode.SetMember) // The assignation syntax...
				{
					var node = (DynamicNode.SetMember)result;

					if (node.Host is DynamicNode.Argument) // x.Column = value;
					{
						builder[node.Name] = node.Value;
						continue;
					}
					if (node.Host is DynamicNode.GetMember) // x.Table.Column = value;
					{
						var host = (DynamicNode.GetMember)node.Host;
						builder[host.Name, node.Name] = node.Value;
						continue;
					}
				}
				if (result is DynamicNode.Binary)
				{
					var node = (DynamicNode.Binary)result;
					if (node.Operation == ExpressionType.Equal)
					{
						var host = (DynamicNode.GetMember)node.Left;

						if (host.Host is DynamicNode.Argument) // x.Column == value;
						{
							builder[host.Name] = node.Right;
							continue;
						}
						if (host.Host is DynamicNode.GetMember) // x.Table.Column == value;
						{
							var member = (DynamicNode.GetMember)host.Host;
							if (member.Host is DynamicNode.Argument)
							{
								builder[member.Name, host.Name] = node.Right;
								continue;
							}
						}
					}
				}
				throw new ArgumentException("Specification #{0}: '{1}' is invalid.".FormatWith(i, parser));
			}

			var record = builder.Create(); builder.Dispose();
			return record;
		}

		/// <summary>
		/// Creates a new record parsing the collection of dynamic lambda expressions provided,
		/// each with the 'x => x.Table.Column = Value' or 'x => x.Column = Value' forms.
		/// <para>The new record carries its own ad-hoc schema and clones of the values given.</para>
		/// </summary>
		/// <param name="specs">The collectoin of dynamic lambda expressions that specify the
		/// contents and schema of the new record.</param>
		/// <returns>A new record.</returns>
		public static IRecord Create(params Func<dynamic, object>[] specs)
		{
			return Create(Schema.DEFAULT_CASESENSITIVE_NAMES, specs);
		}

		/// <summary>
		/// Returns a new record containing the ad-hoc schema and values that describes what are
		/// the changes detected AT the original record compared against the target one, or null
		/// if no changes can be detected.
		/// <para>
		/// The new record returned contains its own ad-hoc schema, cloned from the entries of
		/// the original one, as well as the affected values that as they appeared in the source
		/// record.
		/// </para>
		/// </summary>
		/// <param name="source">The source record.</param>
		/// <param name="target">The target record.</param>
		/// <returns>A new record, with its ad-hoc schema, containing the changes, or null if no
		/// changes can be detected.</returns>
		public static IRecord Changes(this IRecord source, IRecord target)
		{
			if (source == null) throw new NullReferenceException("Source cannot be null.");
			if (source.IsDisposed) throw new ObjectDisposedException(source.ToString());
			if (source.Schema == null) throw new InvalidOperationException("Source '{0}' carries no schema.".FormatWith(source));

			if (target == null) throw new ArgumentNullException("target", "Target cannot be null.");
			if (target.IsDisposed) throw new ObjectDisposedException(target.ToString());
			if (target.Schema == null) throw new InvalidOperationException("Target '{0}' carries no schema.".FormatWith(target));

			var values = new List<object>();
			var entries = new List<ISchemaEntry>();

			for (int i = 0; i < source.Count; i++)
			{
				var sourceEntry = source.Schema[i];
				var targetEntry = target.Schema.FindEntry(sourceEntry.TableName, sourceEntry.ColumnName);

				if (targetEntry == null) targetEntry = target.Schema.FindEntry(sourceEntry.ColumnName, raise: false);
				if (targetEntry == null)
				{
					values.Add(source[i].TryClone());
					entries.Add(sourceEntry.Clone());
				}
				else
				{
					var index = target.Schema.IndexOf(targetEntry);
					var value = target[index];
					var temp = source[i];

					if (!temp.IsEquivalentTo(value))
					{
						values.Add(temp.TryClone());
						entries.Add(sourceEntry.Clone());
					}
				}
			}

			if (entries.Count == 0) return null;

			var schema = new Concrete.Schema(source.Schema.CaseSensitiveNames);
			foreach (var entry in entries) schema.Add(entry);
			entries.Clear();

			var record = new Concrete.Record(schema);
			for (int i = 0; i < values.Count; i++) record[i] = values[i];
			values.Clear();

			return record;
		}
	}
}
// ======================================================== 
