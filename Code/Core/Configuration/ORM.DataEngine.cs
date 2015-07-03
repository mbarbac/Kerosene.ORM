using Kerosene.Tools;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace Kerosene.ORM.Configuration
{
	// =====================================================
	/// <summary>
	/// The entry related to engine options.
	/// </summary>
	public class DataEngineElement : ConfigurationElement
	{
		/// <summary></summary>
		public const string ELEMENT_NAME = "dataEngine";

		/// <summary></summary>
		public const string PROPERTY_RELAX_TRANSFORMERS = "relaxTransformers";

		/// <summary>
		/// Whether to relax data link transformers.
		/// </summary>
		[ConfigurationProperty(PROPERTY_RELAX_TRANSFORMERS, IsRequired = false, DefaultValue = null)]
		public bool? RelaxTransformers
		{
			get { return (bool?)this[PROPERTY_RELAX_TRANSFORMERS]; }
			set { this[PROPERTY_RELAX_TRANSFORMERS] = value; }
		}
	}

	// =====================================================
	public partial class ORMConfiguration
	{
		/// <summary>
		/// Common options for data engines.
		/// </summary>
		[ConfigurationProperty(DataEngineElement.ELEMENT_NAME, IsRequired = false, DefaultValue = null)]
		public DataEngineElement DataEngine
		{
			get { return (DataEngineElement)base[DataEngineElement.ELEMENT_NAME]; }
		}
	}
}
