// ======================================================== MapMemberColumn.cs
namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Linq;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Extends the <see cref="IMapMemberColumn"/> interface.
	/// </summary>
	internal interface IUberMemberColumn : IMapMemberColumn
	{
		/// <summary>
		/// The member this instance is associated with.
		/// </summary>
		new IUberMember Member { get; }

		/// <summary>
		/// The map this instance is associated with.
		/// </summary>
		IUberMap Map { get; }

		/// <summary>
		/// The element info this instance refers to, if any.
		/// </summary>
		ElementInfo ElementInfo { get; }
	}

	// ==================================================== 
	/// <summary>
	/// Represents a column in the primary table that has been explicitly associated with
	/// a member of the map.
	/// </summary>
	public class MapMemberColumn<T> : IMapMemberColumn<T>, IUberMemberColumn where T : class
	{
		MapMember<T> _Member = null;
		string _Name = null;
		bool _WriteEnabled = true; Func<T, object> _WriteRecord = null;
		bool _LoadEnabled = true; Action<object, T> _LoadEntity = null;
		ElementInfo _ElementInfo = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		internal protected MapMemberColumn(MapMember<T> member, string name)
		{
			if (member == null) throw new ArgumentNullException("map", "Meta Map cannot be null.");
			if (member.IsDisposed) throw new ObjectDisposedException(member.ToString());
			_Member = member;

			Name = name;
		}

		/// <summary>
		/// Whether this instance has been disposed or not.
		/// </summary>
		internal bool IsDisposed
		{
			get { return (Member == null || Member.Map == null); }
		}

		/// <summary>
		/// Disposes this instance.
		/// </summary>
		internal virtual void Dispose()
		{
			_Member = null;
			_WriteRecord = null;
			_LoadEntity = null;
			if (_ElementInfo != null) _ElementInfo.Dispose(); _ElementInfo = null;
		}

		~MapMemberColumn()
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

			if (ElementInfo != null &&
				string.Compare(ElementInfo.Name, Name, true) != 0)
				sb.AppendFormat(":On({0})", ElementInfo.FullName);

			var str = sb.ToString();
			return IsDisposed ? "disposed::{0}({1})".FormatWith(GetType().EasyName(), str) : str;
		}

		/// <summary>
		/// Clones this instance.
		/// </summary>
		internal protected virtual MapMemberColumn<T> Clone(MapMember<T> member)
		{
			var temp = new MapMemberColumn<T>(member, Name);
			temp._WriteEnabled = _WriteEnabled; temp._WriteRecord = _WriteRecord;
			temp._LoadEnabled = _LoadEnabled; temp._LoadEntity = _LoadEntity;
			return temp;
		}

		/// <summary>
		/// The member this instance is associated with.
		/// </summary>
		public MapMember<T> Member
		{
			get { return _Member; }
		}
		IMapMember<T> IMapMemberColumn<T>.Member
		{
			get { return this.Member; }
		}
		IMapMember IMapMemberColumn.Member
		{
			get { return this.Member; }
		}
		IUberMember IUberMemberColumn.Member
		{
			get { return this.Member; }
		}

		/// <summary>
		/// The map held by the associated member, if any.
		/// </summary>
		internal IUberMap Map
		{
			get { return Member == null ? null : Member.Map; }
		}
		IUberMap IUberMemberColumn.Map
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
		ElementInfo IUberMemberColumn.ElementInfo
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
				if (Member.IsDisposed) throw new ObjectDisposedException(Member.ToString());
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
		public MapMemberColumn<T> OnWriteRecord(bool enabled)
		{
			WriteEnabled = enabled; return this;
		}
		IMapMemberColumn<T> IMapMemberColumn<T>.OnWriteRecord(bool enabled)
		{
			return this.OnWriteRecord(enabled);
		}
		IMapMemberColumn IMapMemberColumn.OnWriteRecord(bool enabled)
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
				if (Member.IsDisposed) throw new ObjectDisposedException(Member.ToString());
				if (Map.IsDisposed) throw new ObjectDisposedException(Map.ToString());
				if (Map.IsValidated) throw new InvalidOperationException("Map '{0}' is validated.".FormatWith(Map));

				_WriteRecord = value;
			}
		}
		Func<object, object> IMapMemberColumn.WriteRecord
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
		public MapMemberColumn<T> OnWriteRecord(Func<T, object> onWrite)
		{
			WriteRecord = onWrite; return this;
		}
		IMapMemberColumn<T> IMapMemberColumn<T>.OnWriteRecord(Func<T, object> onWrite)
		{
			return this.OnWriteRecord(onWrite);
		}
		IMapMemberColumn IMapMemberColumn.OnWriteRecord(Func<object, object> onWrite)
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
				if (Member.IsDisposed) throw new ObjectDisposedException(Member.ToString());
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
		public MapMemberColumn<T> OnLoadEntity(bool enabled)
		{
			LoadEnabled = enabled; return this;
		}
		IMapMemberColumn<T> IMapMemberColumn<T>.OnLoadEntity(bool enabled)
		{
			return this.OnLoadEntity(enabled);
		}
		IMapMemberColumn IMapMemberColumn.OnLoadEntity(bool enabled)
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
				if (Member.IsDisposed) throw new ObjectDisposedException(Member.ToString());
				if (Map.IsDisposed) throw new ObjectDisposedException(Map.ToString());
				if (Map.IsValidated) throw new InvalidOperationException("Map '{0}' is validated.".FormatWith(Map));

				_LoadEntity = value;
			}
		}
		Action<object, object> IMapMemberColumn.LoadEntity
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
		public MapMemberColumn<T> OnLoadEntity(Action<object, T> onLoad)
		{
			LoadEntity = onLoad; return this;
		}
		IMapMemberColumn<T> IMapMemberColumn<T>.OnLoadEntity(Action<object, T> onLoad)
		{
			return this.OnLoadEntity(onLoad);
		}
		IMapMemberColumn IMapMemberColumn.OnLoadEntity(Action<object, object> onLoad)
		{
			return this.OnLoadEntity(onLoad);
		}

		/// <summary>
		/// Identifies the member into which this database column will be mapped. Members can
		/// be both properties and fields and either public, protected or private ones.
		/// <para>
		/// Note that this member can be a different one than the one this instance depends on,
		/// in case such scenario is needed.
		/// </para>
		/// </summary>
		/// <param name="element">A dynamic lambda expression that resolves into the name of the
		/// member of the type into which this database column will be mapped.</param>
		/// <returns>This instance to permit a fluent chaining syntax.</returns>
		public MapMemberColumn<T> OnElement(Func<dynamic, object> member)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (Member.IsDisposed) throw new ObjectDisposedException(Member.ToString());
			if (Map.IsDisposed) throw new ObjectDisposedException(Map.ToString());
			if (Map.IsValidated) throw new InvalidOperationException("Map '{0}' is validated.".FormatWith(Map));

			if (_ElementInfo != null) _ElementInfo.Dispose();
			_ElementInfo = null;

			if (member != null)
			{
				var name = DynamicInfo.ParseName(member);
				var flags = TypeEx.InstancePublicAndHidden;
				_ElementInfo = ElementInfo.Create<T>(name, raise: true, flags: flags);
			}

			return this;
		}
		IMapMemberColumn<T> IMapMemberColumn<T>.OnElement(Func<dynamic, object> member)
		{
			return this.OnElement(member);
		}
		IMapMemberColumn IMapMemberColumn.OnElement(Func<dynamic, object> member)
		{
			return this.OnElement(member);
		}
	}
}
// ======================================================== 
