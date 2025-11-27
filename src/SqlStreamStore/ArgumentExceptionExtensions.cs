// ReSharper disable once CheckNamespace

namespace System;

using System.Runtime.CompilerServices;

internal static class ArgumentExceptionExtensions {
	extension(ArgumentException) {
		public static void ThrowIfContainsWhiteSpace(
			ReadOnlySpan<char> value,
			[CallerArgumentExpression(nameof(value))]
			string? paramName = null) {
			foreach (var c in value) {
				if (!char.IsWhiteSpace(c))
					continue;
				throw new ArgumentException($"May not contain whitespace", paramName);
			}
		}

		public static void ThrowIfStartsWith(
			ReadOnlySpan<char> value,
			char startsWith,
			[CallerArgumentExpression(nameof(value))]
			string? paramName = null) {
			if (!value.StartsWith(startsWith)) {
				return;
			}

			throw new ArgumentException($"Cannot start with {startsWith}");
		}
	}
}
