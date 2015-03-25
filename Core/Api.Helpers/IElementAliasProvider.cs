// ======================================================== IElementAliasProvider.cs
namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;

	// ==================================================== 
	/// <summary>
	/// Represents an object able to maintain a collection of aliases of elements relevant in
	/// its context.
	/// </summary>
	public interface IElementAliasProvider
	{
		/// <summary>
		/// The collection of aliases used in the context of this instance.
		/// </summary>
		IElementAliasCollection Aliases { get; }
	}
}
// ======================================================== 
