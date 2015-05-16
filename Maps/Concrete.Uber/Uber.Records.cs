using Kerosene.ORM.Core;
using Kerosene.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kerosene.ORM.Maps.Concrete
{
	// ====================================================
	internal static partial class Uber
	{
		/// <summary>
		/// Returns a list containing the entries for the identity columns, either primary key
		/// ones or unique valued ones, or null if no such columns can be found.
		/// </summary>
		internal static List<ISchemaEntry> IdentityList(this ISchema schema)
		{
			var list = schema.PrimaryKeyColumns().ToList();
			if (list.Count == 0) list.AddRange(schema.UniqueValuedColumns());

			return list.Count == 0 ? null : list;
		}


		/// <summary>
		/// Returns a normalized string containing the string representation of the identity
		/// columns, or null if no such identity can be found.
		/// </summary>
		internal static string IdentityString(this IRecord record)
		{
			if (record == null) return null;
			if (record.Schema == null) return null;

			var list = record.Schema.IdentityList(); if (list == null) return null;

			var first = true;
			var sb = new StringBuilder(); foreach (var entry in list)
			{
				if (first) first = false; else sb.Append("-");

				var index = record.Schema.IndexOf(entry);
				var value = record[index];
				sb.AppendFormat("[{0}]", value == null ? "ø" : value.Sketch());
			}

			return first ? null : sb.ToString();
		}
	}
}
