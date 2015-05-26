using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Kerosene.ORM.Maps.Concrete
{
	// ====================================================
	internal interface IUberColumn : IMapColumn
	{
		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		new IUberMap Map { get; }

		/// <summary>
		/// Identifies whether this column has been required by other object and so it cannot be
		/// excluded.
		/// </summary>
		bool Required { get; set; }

		/// <summary>
		/// The element info this instance refers to, if any.
		/// </summary>
		ElementInfo ElementInfo { get; }
	}

	// ====================================================
	/// <summary>
	/// Represents a column in the database associated with a map.
	/// </summary>
	public class MapColumn<T> : IMapColumn<T>, IUberColumn where T : class
	{
		DataMap<T> _Map = null;
		string _Name = null;
		bool _AutoDiscovered = false;
		bool _Excluded = false;
		bool _Required = false;
		string _ElementName = null;
		ElementInfo _ElementInfo = null;
		Func<T, object> _WriteRecord = null; bool _WriteEnabled = true;
		Action<object, T> _LoadEntity = null; bool _LoadEnabled = true;

		/// <summary>
		/// Intializes a new instance.
		/// </summary>
		internal MapColumn(DataMap<T> map, string name)
		{
			if (map == null) throw new ArgumentNullException("map", "Meta Map cannot be null.");
			if (map.IsDisposed) throw new ObjectDisposedException(map.ToString());
			_Map = map;
			_Name = name.Validated("Column Name");
		}

		/// <summary>
		/// Whether this instance has been disposed or not.
		/// </summary>
		internal bool IsDisposed
		{
			get { return Map == null; }
		}

		/// <summary>
		/// Invoked when disposing this instance.
		/// </summary>
		internal protected virtual void OnDispose()
		{
			_Map = null;
			if (_ElementInfo != null) _ElementInfo.Dispose(); _ElementInfo = null;
			_WriteRecord = null;
			_LoadEntity = null;
		}

		/// <summary></summary>
		~MapColumn()
		{
			if (!IsDisposed) OnDispose();
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(_Name ?? string.Empty);
			if (_Excluded) sb.Append(":Excluded");
			if (_AutoDiscovered) sb.Append(":Auto");
			if (_ElementName != null) sb.AppendFormat(":On({0})", _ElementName);

			var str = sb.ToString();
			return IsDisposed ? "disposed::{0}({1})".FormatWith(GetType().EasyName(), str) : str;
		}

		/// <summary>
		/// Invoked when the associated map is validated.
		/// </summary>
		internal protected virtual void OnValidate()
		{
			var flags = TypeEx.FlattenInstancePublicAndHidden;
			var type = _Map.EntityType;
			var sensitive = _Map.Repository.Link.Engine.CaseSensitiveNames;

			var name = _ElementName ?? _Name;
			_ElementInfo = ElementInfo.Create(_Map.EntityType, name, raise: false, flags: flags);

			if (_ElementInfo == null && !sensitive)
			{
				flags |= BindingFlags.IgnoreCase;
				_ElementInfo = ElementInfo.Create(_Map.EntityType, name, raise: false, flags: flags);
			}

			if (_ElementInfo == null && _ElementName != null) throw new NotFoundException(
				"Member '{0}' not found in type '{1}'."
				.FormatWith(_Name, _Map.EntityType.EasyName()));
		}

		/// <summary>
		/// Returns a new instance that is a copy of the original one, but associated with
		/// the given target.
		/// </summary>
		/// <param name="target">The target the new instance with be associated with.</param>
		/// <returns>A new instance.</returns>
		internal MapColumn<T> Clone(DataMap<T> target)
		{
			var cloned = new MapColumn<T>(target, Name); OnClone(cloned);
			return cloned;
		}

		/// <summary>
		/// Invoked when cloning this object to set its state at this point of the inheritance
		/// chain.
		/// </summary>
		/// <param name="cloned">The cloned object.</param>
		protected virtual void OnClone(object cloned)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (cloned == null) throw new ArgumentNullException("cloned", "Cloned object cannot be null.");

			var temp = cloned as MapColumn<T>;
			if (temp == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' instance."
				.FormatWith(cloned.Sketch(), typeof(MapColumn<T>).EasyName()));

			temp._AutoDiscovered = _AutoDiscovered;
			temp._Required = _Required;
			temp._Excluded = _Excluded;
			temp._ElementName = _ElementName;
			temp._WriteRecord = _WriteRecord; temp._WriteEnabled = _WriteEnabled;
			temp._LoadEntity = _LoadEntity; temp._LoadEnabled = _LoadEnabled;
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

		/// <summary>
		/// Identifies whether this column has been required by other object and so it cannot be
		/// excluded.
		/// </summary>
		internal bool Required
		{
			get { return _Required; }
			set
			{
				if (value && Excluded) throw new InvalidOperationException(
					"This column '{0}' is required but it has been excluded previously."
					.FormatWith(this));

				_Required = value;
			}
		}
		bool IUberColumn.Required
		{
			get { return this.Required; }
			set { this.Required = value; }
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

				if (value && Required) throw new InvalidOperationException(
					"Cannot exclude this column '{0}' because it is marked as required."
					.FormatWith(this));

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
			Excluded = excluded;
			return this;
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
		/// The name of the element of the type this column shall be mapped to, if any.
		/// </summary>
		public string ElementName
		{
			get { return _ElementName; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (Map.IsDisposed) throw new ObjectDisposedException(Map.ToString());
				if (Map.IsValidated) throw new InvalidOperationException("Map '{0}' is validated.".FormatWith(Map));

				_ElementName = value.Validated("Element Name", canbeNull: true);
			}
		}

		/// <summary>
		/// Sets the name of the element of the type this column shall be mapped to, if any.
		/// </summary>
		/// <param name="name">The name of the element, or null.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		public MapColumn<T> SetElementName(Func<dynamic, object> name)
		{
			ElementName = DynamicInfo.ParseName(name);
			return this;
		}
		IMapColumn<T> IMapColumn<T>.SetElementName(Func<dynamic, object> name)
		{
			return this.SetElementName(name);
		}
		IMapColumn IMapColumn.SetElementName(Func<dynamic, object> name)
		{
			return this.SetElementName(name);
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
			WriteEnabled = enabled;
			return this;
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
			WriteRecord = onWrite;
			return this;
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
			LoadEnabled = enabled;
			return this;
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
		/// <param name="onLoad">The delegate to invoke, or null.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		public MapColumn<T> OnLoadEntity(Action<object, T> onLoad)
		{
			LoadEntity = onLoad;
			return this;
		}
		IMapColumn<T> IMapColumn<T>.OnLoadEntity(Action<object, T> onLoad)
		{
			return this.OnLoadEntity(onLoad);
		}
		IMapColumn IMapColumn.OnLoadEntity(Action<object, object> onLoad)
		{
			return this.OnLoadEntity(onLoad);
		}
	}
}
