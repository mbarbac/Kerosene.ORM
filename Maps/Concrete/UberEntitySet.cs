// ======================================================== UberEntitySet.cs
namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Represents the collection of entities managed by a given map.
	/// </summary>
	internal class UberEntitySet : IEnumerable<MetaEntity>
	{
		object _SyncRoot = new object();
		Dictionary<string, List<MetaEntity>> _Items = new Dictionary<string, List<MetaEntity>>();
		private const int NODELIST_INITIAL_SIZE = 2;

		/// <summary>
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
		/// Disposes this instance, clearing and disposing all the items in this collection.
		/// </summary>
		internal void Dispose()
		{
			if (_Items != null) Clear();
			_Items = null;
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
			string str = item.IdString; if (str != null)
			{
				List<MetaEntity> node = null; if (!_Items.TryGetValue(str, out node))
				{
					node = new List<MetaEntity>(NODELIST_INITIAL_SIZE);
					_Items.Add(str, node);
				}
				node.Add(item);
			}
			else throw new InvalidOperationException(
				"Entity '{0}' has not a record associated to it."
				.FormatWith(item));
		}

		/// <summary>
		/// Removes the given item from this collection.
		/// </summary>
		internal bool Remove(MetaEntity item)
		{
			bool r = false;
			string str = item.IdString; if (str != null)
			{
				List<MetaEntity> node = null; if (_Items.TryGetValue(str, out node))
				{
					r = node.Remove(item);
					if (node.Count == 0) _Items.Remove(str);
				}
			}
			return r;
		}

		/// <summary>
		/// Clears all the items contained in this collection, disposing them all.
		/// </summary>
		internal void Clear()
		{
			foreach (var kvp in _Items)
			{
				foreach (var meta in kvp.Value)
				{
					meta.Reset(removeMap: false);
				}
			}
			_Items.Clear();
		}

		/// <summary>
		/// Collects and removes the invalid entities this collection may hold.
		/// </summary>
		internal void CollectInvalidEntities()
		{
			var limit = 5000; // TODO: review this hard limit...
			var count = 0;
			var list = new List<MetaEntity>();

			foreach (var node in _Items)
				foreach (var meta in node.Value)
					if (!meta.HasValidEntity) { list.Add(meta); if (++count >= limit) break; }

			foreach (var meta in list)
			{
				DebugEx.IndentWriteLine("\n- Collecting '{0}'...", meta);
				meta.Reset();
				DebugEx.Unindent();
			}

			list.Clear(); list = null;
		}

		/// <summary>
		/// Returns a new array containing the elements of this collection.
		/// </summary>
		internal MetaEntity[] ToArray()
		{
			int count = Count;
			var array = new MetaEntity[count]; if (count > 0)
			{
				int index = 0; foreach (var kvp in _Items)
				{
					kvp.Value.CopyTo(array, index);
					index += kvp.Value.Count;
				}
			}
			return array;
		}

		internal List<MetaEntity> ToList(Predicate<MetaEntity> match)
		{
			var count = Count;
			var list = new List<MetaEntity>(count > 128 ? 16 : 4);
			
			foreach (var kvp in _Items)
			{
				foreach (var meta in kvp.Value)
				{
					if (match == null || match(meta)) list.Add(meta);
				}
			}
			return list;
		}

		/// <summary>
		/// Returns the list that contains all the entities whose identity columns match with
		/// the ones specified by the given record, or null if such node cannot be found.
		/// </summary>
		internal List<MetaEntity> NodeList(Core.IRecord record)
		{
			string str = record.GetIdString();
			List<MetaEntity> node = null; _Items.TryGetValue(str, out node);
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
// ======================================================== 
