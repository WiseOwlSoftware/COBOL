// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json.Serialization;

namespace WiseOwl.Cobol.Listing.Json;

/// <summary>
/// ⛔ THE ONE SERIALIZATION CONTRACT OF THE JSON LISTING (DESIGN-listing.md section 7.3): a <c>System.Text.Json</c>
/// source-generation context, so the writer, the reader and the schema generator (<see cref="ListingJsonSchema"/>) see one
/// set of properties and no reflection runs: camelCase names, enums as their member names, a null left out, nullable
/// annotations respected (a null where the model has none is refused on reading), and an indented document with LF line ends.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    RespectNullableAnnotations = true,
    WriteIndented = true,
    NewLine = "\n")]
[JsonSerializable(typeof(ListingDocument))]
public sealed partial class ListingJsonContext : JsonSerializerContext;
