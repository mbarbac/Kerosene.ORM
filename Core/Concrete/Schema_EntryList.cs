// ======================================================== Schema_EntryList.cs
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
	public partial class Schema : ISchema
	{
		internal class EntryList : IEnumerable<ISchemaEntry>
		{
			List<ISchemaEntry> _List = null;
			bool _CaseSensitive = false;
			Dictionary<string, ISchemaEntry> _ByNormalizedNames = null;

			/// <summary>
			/// Initializes a new instance.
			/// </summary>
			internal EntryList(bool caseSensitiveNames)
			{
				_List = new List<ISchemaEntry>();
				_CaseSensitive = caseSensitiveNames;
				_ByNormalizedNames = new Dictionary<string, ISchemaEntry>(
					caseSensitiveNames ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
			}

			/// <summary>
			/// Returns a new enumerator for this instance.
			/// </summary>
			/// <returns>A new enumerator.</returns>
			public IEnumerator<ISchemaEntry> GetEnumerator()
			{
				return _List.GetEnumerator();
			}
			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}

			/// <summary>
			/// Gets the item stored at the given index.
			/// </summary>
			internal ISchemaEntry this[int index]
			{
				get { return _List[index]; }
			}

			/// <summary>
			/// Gets the index of the first ocurrence of the given item.
			/// </summary>
			internal int IndexOf(ISchemaEntry item)
			{
				return _List.IndexOf(item);
			}

			/// <summary>
			/// Whether this collection contains the given item or not.
			/// </summary>
			internal bool Contains(ISchemaEntry item)
			{
				return _List.Contains(item);
			}

			/// <summary>
			/// The number of items in this collection.
			/// </summary>
			internal int Count
			{
				get { return _List.Count; }
			}

			/// <summary>
			/// Adds the given item into this collection.
			/// </summary>
			internal void Add(ISchemaEntry item)
			{
				_List.Add(item);

				var name = Core.SchemaEntry.NormalizedName(item.TableName, item.ColumnName);
				_ByNormalizedNames[name] = item;
			}

			/// <summary>
			/// Removes the given item from this collection.
			/// </summary>
			internal bool Remove(ISchemaEntry item)
			{
				var name = Core.SchemaEntry.NormalizedName(item.TableName, item.ColumnName);
				_ByNormalizedNames.Remove(name);

				return _List.Remove(item);
			}

			/// <summary>
			/// Clears the items contained in this collection.
			/// </summary>
			internal void Clear()
			{
				_List.Clear();
				_ByNormalizedNames.Clear();
			}

			/// <summary>
			/// Gets a list containing the elements in this collection.
			/// </summary>
			internal List<ISchemaEntry> ToList()
			{
				return new List<ISchemaEntry>(_List);
			}

			/// <summary>
			/// Gets an array containing the elements in this collection.
			/// </summary>
			internal ISchemaEntry[] ToArray()
			{
				return _List.ToArray();
			}

			/// <summary>
			/// Returns the entry with the given table and column name, or null.
			/// </summary>
			internal ISchemaEntry FindEntry(string tableName, string columnName)
			{
				var name = Core.SchemaEntry.NormalizedName(tableName, columnName);
				ISchemaEntry entry = null; _ByNormalizedNames.TryGetValue(name, out entry);
				return entry;
			}

			/// <summary>
			/// Returns a list containing the entries with the given table name.
			/// </summary>
			internal List<ISchemaEntry> FindTable(string tableName)
			{
				return _List.FindAll(x => string.Compare(x.TableName, tableName, !_CaseSensitive) == 0);
			}

			/// <summary>
			/// Returns a list containing the entries with the given column name.
			/// </summary>
			internal List<ISchemaEntry> FindColumn(string columnName)
			{
				return _List.FindAll(x => string.Compare(x.ColumnName, columnName, !_CaseSensitive) == 0);
			}
		}
	}
}
// ======================================================== 
