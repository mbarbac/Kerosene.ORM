// ======================================================== ORMConfiguration.cs
namespace Kerosene.ORM.Configuration
{
	using Kerosene.Tools;
	using System;
	using System.Configuration;
	using System.Linq;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// The configuration handler for the Kerosene framework.
	/// </summary>
	public partial class ORMConfiguration : ConfigurationSection
	{
		public const string SECTION_NAME = "keroseneORM";

		/// <summary>
		/// Returns an instance containing the information of the Kerosene ORM configuration
		/// section, or null if such section does not exist.
		/// </summary>
		/// <returns>An instance with the Kerosene ORM configuration section information, or null.</returns>
		public static ORMConfiguration GetInfo()
		{
			var info = ConfigurationManager.GetSection(SECTION_NAME) as ORMConfiguration;
			return info;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the string representation of this instance.</returns>
		public override string ToString() { return GetType().EasyName(); }
	}

	// ==================================================== 
	public partial class ORMConfiguration
	{
		/// <summary>
		/// The connection string to use, if any.
		/// </summary>
		[ConfigurationProperty(ConnectionStringElement.ELEMENT_NAME, IsRequired = false, DefaultValue = null)]
		public ConnectionStringElement ConnectionString
		{
			get { return (ConnectionStringElement)base[ConnectionStringElement.ELEMENT_NAME]; }
		}
	}
	/// <summary>
	/// The entry that defines the connection string entry to use, if any.
	/// </summary>
	public class ConnectionStringElement : ConfigurationElement
	{
		public const string ELEMENT_NAME = "connectionString";
		public const string PROPERTY_NAME = "name";

		/// <summary>
		/// If not null the name of the connection string entry to use.
		/// </summary>
		[ConfigurationProperty(PROPERTY_NAME, IsRequired = true, DefaultValue = null)]
		public string Name
		{
			get { return ((string)this[PROPERTY_NAME]).Validated(canbeNull: true, emptyAsNull: true); }
			set { this[PROPERTY_NAME] = value; }
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the string representation of this instance.</returns>
		public override string ToString()
		{
			return Name == null ? string.Empty : string.Format("ConnectionString={0}", Name);
		}
	}

	// ==================================================== 
	public partial class ORMConfiguration
	{
		/// <summary>
		/// Whether complex tags are to be kept or not.
		/// </summary>
		[ConfigurationProperty(KeepComplexTagsElement.ELEMENT_NAME, IsRequired = false, DefaultValue = null)]
		public KeepComplexTagsElement KeepComplexTags
		{
			get { return (KeepComplexTagsElement)base[KeepComplexTagsElement.ELEMENT_NAME]; }
		}
	}
	/// <summary>
	/// The entry that defines whether complex tags are to be kept or not.
	/// </summary>
	public class KeepComplexTagsElement : ConfigurationElement
	{
		public const string ELEMENT_NAME = "complexTags";
		public const string PROPERTY_KEEP = "keep";

		/// <summary>
		/// If not null whether complex tags are to be kept or not.
		/// </summary>
		[ConfigurationProperty(PROPERTY_KEEP, IsRequired = false, DefaultValue = null)]
		public bool? Keep
		{
			get { return (bool?)this[PROPERTY_KEEP]; }
			set { this[PROPERTY_KEEP] = value; }
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the string representation of this instance.</returns>
		public override string ToString()
		{
			return Keep == null ? string.Empty : string.Format("KeepComplexTags={0}", Keep);
		}
	}

	// ==================================================== 
	public partial class ORMConfiguration
	{
		/// <summary>
		/// Options for data link transformers.
		/// </summary>
		[ConfigurationProperty(DataLinkTransformersElement.ELEMENT_NAME, IsRequired = false, DefaultValue = null)]
		public DataLinkTransformersElement Transformers
		{
			get { return (DataLinkTransformersElement)base[DataLinkTransformersElement.ELEMENT_NAME]; }
		}
	}
	/// <summary>
	/// The entry that defines options for the data link transformers.
	/// </summary>
	public class DataLinkTransformersElement : ConfigurationElement
	{
		public const string ELEMENT_NAME = "transformers";
		public const string PROPERTY_RELAX = "relax";

		/// <summary>
		/// Whether to relax data link transformers.
		/// </summary>
		[ConfigurationProperty(PROPERTY_RELAX, IsRequired = false, DefaultValue = Core.DataEngine.DEFAULT_RELAX_TRANSFORMERS)]
		public bool? Relax
		{
			get { return (bool?)this[PROPERTY_RELAX]; }
			set { this[PROPERTY_RELAX] = value; }
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the string representation of this instance.</returns>
		public override string ToString()
		{
			return Relax == null ? string.Empty : string.Format("RelaxTransformers={0}", Relax);
		}
	}

