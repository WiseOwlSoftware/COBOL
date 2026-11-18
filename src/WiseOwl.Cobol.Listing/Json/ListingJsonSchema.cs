// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Xml.Linq;

namespace WiseOwl.Cobol.Listing.Json;

/// <summary>
/// ⛔ THE JSON LISTING'S SCHEMA (DESIGN-listing.md section 7.3): GENERATED from the one serialization contract
/// (<see cref="ListingJson"/>) by <see cref="JsonSchemaExporter"/>, with each description taken from the model's XML
/// documentation, and committed as <c>Json/listing-v1.schema.json</c>, which this assembly embeds (<see cref="Committed"/>).
/// The schema therefore cannot drift from the writer: the Unit assembly's <c>ListingJsonSchemaDriftTests</c> regenerates it
/// and is red on any difference from the committed file.
/// </summary>
public static class ListingJsonSchema
{
    /// <summary>The committed schema's file name; the major is in the name (DESIGN-listing.md section 7.3).</summary>
    public const string FileName = "listing-v1.schema.json";

    /// <summary>The JSON Schema dialect the exporter writes.</summary>
    public const string Dialect = "https://json-schema.org/draft/2020-12/schema";

    private const string ResourceName = "WiseOwl.Cobol.Listing.Json." + FileName;

