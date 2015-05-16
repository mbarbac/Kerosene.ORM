using Kerosene.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace Kerosene.ORM.Configuration
{
	// ==================================================== 
	/// <summary>
	/// The entry that defines common options for data engines.
	/// </summary>
	public class DataEngineElement : ConfigurationElement
	{
		/// <summary></summary>
		public const string ELEMENT_NAME = "dataEngine";
		/// <summary></summary>
		public const string PROPERTY_RELAX_TRANSFORMERS = "relaxTransformers";
		/// <summary></summary>
		public const string PROPERTY_COMPLEX_TAGS = "complexTags";

		/// <summary>
		/// Whether to relax data link transformers.
		/// </summary>
		[ConfigurationProperty(PROPERTY_RELAX_TRANSFORMERS, IsRequired = false, DefaultValue = null)]
		public bool? RelaxTransformers
		{
			get { return (bool?)this[PROPERTY_RELAX_TRANSFORMERS]; }
			set { this[PROPERTY_RELAX_TRANSFORMERS] = value; }
		}

		/// <summary>
		/// Whether to keep complex tags or not.
		/// </summary>
		[ConfigurationProperty(PROPERTY_COMPLEX_TAGS, IsRequired = false, DefaultValue = null)]
		public bool? ComplexTags
		{
			get { return (bool?)this[PROPERTY_COMPLEX_TAGS]; }
			set { this[PROPERTY_COMPLEX_TAGS] = value; }
		}
	}

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

	// ==================================================== 
	/// <summary>
	/// The collection of custom engines defined.
	/// </summary>
	public class CustomEngineCollection : ConfigurationElementCollection
	{
		/// <summary>
		/// The name of the element.
		/// </summary>
		public const string COLLECTION_NAME = "customEngines";

		/// <summary></summary>
		protected override ConfigurationElement CreateNewElement()
		{
			return new CustomEngineElement();
		}

		/// <summary></summary>
		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((CustomEngineElement)element).Id;
		}

		/// <summary>
		/// The collection of custom engines read from the configuration files.
		/// </summary>
		public IEnumerable<CustomEngineElement> Items
		{
			get
			{
				foreach (CustomEngineElement entry in this) yield return entry;
			}
		}
	}

	/// <summary></summary>
	public class CustomEngineElement : ConfigurationElement
	{
		/// <summary></summary>
		public const string PROPERTY_ID = "id";
		/// <summary></summary>
		public const string PROPERTY_TYPENAME = "type";
		/// <summary></summary>
		public const string PROPERTY_ASSEMBLYNAME = "assembly";
		/// <summary></summary>
		public const string PROPERTY_INVARIANTNAME = "invariantName";
		/// <summary></summary>
		public const string PROPERTY_SERVERVERSION = "serverVersion";
		/// <summary></summary>
		public const string PROPERTY_CASESENSITIVENAMES = "caseSensitiveNames";
		/// <summary></summary>
		public const string PROPERTY_PARAMETERPREFIX = "parameterPrefix";
		/// <summary></summary>
		public const string PROPERTY_POSITIONALPARAMETERS = "positionalParameters";
		/// <summary></summary>
		public const string PROPERTY_SUPPORTSNATIVESKIPTAKE = "supportsNativeSkipTake";

		/// <summary>
		/// The unique id of this engine in the configuration section.
		/// </summary>
		[ConfigurationProperty(PROPERTY_ID, IsRequired = true)]
		public string Id
		{
			get { return ((string)this[PROPERTY_ID]).NullIfTrimmedIsEmpty(); }
			set { this[PROPERTY_ID] = value; }
		}

		/// <summary>
		/// The class type of the engine.
		/// </summary>
		[ConfigurationProperty(PROPERTY_TYPENAME, IsRequired = true)]
		public string TypeName
		{
			get { return ((string)this[PROPERTY_TYPENAME]).NullIfTrimmedIsEmpty(); }
			set { this[PROPERTY_TYPENAME] = value; }
		}

		/// <summary>
		/// The assembly name where to find the type of the engine.
		/// </summary>
		[ConfigurationProperty(PROPERTY_ASSEMBLYNAME, IsRequired = true)]
		public string AssemblyName
		{
			get { return ((string)this[PROPERTY_ASSEMBLYNAME]).NullIfTrimmedIsEmpty(); }
			set { this[PROPERTY_ASSEMBLYNAME] = value; }
		}

		/// <summary>
		/// If not null, overrides the invariant name by which the engine is to be registered.
		/// </summary>
		[ConfigurationProperty(PROPERTY_INVARIANTNAME, IsRequired = false, DefaultValue = null)]
		public string InvariantName
		{
			get { return ((string)this[PROPERTY_INVARIANTNAME]).NullIfTrimmedIsEmpty(); }
			set { this[PROPERTY_INVARIANTNAME] = value; }
		}

		/// <summary>
		/// If not null, overrides the server version this engine supports, using the semantic
		/// versioning format.
		/// </summary>
		/// <summary>
		/// If not null, overrides the invariant name by which the engine is to be registered.
		/// </summary>
		[ConfigurationProperty(PROPERTY_SERVERVERSION, IsRequired = false, DefaultValue = null)]
		public string ServerVersion
		{
			get { return ((string)this[PROPERTY_SERVERVERSION]).NullIfTrimmedIsEmpty(); }
			set { this[PROPERTY_SERVERVERSION] = value; }
		}

		/// <summary>
		/// If not null, overrides whether the identifiers in the database, as table and column
		/// names, are case sensitive or not.
		/// </summary>
		[ConfigurationProperty(PROPERTY_CASESENSITIVENAMES, IsRequired = false, DefaultValue = null)]
		public bool? CaseSensitiveNames
		{
			get { return (bool?)this[PROPERTY_CASESENSITIVENAMES]; }
			set { this[PROPERTY_CASESENSITIVENAMES] = value; }
		}

		/// <summary>
		/// If not null, overrides the default prefix the database engine uses for the names of the
		/// parameters of a command, or null if no default value is used.
		/// </summary>
		[ConfigurationProperty(PROPERTY_PARAMETERPREFIX, IsRequired = false, DefaultValue = null)]
		public string ParameterPrefix
		{
			get { return ((string)this[PROPERTY_PARAMETERPREFIX]).NullIfTrimmedIsEmpty(); }
			set { this[PROPERTY_PARAMETERPREFIX] = value; }
		}

		/// <summary>
		/// If not null, overrides whether the database engine treats the parameters of a command
		/// by position instead of by name.
		/// </summary>
		[ConfigurationProperty(PROPERTY_POSITIONALPARAMETERS, IsRequired = false, DefaultValue = null)]
		public bool? PositionalParameters
		{
			get { return (bool?)this[PROPERTY_POSITIONALPARAMETERS]; }
			set { this[PROPERTY_POSITIONALPARAMETERS] = value; }
		}

		/// <summary>
		/// If not null, overrides whether the database engine provides a normalized syntax to
		/// implement the skip/take functionality, or rather it has to be emulated by software.
		/// </summary>
		[ConfigurationProperty(PROPERTY_SUPPORTSNATIVESKIPTAKE, IsRequired = false, DefaultValue = null)]
		public bool? SupportsNativeSkipTake
		{
			get { return (bool?)this[PROPERTY_SUPPORTSNATIVESKIPTAKE]; }
			set { this[PROPERTY_SUPPORTSNATIVESKIPTAKE] = value; }
		}
	}

	public partial class ORMConfiguration
	{
		/// <summary>
		/// The collection of custom engines that appears in the Kerosene ORM configuration
		/// section, or null.
		/// </summary>
		[ConfigurationProperty(CustomEngineCollection.COLLECTION_NAME, IsRequired = false, DefaultValue = null)]
		[ConfigurationCollection(typeof(CustomEngineCollection), AddItemName = "add")]
		public CustomEngineCollection CustomEngines
		{
			get { return (CustomEngineCollection)base[CustomEngineCollection.COLLECTION_NAME]; }
		}
	}
}
