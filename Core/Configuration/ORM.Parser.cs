using Kerosene.Tools;
using System;
using System.Configuration;

namespace Kerosene.ORM.Configuration
{
	// =====================================================
	/// <summary>
	/// The entry related to parsing options.
	/// </summary>
	public class ParserElement : ConfigurationElement
	{
		/// <summary></summary>
		public const string ELEMENT_NAME = "parser";
		/// <summary></summary>
		public const string PROPERTY_COMPLEX = "complexTags";

		/// <summary>
		/// If not null whether complex tags are to be kept or not.
		/// </summary>
		[ConfigurationProperty(PROPERTY_COMPLEX, IsRequired = false, DefaultValue = null)]
		public bool? ComplexTags
		{
			get { return (bool?)this[PROPERTY_COMPLEX]; }
			set { this[PROPERTY_COMPLEX] = value; }
		}
	}

	// =====================================================
	public partial class ORMConfiguration
	{
		/// <summary></summary>
		[ConfigurationProperty(ParserElement.ELEMENT_NAME, IsRequired = false, DefaultValue = null)]
		public ParserElement Parser
		{
			get { return (ParserElement)base[ParserElement.ELEMENT_NAME]; }
		}
	}
}
