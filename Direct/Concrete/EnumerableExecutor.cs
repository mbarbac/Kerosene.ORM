namespace Kerosene.ORM.Direct.Concrete
{
	using Kerosene.Tools;
	using System;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents an object able to execute an enumerable command, in a direct connection
	/// scenario, and to produce the collection of records resulting from this execution.
	/// </summary>
	public class EnumerableExecutor : Core.Concrete.EnumerableExecutor, IEnumerableExecutor
	{
		SurrogateDirect _Surrogate = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="cmd">The command this instance will be associated with.</param>
		public EnumerableExecutor(Core.IEnumerableCommand cmd)
			: base(cmd)
		{
			var link = base.Link as IDataLink;
			if (link == null) throw new InvalidOperationException(
				"Link '{0}' of command '{1}' is not a direct link.".FormatWith(base.Link, cmd));
		}

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		protected override void OnDispose(bool disposing)
		{
			if (disposing)
			{
				if (_Surrogate != null && !_Surrogate.IsDisposed) _Surrogate.Dispose();
			}

			base.OnDispose(disposing);
		}

		/// <summary>
		/// The link of the command this enumerator is associated with.
		/// </summary>
		public new IDataLink Link
		{
			get { return (IDataLink)base.Link; }
		}

		/// <summary>
		/// Returns a new enumerator for this instance.
		/// <para>Hack to permit this instance to be enumerated in order to simplify its usage
		/// and syntax.</para>
		/// </summary>
		/// <returns>A self-reference.</returns>
		public new IEnumerableExecutor GetEnumerator()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return this;
		}
		Core.IEnumerableExecutor Core.IEnumerableExecutor.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		/// Invoked to execute the command returning the schema that describes the record to be
		/// produced by that execution.
		/// </summary>
		/// <returns>The schema of the records to be produced.</returns>
		protected override Core.ISchema OnReaderStart()
		{
			_Surrogate = new SurrogateDirect(Command);
			return _Surrogate.OnReaderStart();
		}

		/// <summary>
		/// Invoked to retrieve the next available record, or null if there are no more records
		/// available.
		/// </summary>
		/// <returns>The next record produced, or null.</returns>
		protected override Core.IRecord OnReaderNext()
		{
			return _Surrogate.OnReaderNext();
		}

		/// <summary>
		/// Invoked to reset this enumerator so that is can execute its associated command again.
		/// </summary>
		protected override void OnReset()
		{
			if (_Surrogate != null) _Surrogate.OnReaderReset();
			_Surrogate = null;
		}
	}
}
