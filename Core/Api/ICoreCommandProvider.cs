using Kerosene.Tools;
using System;

namespace Kerosene.ORM.Core
{
	// ==================================================== 
	/// <summary>
	/// Represents the ability of providing a core command that can materialize the operation
	/// this instance refers to.
	/// </summary>
	public interface ICoreCommandProvider
	{
		/// <summary>
		/// Returns a new core command that when executed materializes the operation this instance
		/// refers to, or null if that command cannot be generated for any reasons.
		/// </summary>
		/// <returns>A new core command, or null.</returns>
		ICommand GenerateCoreCommand();
	}
}
