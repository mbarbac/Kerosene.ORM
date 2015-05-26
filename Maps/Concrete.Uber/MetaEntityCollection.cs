using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Kerosene.ORM.Maps.Concrete
{
	// ====================================================
	internal class MetaEntityCollection : IEnumerable<MetaEntity>
	{
		IUberMap _Master = null;
		Dictionary<string, List<MetaEntity>> _Items = new Dictionary<string, List<MetaEntity>>();
		private const int NODE_INITIAL_SIZE = 1;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		internal MetaEntityCollection(IUberMap map)
		{
			_Master = map;
		}

		/// <summary>
		/// Disposes this instance.
		/// </summary>
		internal void Dispose()
		{
			if (_Items != null) Clear(); _Items = null;
			_Master = null;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the string representation of this instance.</returns>
		public override string ToString()
		{
			return string.Format("{0}({1})", GetType().EasyName(), Count);
		}

		/// <summary>
		/// </summary>
		IEnumerable<MetaEntity> ItemsCollection
		{
			get
			{
				foreach (var node in _Items)
					foreach (var item in node.Value) yield return item;
			}
		}

		/// <summary>
		/// Obtains an enumerator for the members of this instance.
		/// </summary>
		public IEnumerator<MetaEntity> GetEnumerator()
		{
			return ItemsCollection.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		/// The number of elements in this collection.
		/// </summary>
		public int Count
		{
			get
			{
				int count = 0; foreach (var kvp in _Items) count += kvp.Value.Count;
				return count;
			}
		}

		/// <summary>
		/// Builds the id string associated with the given record.
		/// Returns null if it cannot be generated or, optionally, raises an exception.
		/// </summary>
		string ObtainId(IRecord record, bool raise)
		{
			var first = true;
			var sb = new StringBuilder(); for (int i = 0; i < _Master.SchemaId.Count; i++)
			{
				var name = _Master.SchemaId[i].ColumnName;
				var entry = record.Schema.FindEntry(name, raise: false); if (entry == null)
				{
					if (raise) throw new InvalidOperationException("Identity column '{0}' not found in record '{1}'.".FormatWith(name, record));
					return null;
				}

				int ix = record.Schema.IndexOf(entry);
				var value = record[ix];

				if (first) first = false; else sb.Append(",");
				sb.AppendFormat("[{0}]", value == null ? "ø" : value.Sketch());
			}
			return sb.ToString();
		}

		/// <summary>
		/// Returns the id string associated with the given entity, generating it if needed.
		/// Returns null if it cannot be generated if needed or, optionally, raises an exception.
		/// Optionally captures the state in a new record if it did not exist previously.
		/// </summary>
		string ObtainId(MetaEntity meta, bool raise, bool captureRecord)
		{
			var str = meta.CollectionId; if (str == null)
			{
				var obj = meta.Entity;
				if (obj == null) throw new InvalidOperationException("MetaEntity '{0}' is invalid.".FormatWith(meta));

				var empty = false;
				var record = meta.Record; if (record == null)
				{
					empty = true;
					record = new Core.Concrete.Record(_Master.Schema); _Master.WriteRecord(obj, record);
					if (captureRecord) meta.Record = record;
				}

				str = ObtainId(record, raise);
				if (empty && !captureRecord) record.Dispose();
			}
			return str;
		}

		/// <summary>
		/// Adds the given element into this collection.
		/// </summary>
		internal void Add(MetaEntity meta)
		{
			string id = ObtainId(meta, raise: true, captureRecord: true);

			List<MetaEntity> node = null; if (!_Items.TryGetValue(id, out node))
			{
				node = new List<MetaEntity>(NODE_INITIAL_SIZE);
				_Items.Add(id, node);
			}
			node.Add(meta);
			meta.CollectionId = id;
			meta.UberMap = _Master;
		}

		/// <summary>
		/// Removes the given element from this collection.
		/// </summary>
		internal bool Remove(MetaEntity meta)
		{
			bool r = false; 
			string id = ObtainId(meta, raise: false, captureRecord: false);

			List<MetaEntity> node = null;
			if (id != null && _Items.TryGetValue(id, out node)) r = node.Remove(meta);

			if (!r)
			{
				foreach (var kvp in _Items)
				{
					r = kvp.Value.Remove(meta); if (r)
					{
						id = kvp.Key;
						node = kvp.Value;
						break;
					}
				}
			}

			if (r)
			{
				meta.CollectionId = null;
				meta.UberMap = null;
				if (node.Count == 0) _Items.Remove(id);
			}

			return r;
		}

		/// <summary>
		/// Clears this collection by removing all the elements it contains.
		/// </summary>
		internal void Clear()
		{
			_Items.Clear();
		}

		/// <summary>
		/// Returns a new array containing the elements of this collection.
		/// </summary>
		internal MetaEntity[] ToArray()
		{
			var array = new MetaEntity[Count];
			int n = 0; foreach (var item in ItemsCollection) array[n++] = item;
			return array;
		}

		/// <summary>
		/// Returns the collection of members sharing the given identity, or null.
		/// </summary>
		internal IEnumerable<MetaEntity> FindNode(string id)
		{
			List<MetaEntity> node = null; if (id != null) _Items.TryGetValue(id, out node);
			return node;
		}

		/// <summary>
		/// Returns the collection of members sharing the given identity, or null.
		/// </summary>
		internal IEnumerable<MetaEntity> FindNode(IRecord record)
		{
			return FindNode(ObtainId(record, raise: false));
		}

		/// <summary>
		/// Returns the collection of members sharing the given identity, or null.
		/// </summary>
		internal IEnumerable<MetaEntity> FindNode(MetaEntity meta)
		{
			return FindNode(ObtainId(meta, raise: false, captureRecord: false));
		}
	}
}
