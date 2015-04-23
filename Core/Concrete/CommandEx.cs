namespace Kerosene.ORM.Core.Concrete
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents an abstract enumerable command to be executed against a database-alike
	/// service.
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
		/// Executes this command and returns a list with the results.
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
		/// - Note that the concrete implementation of this method may emulate this capability
		/// by retrieving all possible records and discarding them until the last one is found.
		/// Client applications may want to modify the logic of the command to avoid using it.
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

		/// <summary>
		/// Creates a new enumerator with an associated converter delegate.
		/// </summary>
		/// <param name="converter">The delegate to use to convert the current record into any
		/// object or reference that will become the 'Current' property while at the current
		/// iteration.</param>
		/// <returns>A new enumerator.</returns>
		public IEnumerableExecutor ConvertBy(Func<IRecord, object> converter)
		{
			var iter = GetEnumerator(); iter.Converter = converter;
			return iter;
		}

		/// <summary>
		/// Creates a new enumerator that returns the strong typed instances that results from
		/// the conversion of the records produced by the database into instances of the given
		/// type. The values of its public properties and fields are populated with the ones of
		/// the matching columns from the database.
		/// </summary>
		/// <typeparam name="T">The type of the receiving instances.</typeparam>
		/// <returns>A new enumerator.</returns>
		public IEnumerableExecutorTo<T> ConvertTo<T>() where T : class
		{
			var iter = new EnumerableExecutorTo<T>(this);
			return iter;
		}
	}

	// ==================================================== 
	/// <summary>
	/// Represents an abstract enumerable and scalar command to be executed against a
	/// database-alike service.
	/// </summary>
	public abstract class CommandEnumSca : CommandEnum, IScalarCommand
	{
		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="link">The link this command is associated with.</param>
		protected CommandEnumSca(IDataLink link) : base(link) { }

		/// <summary>
		/// Creates a new object able to execute this command.
		/// </summary>
		/// <returns>A new executor.</returns>
		public IScalarExecutor GetExecutor()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return Link.CreateScalarExecutor(this);
		}

		/// <summary>
		/// Executes this command and returns the integer produced by that execution.
		/// </summary>
		/// <returns>An integer.</returns>
		public int Execute()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			var x = GetExecutor();
			var r = x.Execute();

			x.Dispose();
			return r;
		}
	}
}
