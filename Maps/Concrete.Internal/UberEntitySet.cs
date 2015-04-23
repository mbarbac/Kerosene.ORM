namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Represents teh collection of entities held by a given map.
	/// </summary>
	internal class UberEntitySet : IEnumerable<MetaEntity>
	{
		/// <summary>
		/// Generates the key to use with the given record, or null if the record contains no
		/// identity columns.
		/// </summary>
		internal static string GetKey(IRecord record)
		{
			var entries = record.Schema.PrimaryKeyColumns().ToList();
			if (entries.Count == 0) entries = record.Schema.UniqueValuedColumns().ToList();
			if (entries.Count == 0) return null;

			StringBuilder sb = new StringBuilder(); bool first = true;
			foreach (var entry in entries)
			{
				if (first) first = false; else sb.Append("-");
				var value = record[entry.ColumnName];
				value = value == null ? "[ø]" : value.Sketch();
				sb.Append(value);
			}

			entries.Clear(); entries = null;
			return sb.ToString();
		}

		/// <summary>
		/// Generates the key to use with the given entity, or null if it has no record or that
		/// record contains no identity columns.
		/// </summary>
		internal static string GetKey(MetaEntity meta)
		{
			return meta.RecordId;
		}

		object _SyncRoot = new object();
		Dictionary<string, List<MetaEntity>> _Items = new Dictionary<string, List<MetaEntity>>();
		private const int NODE_INITIAL_SIZE = 2;
		/// <summary>
		/// 
		/// Initializes a new instance.
		/// </summary>
		internal UberEntitySet() { }

		/// <summary>
		/// An object that can be used to synchronize access to this collection.
		/// </summary>
		internal object SyncRoot
		{
			get { return _SyncRoot; }
		}

		/// <summary>
		/// Returns a new enumerator for this instance.
		/// </summary>
		/// <returns>A new enumerator.</returns>
		public IEnumerator<MetaEntity> GetEnumerator()
		{
			return new InnerEnumerator(this);
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		/// The number of items in this collection.
		/// </summary>
		internal int Count
		{
			get
			{
				int count = 0; foreach (var kvp in _Items) count += kvp.Value.Count;
				return count;
			}
		}

		/// <summary>
		/// Adds the given item into this collection.
		/// </summary>
		internal void Add(MetaEntity item)
		{
			string key = GetKey(item); if (key != null)
			{
				List<MetaEntity> node = null; if (!_Items.TryGetValue(key, out node))
				{
					node = new List<MetaEntity>(NODE_INITIAL_SIZE);
					_Items.Add(key, node);
				}
				node.Add(item);
			}
			else throw new InvalidOperationException(
				"Entity '{0}' has a null id string."
				.FormatWith(item));
		}

		/// <summary>
		/// Removes the given item from this collection.
		/// </summary>
		internal bool Remove(MetaEntity item)
		{
			var r = false;
			var key = GetKey(item); if (key != null)
			{
				List<MetaEntity> node = null; if (_Items.TryGetValue(key, out node))
				{
					r = node.Remove(item);
					if (node.Count == 0) _Items.Remove(key);
				}
			}
			return r;
		}

		/// <summary>
		/// Clears the items contained in this collection.
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
			int num = Count;
			var array = new MetaEntity[num]; if (num > 0)
			{
				int index = 0; foreach (var kvp in _Items)
				{
					kvp.Value.CopyTo(array, index);
					index += kvp.Value.Count;
				}
			}
			return array;
		}

		/// <summary>
		/// Returns the reference to the internal node that keeps all the records that share
		/// the same identity.
		/// </summary>
		internal List<MetaEntity> GetNode(IRecord record)
		{
			List<MetaEntity> node = null;
			string key = GetKey(record);
			if (key != null) _Items.TryGetValue(key, out node);
			return node;
		}

		// ================================================ 
		/// <summary>
		/// Provides enumeration capabilities for a collection of entities.
		/// </summary>
		internal class InnerEnumerator : IEnumerator<MetaEntity>, IDisposable
		{
			UberEntitySet _Master = null;
			MetaEntity _Current = null;
			IEnumerator<KeyValuePair<string, List<MetaEntity>>> _Primary = null;
			IEnumerator<MetaEntity> _Secondary = null;

			/// <summary>
			/// Initializes a new instance.
			/// </summary>
			internal InnerEnumerator(UberEntitySet master)
			{
				_Master = master;
			}

			/// <summary>
			/// Disposes this instance.
			/// </summary>
			public void Dispose()
			{
				Reset();
				_Master = null;
			}

			/// <summary>
			/// Gets the current element produced by the last iteration, or null if no one is
			/// available.
			/// </summary>
			public MetaEntity Current
			{
				get { return _Current; }
			}
			object IEnumerator.Current
			{
				get { return this.Current; }
			}

			/// <summary>
			/// Advances the enumerator to the next element in the collection.
			/// </summary>
			/// <returns>True if an element is available, false otherwise.</returns>
			public bool MoveNext()
			{
				if (_Primary == null) _Primary = _Master._Items.GetEnumerator();

				if (_Secondary == null)
				{
					if (!_Primary.MoveNext())
					{
						Reset(); return false;
					}
					_Secondary = _Primary.Current.Value.GetEnumerator();
				}

				if (!_Secondary.MoveNext())
				{
					_Secondary = null;
					return MoveNext();
				}

				_Current = _Secondary.Current;
				return true;
			}

			/// <summary>
			/// Resets this enumerator preparing it for the next execution.
			/// </summary>
			public void Reset()
			{
				if (_Secondary != null) _Secondary.Dispose(); _Secondary = null;
				if (_Primary != null) _Primary.Dispose(); _Primary = null;
				_Current = null;
			}
		}
	}
}
