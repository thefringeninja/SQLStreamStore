namespace SqlStreamStore.HAL;

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

internal struct ETag : IEquatable<ETag> {
	private readonly string _value;

	public static ETag FromPosition(long position) => new($@"""{position}""");
	public static ETag FromStreamVersion(int streamVersion) => new($@"""{streamVersion}""");
	public static readonly ETag None = default;

	private ETag(string value) {
		ArgumentNullException.ThrowIfNull(value);

		if (value[0] != '"' || value[value.Length - 1] != '"') {
			throw new ArgumentException("ETags bust be enclosed in double quotes.", nameof(value));
		}

		_value = value;
	}

	public bool Equals(ETag other) => string.Equals(_value, other._value);
	public override bool Equals(object? obj) => obj is ETag other && Equals(other);
	public override int GetHashCode() => _value.GetHashCode();
	public static bool operator ==(ETag left, ETag right) => left.Equals(right);
	public static bool operator !=(ETag left, ETag right) => !left.Equals(right);
	public static implicit operator string(ETag etag) => etag._value;
	public static implicit operator string[](ETag etag) => new[] { etag._value };
	public static implicit operator StringValues(ETag etag) => new(etag._value);

	public static implicit operator KeyValuePair<string, string[]>(ETag etag)
		=> new(Constants.Headers.ETag, new[] { etag._value });
}
