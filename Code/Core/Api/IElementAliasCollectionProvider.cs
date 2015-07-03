using Kerosene.Tools;
using System;

namespace Kerosene.ORM.Core
{
	// ==================================================== 
	/// <summary>
	/// Represents an object able to maintain a collection of aliases of elements relevant in
	/// its context.
	/// </summary>
	public interface IElementAliasCollectionProvider
	{
		/// <summary>
		/// The collection of aliases used in the context of this instance.
		/// </summary>
		IElementAliasCollection Aliases { get; }
	}
}
