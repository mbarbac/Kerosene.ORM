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
	/// <summary>
	/// Represents a holder for a proxy type.
	/// </summary>
	internal class ProxyHolder
	{
		/// <summary>
		/// Initializes a new empty instance.
		/// </summary>
		internal ProxyHolder()
		{
			ProxyType = null;
			LazyProperties = new LazyPropertyCollection();
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the string representation of this instance.</returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(ProxyType == null
				? string.Empty
				: ProxyType.BaseType.EasyName());

			if (LazyProperties != null && LazyProperties.Count != 0)
			{
				sb.Append(" ["); bool first = true; foreach (var item in LazyProperties)
				{
					if (first) first = false; else sb.Append(", ");
					sb.Append(item);
				}
				sb.Append("]");
			}

			return sb.ToString();
		}

		/// <summary>
		/// The extended type created to manage the virtual lazy properties of the original
		/// base type.
		/// </summary>
		internal Type ProxyType { get; set; }

		/// <summary>
		/// The collection of lazy properties for which this proxy holder has been created.
		/// </summary>
		internal LazyPropertyCollection LazyProperties { get; private set; }
	}
}
