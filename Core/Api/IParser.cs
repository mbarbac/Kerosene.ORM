using Kerosene.Tools;
using System;

namespace Kerosene.ORM.Core
{
	// ==================================================== 
	/// <summary>
	/// Represents the ability of parsing any arbitrary object, including null references and
	/// dynamic lambda expressions,	and translate it into a string representation the database
	/// engine can understand.
	/// </summary>
	public interface IParser : IDisposableEx
	{
		/// <summary>
		/// The data engine this parser is associated with.
		/// </summary>
		IDataEngine Engine { get; }

		/// <summary>
		/// Parses the given object, including any arbitrary command logic expressed as a
		/// dynamic lambda expression, and returns an string that can be understood by the
		/// underlying database engine.
		/// </summary>
		/// <param name="obj">The object to parse. It can be any object or reference, including
		/// null ones and dynamic lambda expressions.</param>
		/// <param name="pc">If not null the collection of parameters where to place the ones
		/// extracted from the object to parse. If null their string representation is used
		/// instead.</param>
		/// <param name="nulls">If true null references are accepted. Otherwise an exception is
		/// thrown.</param>
		/// <returns>A string contained the parsed input in a syntax that can be understood by
		/// the underlying database engine.</returns>
		string Parse(object obj, IParameterCollection pc = null, bool nulls = true);
	}

	// ==================================================== 
	/// <summary>
	/// Helpers and extensions for working with <see cref="IParser"/> instances.
	/// </summary>
	public static class Parser
	{
		/// <summary>
		/// Whether, by default, complex tags are kept in the result of a parsing, or rather they
		/// are treated as regular arguments and so extracted out from that result.
		/// </summary>
		public const bool DEFAULT_COMPLEX_TAGS = true;

		/// <summary>
		/// Whether if a value transformer for a given type is not found a value transformer for
		/// a base type can be used, or not.
		/// </summary>
		public static bool ComplexTags
		{
			get
			{
				while (!_ComplexTagsCaptured)
				{
					var info = Configuration.ORMConfiguration.GetInfo();
					if (info == null ||
						info.DataEngine == null ||
						info.DataEngine.ComplexTags == null) break;

					_ComplexTags = (bool)info.DataEngine.ComplexTags;
					break;
				}
				_ComplexTagsCaptured = true;
				return _ComplexTags;
			}
			set
			{
				_ComplexTagsCaptured = true;
				_ComplexTags = value;
			}
		}
		static bool _ComplexTags = DEFAULT_COMPLEX_TAGS;
		static bool _ComplexTagsCaptured = false;

		/// <summary>
		/// Splits the given source string, with the form 'main AS alias' in its main and alias
		/// parts, where the alias one is optional.
		/// </summary>
		/// <param name="source">The source string.</param>
		/// <returns>A tuple containing the main and alias parts, where the later can be null if
		/// no alias was found in the source string.</returns>
		public static Tuple<string, string> SplitInMainAndAlias(string source)
		{
			source = source.Validated("Main AS Alias");

			string main = null;
			string alias = null;

			int length = source.LastIndexOf(" AS ", StringComparison.OrdinalIgnoreCase);
			if (length < 0) main = source;
			else
			{
				main = source.Substring(0, length).Trim();
				alias = source.Substring(length + 4).Trim();
			}
			return new Tuple<string, string>(main, alias);
		}

		/// <summary>
		/// Removes the leading dynamic tag, or argument name, from the source string.
		/// </summary>
		/// <param name="source">The source string.</param>
		/// <param name="tag">If not null a string containing the tag to remove, instead of
		/// trying to remove all the characters till the first dot in the source.</param>
		/// <returns>A new string with the leading dynamic tag removed.</returns>
		public static string RemoveTag(string source, string tag = null)
		{
			source = source.Validated("Source");

			if (tag == null)
			{
				int index = source.IndexOf('.');
				if ((index >= 0) && (index < (source.Length - 1))) source = source.Substring(index + 1);

				return source;
			}
			else
			{
				tag = tag.Validated("Tag");
				if (tag.StartsWith(".")) throw new ArgumentException("Tag '{0}' is invalid.".FormatWith(tag));
				if (!tag.EndsWith(".")) tag = tag + ".";
				if (source.StartsWith(tag)) source = source.Remove(0, tag.Length);

				return source;
			}
		}
	}
}
