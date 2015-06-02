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
	internal interface IUberVersionColumn : IMapVersionColumn
	{
		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		new IUberMap Map { get; }
	}

	// ====================================================
	/// <summary>
	/// Represents the column to be used for row version control, if any.
	/// </summary>
	public class MapVersionColumn<T> : IMapVersionColumn<T>, IUberVersionColumn where T : class
	{
		DataMap<T> _Map = null;
		string _Name = null;
		bool _Enabled = true;
		MapColumn<T> _Column = null;
		Func<object, string> _ValueToString = null;

		/// <summary>
		/// Intializes a new instance.
		/// </summary>
		internal MapVersionColumn(DataMap<T> map)
		{
			if (map == null) throw new ArgumentNullException("map", "Meta Map cannot be null.");
			if (map.IsDisposed) throw new ObjectDisposedException(map.ToString());
			_Map = map;
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
			_Column = null;
			_ValueToString = null;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(_Name ?? string.Empty);
			sb.Append(_Enabled ? ":Enabled" : ":Disabled");

			var str = sb.ToString();
			return IsDisposed ? "disposed::{0}({1})".FormatWith(GetType().EasyName(), str) : str;
		}

		/// <summary>
		/// Invoked when the associated map is validated.
		/// </summary>
		internal protected virtual void OnValidate() { }

		/// <summary>
		/// Returns a new instance that is a copy of the original one, but associated with
		/// the given target.
		/// </summary>
		/// <param name="target">The target the new instance with be associated with.</param>
		/// <returns>A new instance.</returns>
		internal MapVersionColumn<T> Clone(DataMap<T> target)
		{
			var cloned = new MapVersionColumn<T>(target); OnClone(cloned);
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

			var temp = cloned as MapVersionColumn<T>;
			if (temp == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' instance."
				.FormatWith(cloned.Sketch(), typeof(MapVersionColumn<T>).EasyName()));

			temp._Name = _Name;
			temp._Enabled = _Enabled;
			temp._ValueToString = _ValueToString;

			temp._Column = temp.Map.Columns.Find(x => x.Name == _Column.Name);
		}

		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		public DataMap<T> Map
		{
			get { return _Map; }
		}
		IDataMap<T> IMapVersionColumn<T>.Map
		{
			get { return this.Map; }
		}
		IDataMap IMapVersionColumn.Map
		{
			get { return this.Map; }
		}
		IUberMap IUberVersionColumn.Map
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
		/// Sets the name of the column to use for row version control purposes. If the name is
		/// null then row version control is not enforced.
		/// </summary>
		/// <param name="name">A dynamic lambda expression that resolves into the name of the
		/// column.</param>
		/// <param name="customize">If not null the delegate to invoke with the column as its
		/// argument in order to permit its customization, if needed.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		public MapVersionColumn<T> SetName(Func<dynamic, object> name, Action<MapColumn<T>> customize = null)
		{
			_Column = Map.Columns.Add(name, raise: false);
			_Column.Required = true;
			_Name = _Column.Name;

			if (customize != null) customize(_Column);
			return this;
		}
		IMapVersionColumn<T> IMapVersionColumn<T>.SetName(Func<dynamic, object> name, Action<IMapColumn<T>> customize)
		{
			return this.SetName(name, customize);
		}
		IMapVersionColumn IMapVersionColumn.SetName(Func<dynamic, object> name, Action<IMapColumn> customize)
		{
			return this.SetName(name, customize);
		}

		/// <summary>
		/// The column item associated with this instance, if any.
		/// </summary>
		internal MapColumn<T> Column
		{
			get { return _Column; }
		}

		/// <summary>
		/// Whether row version control is enabled for delete and update operations, or not.
		/// The getter returns false despite its internal value if the 'Name' property is null.
		/// </summary>
		public bool Enabled
		{
			get { return _Name == null ? false : _Enabled; }
			set { _Enabled = value; }
		}

		/// <summary>
		/// The delegate to invoke to convert whatever value the row version control column has
		/// into a string for comparison purposes.
		/// <para>If this property is null then a default comparison delegate will be used.</para>
		/// </summary>
		public Func<object, string> ValueToString
		{
			get { return _ValueToString; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (Map.IsDisposed) throw new ObjectDisposedException(Map.ToString());
				if (Map.IsValidated) throw new InvalidOperationException("Map '{0}' is validated.".FormatWith(Map));

				_ValueToString = value;
			}
		}

		/// <summary>
		/// Sets the delegate to invoke to convert whatever value the row version control column
		/// has into a string for comparison purposes.
		/// <para>If this property is null then a default comparison delegate will be used.</para>
		/// </summary>
		/// <param name="func">The delegate to set, or null.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		public MapVersionColumn<T> OnValueToString(Func<object, string> func)
		{
			ValueToString = func;
			return this;
		}
		IMapVersionColumn<T> IMapVersionColumn<T>.OnValueToString(Func<object, string> func)
		{
			return this.OnValueToString(func);
		}
		IMapVersionColumn IMapVersionColumn.OnValueToString(Func<object, string> func)
		{
			return this.OnValueToString(func);
		}
	}
}
