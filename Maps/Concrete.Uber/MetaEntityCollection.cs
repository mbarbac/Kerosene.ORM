using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kerosene.ORM.Maps.Concrete
{
	// ====================================================
	internal class MetaEntityCollection
	{
		Dictionary<string, List<MetaEntity>> _Items = new Dictionary<string, List<MetaEntity>>();
		private const int NODE_INITIAL_SIZE = 1;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		internal MetaEntityCollection() { }

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the string representation of this instance.</returns>
		public override string ToString()
		{
			return string.Format("{0}({1})", GetType().EasyName(), Count);
		}

		/// <summary>
		/// The items contained in this collection
		/// </summary>
		internal IEnumerable<MetaEntity> Items
		{
			get
			{
				foreach (var node in _Items)
					foreach (var item in node.Value) yield return item;
			}
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
		/// Adds the given element into this collection.
		/// </summary>
		internal void Add(MetaEntity item)
		{
			string id = item == null ? null : item.IdentityString;
			if (id == null) throw new Exception("Cannot obtain identity for '{0}'.".FormatWith(item.Sketch()));

			List<MetaEntity> node = null; if (!_Items.TryGetValue(id, out node))
			{
				node = new List<MetaEntity>(NODE_INITIAL_SIZE);
				_Items.Add(id, node);
			}
			node.Add(item);
		}

		/// <summary>
		/// Removes the given element from this collection.
		/// </summary>
		internal bool Remove(MetaEntity item)
		{
			var r = false; if (item == null) return false;
			var id = item.IdentityString; if (id != null)
			{
				List<MetaEntity> node = null; if (_Items.TryGetValue(id, out node))
				{
					r = node.Remove(item);
					if (r && node.Count == 0) _Items.Remove(id);
				}
			}
			if (!r)
			{
				List<MetaEntity> node = null; foreach (var kvp in _Items)
				{
					r = kvp.Value.Remove(item);
					if (r)
					{
						id = kvp.Key;
						node = kvp.Value;
						break;
					}
				}
				if (r && node.Count != 0) _Items.Remove(id);
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
		public MetaEntity[] ToArray()
		{
			int num = Count; var array = new MetaEntity[num]; if (num > 0)
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
		/// Returns the actual node that maintains the entities with the given identity.
		/// </summary>
		internal List<MetaEntity> FindNode(string id)
		{
			List<MetaEntity> node = null; if (id != null) _Items.TryGetValue(id, out node);
			return node;
		}

		/// <summary>
		/// Returns the actual node that maintains the entities with the identity obtained
		/// from the given record, or null if no node can be found.
		/// </summary>
		internal List<MetaEntity> FindNode(IRecord record)
		{
			return FindNode(record.IdentityString());
		}

		/// <summary>
		/// Returns the actual node that maintains the entities with the its same identity.
		/// </summary>
		internal List<MetaEntity> FindNode(MetaEntity meta)
		{
			return FindNode(meta.IdentityString);
		}
	}
}
