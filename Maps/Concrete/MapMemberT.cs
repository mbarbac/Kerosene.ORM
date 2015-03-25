// ======================================================== MapMemberT.cs
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
	/// Extends the <see cref="IMapMember"/> interface.
	/// </summary>
	internal interface IUberMember : IMapMember
	{
		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		new IUberMap Map { get; }

		/// <summary>
		/// The element info this instance refers to.
		/// </summary>
		ElementInfo ElementInfo { get; }
		
		/// <summary>
		/// If not null the lazy property associated with this member.
		/// </summary>
		LazyProperty LazyProperty { get; }

		/// <summary>
		/// The collection of columns that have been explicitly defined to support the mapping
		/// of this member.
		/// </summary>
		new IUberMemberColumnCollection Columns { get; }
	}

	// ==================================================== 
	/// <summary>
	/// Represents a member of the type that has been explicitly included in the map. They are
	/// typically used to identify dependency members, eager or lazy ones, or to identify
	/// alternate ways to obtain their contents.
	/// </summary>
	public class MapMember<T> : IMapMember<T>, IUberMember where T : class
	{
		DataMap<T> _Map = null;
		string _Name = null;
		ElementInfo _ElementInfo = null;
		MapMemberColumnCollection<T> _Columns = null;
		MemberDependencyMode _DependencyMode = MemberDependencyMode.None;
		Action<IRecord, T> _CompleteMember = null;
		LazyProperty _LazyProperty = null; bool _LazyPropertyCaptured = false;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		internal protected MapMember(DataMap<T> map, string name)
		{
			if (map == null) throw new ArgumentNullException("map", "Meta Map cannot be null.");
			if (map.IsDisposed) throw new ObjectDisposedException(map.ToString());
			_Map = map;

			name = name.Validated("Member Name");

			_ElementInfo = ElementInfo.Create<T>(name, raise: false, flags: TypeEx.InstancePublicAndHidden);
			if (_ElementInfo == null) throw new NotFoundException(
				"Member '{0}' not found in type '{1}'".FormatWith(name, typeof(T).EasyName()));

			_Name = _ElementInfo.FullName;

			_Columns = new MapMemberColumnCollection<T>(this);
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
			_CompleteMember = null;
			if (_Columns != null) _Columns.Dispose(); _Columns = null;
			if (_ElementInfo != null) _ElementInfo.Dispose(); _ElementInfo = null;
			_LazyProperty = null;
		}

		~MapMember()
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
			if (DependencyMode != MemberDependencyMode.None) sb.AppendFormat("(:{0})", DependencyMode);
			if (LazyProperty != null) sb.Append("(:Lazy)");
			if (Columns != null && Columns.Count != 0) sb.AppendFormat(" {0}", Columns);

			var str = sb.ToString();
			return IsDisposed ? "disposed::{0}({1})".FormatWith(GetType().EasyName(), str) : str;
		}

		/// <summary>
		/// Clones this instance.
		/// </summary>
		internal protected virtual MapMember<T> Clone(DataMap<T> map)
		{
			var temp = new MapMember<T>(map, Name);
			temp._DependencyMode = _DependencyMode;
			temp._Columns.Dispose(); temp._Columns = _Columns.Clone(temp);
			temp._CompleteMember = _CompleteMember;
			return temp;
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
		/// The name of the member of the type this instance refers to.
		/// </summary>
		public string Name
		{
			get { return _Name; }
		}

		/// <summary>
		/// The element info this instance refers to.
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
		/// If not null the lazy property associated with this member.
		/// </summary>
		internal LazyProperty LazyProperty
		{
			get
			{
				if (!_LazyPropertyCaptured && _Map != null && _Map.ProxyHolder != null)
				{
					_LazyProperty = _Map.ProxyHolder.LazyProperties.FindByName(Name);
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
		/// How this member depends on its host instance regarding to a change operation
		/// (insert, delete, update) initiated on that host.
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
		/// Sets how this member depends on its host instance regarding to a change operation
		/// (insert, delete, update) initiated on that host.
		/// </summary>
		/// <param name="mode">The dependency mode value to set for this instance.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		public MapMember<T> SetDependencyMode(MemberDependencyMode mode)
		{
			DependencyMode = mode; return this;
		}
		IMapMember<T> IMapMember<T>.SetDependencyMode(MemberDependencyMode mode)
		{
			return this.SetDependencyMode(mode);
		}
		IMapMember IMapMember.SetDependencyMode(MemberDependencyMode mode)
		{
			return this.SetDependencyMode(mode);
		}

		/// <summary>
		/// If not null the delegate to invoke with (record, entity) arguments to complete the
		/// value of this member. This delegate can invoke any actions it may need, and then
		/// setting the value of the associated member is its sole responsibility.
		/// </summary>
		public Action<IRecord, T> CompleteMember
		{
			get { return _CompleteMember; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (Map.IsDisposed) throw new ObjectDisposedException(Map.ToString());

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
		/// Sets the delegate to invoke, if it is not null, with (record, entity) arguments to
		/// complete the value of this member. This delegate can invoke any actions it may need,
		/// and then setting the value of the associated member is its sole responsibility.
		/// </summary>
		/// <param name="onComplete"></param>
		/// <returns></returns>
		public MapMember<T> OnComplete(Action<IRecord, T> onComplete)
		{
			CompleteMember = onComplete; return this;
		}
		IMapMember<T> IMapMember<T>.OnComplete(Action<IRecord, T> onComplete)
		{
			return this.OnComplete(onComplete);
		}
		IMapMember IMapMember.OnComplete(Action<IRecord, object> onComplete)
		{
			return this.OnComplete(onComplete);
		}

		/// <summary>
		/// The collection of columns that have been explicitly defined to support the mapping
		/// of this member, if any.
		/// </summary>
		public MapMemberColumnCollection<T> Columns
		{
			get { return _Columns; }
		}
		IMapMemberColumnCollection<T> IMapMember<T>.Columns
		{
			get { return this.Columns; }
		}
		IMapMemberColumnCollection IMapMember.Columns
		{
			get { return this.Columns; }
		}
		IUberMemberColumnCollection IUberMember.Columns
		{
			get { return this.Columns; }
		}

		/// <summary>
		/// Adds explicitly a new column to support the mapping of this member.
		/// </summary>
		/// <param name="name">A dynamic lambda expression that resolves into the name of the
		/// database column.</param>
		/// <param name="onCreate">If not null the delegate to invoke with the newly created
		/// column to further refine its rules and operations.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		public MapMember<T> WithColumn(Func<dynamic, object> name, Action<MapMemberColumn<T>> onCreate = null)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (Map.IsDisposed) throw new ObjectDisposedException(Map.ToString());
			if (Map.IsValidated) throw new InvalidOperationException("Map '{0}' is validated.".FormatWith(Map));

			if (name == null) throw new ArgumentNullException("name", "Name specification cannot be null.");
			var temp = DynamicInfo.ParseName(name);

			var entry = _Columns.Add(x => temp);
			if (onCreate != null) onCreate(entry);

			return this;
		}
		IMapMember<T> IMapMember<T>.WithColumn(Func<dynamic, object> name, Action<IMapMemberColumn<T>> onCreate)
		{
			return this.WithColumn(name, onCreate);
		}
		IMapMember IMapMember.WithColumn(Func<dynamic, object> name, Action<IMapMemberColumn> onCreate)
		{
			return this.WithColumn(name, onCreate);
		}

		/// <summary>
		/// Removes from this instance the column whose name is given.
		/// </summary>
		/// <param name="name">A dynamic lambda expression that resolves into the name of the
		/// database column.</param>
		/// <returns>True if the column has been removed, false otherwise.</returns>
		public bool RemoveColum(Func<dynamic, object> name)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (Map.IsDisposed) throw new ObjectDisposedException(Map.ToString());
			if (Map.IsValidated) throw new InvalidOperationException("Map '{0}' is validated.".FormatWith(Map));

			if (name == null) return false;
			var temp = DynamicInfo.ParseName(name);

			var sensitive = Map.Repository.Link.Engine.CaseSensitiveNames;
			var entry = _Columns.FirstOrDefault<MapMemberColumn<T>>(x => string.Compare(x.Name, temp, !sensitive) == 0);

			return entry == null ? false : _Columns.Remove(entry);
		}
	}
}
// ======================================================== 
