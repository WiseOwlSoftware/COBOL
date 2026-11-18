// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace WiseOwl.Cobol.Tests.Shared;

/// <summary>
/// A validator of a JSON instance against a JSON Schema (draft 2020-12) restricted to the keywords
/// <c>System.Text.Json.Schema.JsonSchemaExporter</c> writes for the listing's contract (DESIGN-listing.md section 10 item 5):
/// <c>type</c>, <c>properties</c>, <c>required</c>, <c>items</c>, <c>enum</c>, <c>format</c> (<c>date-time</c>) and the
/// annotations. A keyword outside that set is itself an error, never skipped, so a schema this validator does not fully
/// understand cannot pass an instance by default.
/// </summary>
internal static class JsonSchemaSubset
{
    private static readonly HashSet<string> Annotations = ["$schema", "$id", "title", "description"];

    /// <summary>Every way <paramref name="instance"/> breaks <paramref name="schema"/>, each naming its path (<c>$.lines[0].kind</c>).</summary>
    public static IReadOnlyList<string> Validate(JsonNode schema, JsonNode? instance)
    {
        var errors = new List<string>();
        Check(schema, instance, "$", errors);
        return errors;
    }

    private static void Check(JsonNode? schemaNode, JsonNode? instance, string path, List<string> errors)
    {
        if (schemaNode is JsonValue v && v.TryGetValue(out bool any))
        {
            if (!any) errors.Add($"{path}: the schema admits nothing here");
            return;
        }
        if (schemaNode is not JsonObject schema)
        {
            errors.Add($"{path}: the schema node is neither an object nor a boolean");
            return;
        }
        foreach (var (keyword, value) in schema)
        {
            switch (keyword)
            {
                case "type":
                    string[] types = value is JsonArray a ? [.. a.Select(t => (string)t!)] : [(string)value!];
                    if (!types.Contains(TypeOf(instance), StringComparer.Ordinal)
                        && !(TypeOf(instance) == "integer" && types.Contains("number", StringComparer.Ordinal)))
                        errors.Add($"{path}: a {TypeOf(instance)} where the schema allows {string.Join(" or ", types)}");
                    break;
                case "enum":
                    if (!value!.AsArray().Any(e => JsonNode.DeepEquals(e, instance)))
                        errors.Add($"{path}: {instance?.ToJsonString() ?? "null"} is not one of {value.ToJsonString()}");
                    break;
                case "format":
                    if ((string?)value != "date-time") errors.Add($"{path}: the format {value} is not one this validator checks");
                    else if (instance is JsonValue s && s.TryGetValue(out string? text)
                        && !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                        errors.Add($"{path}: '{text}' is not a date-time");
                    break;
                case "required":
                    if (instance is JsonObject required)
                        foreach (var name in value!.AsArray().Select(n => (string)n!))
                            if (!required.ContainsKey(name)) errors.Add($"{path}: the required property '{name}' is missing");
                    break;
                case "properties":
                    if (instance is JsonObject obj)
                        foreach (var (name, propertySchema) in value!.AsObject())
                            if (obj.TryGetPropertyValue(name, out var property))
                                Check(propertySchema, property, path + "." + name, errors);
                    break;
                case "items":
                    if (instance is JsonArray array)
                        for (int i = 0; i < array.Count; i++)
                            Check(value, array[i], path + "[" + i.ToString(CultureInfo.InvariantCulture) + "]", errors);
                    break;
                default:
                    if (!Annotations.Contains(keyword)) errors.Add($"{path}: the schema keyword '{keyword}' is not one this validator checks");
                    break;
            }
        }
    }

    private static string TypeOf(JsonNode? node) => node?.GetValueKind() switch
    {
        null or JsonValueKind.Null => "null",
        JsonValueKind.Object => "object",
        JsonValueKind.Array => "array",
        JsonValueKind.String => "string",
        JsonValueKind.True or JsonValueKind.False => "boolean",
        JsonValueKind.Number => node.AsValue().TryGetValue(out long _) ? "integer" : "number",
        var kind => throw new InvalidOperationException($"no JSON type for {kind}"),
    };
}
