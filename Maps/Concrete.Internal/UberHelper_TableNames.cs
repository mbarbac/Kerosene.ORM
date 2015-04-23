namespace Kerosene.ORM.Maps.Concrete
{
	using Kerosene.ORM.Core;
	using Kerosene.Tools;
	using System;
	using System.Collections.Generic;
	using System.Linq;

	// ==================================================== 
	internal static partial class UberHelper
	{
		/// <summary>
		/// Generates a list of possible table names given the name source by using some
		/// pluralization rules.
		/// </summary>
		static List<string> TableNameCandidatesPlural(string source)
		{
			List<string> list = new List<string>();

			list.Add(source + "s"); // english: bath / spanish: standard
			list.Add(source + "S");

			list.Add(source + "es"); // english: dish / spanish: standard
			list.Add(source + "ES");

			if (source.EndsWith("y")) list.Add(source.Substring(0, source.Length - 1) + "ies"); // country
			if (source.EndsWith("Y")) list.Add(source.Substring(0, source.Length - 1) + "IES");

			if (source.EndsWith("d")) list.Add(source.Substring(0, source.Length - 1) + "ren"); // child
			if (source.EndsWith("D")) list.Add(source.Substring(0, source.Length - 1) + "REN");

			if (source.EndsWith("f")) list.Add(source.Substring(0, source.Length - 1) + "ves"); // leaf
			if (source.EndsWith("F")) list.Add(source.Substring(0, source.Length - 1) + "VES");

			if (source.EndsWith("fe")) list.Add(source.Substring(0, source.Length - 2) + "ves"); // knife
			if (source.EndsWith("FE")) list.Add(source.Substring(0, source.Length - 2) + "VES");

			if (source.Contains("oo")) list.Add(source.Replace("oo", "ee")); // foot
			if (source.Contains("OO")) list.Add(source.Replace("OO", "EE"));

			if (source.Contains("man")) list.Add(source.Replace("man", "men")); // woman
			if (source.Contains("MAN")) list.Add(source.Replace("MAN", "MEN"));

			if (source.Contains("person")) list.Add(source.Replace("person", "people")); // people
			if (source.Contains("Person")) list.Add(source.Replace("Person", "People"));
			if (source.Contains("PERSON")) list.Add(source.Replace("PERSON", "PEOPLE"));

			list.Add(source + "n"); // bee (old english)
			list.Add(source + "N");

			list.Add(source + "en"); // ox
			list.Add(source + "EN");

			list.Add(source + "x"); // beau
			list.Add(source + "X");

			if (source.EndsWith("a")) list.Add(source + "e"); // alumna
			if (source.EndsWith("A")) list.Add(source + "E");

			if (source.EndsWith("a")) list.Add(source.Substring(0, source.Length - 1) + "i");
			if (source.EndsWith("A")) list.Add(source.Substring(0, source.Length - 1) + "I");

			if (source.EndsWith("x")) list.Add(source.Substring(0, source.Length - 2) + "ices"); // matrix
			if (source.EndsWith("X")) list.Add(source.Substring(0, source.Length - 2) + "ICES");

			if (source.EndsWith("is")) list.Add(source.Substring(0, source.Length - 2) + "es"); // axis
			if (source.EndsWith("IS")) list.Add(source.Substring(0, source.Length - 2) + "ES");

			if (source.EndsWith("um")) list.Add(source.Substring(0, source.Length - 2) + "a"); // addendum
			if (source.EndsWith("UM")) list.Add(source.Substring(0, source.Length - 2) + "A");

			if (source.EndsWith("na")) list.Add(source.Substring(0, source.Length - 1) + "ous"); // phenomena
			if (source.EndsWith("NA")) list.Add(source.Substring(0, source.Length - 1) + "OUS");

			if (source.EndsWith("us")) list.Add(source.Substring(0, source.Length - 2) + "i"); // alumnus
			if (source.EndsWith("US")) list.Add(source.Substring(0, source.Length - 2) + "I");

			if (source.EndsWith("us")) list.Add(source.Substring(0, source.Length - 2) + "ora"); // corpus
			if (source.EndsWith("US")) list.Add(source.Substring(0, source.Length - 2) + "ORA");

			if (source.EndsWith("us")) list.Add(source.Substring(0, source.Length - 2) + "era"); // genus
			if (source.EndsWith("US")) list.Add(source.Substring(0, source.Length - 2) + "ERA");

			if (source.EndsWith("o")) list.Add(source.Substring(0, source.Length - 1) + "i"); // biscotto
			if (source.EndsWith("O")) list.Add(source.Substring(0, source.Length - 1) + "I");

			return list;
		}

		/// <summary>
		/// Returns a suitable table name on the data link based upon the given source, or null
		/// if no table can be found.
		/// </summary>
		internal static string LocateTableName(IDataLink link, string source)
		{
			if (link == null) throw new ArgumentNullException("link", "Data link cannot be null.");
			source = source.Validated("Type Name Source");

			var names = new List<string>();
			names.Add(source);
			names.AddRange(TableNameCandidatesPlural(source));

			foreach (var name in names)
			{
				var found = false;
				var cmd = link.From(x => name).Top(1);
				var rec = (IRecord)null;
				var opened = link.IsOpen;

				try { rec = (IRecord)cmd.First(); found = true; }
				catch { }
				finally
				{
					if (rec != null) rec.Dispose(disposeSchema: true);
					if (cmd != null) cmd.Dispose();
					if (!opened) link.Close();
				}

				if (found) return name;
			}

			return null;
		}
	}
}
