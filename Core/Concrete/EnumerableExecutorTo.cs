// ======================================================== EnumerableExecutorTo.cs
namespace Kerosene.ORM.Core.Concrete
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using System.Runtime.Serialization;

	// ==================================================== 
	/// <summary>
	/// Represents an object able to execute an enumerable command and to produce a collection
	/// of strong-typed instances that result from this execution. Public properties and fields
	/// of the receiving type are populated with the values of the matching columns from the
	/// records obtained from the database.
	/// </summary>
	public class EnumerableExecutorTo<T> : IEnumerableExecutorTo<T> where T : class
	{
		bool _IsDisposed = false;
		IEnumerableExecutor _Iterator = null;
		List<ElementInfo> _Elements = null;
		ConstructorInfo _Constructor = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="cmd">The command this instance will be associated with.</param>
		public EnumerableExecutorTo(IEnumerableCommand cmd)
		{
			if (cmd == null) throw new ArgumentNullException("command", "Command cannot be null.");
			if (cmd.IsDisposed) throw new ObjectDisposedException(cmd.ToString());
			if (cmd.Link.IsDisposed) throw new ObjectDisposedException("Link '{0}' of command '{1}' is disposed.".FormatWith(cmd.Link, cmd));

			if ((_Iterator = cmd.GetEnumerator()) == null)
				throw new CannotCreateException("Cannot create a base iterator for command '{0}'.".FormatWith(cmd));

			_Elements = new List<ElementInfo>();

			Type type = typeof(T);
			bool sensitive = cmd.Link.Engine.CaseSensitiveNames;

			var fields = type.GetFields(); foreach (var field in fields)
			{
				var temp = _Elements.Find(x => string.Compare(x.Name, field.Name, !sensitive) == 0);
				if (temp == null) _Elements.Add(new ElementInfo(field));
			}
			var props = type.GetProperties(); foreach (var prop in props)
			{
				var temp = _Elements.Find(x => string.Compare(x.Name, prop.Name, !sensitive) == 0);
				if (temp == null) _Elements.Add(new ElementInfo(prop));
			}

			var cons = type.GetConstructors(TypeEx.InstancePublicAndHidden);
			foreach (var con in cons)
			{
				var pars = con.GetParameters();
				if (pars.Length == 0) { _Constructor = con; break; }
			}

			// Sets the iterator's converter to this instance's one...
			_Iterator.Converter = this.Converter;
		}

		/// <summary>
		/// The converter to use with the base core enumerator.
		/// </summary>
		internal object Converter(IRecord record)
		{
			T obj = null;
			Type type = typeof(T);
			bool sensitive = _Iterator.Command.Link.Engine.CaseSensitiveNames;

			if (_Constructor != null) obj = (T)_Constructor.Invoke(null);
			else obj = (T)FormatterServices.GetUninitializedObject(type);

			for (int i = 0; i < record.Count; i++)
			{
				var entry = record.Schema[i];
				var element = _Elements.Find(x => string.Compare(x.Name, entry.ColumnName, !sensitive) == 0);

				if (element != null && element.CanWrite)
				{
					var value = record[i].ConvertTo(element.ElementType);
					element.SetValue(obj, value);
				}
			}

			return obj;
		}

		/// <summary>
		/// Whether this instance has been disposed or not.
		/// </summary>
		public bool IsDisposed
		{
			get { return _IsDisposed; }
		}

		/// <summary>
		/// Disposes this instance.
		/// </summary>
		public void Dispose()
		{
			if (!IsDisposed) { OnDispose(true); GC.SuppressFinalize(this); }
		}

		~EnumerableExecutorTo()
		{
			if (!IsDisposed) OnDispose(false);
		}

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		protected virtual void OnDispose(bool disposing)
		{
			if (_Iterator != null) _Iterator.Dispose(); _Iterator = null;
			if (_Elements != null) _Elements.Clear(); _Elements = null;
			_Constructor = null;

			_IsDisposed = true;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			var str = string.Format("{0}({1})",
				GetType().EasyName(),
				Command == null ? string.Empty : Command.ToString()); str = null;

			return IsDisposed ? string.Format("disposed::{0}", str) : str;
		}

		/// <summary>
		/// Returns a new enumerator for this instance.
		/// <para>Hack to permit this instance to be enumerated in order to simplify its usage
		/// and syntax.</para>
		/// </summary>
		/// <returns>A self-reference.</returns>
		public EnumerableExecutorTo<T> GetEnumerator()
		{
			return this;
		}
		IEnumerableExecutorTo<T> IEnumerableExecutorTo<T>.GetEnumerator()
		{
			return this;
		}

		/// <summary>
		/// The command this instance is associated with.
		/// </summary>
		public IEnumerableCommand Command
		{
			get { return _Iterator == null ? null : _Iterator.Command; }
		}

		/// <summary>
		/// Gets the schema of the records produced by the execution of the associated command.
		/// This property is null until the command has been executed, or when this instance
		/// has been disposed.
		/// </summary>
		public ISchema Schema
		{
			get { return _Iterator == null ? null : _Iterator.Schema; }
		}

		/// <summary>
		/// Gets the current record produced by the last iteration of the command. This property
		/// is null if this instance is disposed, if the command has not been executed yet, or
		/// if there are no more records available.
		/// </summary>
		public IRecord CurrentRecord
		{
			get { return _Iterator == null ? null : _Iterator.CurrentRecord; }
		}

		/// <summary>
		/// Gets the current object being enumerated, or null if this enumerator has not been
		/// executed yet, or if there are not more objects available.
		/// </summary>
		public T Current
		{
			get { return _Iterator == null ? null : (T)_Iterator.Current; }
		}
		object IEnumerator.Current
		{
			get { return this.Current; }
		}

		/// <summary>
		/// Executes the command if it has not been executed yet in this instance. Returns true
		/// if a new record is available, or false otherwise.
		/// </summary>
		public bool MoveNext()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return _Iterator.MoveNext();
		}

		/// <summary>
		/// Resets this enumerator preparing it for a fresh new execution.
		/// </summary>
		public void Reset()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			_Iterator.Reset();
		}

		/// <summary>
		/// Executes the associated command and returns a list with the results.
		/// </summary>
		/// <returns>A list with the results of the execution.</returns>
		public List<T> ToList()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return _Iterator.ToList().Cast<T>().ToList();
		}

		/// <summary>
		/// Executes the associated command and returns an array with the results.
		/// </summary>
		/// <returns>An array with the results of the execution.</returns>
		public T[] ToArray()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return this.ToList().ToArray();
		}

		/// <summary>
		/// Executes the associated command and returns the first result produced from the
		/// database, or null if it produced no results.
		/// </summary>
		/// <returns>The first result produced, or null.</returns>
		public T First()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return (T)_Iterator.First();
		}

		/// <summary>
		/// Executes the associated command and returns the last result produced from the
		/// database, or null if it produced no results.
		/// <para>
		/// - Note that the concrete implementation of this method may emulate this capability
		/// by retrieving all possible records and discarding them until the last one is found.
		/// Client applications may want to modify the logic of the command to avoid using it.
		/// </para>
		/// </summary>
		/// <returns>The first result produced, or null.</returns>
		public T Last()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return (T)_Iterator.Last();
		}
	}
}
// ======================================================== 
