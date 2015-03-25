// ======================================================== ISchemaEntry.cs
namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	// ==================================================== 
	/// <summary>
	/// Represents the metadata of a given table-column entry on a given a given schema.
	/// </summary>
	public interface ISchemaEntry : IDisposableEx, ICloneable, ISerializable, IEquivalent<ISchemaEntry>
	{
		/// <summary>
		/// Returns a new instance that otherwise is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		new ISchemaEntry Clone();

		/// <summary>
		/// The collection this instance belongs to, if any.
		/// </summary>
		ISchema Owner { get; set; }

		/// <summary>
		/// The table this entry refers to, or null if it is the default one in a given context.
		/// </summary>
		string TableName { get; set; }

		/// <summary>
		/// The column name this entry refers to.
		/// </summary>
		string ColumnName { get; set; }

		/// <summary>
		/// Whether the column this instance refers to is a primary key one or not.
		/// </summary>
		bool IsPrimaryKeyColumn { get; set; }

		/// <summary>
		/// Whether the column this instance refers to is an unique valued one or not.
		/// </summary>
		bool IsUniqueValuedColumn { get; set; }

		/// <summary>
		/// Whether the column this instance refers to is read only one or not.
		/// </summary>
		bool IsReadOnlyColumn { get; set; }

		/// <summary>
		/// The current metadata this instance carries (including the standard properties), as
		/// a collection of name-value pairs, where their names are case insensitive.
		/// </summary>
		IEnumerable<KeyValuePair<string, object>> Metadata { get; }

		/// <summary>
		/// The number of metadata entries this instance contains, always taking into consideratin
		/// the standard ones.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Returns whether a metadata entry with the given case insensitive name exists is in
		/// this collection.
		/// </summary>
		/// <param name="metadata">The metadata name to validate.</param>
		/// <returns>True if a metadata entry with the given name is part of this collection, or
		/// false otherwise.</returns>
		bool Contains(string metadata);

		/// <summary>
		/// Gets or sets the value of the metadata entry whose case insensitive name is given.
		/// <para>- The getter throws an exception if the requested entry does not exist.</para>
		/// <para>- The setter adds a new entry if the requested one did not exist.</para>
		/// </summary>
		/// <param name="metadata">The name of the metadata entry.</param>
		/// <returns>The member at the given position.</returns>
		object this[string metadata] { get; set; }

		/// <summary>
		/// Removes from this collection the metadata entry whose case insensitive name is given.
		/// Returns true if it has been removed succesfully, or false otherwise.
		/// </summary>
		/// <param name="metadata">The name of the metadata entry.</param>
		/// <returns>True if the metadata entry has been removed succesfully, or false otherwise.</returns>
		bool Remove(string metadata);

		/// <summary>
		/// Clears all the metadata entries this instance may carry.
		/// <para>- Note that, as a side effect, the standard properties will return their default
		/// values.</para>
		/// </summary>
		void Clear();

		/// <summary>
		/// The tag to identify the 'table name' entry in a metadata collection.
		/// </summary>
		string TableNameTag { get; set; }

		/// <summary>
		/// The tag to identify the 'column name' entry in a metadata collection.
		/// </summary>
		string ColumnNameTag { get; set; }

		/// <summary>
		/// The tag to identify the 'is primary key' entry in a metadata collection.
		/// </summary>
		string IsPrimaryKeyColumnTag { get; set; }

		/// <summary>
		/// The tag to identify the 'is unique valued' entry in a metadata collection.
		/// </summary>
		string IsUniqueValuedColumnTag { get; set; }

		/// <summary>
		/// The tag to identify the 'is read only' entry in a metadata collection.
		/// </summary>
		string IsReadOnlyColumnTag { get; set; }
	}

	// ==================================================== 
	/// <summary>
	/// Helpers and extensions for working with <see cref="ISchemaEntry"/> instances.
	/// </summary>
	public static class SchemaEntry
	{
		/// <summary>
		/// Returns a validated table name.
		/// </summary>
		/// <param name="table">The table name to validate. Can be null if it is the default
		/// one in a given context.</param>
		/// <returns>The validated table name.</returns>
		public static string ValidateTable(string table)
		{
			return table.Validated("Table Name", canbeNull: true);
		}

		/// <summary>
		/// Returns a validated column name.
		/// </summary>
		/// <param name="column">The column name to validate.</param>
		/// <returns>The validated column name.</returns>
		public static string ValidateColumn(string column)
		{
			return column.Validated("Column Name");
		}

		/// <summary>
		/// Returns a normalized entry name based upon the table and column ones.
		/// <para>This method does not throw any execptions even if such names are not valid ones.</para>
		/// </summary>
		/// <param name="tableName">The table name, or null if it is the dafault one in its context.</param>
		/// <param name="columnName">The column name.</param>
		/// <returns>The normalized schema entry name.</returns>
		public static string NormalizedName(string tableName, string columnName)
		{
			tableName = tableName.EmptyIfTrimmedIsNull();
			columnName = columnName.EmptyIfTrimmedIsNull();

			return string.Format("{0}.{1}", tableName, columnName);
		}

		/// <summary>
		/// The default tag to identify the 'table name' entry in a metadata collection.
		/// </summary>
		public const string DEFAULT_TABLE_NAME_TAG = "BaseTableName";

		/// <summary>
		/// The default tag to identify the 'column name entry' in a metadata collection.
		/// </summary>
		public const string DEFAULT_COLUMN_NAME_TAG = "ColumnName";

		/// <summary>
		/// The default tag to identify the 'is primary key' entry in a metadata collection.
		/// </summary>
		public const string DEFAULT_IS_PRIMARY_KEY_COLUMN_TAG = "IsKey";

		/// <summary>
		/// The default tag to identify the 'is unique valued' entry in a metadata collection.
		/// </summary>
		public const string DEFAULT_IS_UNIQUE_VALUED_COLUMN_TAG = "IsUnique";

		/// <summary>
		/// The default tag to identify the 'is read only' entry in a metadata collection.
		/// </summary>
		public const string DEFAULT_IS_READ_ONLY_COLUMN_TAG = "IsReadOnly";
	}
}
// ======================================================== 
