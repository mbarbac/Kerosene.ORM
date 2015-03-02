// ======================================================== RepositoryFactory.cs
namespace Kerosene.ORM.Maps
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;

	// ==================================================== 
	/// <summary>
	/// Factory class to create <see cref="IDataRepository"/> instances.
	/// </summary>
	public static class RepositoryFactory
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
