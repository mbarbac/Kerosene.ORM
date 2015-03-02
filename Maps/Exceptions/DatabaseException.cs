// ======================================================== DatabaseException.cs
namespace Kerosene.ORM.Maps
{
	using System;

	// ==================================================== 
	/// <summary>
	/// Represents a generic error condition detected when executing a command against an
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
