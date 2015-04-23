namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;

	// ==================================================== 
	/// <summary>
	/// Represents an object able to maintain the name of the primary table it refers to.
	/// </summary>
	public interface ITableNameProvider
	{
		/// <summary>
		/// The name of the primary table this instance refers to.
		/// </summary>
		string TableName { get; }
	}
}
