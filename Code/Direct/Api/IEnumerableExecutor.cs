using Kerosene.Tools;
using System;
using System.Linq;

namespace Kerosene.ORM.Direct
{
	// ==================================================== 
	/// <summary>
	/// Represents an object able to execute an enumerable command, in a direct connection
	/// scenario, and to produce the collection of records resulting from this execution.
	/// </summary>
	public interface IEnumerableExecutor : Core.IEnumerableExecutor
	{
		/// <summary>
		/// Returns a new enumerator for this instance.
		/// <para>Hack to permit this instance to be enumerated in order to simplify its usage
		/// and syntax.</para>
		/// </summary>
		/// <returns>A self-reference.</returns>
		new IEnumerableExecutor GetEnumerator();
	}
}
