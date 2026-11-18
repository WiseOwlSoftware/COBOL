// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json.Serialization;

namespace WiseOwl.Cobol.Listing.Json;

/// <summary>
/// The JSON listing's source-generated metadata (DESIGN-listing.md section 7.3): camelCase names, a null left out, nullable
/// annotations respected (a null where the model has none is refused on reading), and an indented document with LF line
/// ends. It is the base of the one contract, <see cref="ListingJson"/>, which adds the contract's rules (required properties,
/// no null element, enum values as names); nothing serializes through this context alone, so it is internal.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    RespectNullableAnnotations = true,
    WriteIndented = true,
    NewLine = "\n")]
[JsonSerializable(typeof(ListingDocument))]
internal sealed partial class ListingJsonContext : JsonSerializerContext;
