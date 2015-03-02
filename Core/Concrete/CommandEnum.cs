// ======================================================== CommandEnum.cs
namespace Kerosene.ORM.Core.Concrete
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Represents an abstract enumerable command that can be executed against an underlying
	/// database-alike service.
	/// </summary>
	public abstract class CommandEnum : Command, IEnumerableCommand
	{
		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="link">The link this command is associated with.</param>
		protected CommandEnum(IDataLink link) : base(link) { }

		/// <summary>
		/// Creates a new object able to execute this command.
		/// </summary>
		/// <returns>A new enumerator.</returns>
		public IEnumerableExecutor GetEnumerator()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return Link.CreateEnumerableExecutor(this);
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		/// Creates a new enumerator for this command and sets its converter in the same
		/// operation.
		/// </summary>
		/// <param name="converter">The converter to set, or null to clear it.</param>
		/// <returns>The new enumerator.</returns>
		public IEnumerableExecutor ConvertBy(Func<IRecord, object> converter)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			var iter = GetEnumerator();
			return iter.ConvertBy(converter);
		}

		/// <summary>
		/// Executes this and returns a list with the results.
		/// </summary>
		/// <returns>A list with the results of the execution.</returns>
		public List<object> ToList()
		{
			if (IsDisposed) throw new ObjectDisposedException(ToString());

			var iter = GetEnumerator();
			var temp = iter.ToList();

			iter.Dispose();
			return temp;
		}

		/// <summary>
		/// Executes this command and returns an array with the results.
		/// </summary>
		/// <returns>An array with the results of the execution.</returns>
		public object[] ToArray()
		{
			return ToList().ToArray();
		}

		/// <summary>
		/// Executes this command and returns the first result produced from the database, or
		/// null if it produced no results.
		/// </summary>
		/// <returns>The first result produced, or null.</returns>
		public object First()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			var iter = GetEnumerator();
			var temp = iter.First();

			iter.Dispose();
			return temp;
		}

		/// <summary>
		/// Executes this command and returns the last result produced from the database, or
		/// null if it produced no results.
		/// <para>
		/// This method is provided as a fall-back mechanism as it retrieves all possible results
		/// discarding them until the last one is found. Client applications may want to modify
		/// the logic of the command to avoid using it.
		/// </para>
		/// </summary>
		/// <returns>The first result produced, or null.</returns>
		public object Last()
		{
			if (IsDisposed) throw new ObjectDisposedException(ToString());

			var iter = GetEnumerator();
			var temp = iter.Last();

			iter.Dispose();
			return temp;
		}
	}
}
// ======================================================== 