	// ==================================================== 
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
	/// <summary>
	/// The collection of custome engines defined.
	/// </summary>
	public class CustomEngineCollection : ConfigurationElementCollection
	{
		public const string COLLECTION_NAME = "customEngines";

		protected override ConfigurationElement CreateNewElement()
		{
			return new CustomEngineElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((CustomEngineElement)element).Id;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the string representation of this instance.</returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder(); if (Count != 0)
			{
				sb.Append("Engines=");
				if (Count > 1) sb.Append("[");

				bool first = true; foreach (CustomEngineElement engine in this)
				{
					if (first) first = false; else sb.Append(", ");
					sb.Append(engine);
				}

				if (Count > 1) sb.Append("]");
			}
			return sb.ToString();
		}
	}
	public class CustomEngineElement : ConfigurationElement
	{
		public const string PROPERTY_ID = "id";
		public const string PROPERTY_TYPENAME = "type";
		public const string PROPERTY_ASSEMBLYNAME = "assembly";
		public const string PROPERTY_INVARIANTNAME = "invariantName";
		public const string PROPERTY_SERVERVERSION = "serverVersion";
		public const string PROPERTY_CASESENSITIVENAMES = "caseSensitiveNames";
		public const string PROPERTY_PARAMETERPREFIX = "parameterPrefix";
		public const string PROPERTY_POSITIONALPARAMETERS = "positionalParameters";
		public const string PROPERTY_SUPPORTSNATIVESKIPTAKE = "supportsNativeSkipTake";

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the string representation of this instance.</returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendFormat("[{0}, Type={1}, Assembly={2}", Id, TypeName, AssemblyName);
			if (InvarianteName != null) sb.AppendFormat(", Provider={0}", InvarianteName);
			if (ServerVersion != null) sb.AppendFormat(", ServerVersion={0}", ServerVersion);
			if (CaseSensitiveNames != null) sb.AppendFormat(", CaseSensitiveNames={0}", CaseSensitiveNames);
			if (ParameterPrefix != null) sb.AppendFormat(", Prefix={0}", ParameterPrefix);
			if (PositionalParameters != null) sb.AppendFormat(", PositionalParameters={0}", PositionalParameters);
			if (SupportsNativeSkipTake != null) sb.AppendFormat(", NativeSkipTake={0}", SupportsNativeSkipTake);
			sb.Append("]");

			return sb.ToString();
		}

		/// <summary>
		/// The unique id of this engine in the configuration section.
		/// </summary>
		[ConfigurationProperty(PROPERTY_ID, IsRequired = true)]
		public string Id
		{
			get { return ((string)this[PROPERTY_ID]).Validated(canbeNull: true, emptyAsNull: true); }
			set { this[PROPERTY_ID] = value; }
		}

		/// <summary>
		/// The class type of the engine.
		/// </summary>
		[ConfigurationProperty(PROPERTY_TYPENAME, IsRequired = true)]
		public string TypeName
		{
			get { return ((string)this[PROPERTY_TYPENAME]).Validated(canbeNull: true, emptyAsNull: true); }
			set { this[PROPERTY_TYPENAME] = value; }
		}

		/// <summary>
		/// The assembly name where to find the type of the engine.
		/// </summary>
		[ConfigurationProperty(PROPERTY_ASSEMBLYNAME, IsRequired = true)]
		public string AssemblyName
		{
			get { return ((string)this[PROPERTY_ASSEMBLYNAME]).Validated(canbeNull: true, emptyAsNull: true); }
			set { this[PROPERTY_ASSEMBLYNAME] = value; }
		}

		/// <summary>
		/// If not null, overrides the invariant name by which the engine is to be registered.
		/// </summary>
		[ConfigurationProperty(PROPERTY_INVARIANTNAME, IsRequired = false, DefaultValue = null)]
		public string InvarianteName
		{
			get { return ((string)this[PROPERTY_INVARIANTNAME]).Validated(canbeNull: true, emptyAsNull: true); }
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
			get { return ((string)this[PROPERTY_SERVERVERSION]).Validated(canbeNull: true, emptyAsNull: true); }
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
			get { return ((string)this[PROPERTY_PARAMETERPREFIX]).Validated(canbeNull: true, emptyAsNull: true); }
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
}
// ======================================================== 
