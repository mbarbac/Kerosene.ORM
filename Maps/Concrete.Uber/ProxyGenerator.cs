using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Kerosene.ORM.Maps.Concrete
{
	// ====================================================
	/// <summary>
	/// Represents the capability of generating proxy types extending those with virtual lazy
	/// properties. This class is intended for internal usage only.
	/// </summary>
	public static partial class ProxyGenerator
	{
		static ProxyHolderCollection _ProxyHolders = new ProxyHolderCollection();
		static object _ProxyLock = new object();

		/// <summary>
		/// The global collection of proxy ProxyHolders.
		/// </summary>
		internal static ProxyHolderCollection ProxyHolders
		{
			get { return _ProxyHolders; }
		}

		/// <summary>
		/// An object that can be used to synchronize the operations related with the global
		/// list of proxy holders.
		/// </summary>
		internal static object ProxyLock
		{
			get { return _ProxyLock; }
		}

		private const string PROXY_ASSEMBLY_NAME = "KeroseneRunTimeProxies";
		private const int MAX_PROXY_NAME_LENGTH = 512 - 1;
		private const string COMPLETEDFLAG_SUFFIX = "_Completed";
		private const string SOURCEBACK_SUFFIX = "_Source";

		static AssemblyBuilder _AssemblyBuilder = null;
		static ModuleBuilder _ModuleBuilder = null;

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
			// Assumes that members is validated and so they have ElementInfo properly set...
			var list = new List<LazyProperty>(); foreach (var member in map.Members)
			{
				if (!member.ElementInfo.IsProperty) continue; // Only properties can be virtual...
				if (member.ElementInfo.IsMultipart) continue; // Multipart properties not supported...
				if (member.CompleteMember == null) continue; // If no delegate property is not considered lazy...

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
			var name = type.EasyName(chain: true);

			foreach (var entry in list) name = string.Format("{0}_{1}", name, entry.Name);
			if (name.Length > MAX_PROXY_NAME_LENGTH)
				throw new ArgumentOutOfRangeException(
					"Lenght of proxy name '{0}' is too big.".FormatWith(name));

			name = name.Replace(".", "$");
			return name;
		}

		/// <summary>
		/// Returns the proxy holder associated with the given map, creating and instance if it
		/// was not created yet. Returns null if no proxy type is needed because the type of the
		/// managed entities contains no lazy properties.
		/// </summary>
		internal static ProxyHolder Locate<T>(DataMap<T> map) where T : class
		{
			InitializeProxyGenerator();
			var list = GetLazyProperties<T>(map);
			var name = GetProxyTypeName<T>(list); if (name == null) return null;

			ProxyHolder holder = null; lock (ProxyLock)
			{
				// If the appropriate holder exists, just return it...
				holder = ProxyHolders.Where(x => x.ProxyType.Name == name).FirstOrDefault();
				if (holder != null) { list.Clear(); return holder; }

				// Otherwise, create a new one - we cannot yet add it as its type is not set yet...
				holder = new ProxyHolder();

				// Capturing the lazy properties...
				foreach (var lazy in list)
					holder.LazyProperties.Add(lazy);

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
				holder.ProxyType = proxyBuilder.CreateType();

				// Before releasing the lock let's cache some relevant information...
				var type = holder.ProxyType; foreach (var lazy in holder.LazyProperties)
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
				ProxyHolders.Add(holder);
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
			var holder = ProxyHolders.Find(type);
			var lazy = holder.LazyProperties.Find(name);

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
			var holder = ProxyHolders.Find(type);
			var lazy = holder.LazyProperties.Find(name);

			var value = lazy.SourceBackGetter.Invoke(entity, null);

			var completed = (bool)lazy.LazyCompletedFlag.GetValue(entity);
			while (!completed)
			{
				var meta = MetaEntity.Locate(entity, create: false);
				if (meta == null) break;
				if (meta.Record == null) break;
				if (meta.UberMap == null) break;

				var member = meta.UberMap.Members.Find(x => x.Name == name);
				if (member == null) break;
				if (member.CompleteMember == null) break;

				lazy.LazyCompletedFlag.SetValue(entity, true);

				member.CompleteMember(meta.Record, entity);
				value = lazy.SourceBackGetter.Invoke(entity, null);

				meta.CaptureMetaMemberChilds(member);

				break;
			}

			return value;
		}
	}
}
