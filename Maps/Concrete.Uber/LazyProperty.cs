using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Kerosene.ORM.Maps.Concrete
{
	// ====================================================
	/// <summary>
	/// Represents a lazy property used by a proxy type.
	/// </summary>
	internal class LazyProperty
	{
		/// <summary>
		/// Initializes a new empty instance.
		/// </summary>
		internal LazyProperty() { }

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the string representation of this instance.</returns>
		public override string ToString()
		{
			return Name ?? string.Empty;
		}

		/// <summary>
		/// The given name of this lazy property for identification purposes, or null if this
		/// instance is not initialized yet.
		/// </summary>
		internal string Name
		{
			get { return OriginalProperty == null ? null : OriginalProperty.Name; }
		}

		/// <summary>
		/// The original property this instance refers to.
		/// </summary>
		internal PropertyInfo OriginalProperty { get; set; }

		/// <summary>
		/// The getter for the original property.
		/// </summary>
		internal MethodInfo OriginalGetter { get; set; }

		/// <summary>
		/// The setter for the original property.
		/// </summary>
		internal MethodInfo OriginalSetter { get; set; }

		/// <summary>
		/// The field to hold if the proxied virtual lazy property has been loaded (completed) or
		/// not.
		/// </summary>
		internal FieldInfo LazyCompletedFlag { get; set; }

		/// <summary>
		/// The property on the extended type.
		/// </summary>
		internal PropertyInfo ExtendedProperty { get; set; }

		/// <summary>
		/// The getter of the property on the extended type.
		/// </summary>
		internal MethodInfo ExtendedGetter { get; set; }

		/// <summary>
		/// The setter of the property on the extended type.
		/// </summary>
		internal MethodInfo ExtendedSetter { get; set; }

		/// <summary>
		/// Access back to the source property on the extended type.
		/// </summary>
		internal PropertyInfo SourceBackProperty { get; set; }

		/// <summary>
		/// The getter to access back the source property on the extended type.
		/// </summary>
		internal MethodInfo SourceBackGetter { get; set; }

		/// <summary>
		/// The setter to access back the source property on the extended type.
		/// </summary>
		internal MethodInfo SourceBackSetter { get; set; }
	}
}
