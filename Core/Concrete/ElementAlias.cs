namespace Kerosene.ORM.Core.Concrete
{
	using Kerosene.Tools;
	using System;
	using System.Linq;
	using System.Runtime.Serialization;

	// ==================================================== 
	/// <summary>
	/// Represents the alias associated with a given element in a given context.
	/// </summary>
	[Serializable]
	public class ElementAlias : IElementAlias
	{
		bool _IsDisposed = false;
		IElementAliasCollection _Owner = null;
		string _Element = null;
		string _Alias = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		public ElementAlias() { }

		/// <summary>
		/// Whether this instance has been disposed or not.
		/// </summary>
		public bool IsDisposed
		{
			get { return _IsDisposed; }
		}

		/// <summary>
		/// Disposes this instance.
		/// </summary>
		public void Dispose()
		{
			if (!IsDisposed) { OnDispose(true); GC.SuppressFinalize(this); }
		}

		~ElementAlias()
		{
			if (!IsDisposed) OnDispose(false);
		}

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		protected virtual void OnDispose(bool disposing)
		{
			if (disposing)
			{
				if (_Owner != null)
				{
					var temp = _Owner; _Owner = null;
					if (temp != null && !temp.IsDisposed) temp.Remove(this);
				}
			}
			_Owner = null;

			_IsDisposed = true;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			var str = string.Format("{0} => {1}",
				Element ?? (IsDisposed ? string.Empty : "."),
				Alias ?? string.Empty);

			return IsDisposed ? string.Format("disposed::{0}({1})", GetType().EasyName(), str) : str;
		}

		/// <summary>
		/// Call-back method required for custom serialization.
		/// </summary>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			info.AddValue("Element", _Element);
			info.AddValue("Alias", _Alias);
		}

		/// <summary>
		/// Protected initializer required for custom serialization.
		/// </summary>
		protected ElementAlias(SerializationInfo info, StreamingContext context)
		{
			_Element = info.GetString("Element");
			_Alias = info.GetString("Alias");
		}

		/// <summary>
		/// Returns a new instance that otherwise is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		public ElementAlias Clone()
		{
			var cloned = new ElementAlias();
			OnClone(cloned); return cloned;
		}
		IElementAlias IElementAlias.Clone()
		{
			return this.Clone();
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		/// <summary>
		/// Invoked when cloning this object to set its state at this point of the inheritance
		/// chain.
		/// </summary>
		/// <param name="cloned">The cloned object.</param>
		protected virtual void OnClone(object cloned)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			var temp = cloned as ElementAlias;
			if (cloned == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' one."
				.FormatWith(cloned.Sketch(), typeof(ElementAlias).EasyName()));

			temp.Element = Element;
			temp.Alias = Alias;
		}

		/// <summary>
		/// Returns true if this object can be considered as equivalent to the target one given.
		/// </summary>
		/// <param name="target">The target object this one will be tested for equivalence.</param>
		/// <returns>True if this object can be considered as equivalent to the target one given.</returns>
		public bool EquivalentTo(IElementAlias target)
		{
			return OnEquivalentTo(target);
		}

		/// <summary>
		/// Invoked to test equivalence at this point of the inheritance chain.
		/// </summary>
		/// <param name="target">The target this instance will be tested for equivalence against.</param>
		/// <returns>True if at this level on the inheritance chain this instance can be considered
		/// equivalent to the target instance given.</returns>
		protected virtual bool OnEquivalentTo(object target)
		{
			if (object.ReferenceEquals(this, target)) return true;
			var temp = target as IElementAlias; if (temp == null) return false;
			if (temp.IsDisposed) return false;
			if (IsDisposed) return false;

			bool thiscs = this.Owner == null ? Core.ElementAliasCollection.DEFAULT_CASE_SENSITIVE_NAMES : this.Owner.CaseSensitiveNames;
			bool othercs = temp.Owner == null ? Core.ElementAliasCollection.DEFAULT_CASE_SENSITIVE_NAMES : temp.Owner.CaseSensitiveNames;
			if (thiscs != othercs) return false;

			if (string.Compare(this.Element, temp.Element, !thiscs) != 0) return false;
			if (string.Compare(this.Alias, temp.Alias, !thiscs) != 0) return false;

			return true;
		}

		/// <summary>
		/// The collection this instance belongs to, if any.
		/// </summary>
		public IElementAliasCollection Owner
		{
			get { return _Owner; }
			set
			{
				if (value == null)
				{
					var temp = _Owner; _Owner = null;
					if (temp != null && !temp.IsDisposed) temp.Remove(this);
				}
				else
				{
					if (IsDisposed) throw new ObjectDisposedException(this.ToString());

					if (object.ReferenceEquals(value, _Owner)) return;
					if (_Owner != null) throw new NotOrphanException(
						"This '{0}' is not an orphan one.".FormatWith(this));

					// To intercept the re-entrant operation...
					if (!value.Contains(this)) value.Add(this);
					_Owner = value;
				}
			}
		}

		/// <summary>
		/// The element being aliased, or null if it is the default one in a given context. A
		/// given element can have as many aliases as needed.
		/// </summary>
		public string Element
		{
			get { return _Element; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (Owner != null) throw new NotOrphanException("This '{0}' is not orphan.".FormatWith(this));

				_Element = Core.ElementAlias.ValidateElement(value);
			}
		}

		/// <summary>
		/// The alias of the element this instance refers to. Aliases are unique in a given
		/// context.
		/// </summary>
		public string Alias
		{
			get { return _Alias; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (Owner != null) throw new NotOrphanException("This '{0}' is not orphan.".FormatWith(this));

				_Alias = Core.ElementAlias.ValidateAlias(value);
			}
		}
	}
}
