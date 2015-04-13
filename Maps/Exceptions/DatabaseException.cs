// ======================================================== DatabaseException.cs
namespace Kerosene.ORM.Maps
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents a generic error that happend in the execution of a command against an
	/// underlying database.
	/// </summary>
	[Serializable]
	public class DatabaseException : Exception
	{
		public DatabaseException() { }
		public DatabaseException(string message) : base(message) { }
		public DatabaseException(string message, Exception inner) : base(message, inner) { }
	}
}
// ======================================================== 
