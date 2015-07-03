using Kerosene.Tools;
using System;
using System.Collections.Generic;

namespace Kerosene.ORM.Core
{
	// ====================================================
	/// <summary>
	/// Represents the type of an underlying database engine.
	/// </summary>
	public interface IDataEngine : IDisposableEx, ICloneable
	{
		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		new IDataEngine Clone();

		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <param name="settings">A dictionary containing the names and values of the properties
		/// that has to be changed with respect to the original ones, or null if these changes
		/// are not needed.</param>
		/// <returns>A new instance.</returns>
		IDataEngine Clone(IDictionary<string, object> settings);

		/// <summary>
		/// The engine invariant name. The value of this property typically corresponds to the
		/// ADO.NET provider's invariant name, if such is used, but this correspondence is not
		/// mandatory.
		/// </summary>
		/// <remarks>There might be several instances registered sharing the same invariant name.
		/// In this case resolution can be forced using the min and max version arguments.</remarks>
		string InvariantName { get; }

		/// <summary>
		/// The version specification the database engine identifies itself to be, or null if
		/// this information is not available.
		/// </summary>
		string ServerVersion { get; }

		/// <summary>
		/// Whether the identifiers in the database, as table and column names, are case sensitive
		/// or not.
		/// </summary>
		bool CaseSensitiveNames { get; }

		/// <summary>
		/// The default prefix the database engine uses for the names of the parameters of its
		/// command.
		/// </summary>
		string ParameterPrefix { get; }

		/// <summary>
		/// Whether the database engine treats the parameters of a command by position instead
		/// of by name.
		/// </summary>
		bool PositionalParameters { get; }

		/// <summary>
		/// Whether the database engine provides a normalized syntax to implement the skip/take
		/// functionality, or rather it has to be emulated by software.
		/// </summary>
		bool SupportsNativeSkipTake { get; }

		/// <summary>
		/// The parser this engine is associated with.
		/// </summary>
		IParser Parser { get; }

		/// <summary>
		/// Factory method to create a new collection of parameters adapted to this instance.
		/// </summary>
		/// <returns>A new collection of parameters.</returns>
		IParameterCollection CreateParameterCollection();

		/// <summary>
		/// Factory method to create a new collection of element aliases adapted to this
		/// instance.
		/// </summary>
		/// <returns>A new collection of element aliases.</returns>
		IElementAliasCollection CreateElementAliasCollection();

		/// <summary>
		/// Creates a new raw command adapted to this engine.
		/// </summary>
		/// <param name="link">The link associated with the new command.</param>
		/// <returns>The new command.</returns>
		IRawCommand CreateRawCommand(IDataLink link);

		/// <summary>
		/// Creates a new query command adapted to this engine.
		/// </summary>
		/// <param name="link">The link associated with the new command.</param>
		/// <returns>The new command.</returns>
		IQueryCommand CreateQueryCommand(IDataLink link);

		/// <summary>
		/// Creates a new insert command adapted to this engine.
		/// </summary>
		/// <param name="link">The link associated with the new command.</param>
		/// <param name="table">The table affected by this command.</param>
		/// <returns>The new command.</returns>
		IInsertCommand CreateInsertCommand(IDataLink link, Func<dynamic, object> table);

		/// <summary>
		/// Creates a new delete command adapted to this engine.
		/// </summary>
		/// <param name="link">The link associated with the new command.</param>
		/// <param name="table">The table affected by this command.</param>
		/// <returns>The new command.</returns>
		IDeleteCommand CreateDeleteCommand(IDataLink link, Func<dynamic, object> table);

		/// <summary>
		/// Creates a new update command adapted to this engine.
		/// </summary>
		/// <param name="link">The link associated with the new command.</param>
		/// <param name="table">The table affected by this command.</param>
		/// <returns>The new command.</returns>
		IUpdateCommand CreateUpdateCommand(IDataLink link, Func<dynamic, object> table);

		/// <summary>
		/// Registers into this instance a new transformer for the given type, that will be
		/// invoked when objects of that type have to be converted into objects understood by
		/// the underlying database engine.
		/// </summary>
		/// <typeparam name="T">The type of the source values to be converted.</typeparam>
		/// <param name="func">The delegate to invoke to convert the values of its type into
		/// whatever objects the underlying database can understand.</param>
		void AddTransformer<T>(Func<T, object> func);

		/// <summary>
		/// Removes the transformer that may have been registered into this instance for the
		/// given type. Returns true if it has been removed, or false otherwise.
		/// </summary>
		/// <typeparam name="T">The type of the values of the transformer to remove.</typeparam>
		/// <returns>True if the transformer has been removed, or false otherwise</returns>
		bool RemoveTransformer<T>();

