// ======================================================== IDataEngine.cs
namespace Kerosene.ORM.Direct
{
	using Kerosene.Tools;
	using System;
	using System.Collections.Generic;
	using System.Data.Common;

	// ==================================================== 
	/// <summary>
	/// Represents an underlying database engine in a direct connection scenario. Maintains its
	/// main characteristics and acts as a factory to create concrete objects adapted to it.
	/// </summary>
	public interface IDataEngine : Core.IDataEngine
	{
		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		new IDataEngine Clone();

		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <param name="settings">A dictionary containing the names and values of the properties
		/// that has to be changed with respect to the original ones, or null if these changes
		/// are not needed.
		/// <returns>A new instance.</returns>
		new IDataEngine Clone(IDictionary<string, object> settings);

		/// <summary>
		/// Gets the provider factory associated with this engine.
		/// </summary>
		DbProviderFactory ProviderFactory { get; }
	}
}
// ======================================================== 
