// ======================================================== DataRepository.cs
namespace Kerosene.ORM.Maps
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Helpers and extensions for working with <see cref="IDataRepository"/> instances.
	/// </summary>
	public static partial class DataRepository
	{
		/// <summary>
		/// Creates a new repository instance associated with the given link.
		/// </summary>
		/// <param name="link">The link reference.</param>
		/// <returns>The newly created repository.</returns>
		public static IDataRepository Create(IDataLink link)
		{
			return new Concrete.DataRepository(link);
		}
	}
}
// ======================================================== 
