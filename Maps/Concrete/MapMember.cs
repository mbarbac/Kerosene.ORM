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
	internal interface IUberMember : IMapMember
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
		/// The lazy property associated with this member, if any.
		/// </summary>
		LazyProperty LazyProperty { get; }
	}

	// ====================================================
	/// <summary>
	/// Represents a member of the type for which either a dependency and/or a completion method
	/// has been defined.
	/// </summary>
	public class MapMember<T> : IMapMember<T>, IUberMember where T : class
	{
		DataMap<T> _Map = null;
		string _Name = null;
		ElementInfo _ElementInfo = null;
		MemberDependencyMode _DependencyMode = MemberDependencyMode.None;
		Action<IRecord, T> _CompleteMember = null;
		LazyProperty _LazyProperty = null; bool _LazyPropertyCaptured = false;
		List<MapColumn<T>> _Columns = new List<MapColumn<T>>();

		/// <summary>
		/// Intializes a new instance.
		/// </summary>
		internal MapMember(DataMap<T> map, string name)
		{
			if (map == null) throw new ArgumentNullException("map", "Meta Map cannot be null.");
			if (map.IsDisposed) throw new ObjectDisposedException(map.ToString());
			_Map = map;
			_Name = name.Validated("Member Name");
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
			try
			{
				if (_ElementInfo != null) _ElementInfo.Dispose();
			}
			catch { }

			_Map = null;
			_ElementInfo = null;
			_CompleteMember = null;
			_Columns = null;
			_LazyProperty = null;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(_Name ?? string.Empty);
			if (DependencyMode != MemberDependencyMode.None) sb.AppendFormat(":Is{0}", DependencyMode);
			//if (LazyProperty != null) sb.Append(":Lazy");

			if (_Columns != null && _Columns.Count != 0)
			{
				sb.Append(" ["); bool first = true; foreach (var col in _Columns)
				{
					if (first) first = false; else sb.Append(", ");
					sb.Append(col.Name);
				}
				sb.Append("]");
			}

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

			_ElementInfo = ElementInfo.Create(_Map.EntityType, _Name, raise: false, flags: flags);

			if (_ElementInfo == null && !sensitive)
			{
				flags |= BindingFlags.IgnoreCase;
				_ElementInfo = ElementInfo.Create(_Map.EntityType, _Name, raise: false, flags: flags);
			}

			if (_ElementInfo == null) throw new NotFoundException(
				"Member '{0}' not found in type '{1}'."
				.FormatWith(_Name, _Map.EntityType.EasyName()));
		}

		/// <summary>
		/// Returns a new instance that is a copy of the original one, but associated with
		/// the given target.
		/// </summary>
		/// <param name="target">The target the new instance with be associated with.</param>
		/// <returns>A new instance.</returns>
		internal MapMember<T> Clone(DataMap<T> target)
		{
			var cloned = new MapMember<T>(target, Name); OnClone(cloned);
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

			var temp = cloned as MapMember<T>;
			if (temp == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' instance."
				.FormatWith(cloned.Sketch(), typeof(MapMember<T>).EasyName()));

			temp._DependencyMode = _DependencyMode;
			temp._CompleteMember = _CompleteMember;

			foreach (var col in _Columns)
			{
				var cache = temp.Map.Columns.Find(x => x.Name == col.Name);
				temp._Columns.Add(cache);
			}
		}

		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		public DataMap<T> Map
		{
			get { return _Map; }
		}
		IDataMap<T> IMapMember<T>.Map
		{
			get { return this.Map; }
		}
		IDataMap IMapMember.Map
		{
			get { return this.Map; }
		}
		IUberMap IUberMember.Map
		{
			get { return this.Map; }
		}

		/// <summary>
		/// The name of the member this instance refers to.
		/// </summary>
		public string Name
		{
			get { return _Name; }
		}

		/// <summary>
		/// The element info this instance refers to, if any.
		/// </summary>
		internal ElementInfo ElementInfo
		{
			get { return _ElementInfo; }
		}
		ElementInfo IUberMember.ElementInfo
		{
			get { return this.ElementInfo; }
		}

		/// <summary>
		/// The lazy property associated with this member, if any.
		/// </summary>
		internal LazyProperty LazyProperty
		{
			get
			{
				if (!_LazyPropertyCaptured && _Map != null && _Map.IsValidated)
				{
					if (_Map.ProxyHolder != null) _LazyProperty = _Map.ProxyHolder.LazyProperties.Find(Name);
					_LazyPropertyCaptured = true;
				}
				return _LazyProperty;
			}
		}
		LazyProperty IUberMember.LazyProperty
		{
			get { return this.LazyProperty; }
		}

		/// <summary>
		/// The type of the dependency defined for this member, if any.
		/// </summary>
		public MemberDependencyMode DependencyMode
		{
			get { return _DependencyMode; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (Map.IsDisposed) throw new ObjectDisposedException(Map.ToString());
				if (Map.IsValidated) throw new InvalidOperationException("Map '{0}' is validated.".FormatWith(Map));

				_DependencyMode = value;
			}
		}

		/// <summary>
		/// Sets the type of the dependency defined for this member, if any.
		/// </summary>
		/// <param name="mode">The dependency mode.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		public MapMember<T> SetDependencyMode(MemberDependencyMode mode)
		{
			DependencyMode = mode;
			return this;
		}
		IMapMember IMapMember.SetDependencyMode(MemberDependencyMode mode)
		{
			return this.SetDependencyMode(mode);
		}

		/// <summary>
		/// If not null maintains the delegate to invoke with (record, entity) arguments to
		/// complete the value of the member this instance refers to.
		/// </summary>
		public Action<IRecord, T> CompleteMember
		{
			get { return _CompleteMember; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (Map.IsDisposed) throw new ObjectDisposedException(Map.ToString());
				if (Map.IsValidated) throw new InvalidOperationException("Map '{0}' is validated.".FormatWith(Map));

				_CompleteMember = value;
			}
		}
		Action<IRecord, object> IMapMember.CompleteMember
		{
			get
			{
				Action<IRecord, object> del = null; if (this.CompleteMember != null)
				{
					del = (rec, obj) => { this.CompleteMember(rec, (T)obj); };
				}
				return del;
			}
			set { this.CompleteMember = value; }
		}

		/// <summary>
		/// Sets the delegate to invoke with (record, entity) arguments to complete the value
		/// of the member this instance refers to.
		/// </summary>
		/// <param name="onComplete">The delegate to invoke, or null.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		public MapMember<T> OnComplete(Action<IRecord, T> onComplete)
		{
			CompleteMember = onComplete;
			return this;
		}
		IMapMember<T> IMapMember<T>.OnComplete(Action<Core.IRecord, T> onComplete)
		{
			return this.OnComplete(onComplete);
		}
		IMapMember IMapMember.OnComplete(Action<Core.IRecord, object> onComplete)
		{
			return this.OnComplete(onComplete);
		}

		/// <summary>
		/// Identifies a column needed to support this member and, if the column did not exist
		/// previously in the map, it is created.
		/// </summary>
		/// <param name="name">A dynamic lambda expression that resolves into the name of the
		/// column.</param>
		/// <param name="customize">If not null the delegate to invoke with the column as its
		/// argument in order to permit its customization, if needed.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		public MapMember<T> WithColumn(Func<dynamic, object> name, Action<MapColumn<T>> customize = null)
		{
			var item = Map.Columns.Add(name, raise: false);
			item.Required = true;
			_Columns.Add(item);

			if (customize != null) customize(item);
			return this;
		}
		IMapMember<T> IMapMember<T>.WithColumn(Func<dynamic, object> name, Action<IMapColumn<T>> customize)
		{
			return this.WithColumn(name, customize);
		}
		IMapMember IMapMember.WithColumn(Func<dynamic, object> name, Action<IMapColumn> customize)
		{
			return this.WithColumn(name, customize);
		}

		/// <summary>
		/// The collection of columns required by this member.
		/// </summary>
		internal List<MapColumn<T>> Columns
		{
			get { return _Columns; }
		}
	}
}