    /// <summary>The schema this compiler's JSON listings follow, as committed and embedded in this assembly.</summary>
    public static string Committed()
    {
        using var stream = typeof(ListingJsonSchema).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"the assembly embeds no resource {ResourceName}");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Generate the schema from the contract (<see cref="ListingJson.Document"/>), describing each property, type and enum value from
    /// <paramref name="documentation"/>, the XML documentation file this assembly is built with. A property or type the
    /// documentation does not describe is refused, never published without its meaning.
    /// </summary>
    /// <exception cref="InvalidOperationException">A property, type or enum value of the contract has no documentation.</exception>
    public static string Generate(XDocument documentation)
    {
        var docs = new XmlDocumentation(documentation);
        var exporter = new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = (context, node) => Describe(context, node, docs),
        };
        var generated = ListingJson.Document.GetJsonSchemaAsNode(exporter).AsObject();
        var schema = new JsonObject
        {
            ["$schema"] = Dialect,
            ["$id"] = ListingDocument.SchemaUrn,
            ["title"] = "WiseOwl COBOL source listing, schema version " + ListingDocument.CurrentSchemaVersion,
        };
        foreach (string name in generated.Select(p => p.Key).ToList())
        {
            var value = generated[name];
            generated.Remove(name);
            schema[name] = value;
        }
        return schema.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true,
            NewLine = "\n",
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,   // a file, not HTML: text as written
        }) + "\n";
    }

    /// <summary>Put the contract on one schema node: an enum of the model is its members' names (the contract's own
    /// converter, which the exporter cannot describe, writes exactly those), an object's <c>required</c> is the contract's
    /// required properties (<see cref="ListingJson.IsRequired"/>, the reader's one rule), and the documentation's description
    /// goes first: a property's from its member (or its record parameter), a type's from its summary, and for an enum each
    /// value's meaning after it.</summary>
    private static JsonNode Describe(JsonSchemaExporterContext context, JsonNode node, XmlDocumentation docs)
    {
        var type = Nullable.GetUnderlyingType(context.TypeInfo.Type) ?? context.TypeInfo.Type;
        bool modelEnum = type.IsEnum && IsModelType(type);
        if (modelEnum)
        {
            var names = new JsonArray([.. Enum.GetNames(type).Select(n => (JsonNode?)JsonValue.Create(n))]);
            if (type != context.TypeInfo.Type) names.Add(null);
            node = new JsonObject { ["enum"] = names };
        }
        if (node is not JsonObject schema) return node;
        string? description = context.PropertyInfo is { } property
            ? docs.Property(ClrProperty(property))
            : IsModelType(type) ? docs.Type(type) : null;
        if (modelEnum)
            description = (description is null ? "" : description + " ")
                + string.Join(" ", Enum.GetNames(type).Select(n => n + ": " + docs.EnumValue(type, n)));
        if (description is not null) schema.Insert(0, "description", description);
        if (context.TypeInfo.Kind == JsonTypeInfoKind.Object)
        {
            schema.Remove("required");
            var required = context.TypeInfo.Properties.Where(ListingJson.IsRequired).Select(p => (JsonNode?)p.Name).ToArray();
            if (required.Length > 0) schema["required"] = new JsonArray(required);
        }
        return schema;
    }

    /// <summary>Whether <paramref name="type"/> is a type of the model (the listing's own), not a framework type such as a list
    /// or a string, which the documentation does not describe.</summary>
    private static bool IsModelType(Type type) => type.Assembly == typeof(ListingDocument).Assembly;

    /// <summary>The CLR property a contract property serializes: the one whose JSON name, under the contract's naming policy or
    /// its own <see cref="JsonPropertyNameAttribute"/>, is the contract property's.</summary>
    private static PropertyInfo ClrProperty(JsonPropertyInfo property) =>
        property.DeclaringType.GetProperties(BindingFlags.Public | BindingFlags.Instance).SingleOrDefault(p =>
            (p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? JsonNamingPolicy.CamelCase.ConvertName(p.Name)) == property.Name)
        ?? throw new InvalidOperationException($"no property of {property.DeclaringType} serializes as '{property.Name}'");

    /// <summary>The model's XML documentation, read by member id (<c>T:</c>, <c>P:</c>, <c>F:</c>), each entry's text flattened
    /// to one line: a <c>see</c> reference becomes the name it names, a <c>c</c> element its text.</summary>
    private sealed class XmlDocumentation(XDocument documentation)
    {
        private readonly Dictionary<string, XElement> _members = documentation.Descendants("member")
            .Where(m => m.Attribute("name") is not null)
            .ToDictionary(m => (string)m.Attribute("name")!, StringComparer.Ordinal);

        public string Type(Type type) => Summary("T:" + type.FullName)
            ?? throw Undocumented("type " + type.FullName);

        public string EnumValue(Type type, string member) => Summary("F:" + type.FullName + "." + member)
            ?? throw Undocumented("enum value " + type.FullName + "." + member);

        /// <summary>A property's description: its own summary, or the <c>param</c> of the positional record that declares it.</summary>
        public string Property(PropertyInfo property)
        {
            var declaring = property.DeclaringType!;
            if (Summary("P:" + declaring.FullName + "." + property.Name) is { } own) return own;
            if (_members.TryGetValue("T:" + declaring.FullName, out var typeDoc)
                && typeDoc.Elements("param").FirstOrDefault(p => (string?)p.Attribute("name") == property.Name) is { } param)
                return Flatten(param);
            throw Undocumented("property " + declaring.FullName + "." + property.Name);
        }

        private string? Summary(string id) =>
            _members.TryGetValue(id, out var member) && member.Element("summary") is { } summary ? Flatten(summary) : null;

        private static InvalidOperationException Undocumented(string what) =>
            new($"the JSON listing's {what} has no XML documentation: document it, so the schema states its meaning");

        private static string Flatten(XElement element)
        {
            var text = new StringBuilder();
            foreach (var node in element.DescendantNodes())
            {
                if (node is XText t && t.Parent is { } parent && parent.Name.LocalName is not ("see" or "seealso" or "paramref" or "typeparamref"))
                    text.Append(t.Value);
                else if (node is XElement e && e.Name.LocalName is "see" or "seealso" or "paramref" or "typeparamref")
                    text.Append(e.HasElements || !string.IsNullOrEmpty(e.Value) ? e.Value : ReferenceName(e));
                else if (node is XElement { Name.LocalName: "para" }) text.Append(' ');
            }
            return string.Join(' ', text.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }

        private static string ReferenceName(XElement reference)
        {
            if ((string?)reference.Attribute("langword") is { } word) return word;
            if ((string?)reference.Attribute("name") is { } name) return name;
            string cref = (string?)reference.Attribute("cref") ?? "";
            cref = cref.Contains(':', StringComparison.Ordinal) ? cref[(cref.IndexOf(':', StringComparison.Ordinal) + 1)..] : cref;
            int paren = cref.IndexOf('(', StringComparison.Ordinal);
            if (paren >= 0) cref = cref[..paren];
            return cref[(cref.LastIndexOf('.') + 1)..];
        }
    }
}
