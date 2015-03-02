// ======================================================== IElementAlias.cs
namespace Kerosene.ORM.Core
{
	using Kerosene.Tools;
	using System;
	using System.Linq;
	using System.Runtime.Serialization;

	// ==================================================== 
	/// <summary>
	/// Represents one alias of an element in a given context.
	/// </summary>
	public interface IElementAlias
		: IDisposableEx, ICloneable, ISerializable, IEquivalent<IElementAlias>
	{
		/// <summary>
		/// Returns a new instance that otherwise is a copy of the original one.
		/// </summary>
		/// <returns>A new instance.</returns>
		new IElementAlias Clone();

		/// <summary>
		/// The collection this instance belongs to, if any.
		/// </summary>
		IElementAliasCollection Owner { get; set; }

		/// <summary>
		/// The element being aliased, or null if it is the default one in a given context. A
		/// given element can have as many aliases as needed.
		/// </summary>
		string Element { get; set; }

		/// <summary>
		/// The alias of the element this instance refers to. Aliases are unique in a given
		/// context.
		/// </summary>
		string Alias { get; set; }
	}

	// ==================================================== 
	/// <summary>
	/// Helpers and extensions for working with <see cref="IElementAlias"/> instances.
	/// </summary>
	public static class ElementAlias
	{
		/// <summary>
		/// Returns a validated element.
		/// </summary>
		/// <param name="element">The element to validate. Can be null if it refers to the
		/// default element in a given context.</param>
		/// <returns>The validated element name.</returns>
		public static string ValidateElement(string element)
		{
			return element.Validated("Element", canbeNull: true);
		}

		/// <summary>
		/// Returns a validated alias.
		/// </summary>
		/// <param name="alias">The alias to validate.</param>
		/// <returns>The validated alias.</returns>
		public static string ValidateAlias(string alias)
		{
			return alias.Validated("Alias");
		}
	}
}
// ======================================================== 
