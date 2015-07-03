using Kerosene.Tools;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Kerosene.ORM.Core.Concrete
{
	// ==================================================== 
	/// <summary>
	/// Represents a generic parameter of a command.
	/// </summary>
	[Serializable]
	public class Parameter : IParameter
	{
		bool _IsDisposed = false;
		IParameterCollection _Owner = null;
		string _Name = null;
		object _Value = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		public Parameter() { }

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

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		protected virtual void OnDispose(bool disposing)
		{
			if (disposing)
			{
				try
				{
					if (_Owner != null)
					{
						var temp = _Owner; _Owner = null;
						if (temp != null && !temp.IsDisposed) temp.Remove(this);
					}
				}
				catch { }
			}

			_Owner = null;
			_Value = disposing ? (_Value == null ? null : _Value.ToString()) : null;

			_IsDisposed = true;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			var str = string.Format("{0} = '{1}'",
				Name ?? string.Empty,
				Value.Sketch());

			return IsDisposed ? string.Format("disposed::{0}({1})", GetType().EasyName(), str) : str;
		}

		/// <summary>
		/// Call-back method required for custom serialization.
		/// </summary>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			info.AddValue("Name", _Name);
			info.AddExtended("Value", _Value);
		}

		/// <summary>
		/// Protected initializer required for custom serialization.
		/// </summary>
		protected Parameter(SerializationInfo info, StreamingContext context)
		{
			_Name = info.GetString("Name");
			_Value = info.GetExtended("Value");
		}

		/// <summary>
		/// Returns a new instance that otherwise is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		public Parameter Clone()
		{
			var cloned = new Parameter();
			OnClone(cloned); return cloned;
		}
		IParameter IParameter.Clone()
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
			var temp = cloned as Parameter;
			if (temp == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' one."
				.FormatWith(cloned.Sketch(), typeof(Parameter).EasyName()));

			temp.Name = Name;
			temp.Value = Value.TryClone();
		}

		/// <summary>
		/// Returns true if this object can be considered as equivalent to the target one given.
		/// </summary>
		/// <param name="target">The target object this one will be tested for equivalence.</param>
		/// <returns>True if this object can be considered as equivalent to the target one given.</returns>
		public bool EquivalentTo(IParameter target)
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
			var temp = target as IParameter; if (temp == null) return false;
			if (temp.IsDisposed) return false;
			if (IsDisposed) return false;

			bool thiscs = this.Owner == null ? Core.ParameterCollection.DEFAULT_CASE_SENSITIVE_NAMES : this.Owner.CaseSensitiveNames;
			bool othercs = temp.Owner == null ? Core.ParameterCollection.DEFAULT_CASE_SENSITIVE_NAMES : temp.Owner.CaseSensitiveNames;
			if (thiscs != othercs) return false;

			if (string.Compare(this.Name, temp.Name, !thiscs) != 0) return false;
			if (!Value.IsEquivalentTo(temp.Value)) return false;

			return true;
		}

		/// <summary>
		/// The collection this instance belongs to, if any.
		/// </summary>
		public IParameterCollection Owner
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
		/// The name of this command as it will be represented when used in a command against
		/// an underlying database.
		/// </summary>
		public string Name
		{
			get { return _Name; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				if (Owner != null) throw new NotOrphanException("This '{0}' is not orphan.".FormatWith(this));

				_Name = Core.Parameter.ValidateName(value);
			}
		}

		/// <summary>
		/// The value or reference held by this instance, that when the command is executed will
		/// be converted into an appropriate value understood by the underlying database.
		/// </summary>
		/// <remarks>This property can be changed even if this instance is not an orphan one to
		/// permit an easy reutilization.</remarks>
		public object Value
		{
			get { return _Value; }
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());
				_Value = value;
			}
		}
	}
}
