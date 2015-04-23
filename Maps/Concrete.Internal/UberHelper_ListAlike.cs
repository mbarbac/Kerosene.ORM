namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	// ==================================================== 
	internal static partial class UberHelper
	{
		/// <summary>
		/// Whether the type is a list-alike one...
		/// </summary>
		internal static bool IsListAlike(this Type type)
		{
			if (typeof(ICollection<>).IsAssignableFrom(type)) return true;
			if (typeof(ICollection).IsAssignableFrom(type)) return true;

			return false;
		}

		/// <summary>
		/// Returns the type of the members of the collection or list, or null if the given type
		/// is not a ICollection.
		/// </summary>
		internal static Type ListAlikeMemberType(this Type listAlike)
		{
			if (typeof(ICollection<>).IsAssignableFrom(listAlike)) // ICollection<T>, List<T>...
			{
				return listAlike.GetGenericArguments()[0];
			}
			if (typeof(ICollection).IsAssignableFrom(listAlike)) // ICollection, IList...
			{
				return typeof(object);
			}
			return null; // Others...
		}
	}
}