		/// <summary>
		/// Clears all the transformers that may have been registered into this instance.
		/// </summary>
		void ClearTransformers();

		/// <summary>
		/// Gets a collection with the types for which transformers have been registered into
		/// this instance.
		/// </summary>
		/// <returns>A collection with the registered types.</returns>
		IEnumerable<Type> GetTransformerTypes();

		/// <summary>
		/// Gets whether a transformer for the given type of values is already registered into
		/// this instance or not.
		/// </summary>
		/// <param name="type">The type of the values of the transformer.</param>
		/// <returns>True if a transformer is registered, false otherwise.</returns>
		bool IsTransformerRegistered(Type type);

		/// <summary>
		/// Gets whether a transformer for the given type of values is already registered into
		/// this instance or not.
		/// </summary>
		/// <typeparam name="T">The type of the values of the transformer.</typeparam>
		/// <returns>True if a transformer is registered, false otherwise.</returns>
		bool IsTransformerRegistered<T>();

		/// <summary>
		/// Whether, if a transformer for a given type is not found, a transformer for a base
		/// type can be used, or not.
		/// </summary>
		bool RelaxTransformers { get; set; }

		/// <summary>
		/// Returns an object that, if a transformer is registered for the type of the source
		/// value, is the result of that transformation, or otherwise is the original value
		/// itself.
		/// </summary>
		/// <param name="value">The value to tranform.</param>
		/// <returns>The value transformed, or the original one.</returns>
		object TryTransform(object value);
	}

	// ====================================================
	/// <summary>
	/// Helpers and extensions for working with <see cref="IDataEngine"/> instances.
	/// </summary>
	public static partial class DataEngine
	{
		/// <summary>
		/// Whether, by default, the identifiers in the database, as table and column names,
		/// are treated as case sensitive or not.
		/// </summary>
		public const bool DEFAULT_CASESENSITIVE_NAMES = false;

		/// <summary>
		/// The default prefix engines use for the names of the parameters of their commands.
		/// </summary>
		public const string DEFAULT_PARAMETER_PREFIX = "#";

		/// <summary>
		/// Whether, by default, engines treat the parameters of a command by position instead
		/// of by name.
		/// </summary>
		public const bool DEFAULT_POSITIONAL_PARAMETERS = false;

		/// <summary>
		/// Whether, by default, engines provide a normalized syntax to implement the skip/take
		/// functionality, or rather it has to be emulated by software.
		/// </summary>
		public const bool DEFAULT_SUPPORT_NATIVE_SKIPTAKE = false;

		/// <summary>
		/// Whether, by default, if a value transformer for a given type is not found a value
		/// transformer for a base type can be used, or not.
		/// </summary>
		public const bool DEFAULT_RELAX_TRANSFORMERS = true;

		/// <summary>
		/// Whether if a value transformer for a given type is not found a value transformer for
		/// a base type can be used, or not.
		/// </summary>
		public static bool RelaxTransformers
		{
			get
			{
				while (!_RelaxTransformersCaptured)
				{
					var info = Configuration.ORMConfiguration.GetInfo();
					if (info == null ||
						info.DataEngine == null ||
						info.DataEngine.RelaxTransformers == null) break;

					_RelaxTransformers = (bool)info.DataEngine.RelaxTransformers;
					break;
				}
				_RelaxTransformersCaptured = true;
				return _RelaxTransformers;
			}
			set
			{
				_RelaxTransformersCaptured = true;
				_RelaxTransformers = value;
			}
		}
		static bool _RelaxTransformers = DEFAULT_RELAX_TRANSFORMERS;
		static bool _RelaxTransformersCaptured = false;

		/// <summary>
		/// Returns a validated invariant name.
		/// </summary>
		/// <param name="invariantName">The invariant name to validate.</param>
		/// <returns>The invariant name.</returns>
		public static string ValidateInvariantName(string invariantName)
		{
			return invariantName.Validated("Invariant Name");
		}

		/// <summary>
		/// Returns a validated server version.
		/// </summary>
		/// <param name="version">The server version to validate.</param>
		/// <returns>The server version.</returns>
		public static string ValidateServerVersion(string version)
		{
			return version.NullIfTrimmedIsEmpty();
		}

		/// <summary>
		/// Returns a validated parameter prefix.
		/// </summary>
		/// <param name="prefix">The parameter prefix to validate.</param>
		/// <returns>The parameter prefix.</returns>
		public static string ValidateParameterPrefix(string prefix)
		{
			return prefix.Validated("Parameter Prefix");
		}
	}
}
