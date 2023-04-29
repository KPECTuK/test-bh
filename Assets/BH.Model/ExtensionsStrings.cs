using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BH.Model
{
	public static class ExtensionsStrings
	{
		/// <summary> spotted result: success </summary>
		public const string LOG_SPR_SUCCESS_S = "<color=lime>success</color>";
		/// <summary> spotted result: fail </summary>
		public const string LOG_SPR_FAIL_S = "<color=red>fail</color>";
		/// <summary> spotted result: fail </summary>
		public const string LOG_SPR_EMPTY_S = "<color=red>(null)</color>";

		private static int _indent;

		private class Indent : IDisposable
		{
			public Indent()
			{
				_indent += 2;
			}

			public void Dispose()
			{
				_indent -= 2;
			}
		}

		public static IDisposable WithGlobalIndent()
		{
			return new Indent();
		}

		private static int _prefixSize;

		private static string LogPrefix(string source)
		{
			var stack = new StackTrace();
			var frame = stack.GetFrame(2);
			var info = frame.GetMethod();
			var prefix = $"{info.DeclaringType?.NameNice() ?? "unknown"}.{info.Name}";
			_prefixSize = _prefixSize > prefix.Length ? _prefixSize : prefix.Length;
			var builder = new StringBuilder()
				.Append("<color=cyan>[")
				.AppendFormat($"{{0,{_prefixSize}}}", prefix)
				.Append("(..)]</color> ")
				.Append(new string(' ', _indent))
				.Append(source);
			return builder.ToString();
		}

		private static string LogWrap(string source)
		{
			var isDebug = source.Contains("[?]") || source.Contains("[DEBUG]");
			return isDebug ? $"<color=magenta>{source}</color>" : source;
		}

		private static string LogFilter(string source)
		{
			source ??= LOG_SPR_EMPTY_S;
			return source;
		}

		private static StringBuilder LogPrefix(StringBuilder source)
		{
			var stack = new StackTrace();
			var frame = stack.GetFrame(2);
			var info = frame.GetMethod();
			var prefix = $"{info.DeclaringType?.Name ?? "unknown"}.{info.Name}";
			_prefixSize = _prefixSize > prefix.Length ? _prefixSize : prefix.Length;
			var builder = new StringBuilder()
				.Append("<color=cyan>[")
				.AppendFormat($"{{0,{_prefixSize}}}", prefix)
				.Append("(..)]</color> ");
			return source.Insert(0, builder.ToString());
		}

		private static StringBuilder LogWrap(StringBuilder source)
		{
			const string CASE_01 = "[?]";
			var case01Num = 0;
			const string CASE_02 = "[DEBUG]";
			var case02Num = 0;

			for(var index = 0; index < source.Length; index++)
			{
				if(case01Num != CASE_01.Length && source[index] == CASE_01[case01Num])
				{
					case01Num++;
				}
				if(case02Num != CASE_02.Length && source[index] == CASE_02[case02Num])
				{
					case02Num++;
				}
			}

			var isDebug =
				case01Num == CASE_01.Length ||
				case02Num == CASE_02.Length;

			if(isDebug)
			{
				source.Insert(0, "<color=magenta>").Append("</color>");
			}

			return source;
		}

		private static StringBuilder LogFilter(StringBuilder source)
		{
			source ??= new StringBuilder(LOG_SPR_EMPTY_S);
			return source;
		}

		[Conditional("DEBUG")]
		public static void Log(this StringBuilder source)
		{
			source = LogFilter(source);
			source = LogPrefix(source);
			source = LogWrap(source);
			UnityEngine.Debug.Log(source.ToString());
		}

		[Conditional("DEBUG")]
		public static void Log(this string source)
		{
			source = LogFilter(source);
			source = LogPrefix(source);
			source = LogWrap(source);
			UnityEngine.Debug.Log(source);
		}

		[Conditional("DEBUG")]
		public static void LogWarning(this StringBuilder source)
		{
			source = LogFilter(source);
			source = LogPrefix(source);
			source = LogWrap(source);
			UnityEngine.Debug.LogWarning(source.ToString());
		}

		[Conditional("DEBUG")]
		public static void LogWarning(this string source)
		{
			source = LogFilter(source);
			source = LogPrefix(source);
			source = LogWrap(source);
			UnityEngine.Debug.LogWarning(source);
		}

		public static void LogError(this StringBuilder source)
		{
			source = LogFilter(source);
			source = LogPrefix(source);
			source = LogWrap(source);
			UnityEngine.Debug.LogError(source.ToString());
		}

		public static void LogError(this string source)
		{
			source = LogFilter(source);
			source = LogPrefix(source);
			source = LogWrap(source);
			UnityEngine.Debug.LogError(source);
		}

		public static string ToText<T>(this IEnumerable<T> source, string header = null, Func<T, string> renderer = null)
		{
			var rows = 0;
			var builder = new StringBuilder();
			renderer ??= _ => _.ToString();
			if(source == null)
			{
				return
					(header != null
						? $"{header} [rows: 0] [null]"
						: "[null]")
					.TrimEnd(Environment.NewLine.ToCharArray());
			}

			if(source is string @string)
			{
				using var reader = new StringReader(@string);
				string buffer;
				var lineNumber = -1;
				builder
					.AppendLine()
					.Append($"  {++lineNumber:D2}: ")
					.Append($"{new string(' ', 6)}10 |")
					.Append($"{new string(' ', 6)}20 |")
					.Append($"{new string(' ', 6)}30 |")
					.AppendLine();
				while((buffer = reader.ReadLine()) != null)
				{
					builder
						.Append($"  {++lineNumber:D2}: ")
						.Append(buffer)
						.AppendLine();
				}
			}
			else
			{
				foreach(var item in source)
				{
					builder.AppendLine(item == null
						? "[null]"
						: $"{++rows:D3}: {renderer(item)}");
				}
			}

			return
				(header != null
					? $"{header} [rows: {rows}]\n{(builder.Length == 0 ? "[empty]" : builder.ToString())}"
					: builder.ToString())
				.TrimEnd(Environment.NewLine.ToCharArray());
		}

		public static string ToText(this IEnumerable source, string header = null, Func<object, string> renderer = null)
		{
			return ToText(source.Cast<object>(), header, renderer);
		}

		public static string ToText<T>(this T exception) where T : Exception
		{
			if(ReferenceEquals(null, exception))
			{
				return "<exception is null>";
			}

			var builder = new StringBuilder();

			void Splitter(string message, string indent)
			{
				var split = message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
				for(var index = 0; index < split.Length; index++)
				{
					builder.Append(indent);
					builder.Append("  ");
					builder.Append(split[index].TrimStart(' '));
					builder.AppendLine();
				}
			}

			var current = exception as Exception;
			var iterations = 2;
			while(current != null)
			{
				var indent = new string(' ', iterations);
				builder.Append(indent);
				builder.Append(typeof(T));
				builder.AppendLine();

				if(!ReferenceEquals(null, current.Message))
				{
					builder.Append(indent);
					builder.AppendLine("Message:");
					Splitter(current.Message, indent);
				}

				if(!ReferenceEquals(null, current.StackTrace))
				{
					builder.Append(indent);
					builder.AppendLine("Stack Trace:");
					Splitter(current.StackTrace, indent);
				}

				builder.Append(indent);
				builder.AppendLine("--");
				// iterations += 2;
				current = current.InnerException;
			}

			return builder.ToString();
		}

		// ReSharper disable once StringLiteralTypo
		private static readonly char[] _base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();

		public static string MakeUnique(this string source, int length = 6, int @base = 0)
		{
			@base = @base == 0 ? _base62Chars.Length : @base;
			var builder = new StringBuilder(source, source.Length + length);
			for(var ctr = 0; ctr < length; ctr++)
			{
				var index = Mathf.FloorToInt(UnityEngine.Random.value * @base) % _base62Chars.Length;
				builder.Append(index);
			}

			return builder.ToString();
		}

		public static int HashLy(this string source)
		{
			uint hash = 0;
			var sequence = Encoding.Unicode.GetBytes(source);
			sequence = sequence.Length > 0
				? sequence
				: Encoding.Unicode.GetBytes("undefined_".MakeUnique(24));
			unchecked
			{
				// ReSharper disable once unknown
				for(var ctr = 0; ctr < sequence.Length; ctr++)
				{
					hash = hash * 1664525 + sequence[ctr] + 1013904223;
				}
			}

			return (int)hash;
		}

		private static readonly Regex _typeTemplate = new("(?'name'[^`]*)");

		public static string DumpType(this Type source)
		{
			var builder = new StringBuilder();
			while(source != null)
			{
				builder
					.Append(source == typeof(object) ? "[O]" : source.NameNice())
					.Append(source == typeof(object) ? string.Empty : " > ");
				source = source.BaseType;
			}

			return builder.ToString();
		}

		private static string WriteArgs(Type type)
		{
			return
				type.IsGenericType
					? type.GetGenericArguments()
						.Aggregate(new StringBuilder(), (builder, _) => builder.Append(NameNice(_) + ", "))
						.ToString()
						.TrimEnd(", ".ToArray())
					: string.Empty;
		}

		public static string NameNice(this Type source)
		{
			if(source == null)
			{
				return "null";
			}

			return
				source.IsGenericType
					? $"{_typeTemplate.Match(source.Name).Groups["name"].Value}<{WriteArgs(source)}>"
					: source.Name;
		}

		private static readonly Regex _color = new("#?(?'r'[a-fA-F0-9]{2})(?'g'[a-fA-F0-9]{2})(?'b'[a-fA-F0-9]{2})(?'a'[a-fA-F0-9]{2})?");

		public static Color ToColor(this string source)
		{
			var match = _color.Match(source);
			
			if(match.Success)
			{
				var r = byte.Parse(match.Groups["r"].Value, NumberStyles.HexNumber) / 255f;
				var g = byte.Parse(match.Groups["g"].Value, NumberStyles.HexNumber) / 255f;
				var b = byte.Parse(match.Groups["b"].Value, NumberStyles.HexNumber) / 255f;
				var a = match.Groups["a"].Success ? byte.Parse(match.Groups["a"].Value, NumberStyles.HexNumber) / 255f : 1f;
				return new Color(r, g, b, a);
			}

			return Color.magenta;
		}

		private static readonly Regex _nameFilter = new(@"((\\s|_|\.)?\(Clone\))?", RegexOptions.Compiled);

		public static string CleanUpName(this string source)
		{
			return _nameFilter.Replace(source, string.Empty);
		}
	}
}
