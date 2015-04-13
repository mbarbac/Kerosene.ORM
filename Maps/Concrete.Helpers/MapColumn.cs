// ======================================================== MapColumn.cs
namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Linq;
	using System.Text;
	
	// ==================================================== 
	/// <summary>
	/// Extends the <see cref="IMapColumn"/> interface.
	/// </summary>
	internal interface IUberColumn : IMapColumn
	{
		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		new IUberMap Map { get; }

		/// <summary>
		/// The element info this instance refers to, if any.
		/// </summary>
		ElementInfo ElementInfo { get; }

		/// <summary>
		/// Whether this column has been automatically discovered and included in the map as
		/// part of the process of map validation.
		/// </summary>
		new bool AutoDiscovered { get; set; }
	}

	// ==================================================== 
	/// <summary>
	/// Represents a column in the primary table that has been associated with a map.
	/// </summary>
	public class MapColumn<T> : IMapColumn<T>, IUberColumn where T : class
	{
		DataMap<T> _Map = null;
		string _Name = null;
		bool _Excluded = false;
		bool _AutoDiscovered = false;
		bool _WriteEnabled = true; Func<T, object> _WriteRecord = null;
		bool _LoadEnabled = true; Action<object, T> _LoadEntity = null;
		ElementInfo _ElementInfo = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		internal protected MapColumn(DataMap<T> map, string name)
		{
			if (map == null) throw new ArgumentNullException("map", "Meta Map cannot be null.");
			if (map.IsDisposed) throw new ObjectDisposedException(map.ToString());
			_Map = map;

			Name = name;
		}

		/// <summary>
		/// Whether this instance has been disposed or not.
		/// </summary>
		internal bool IsDisposed
		{
			get { return Map == null; }
		}

		/// <summary>
		/// Disposes this instance.
		/// </summary>
		internal virtual void Dispose()
		{
			_Map = null;
			_WriteRecord = null;
			_LoadEntity = null;
			if (_ElementInfo != null) _ElementInfo.Dispose(); _ElementInfo = null;
		}

		~MapColumn()
		{
			if (!IsDisposed) Dispose();
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(Name ?? string.Empty);
			if (Excluded) sb.Append(":Excluded");
			if (AutoDiscovered) sb.Append(":Auto");

			if (ElementInfo != null &&
				string.Compare(ElementInfo.Name, Name, true) != 0)
				sb.AppendFormat(":On({0})", ElementInfo.FullName);

			var str = sb.ToString();
			return IsDisposed ? "disposed::{0}({1})".FormatWith(GetType().EasyName(), str) : str;
		}

		/// <summary>
		/// Clones this instance.
		/// </summary>
		internal protected virtual MapColumn<T> Clone(DataMap<T> map)
		{
			var temp = new MapColumn<T>(map, Name);
			temp._Excluded = _Excluded;
			temp._WriteEnabled = _WriteEnabled; temp._WriteRecord = _WriteRecord;
			temp._LoadEnabled = _LoadEnabled; temp._LoadEntity = _LoadEntity;
			return temp;
		}

		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		public DataMap<T> Map
		{
			get { return _Map; }
		}
		IDataMap<T> IMapColumn<T>.Map
		{
			get { return this.Map; }
		}
		IDataMap IMapColumn.Map
		{
			get { return this.Map; }
		}
		IUberMap IUberColumn.Map
		{
			get { return this.Map; }
		}

		/// <summary>
		/// The name of the database column.
		/// </summary>
		public string Name
		{
			get { return _Name; }
			private set
			{
				_Name = value.Validated("Column Name");

				if (_ElementInfo != null) _ElementInfo.Dispose();
				_ElementInfo = null;

				var flags = TypeEx.InstancePublicAndHidden;
				var type = typeof(T);
				var sensitive = Map.Repository.Link.Engine.CaseSensitiveNames;

				var props = type.GetProperties(flags);
				var prop = props.FirstOrDefault(x => string.Compare(x.Name, _Name, !sensitive) == 0);
				if (prop != null) _ElementInfo = new ElementInfo(prop);

				else
				{
					var fields = type.GetFields(flags);
					var field = fields.FirstOrDefault(x => string.Compare(x.Name, _Name, !sensitive) == 0);
					if (field != null) _ElementInfo = new ElementInfo(field);
				}
			}
		}

		/// <summary>
		/// The element info this instance refers to, if any.
		/// </summary>
		internal ElementInfo ElementInfo
		{
			get { return _ElementInfo; }
		}
		ElementInfo IUberColumn.ElementInfo
		{
			get { return this.ElementInfo; }
		}

		/// <summary>
		/// Whether this column has been automatically discovered and included in the map as
		/// part of the process of map validation.
		/// </summary>
		public bool AutoDiscovered
		{
			get { return _AutoDiscovered; }
			internal set { _AutoDiscovered = value; }
		}
		bool IUberColumn.AutoDiscovered
		{
			get { return this.AutoDiscovered; }
			set { this.AutoDiscovered = value; }
		}

		/// <summary>
		/// Whether this database column is explicitly excluded from the map so that it will not
		/// be taken into consideration for any map operations.
		/// </summary>
		public bool Excluded
		{
			get { return _Excluded; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (Map.IsDisposed) throw new ObjectDisposedException(Map.ToString());
				if (Map.IsValidated) throw new InvalidOperationException("Map '{0}' is validated.".FormatWith(Map));

				_Excluded = value;
			}
		}

		/// <summary>
		/// Sets whether this database column is explicitly excluded from the map so that it will
		/// not be taken into consideration for any map operations.
		/// </summary>
		/// <param name="excluded">True to exclude the database column from the map.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		public MapColumn<T> SetExcluded(bool excluded)
		{
			Excluded = excluded; return this;
		}
		IMapColumn<T> IMapColumn<T>.SetExcluded(bool excluded)
		{
			return this.SetExcluded(excluded);
		}
		IMapColumn IMapColumn.SetExcluded(bool excluded)
		{
			return this.SetExcluded(excluded);
		}

		/// <summary>
		/// Whether writing into the database record for this column is enabled or not.
		/// </summary>
		public bool WriteEnabled
		{
			get { return _WriteEnabled; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (Map.IsDisposed) throw new ObjectDisposedException(Map.ToString());
				if (Map.IsValidated) throw new InvalidOperationException("Map '{0}' is validated.".FormatWith(Map));

				_WriteEnabled = value;
			}
		}

		/// <summary>
		/// Sets whether writing into the database record for this column is enabled or not.
		/// </summary>
		/// <param name="enabled">True or false.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		public MapColumn<T> OnWriteRecord(bool enabled)
		{
			WriteEnabled = enabled; return this;
		}
		IMapColumn<T> IMapColumn<T>.OnWriteRecord(bool enabled)
		{
			return this.OnWriteRecord(enabled);
		}
		IMapColumn IMapColumn.OnWriteRecord(bool enabled)
		{
			return this.OnWriteRecord(enabled);
		}

		/// <summary>
		/// If not null the delegate to invoke with an (entity) argument to obtain the value
		/// to write into the database record for this column.
		/// </summary>
		public Func<T, object> WriteRecord
		{
			get { return _WriteRecord; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (Map.IsDisposed) throw new ObjectDisposedException(Map.ToString());
				if (Map.IsValidated) throw new InvalidOperationException("Map '{0}' is validated.".FormatWith(Map));

				_WriteRecord = value;
			}
		}
		Func<object, object> IMapColumn.WriteRecord
		{
			get
			{
				Func<object, object> del = null; if (this.WriteRecord != null)
				{
					del = (obj) => { return this.WriteRecord((T)obj); };
				}
				return del;
			}
			set { this.WriteRecord = value; }
		}

		/// <summary>
		/// Sets the delegate to invoke with an (entity) argument to obtain the value to write
		/// into the database record for this column.
		/// </summary>
		/// <param name="onWrite">The delegate to invoke, or null.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		public MapColumn<T> OnWriteRecord(Func<T, object> onWrite)
		{
			WriteRecord = onWrite; return this;
		}
		IMapColumn<T> IMapColumn<T>.OnWriteRecord(Func<T, object> onWrite)
		{
			return this.OnWriteRecord(onWrite);
		}
		IMapColumn IMapColumn.OnWriteRecord(Func<object, object> onWrite)
		{
			return this.OnWriteRecord(onWrite);
		}

		/// <summary>
		/// Whether loading the value from the database record into the entity is enabled or not
		/// for this column.
		/// </summary>
		public bool LoadEnabled
		{
			get { return _LoadEnabled; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (Map.IsDisposed) throw new ObjectDisposedException(Map.ToString());
				if (Map.IsValidated) throw new InvalidOperationException("Map '{0}' is validated.".FormatWith(Map));

				_LoadEnabled = value;
			}
		}

		/// <summary>
		/// Sets loading the value from the database record into the entity is enabled or not
		/// for this column.
		/// </summary>
		/// <param name="enabled">True or false.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		public MapColumn<T> OnLoadEntity(bool enabled)
		{
			LoadEnabled = enabled; return this;
		}
		IMapColumn<T> IMapColumn<T>.OnLoadEntity(bool enabled)
		{
			return this.OnLoadEntity(enabled);
		}
		IMapColumn IMapColumn.OnLoadEntity(bool enabled)
		{
			return this.OnLoadEntity(enabled);
		}

		/// <summary>
		/// If not null the delegate to invoke with (value, entity) arguments to load into the
		/// entity the value from the database record for this column.
		/// </summary>
		public Action<object, T> LoadEntity
		{
			get { return _LoadEntity; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (Map.IsDisposed) throw new ObjectDisposedException(Map.ToString());
				if (Map.IsValidated) throw new InvalidOperationException("Map '{0}' is validated.".FormatWith(Map));

				_LoadEntity = value;
			}
		}
		Action<object, object> IMapColumn.LoadEntity
		{
			get
			{
				Action<object, object> del = null; if (this.LoadEntity != null)
				{
					del = (value, obj) => { this.LoadEntity(value, (T)obj); };
				}
				return del;
			}
			set { this.LoadEntity = value; }
		}

		/// <summary>
		/// Sets the delegate to invoke with (value, entity) arguments to load into the
		/// entity the value from the database record for this column.
		/// </summary>
		/// <param name="onWrite">The delegate to invoke, or null.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		public MapColumn<T> OnLoadEntity(Action<object, T> onLoad)
		{
			LoadEntity = onLoad; return this;
		}
		IMapColumn<T> IMapColumn<T>.OnLoadEntity(Action<object, T> onLoad)
		{
			return this.OnLoadEntity(onLoad);
		}
		IMapColumn IMapColumn.OnLoadEntity(Action<object, object> onLoad)
		{
			return this.OnLoadEntity(onLoad);
		}

		/// <summary>
		/// Identifies the member into which this database column will be mapped. Members can
		/// be both properties and fields and either public, protected or private ones.
		/// </summary>
		/// <param name="element">A dynamic lambda expression that resolves into the name of the
		/// member of the type into which this database column will be mapped.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		public MapColumn<T> OnElement(Func<dynamic, object> element)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (Map.IsDisposed) throw new ObjectDisposedException(Map.ToString());
			if (Map.IsValidated) throw new InvalidOperationException("Map '{0}' is validated.".FormatWith(Map));

			if (_ElementInfo != null) _ElementInfo.Dispose();
			_ElementInfo = null;

			if (element != null)
			{
				var name = DynamicInfo.ParseName(element);
				var flags = TypeEx.InstancePublicAndHidden;
				_ElementInfo = ElementInfo.Create<T>(name, raise: true, flags: flags);
			}

			return this;
		}
		IMapColumn<T> IMapColumn<T>.OnElement(Func<dynamic, object> member)
		{
			return this.OnElement(member);
		}
		IMapColumn IMapColumn.OnElement(Func<dynamic, object> member)
		{
			return this.OnElement(member);
		}
	}
}
// ======================================================== 
