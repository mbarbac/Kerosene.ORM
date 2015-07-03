using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Kerosene.ORM.Maps.Concrete
{
	// ====================================================
	internal interface IUberCommand : IMetaCommand
	{
		/// <summary>
		/// The map this command is associated with.
		/// </summary>
		IUberMap UberMap { get; }
	}

	// ====================================================
	/// <summary>
	/// Represents a command related to the entities managed by the map it is associated with.
	/// </summary>
	public abstract class MetaCommand<T> : IMetaCommand<T>, IUberCommand where T : class
	{
		bool _IsDisposed = false;
		DataMap<T> _Map = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="map">The map this command will be associated with.</param>
		protected MetaCommand(DataMap<T> map)
		{
			if (map == null) throw new ArgumentNullException("map", "Map cannot be null.");
			if (map.IsDisposed) throw new ObjectDisposedException(map.ToString());

			map.Validate();
			_Map = map;
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

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		protected virtual void OnDispose(bool disposing)
		{
			_Map = null;
			_IsDisposed = true;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			return this.TraceString();
		}

		/// <summary>
		/// Returns a new core command that when executed materializes the operation this instance
		/// refers to, or null if that command cannot be generated for any reasons.
		/// </summary>
		/// <returns>A new core command, or null.</returns>
		ICommand ICoreCommandProvider.GenerateCoreCommand()
		{
			throw new NotSupportedException(
				"Abstract ICoreCommandProvider::{0}() invoked."
				.FormatWith(GetType().EasyName()));
		}

		/// <summary>
		/// The map this command is associated with.
		/// </summary>
		public DataMap<T> Map
		{
			get { return _Map; }
		}
		IDataMap<T> IMetaCommand<T>.Map
		{
			get { return this.Map; }
		}
		IDataMap IMetaCommand.Map
		{
			get { return this.Map; }
		}

		/// <summary>
		/// The map this command is associated with.
		/// </summary>
		internal IUberMap UberMap
		{
			get { return this.Map; }
		}
		IUberMap IUberCommand.UberMap
		{
			get { return this.UberMap; }
		}

		/// <summary>
		/// The repository reference held by the associated map, if any.
		/// </summary>
		public DataRepository Repository
		{
			get { return Map == null ? null : Map.Repository; }
		}

		/// <summary>
		/// The link reference held by the associated respository, if any.
		/// </summary>
		public IDataLink Link
		{
			get { return Repository == null ? null : Repository.Link; }
		}

		/// <summary>
		/// Whether the state and contents maintained in this instance permits the execution
		/// of this command or not.
		/// </summary>
		public bool CanBeExecuted
		{
			get
			{
				if (IsDisposed) return false;

				var cmd = ((ICoreCommandProvider)this).GenerateCoreCommand();
				var r = cmd == null ? false : cmd.CanBeExecuted;

				if (cmd != null) cmd.Dispose();
				return r;
			}
		}

		/// <summary>
		/// Generates a trace string for this command built by generating the actual text of the
		/// command in a syntax the underlying database can understand, and appending to it the
		/// name and value of parameters the command will use, if any.
		/// </summary>
		/// <returns>The requested trace string.</returns>
		public string TraceString()
		{
			var cmd = ((ICoreCommandProvider)this).GenerateCoreCommand();
			var str = cmd == null ? string.Empty : cmd.TraceString();

			var temp = IsDisposed
				? string.Format("disposed::{0}({1})", GetType().EasyName(), str)
				: (cmd == null
					? string.Format("empty::{0}({1})", GetType().EasyName(), OnTraceCommandEmpty())
					: str);

			return temp;
		}

		/// <summary>
		/// Invoked to obtain additional info when tracing an empty command.
		/// </summary>
		protected virtual string OnTraceCommandEmpty() { return string.Empty; }
	}
}
