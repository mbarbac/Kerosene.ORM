// ======================================================== IParameter.cs
namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;
	using System.Runtime.Serialization;

	// ==================================================== 
	/// <summary>
	/// Represents a parameter of a command in an agnostic database-independent way.
	/// </summary>
	public interface IParameter : IDisposableEx, ICloneable, ISerializable, IEquivalent<IParameter>
	{
		/// <summary>
		/// Returns a new instance that otherwise is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		new IParameter Clone();

		/// <summary>
		/// The collection this instance belongs to, if any.
		/// </summary>
		IParameterCollection Owner { get; set; }

		/// <summary>
		/// The name of this command as it will be represented when used in a command against
		/// an underlying database.
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// The value or reference held by this instance, that when the command is executed will
		/// be converted into an appropriate value understood by the underlying database.
		/// </summary>
		/// <remarks>This property can be changed even if this instance is not an orphan one to
		/// permit an easy reutilization.</remarks>
		object Value { get; set; }
	}

	// ==================================================== 
	/// <summary>
	/// Helpers and extensions for working with <see cref="IParameter"/> instances.
	/// </summary>
	public static class Parameter
	{
		/// <summary>
		/// Returns a validated parameter name.
		/// </summary>
		/// <param name="name">The parameter name to validate.</param>
		/// <returns>The validated parameter name.</returns>
		public static string ValidateName(string name)
		{
			return name.Validated("Name");
		}
	}
}
// ======================================================== 
