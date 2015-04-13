// ======================================================== MapVersionColumn.cs
namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Extends the <see cref="IMapVersionColumn"/> interface.
	/// </summary>
	internal interface IUberVersionColumn : IMapVersionColumn
	{
		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		new IUberMap Map { get; }
	}

	// ==================================================== 
	/// <summary>
	/// If this instance is not empty represents the column in the primary table that will be
	/// used for row version control purposes.
	/// </summary>
	public class MapVersionColumn<T> : IMapVersionColumn<T>, IUberVersionColumn where T : class
	{
		DataMap<T> _Map = null;
		string _Name = null;
		Func<object, string> _ValueToString = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		internal protected MapVersionColumn(DataMap<T> map)
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
		/// Disposes this instance.
		/// </summary>
		internal virtual void Dispose()
		{
			_Map = null;
			_ValueToString = null;
		}

		~MapVersionColumn()
		{
			if (!IsDisposed) Dispose();
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			var str = Name ?? string.Empty;
			return IsDisposed ? "disposed::{0}({1})".FormatWith(GetType().EasyName(), str) : str;
		}

		/// <summary>
		/// Clones this instance.
		/// </summary>
		internal protected virtual MapVersionColumn<T> Clone(DataMap<T> map)
		{
			var temp = new MapVersionColumn<T>(map);
			temp._Name = _Name;
			temp._ValueToString = _ValueToString;
			return temp;
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
		/// The name of the column to be used for row version control, if any.
		/// </summary>
		public string Name
		{
			get { return _Name; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (Map.IsDisposed) throw new ObjectDisposedException(Map.ToString());
				if (Map.IsValidated) throw new InvalidOperationException("Map '{0}' is validated.".FormatWith(Map));

				_Name = value.Validated("Column Name", canbeNull: true);
			}
		}

		/// <summary>
		/// Sets the name of the database column. If this value is null the row version control is
		/// disabled.
		/// </summary>
		/// <param name="name">A dynamic lambda expression that resolves into the name of the
		/// database column. If this argument is null, or resolves into null, row version control
		/// is disable.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		public MapVersionColumn<T> SetName(Func<dynamic, object> name)
		{
			var temp = name == null ? null : DynamicInfo.ParseName(name);
			Name = temp; return this;
		}
		IMapVersionColumn<T> IMapVersionColumn<T>.SetName(Func<dynamic, object> name)
		{
			return this.SetName(name);
		}
		IMapVersionColumn IMapVersionColumn.SetName(Func<dynamic, object> name)
		{
			return this.SetName(name);
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
			ValueToString = func; return this;
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
// ======================================================== 
