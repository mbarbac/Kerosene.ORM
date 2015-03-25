// ======================================================== ProxyGenerator.cs
namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.Tools;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using System.Reflection.Emit;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Represents the capability of generating extended types to manage the getters and setters
	/// of the virtual lazy properties of their original base types. This class is intended for
	/// internal usage only.
	/// </summary>
	public static partial class ProxyGenerator
	{
		internal const string PROXY_ASSEMBLY_NAME = "KeroseneRunTimeProxies";
		internal const int MAX_PROXY_NAME_LENGTH = 512 - 1;
		internal const string COMPLETEDFLAG_SUFFIX = "_Completed";
		internal const string SOURCEBACK_SUFFIX = "_Source";

		static ProxyHolderSet _Holders = new ProxyHolderSet();
		static AssemblyBuilder _AssemblyBuilder = null;
		static ModuleBuilder _ModuleBuilder = null;

		/// <summary>
		/// The collection of proxy holders that have been generated.
		/// </summary>
		internal static ProxyHolderSet Holders
		{
			get { return _Holders; }
		}

		/// <summary>
		/// Initializes the proxy generator if needed.
		/// </summary>
		static void InitializeProxyGenerator()
		{
			if (_AssemblyBuilder == null)
			{
				AssemblyName asmName = new AssemblyName(PROXY_ASSEMBLY_NAME);

				_AssemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
					asmName,
					AssemblyBuilderAccess.Run);

				_ModuleBuilder = _AssemblyBuilder.DefineDynamicModule(
					_AssemblyBuilder.GetName().Name,
					emitSymbolInfo: false);
			}
		}

		/// <summary>
		/// Gets the list of virtual lazy properties associated to the type of the entities
		/// managed by the given map. The list is obtained from the collection of members that
		/// is currently defined for the map.
		/// </summary>
		static List<LazyProperty> GetLazyProperties<T>(DataMap<T> map) where T : class
		{
			var list = new List<LazyProperty>(); foreach (var member in map.Members)
			{
				if (!member.ElementInfo.IsProperty) continue; // Only properties can be virtual...
				if (member.ElementInfo.IsMultipart) continue; // Multipart lazy properties are not supported...
				if (member.CompleteMember == null) continue; // If no delegate there is no lazyness...

				var lazy = new LazyProperty();
				lazy.OriginalProperty = member.ElementInfo.PropertyInfo;
				lazy.OriginalGetter = lazy.OriginalProperty.GetGetMethod(nonPublic: true);
				lazy.OriginalSetter = lazy.OriginalProperty.GetSetMethod(nonPublic: true);

				bool valid = false;
				if (lazy.OriginalGetter != null && lazy.OriginalGetter.IsVirtual && !lazy.OriginalGetter.IsPrivate) valid = true;
				if (lazy.OriginalSetter != null && lazy.OriginalSetter.IsVirtual && !lazy.OriginalSetter.IsPrivate) valid = true;

				if (valid) list.Add(lazy);
			}

			list.Sort((a, b) => string.Compare(a.Name, b.Name));
			return list;
		}

		/// <summary>
		/// Gets the name of the proxy type associated with the given type and list of lazy
		/// properties, or null if no such proxy is needed.
		/// </summary>
		static string GetProxyTypeName<T>(List<LazyProperty> list) where T : class
		{
			if (list == null || list.Count == 0) return null;

			var type = typeof(T);
			var name = type.EasyName(depth: int.MaxValue);

			foreach (var entry in list) name = string.Format("{0}_{1}", name, entry.Name);
			if (name.Length > MAX_PROXY_NAME_LENGTH) throw new ArgumentOutOfRangeException(
				"Lenght of proxy name '{0}' is too big.".FormatWith(name));

			name = name.Replace(".", "$");
			return name;
		}

		/// <summary>
		/// Returns a holder valid for the given map, or null if not such holder is needed based
		/// upon the defined members of the map and its virtual lazy properties.
		/// </summary>
		/// <typeparam name="T">The type of the entities managed by the map.</typeparam>
		/// <param name="map">The map whose holder is to be obtained.</param>
		/// <returns>The requested holder or null.</returns>
		internal static ProxyHolder Locate<T>(DataMap<T> map) where T : class
		{
			InitializeProxyGenerator();
			var list = GetLazyProperties<T>(map);
			var name = GetProxyTypeName<T>(list); if (name == null) return null;

			ProxyHolder holder = null; lock (Holders.SyncRoot)
			{
				// If the appropriate holder exists, just return it...
				holder = Holders.Where(x => x.ExtendedType.Name == name).FirstOrDefault();
				if (holder != null) { list.Clear(); return holder; }

				// Otherwise, create a new one - we cannot yet add it as its type is not set yet...
				holder = new ProxyHolder();

				// Capturing the lazy properties...
				foreach (var lazy in list)
				{
					lazy.Holder = holder;
					holder.LazyProperties.Add(lazy);
				}

				// Preparing the builders...
				Type baseType = typeof(T);
				ILGenerator il = null;

				var proxyBuilder = _ModuleBuilder.DefineType(name,
					TypeAttributes.Public | TypeAttributes.AutoLayout |
					TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass |
					TypeAttributes.BeforeFieldInit,
					baseType, null);

				// The fields that maintain whether the extended property is loaded or not...
				foreach (var lazy in holder.LazyProperties)
					lazy.LazyCompletedFlag = proxyBuilder.DefineField(lazy.Name + COMPLETEDFLAG_SUFFIX,
						typeof(bool),
						FieldAttributes.Public);

				// Replicating the constructors on the extended type...
				var baseCons = baseType.GetConstructors(TypeEx.InstancePublicAndHidden);
				foreach (var baseCon in baseCons)
				{
					var pars = baseCon.GetParameters();
					var types = pars.Select(x => x.ParameterType).ToArray();

					var proxyCon = proxyBuilder.DefineConstructor(
						baseCon.Attributes,
						baseCon.CallingConvention,
						types);

					il = proxyCon.GetILGenerator(); foreach (var element in holder.LazyProperties)
					{
						il.Emit(OpCodes.Ldarg_0);
						il.Emit(OpCodes.Ldc_I4_0);
						il.Emit(OpCodes.Stfld, element.LazyCompletedFlag);
					}
					il.Emit(OpCodes.Ldarg_0);
					for (int i = 0; i < types.Length; i++) il.Emit(OpCodes.Ldarg, i + 1);
					il.Emit(OpCodes.Call, baseCon);
					il.Emit(OpCodes.Ret);
				}

				// Replicating the virtual properties on the extended type...
				var onProxySetter = typeof(ProxyGenerator).GetMethod("OnProxySetter", BindingFlags.Public | BindingFlags.Static);
				var onProxyGetter = typeof(ProxyGenerator).GetMethod("OnProxyGetter", BindingFlags.Public | BindingFlags.Static);

				// Treating the lazy properties...
				foreach (var lazy in holder.LazyProperties)
				{
					lazy.ExtendedProperty = proxyBuilder.DefineProperty(lazy.Name,
						lazy.OriginalProperty.Attributes,
						lazy.OriginalProperty.PropertyType, null);

					lazy.SourceBackProperty = proxyBuilder.DefineProperty(lazy.Name + SOURCEBACK_SUFFIX,
						lazy.OriginalProperty.Attributes,
						lazy.OriginalProperty.PropertyType, null);

					// The getter...
					if (lazy.OriginalGetter != null && lazy.OriginalGetter.IsVirtual && !lazy.OriginalGetter.IsPrivate)
					{
						var sourceBackMethod = proxyBuilder.DefineMethod("get_" + lazy.SourceBackProperty.Name,
							MethodAttributes.Public |
							MethodAttributes.SpecialName |
							MethodAttributes.HideBySig | MethodAttributes.Virtual,
							lazy.SourceBackProperty.PropertyType, null);

						il = sourceBackMethod.GetILGenerator();
						il.Emit(OpCodes.Ldarg_0);
						il.Emit(OpCodes.Call, lazy.OriginalGetter);
						il.Emit(OpCodes.Ret);

						((PropertyBuilder)lazy.SourceBackProperty).SetGetMethod(sourceBackMethod);

						var extendedMethod = proxyBuilder.DefineMethod("get_" + lazy.ExtendedProperty.Name,
							MethodAttributes.Public |
							MethodAttributes.SpecialName |
							MethodAttributes.HideBySig | MethodAttributes.Virtual,
							lazy.ExtendedProperty.PropertyType, null);

						il = extendedMethod.GetILGenerator();
						il.Emit(OpCodes.Ldarg_0);
						il.Emit(OpCodes.Ldstr, lazy.ExtendedProperty.Name);
						il.Emit(OpCodes.Call, onProxyGetter);
						il.Emit(OpCodes.Ret);

						((PropertyBuilder)lazy.ExtendedProperty).SetGetMethod(extendedMethod);
					}

					// The setter...
					if (lazy.OriginalSetter != null && lazy.OriginalSetter.IsVirtual && !lazy.OriginalSetter.IsPrivate)
					{
						var sourceBackMethod = proxyBuilder.DefineMethod("set_" + lazy.SourceBackProperty.Name,
							MethodAttributes.Public |
							MethodAttributes.SpecialName |
							MethodAttributes.HideBySig | MethodAttributes.Virtual,
							null, new[] { lazy.SourceBackProperty.PropertyType });

						il = sourceBackMethod.GetILGenerator();
						il.Emit(OpCodes.Ldarg_0);
						il.Emit(OpCodes.Ldarg_1);
						il.Emit(OpCodes.Call, lazy.OriginalSetter);
						il.Emit(OpCodes.Ret);

						((PropertyBuilder)lazy.SourceBackProperty).SetSetMethod(sourceBackMethod);

						var extendedMethod = proxyBuilder.DefineMethod("Set_" + lazy.ExtendedProperty.Name,
							MethodAttributes.Public |
							MethodAttributes.SpecialName |
							MethodAttributes.HideBySig | MethodAttributes.Virtual,
							null, new[] { lazy.ExtendedProperty.PropertyType });

						il = extendedMethod.GetILGenerator();
						il.Emit(OpCodes.Ldarg_0);
						il.Emit(OpCodes.Ldarg_1);
						il.Emit(OpCodes.Ldstr, lazy.ExtendedProperty.Name);
						il.Emit(OpCodes.Call, onProxySetter);
						il.Emit(OpCodes.Ret);

						((PropertyBuilder)lazy.ExtendedProperty).SetSetMethod(extendedMethod);
					}
				}

				// Generating the type...
				holder.ExtendedType = proxyBuilder.CreateType();

				// Before releasing the lock let's cache some relevant information...
				var type = holder.ExtendedType; foreach (var lazy in holder.LazyProperties)
				{
					lazy.LazyCompletedFlag = type.GetField(lazy.LazyCompletedFlag.Name, TypeEx.InstancePublicAndHidden);

					lazy.ExtendedProperty = type.GetProperty(lazy.ExtendedProperty.Name, TypeEx.InstancePublicAndHidden);
					lazy.ExtendedGetter = lazy.ExtendedProperty == null ? null : lazy.ExtendedProperty.GetGetMethod(nonPublic: true);
					lazy.ExtendedSetter = lazy.ExtendedProperty == null ? null : lazy.ExtendedProperty.GetSetMethod(nonPublic: true);

					lazy.SourceBackProperty = type.GetProperty(lazy.SourceBackProperty.Name, TypeEx.InstancePublicAndHidden);
					lazy.SourceBackGetter = lazy.SourceBackProperty == null ? null : lazy.SourceBackProperty.GetGetMethod(nonPublic: true);
					lazy.SourceBackSetter = lazy.SourceBackProperty == null ? null : lazy.SourceBackProperty.GetSetMethod(nonPublic: true);
				}
				 
				// Now we can add the holder into the collection as we have set its type...
				Holders.Add(holder);
			}

			return holder;
		}
	}

	// ==================================================== 
	public static partial class ProxyGenerator
	{
		/// <summary>
		/// Call-back method used to set the value of a virtual lazy property.
		/// <para>FOR INTERNAL USAGE ONLY.</para>
		/// </summary>
		/// <remarks>Needs to be a public method in a public class.</remarks>
		public static void OnProxySetter(object entity, object value, string name)
		{
			var type = entity.GetType();
			var holder = Holders.FindByExtended(type);
			var lazy = holder.LazyProperties.FindByName(name);

			lazy.LazyCompletedFlag.SetValue(entity, true); // Avoiding re-entrance...
			lazy.SourceBackSetter.Invoke(entity, new[] { value });
		}

		/// <summary>
		/// Call-back method used to get the value of a virtual lazy property.
		/// <para>FOR INTERNAL USAGE ONLY.</para>
		/// </summary>
		/// <remarks>Needs to be a public method in a public class.</remarks>
		public static object OnProxyGetter(object entity, string name)
		{
			var type = entity.GetType();
			var holder = Holders.FindByExtended(type);
			var lazy = holder.LazyProperties.FindByName(name);

			var value = lazy.SourceBackGetter.Invoke(entity, null);

			var completed = (bool)lazy.LazyCompletedFlag.GetValue(entity);
			while (!completed)
			{
				var meta = MetaEntity.Locate(entity, create: false);
				if (meta == null) break;
				if (meta.Record == null) break;
				if (meta.UberMap == null) break;

				var member = meta.UberMap.Members.FirstOrDefault<IUberMember>(x => x.Name == name);
				if (member == null) break;
				if (member.CompleteMember == null) break;

				lazy.LazyCompletedFlag.SetValue(entity, true);
				member.CompleteMember(meta.Record, entity);
				value = lazy.SourceBackGetter.Invoke(entity, null);

				if (UberHelper.TrackChildEntities && 
					member.DependencyMode == MemberDependencyMode.Child &&
					member.ElementInfo.ElementType.IsListAlike())
				{
					type = member.ElementInfo.ElementType.ListAlikeMemberType();
					if (type != null && type.IsClass)
					{
						if (!meta.TrackedChilds.ContainsKey(member.Name))
							meta.TrackedChilds.Add(member.Name, new HashSet<object>());

						var childs = meta.TrackedChilds[member.Name];
						childs.Clear();
						
						var iter = member.ElementInfo.GetValue(entity) as IEnumerable;
						foreach (var item in iter) childs.Add(item);
					}
				}

				break;
			}

			return value;
		}
	}

	// ==================================================== 
	/// <summary>
	/// Maintains the relevant information of a type that has been extended to support virtual
	/// lazy properties. This class is intended for internal usage only.
	/// </summary>
	public class ProxyHolder
	{
		/// <summary>
		/// Initializes a new empty instance.
		/// </summary>
		internal ProxyHolder()
		{
			LazyProperties = new ProxyHolderLazySet();
		}

		/// <summary>
		/// Clears all the resources held by this instance.
		/// </summary>
		internal void Dispose()
		{
			LazyProperties.Clear();
			LazyProperties = null;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the string representation of this instance.</returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendFormat(ExtendedType != null ? ExtendedType.EasyName() : "<unknown>");

			if (LazyProperties != null && LazyProperties.Count != 0)
			{
				sb.Append(" ["); bool first = true; foreach (var item in LazyProperties)
				{
					if (first) first = false; else sb.Append(", ");
					sb.Append(item);
				}
				sb.Append("]");
			}

			return sb.ToString();
		}

		/// <summary>
		/// The extended type created to manage the getters and setter of the virtual lazy
		/// properties of the original base type.
		/// </summary>
		internal Type ExtendedType { get; set; }

		/// <summary>
		/// The collection of lazy properties for which this holder was created.
		/// </summary>
		internal ProxyHolderLazySet LazyProperties { get; private set; }
	}

	// ==================================================== 
	/// <summary>
	/// Represents a virtual lazy property in an extended type. This class is intended for
	/// internal usage only.
	/// </summary>
	public class LazyProperty
	{
		/// <summary>
		/// Initializes a new empty instance.
		/// </summary>
		internal LazyProperty() { }

		/// <summary>
		/// Clears all the resources held by this instance.
		/// </summary>
		internal void Dispose()
		{
			Holder = null;
			OriginalProperty = null; OriginalGetter = null; OriginalSetter = null;
			ExtendedProperty = null; ExtendedGetter = null; ExtendedSetter = null;
			SourceBackProperty = null; SourceBackGetter = null; SourceBackSetter = null;
		}

		/// <summary>
		/// The holder where this lazy property is defined.
		/// </summary>
		internal ProxyHolder Holder { get; set; }

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the string representation of this instance.</returns>
		public override string ToString()
		{
			return OriginalProperty == null ? string.Empty : OriginalProperty.Name;
		}

		/// <summary>
		/// The given name of this lazy property for identification purposes, or null if this
		/// instance is not initialized yet.
		/// </summary>
		internal string Name
		{
			get { return OriginalProperty == null ? null : OriginalProperty.Name; }
		}

		/// <summary>
		/// The original property this instance refers to.
		/// </summary>
		internal PropertyInfo OriginalProperty { get; set; }

		/// <summary>
		/// The getter for the original property.
		/// </summary>
		internal MethodInfo OriginalGetter { get; set; }

		/// <summary>
		/// The setter for the original property.
		/// </summary>
		internal MethodInfo OriginalSetter { get; set; }

		/// <summary>
		/// The field to hold if the proxied virtual lazy property has been loaded (completed) or
		/// not.
		/// </summary>
		internal FieldInfo LazyCompletedFlag { get; set; }

		/// <summary>
		/// The property on the extended type.
		/// </summary>
		internal PropertyInfo ExtendedProperty { get; set; }

		/// <summary>
		/// The getter of the property on the extended type.
		/// </summary>
		internal MethodInfo ExtendedGetter { get; set; }

		/// <summary>
		/// The setter of the property on the extended type.
		/// </summary>
		internal MethodInfo ExtendedSetter { get; set; }

		/// <summary>
		/// Access back to the source property on the extended type.
		/// </summary>
		internal PropertyInfo SourceBackProperty { get; set; }

		/// <summary>
		/// The getter to access back the source property on the extended type.
		/// </summary>
		internal MethodInfo SourceBackGetter { get; set; }

		/// <summary>
		/// The setter to access back the source property on the extended type.
		/// </summary>
		internal MethodInfo SourceBackSetter { get; set; }
	}
}
// ======================================================== 
