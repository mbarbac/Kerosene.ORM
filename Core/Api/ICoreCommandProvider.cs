// ======================================================== ICoreCommandProvider.cs
namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents the ability of providing a core command that when executed will implement
	/// the operation this instance refers to.
	/// </summary>
	public interface ICoreCommandProvider
	{
		/// <summary>
		/// Returns a new core command that when executed implements the operation this instance
		/// refers to, or null if that command cannot be generated for any reasons.
		/// </summary>
		/// <returns>A new core command, or null.</returns>
		ICommand GenerateCoreCommand();
	}
}// ======================================================== 
