// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace CobolNet.Runtime.Collation.Cldr;

/// <summary>
/// The parser of CLDR collation data: the LDML XML file (<c>common/collation/&lt;locale&gt;.xml</c> — the
/// <c>&lt;identity&gt;</c>, <c>&lt;defaultCollation&gt;</c> and every <c>&lt;collation type="…"&gt;&lt;cr&gt;</c>), its
/// JSON mirror (<see cref="ParseJson"/>; the shape is documented in Collation/CLDR/README.md — CLDR itself publishes
/// no JSON for collation), and the RULE SYNTAX inside them (<see cref="ParseRules"/>: UTS #35 Part 5 "Collation
/// Tailorings" / the ICU rule syntax — resets, the five relation operators, starred relations with ranges, prefix
/// contexts, extensions, quoting and escapes, comments, the <c>[setting …]</c> options, logical reset positions,
/// <c>[import …]</c>, and the UnicodeSet arguments of <c>[suppressContractions]</c> / <c>[optimize]</c>).
/// <para>Parsing is TOTAL over CLDR release-48-2's 135 files (a drift test parses every embedded file); an unknown
/// setting is recorded, never silently dropped.</para>
/// </summary>
public static class CldrParser
{
    // ---- LDML XML -----------------------------------------------------------------------------------------------

    /// <summary>Parse an LDML collation file.</summary>
    public static CldrLocaleData ParseXml(Stream stream, string source)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, IgnoreComments = true, IgnoreWhitespace = true, XmlResolver = null };
        using var reader = XmlReader.Create(stream, settings);
        return ParseXml(reader, source);
    }

    /// <summary>Parse an LDML collation file from text.</summary>
    public static CldrLocaleData ParseXml(string xml, string source)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, IgnoreComments = true, IgnoreWhitespace = true, XmlResolver = null };
        using var reader = XmlReader.Create(new StringReader(xml), settings);
        return ParseXml(reader, source);
    }

    private static CldrLocaleData ParseXml(XmlReader reader, string source)
    {
        var doc = XDocument.Load(reader, LoadOptions.None);
        var ldml = doc.Root ?? throw new FormatException($"{source}: empty document");
        var identity = ldml.Element("identity");
        string language = identity?.Element("language")?.Attribute("type")?.Value ?? "root";
        string? script = identity?.Element("script")?.Attribute("type")?.Value;
        string? territory = identity?.Element("territory")?.Attribute("type")?.Value;
        string? variant = identity?.Element("variant")?.Attribute("type")?.Value;
        string? version = identity?.Element("version")?.Attribute("number")?.Value;
        string? defaultCollation = null;
        var collations = new List<CldrCollation>();
        foreach (var collationsElement in ldml.Elements("collations"))
        {
            defaultCollation ??= collationsElement.Element("defaultCollation")?.Value.Trim();
            foreach (var c in collationsElement.Elements("collation"))
            {
                string type = c.Attribute("type")?.Value ?? "standard";
                var rulesText = new StringBuilder();
                foreach (var cr in c.Elements("cr")) rulesText.Append(cr.Value).Append('\n');
                collations.Add(MakeCollation(type, c.Attribute("alt")?.Value, c.Attribute("draft")?.Value, c.Attribute("references")?.Value, rulesText.ToString()));
            }
        }
        string tag = language == "root" ? "root" : string.Join('-', new[] { language, script, territory, variant }.Where(x => !string.IsNullOrEmpty(x))!);
        return new CldrLocaleData(tag, language, script, territory, variant, source, version, defaultCollation, collations);
    }

    // ---- JSON mirror -----------------------------------------------------------------------------------------------

    /// <summary>Parse the JSON mirror of a collation file (README §"JSON form"): an object with <c>locale</c>
    /// (or <c>language</c>/<c>script</c>/<c>territory</c>/<c>variant</c>), optional <c>version</c>, and
    /// <c>collations</c> — either an object keyed by type (<c>{"standard": {"rules": "&amp;N&lt;ñ"}, "defaultCollation":
    /// "standard"}</c>) or an array of <c>{"type": …, "rules": …}</c> objects; each collation object may carry
    /// <c>alt</c>, <c>draft</c>, <c>references</c>.</summary>
    public static CldrLocaleData ParseJson(string json, string source)
    {
        ArgumentNullException.ThrowIfNull(json);
        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new FormatException($"{source}: the JSON root must be an object");
        string? Str(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        string? language = Str(root, "language"), script = Str(root, "script"), territory = Str(root, "territory"), variant = Str(root, "variant");
        string? locale = Str(root, "locale");
        if (language is null && locale is not null)
        {
            var parts = locale.Replace('_', '-').Split('-');
            language = parts[0];
            foreach (var p in parts.Skip(1))
            {
                if (p.Length == 4 && char.IsLetter(p[0])) script ??= p;
                else if (p.Length is 2 or 3 && territory is null && (p.All(char.IsUpper) || p.All(char.IsDigit))) territory = p;
                else variant ??= p;
            }
        }
        language ??= "root";
        string? version = Str(root, "version");
        string? defaultCollation = null;
        var collations = new List<CldrCollation>();
        if (root.TryGetProperty("collations", out var coll))
        {
            if (coll.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in coll.EnumerateObject())
                {
                    if (prop.NameEquals("defaultCollation")) { defaultCollation = prop.Value.GetString(); continue; }
                    if (prop.Value.ValueKind == JsonValueKind.String) { collations.Add(MakeCollation(prop.Name, null, null, null, prop.Value.GetString() ?? "")); continue; }
                    if (prop.Value.ValueKind != JsonValueKind.Object) throw new FormatException($"{source}: collation '{prop.Name}' must be an object or a rules string");
                    collations.Add(MakeCollation(Str(prop.Value, "type") ?? prop.Name, Str(prop.Value, "alt"), Str(prop.Value, "draft"), Str(prop.Value, "references"), Str(prop.Value, "rules") ?? ""));
                }
            }
            else if (coll.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in coll.EnumerateArray())
                    collations.Add(MakeCollation(Str(e, "type") ?? "standard", Str(e, "alt"), Str(e, "draft"), Str(e, "references"), Str(e, "rules") ?? ""));
            }
            else throw new FormatException($"{source}: 'collations' must be an object or an array");
        }
        defaultCollation ??= Str(root, "defaultCollation");
        string tag = language == "root" ? "root" : string.Join('-', new[] { language, script, territory, variant }.Where(x => !string.IsNullOrEmpty(x))!);
        return new CldrLocaleData(tag, language, script, territory, variant, source, version, defaultCollation, collations);
    }

    private static CldrCollation MakeCollation(string type, string? alt, string? draft, string? references, string rulesText)
    {
        var rules = ParseRules(rulesText, out var settings, out var imports, out var unsupported);
        return new CldrCollation(type, alt, draft, references, rulesText, rules, settings, imports, unsupported);
    }

    // ---- the rule syntax -----------------------------------------------------------------------------------------

    /// <summary>Parse a rules text into resets/relations/import positions, its settings, its imports and the list of
    /// unsupported constructs. Malformed syntax is a <see cref="FormatException"/> naming the line.</summary>
    public static IReadOnlyList<CldrRule> ParseRules(string text, out CldrSettings settings, out IReadOnlyList<CldrImport> imports, out IReadOnlyList<string> unsupported)
    {
        var p = new RuleParser(text ?? "");
        p.Run();
        settings = p.Settings;
        imports = p.Imports;
        unsupported = p.Unsupported;
        return p.Rules;
    }

    /// <summary>The characters that end an unquoted string in the rule syntax: the ASCII syntax characters and rule
    /// white space.</summary>
    private static bool IsSyntax(char c) => c is '&' or '<' or '=' or '/' or '|' or '[' or ']' or '\'' or '#' or '*' or '"' or '@' or '!' or '-' or '$' or '%' or '^' or '(' or ')' or '{' or '}' or ';' or ',' or '.' or ':' or '?' or '`' or '~' or '+' or '\\'
        || IsRuleWhiteSpace(c);

    /// <summary>Unicode <c>Pattern_White_Space</c> — what the rule syntax skips between tokens: TAB..CR, SPACE, NEL,
    /// LEFT-TO-RIGHT MARK, RIGHT-TO-LEFT MARK (Arabic files put them between rules), LINE SEPARATOR, PARAGRAPH SEPARATOR.</summary>
    private static bool IsRuleWhiteSpace(char c) => c is (>= '\t' and <= '\r') or ' ' or '\u0085' or '\u200E' or '\u200F' or '\u2028' or '\u2029';

    /// <summary>The characters ICU requires to be quoted when they occur in a rule string, and that
    /// <see cref="Quote"/> therefore quotes: the syntax characters and whitespace.</summary>
    internal static string Quote(string s)
    {
        if (s.Length == 0) return "''";
        bool needs = false;
        foreach (char c in s) if (IsSyntax(c)) { needs = true; break; }
        if (!needs) return s;
        var sb = new StringBuilder("'");
        foreach (char c in s) { if (c == '\'') sb.Append("''"); else sb.Append(c); }
        return sb.Append('\'').ToString();
    }

    internal static string Operator(CldrRelationStrength s) => s switch
    {
        CldrRelationStrength.Primary => "<",
        CldrRelationStrength.Secondary => "<<",
        CldrRelationStrength.Tertiary => "<<<",
        CldrRelationStrength.Quaternary => "<<<<",
        _ => "=",
    };

    internal static string PositionName(CldrSpecialPosition p) => p switch
    {
        CldrSpecialPosition.FirstTertiaryIgnorable => "first tertiary ignorable",
        CldrSpecialPosition.LastTertiaryIgnorable => "last tertiary ignorable",
        CldrSpecialPosition.FirstSecondaryIgnorable => "first secondary ignorable",
        CldrSpecialPosition.LastSecondaryIgnorable => "last secondary ignorable",
        CldrSpecialPosition.FirstPrimaryIgnorable => "first primary ignorable",
        CldrSpecialPosition.LastPrimaryIgnorable => "last primary ignorable",
        CldrSpecialPosition.FirstVariable => "first variable",
        CldrSpecialPosition.LastVariable => "last variable",
        CldrSpecialPosition.FirstRegular => "first regular",
        CldrSpecialPosition.LastRegular => "last regular",
        CldrSpecialPosition.FirstImplicit => "first implicit",
        CldrSpecialPosition.LastImplicit => "last implicit",
        CldrSpecialPosition.FirstTrailing => "first trailing",
        _ => "last trailing",
    };

    private static bool TryParsePosition(string name, out CldrSpecialPosition position)
    {
        string n = string.Join(' ', name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
        foreach (CldrSpecialPosition p in Enum.GetValues<CldrSpecialPosition>())
            if (PositionName(p) == n) { position = p; return true; }
        position = default;
        return false;
    }

    /// <summary>Parse the (subset of the) UnicodeSet syntax CLDR's collation files use for <c>[suppressContractions]</c>
    /// and <c>[optimize]</c>: <c>[</c> items <c>]</c> where an item is a character, an escape (<c>\uXXXX</c>,
    /// <c>\UXXXXXXXX</c>, <c>\x{…}</c>), a quoted string, a range <c>a-z</c>, or a nested set (union). Whitespace is
    /// ignored. Negation, properties and set operations are not part of the collation data and are rejected.</summary>
    public static IReadOnlyList<int> ParseUnicodeSet(string set)
    {
        ArgumentNullException.ThrowIfNull(set);
        var s = set.Trim();
        if (s.Length < 2 || s[0] != '[' || s[^1] != ']') throw new FormatException($"a UnicodeSet is bracketed: {s}");
        var result = new List<int>();
        int i = 0;
        ParseSetInto(s, ref i, result);
        return result.Distinct().OrderBy(x => x).ToArray();
    }

    private static void ParseSetInto(string s, ref int i, List<int> into)
    {
        if (s[i] != '[') throw new FormatException("expected '['");
        i++;
        if (i < s.Length && s[i] == '^') throw new FormatException("negated UnicodeSets are not supported in collation data");
        int? pending = null;
        bool rangeDash = false;
        while (i < s.Length)
        {
            char c = s[i];
            if (c == ']') { i++; if (pending is { } p) into.Add(p); return; }
            if (char.IsWhiteSpace(c)) { i++; continue; }
            if (c == '[')
            {
                if (pending is { } p) { into.Add(p); pending = null; }
                ParseSetInto(s, ref i, into);
                continue;
            }
            if (c == '\\' && i + 1 < s.Length && s[i + 1] == 'p') throw new FormatException("property sets are not supported in collation data");
            if (c == '-' && pending is not null && !rangeDash) { rangeDash = true; i++; continue; }
            int cp = ReadSetChar(s, ref i);
            if (rangeDash && pending is { } lo)
            {
                if (cp < lo) throw new FormatException($"UnicodeSet range out of order: {lo:X4}-{cp:X4}");
                for (int x = lo; x <= cp; x++) into.Add(x);
                pending = null;
                rangeDash = false;
                continue;
            }
            if (pending is { } prev) into.Add(prev);
            pending = cp;
        }
        throw new FormatException("unterminated UnicodeSet");
    }

    private static int ReadSetChar(string s, ref int i)
    {
        char c = s[i];
        if (c == '\\') return ReadEscape(s, ref i);
        if (c == '\'')
        {
            // A quoted character (rare in sets); '' is the apostrophe.
            if (i + 1 < s.Length && s[i + 1] == '\'') { i += 2; return '\''; }
            int close = s.IndexOf('\'', i + 1);
            if (close < 0) throw new FormatException("unterminated quote in UnicodeSet");
            string inner = s[(i + 1)..close];
            i = close + 1;
            return char.ConvertToUtf32(inner, 0);
        }
        int cp = char.ConvertToUtf32(s, i);
        i += cp > 0xFFFF ? 2 : 1;
        return cp;
    }

    /// <summary>The escape at <c>s[i]</c> (a backslash): <c>\uXXXX</c>, <c>\UXXXXXXXX</c>, <c>\x{H…}</c>, <c>\xHH</c>,
    /// or a backslash-quoted literal character.</summary>
    private static int ReadEscape(string s, ref int i)
    {
        if (i + 1 >= s.Length) throw new FormatException("dangling backslash");
        char e = s[i + 1];
        int cp;
        switch (e)
        {
            case 'u': cp = Hex(s, i + 2, 4); i += 6; return cp;
            case 'U': cp = Hex(s, i + 2, 8); i += 10; return cp;
            case 'x':
                if (i + 2 < s.Length && s[i + 2] == '{')
                {
                    int close = s.IndexOf('}', i + 3);
                    if (close < 0) throw new FormatException("unterminated \\x{…}");
                    cp = int.Parse(s.AsSpan(i + 3, close - i - 3), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    i = close + 1;
                    return cp;
                }
                cp = Hex(s, i + 2, 2); i += 4; return cp;
            default:
                cp = char.ConvertToUtf32(s, i + 1);
                i += 1 + (cp > 0xFFFF ? 2 : 1);
                return cp;
        }
    }

    private static int Hex(string s, int start, int count)
    {
        if (start + count > s.Length) throw new FormatException("truncated escape");
        return int.Parse(s.AsSpan(start, count), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    /// <summary>The recursive-descent parser over one rules text.</summary>
    private sealed class RuleParser(string text)
    {
        private readonly string _t = text;
        private int _i;
        private int _line = 1;

        public List<CldrRule> Rules { get; } = [];
        public List<CldrImport> Imports { get; } = [];
        public List<string> Unsupported { get; } = [];
        public CldrSettings Settings { get; private set; } = new();

        private bool _sawReset;

        public void Run()
        {
            while (true)
            {
                SkipSpaceAndComments();
                if (_i >= _t.Length) return;
                char c = _t[_i];
                int line = _line;
                if (c == '&') { _i++; ParseReset(line); continue; }
                if (c == '[') { ParseSetting(line); continue; }
                if (c == '<' || c == '=') { ParseRelation(line); continue; }
                throw Error($"unexpected '{c}' — a rule starts with '&', a relation operator or a '[setting]'");
            }
        }

        private void ParseReset(int line)
        {
            SkipSpaceAndComments();
            int before = 0;
            CldrSpecialPosition? position = null;
            string? text = null;
            if (Peek('['))
            {
                string content = ReadBracket();
                string trimmed = content.Trim();
                if (trimmed.StartsWith("before", StringComparison.OrdinalIgnoreCase))
                {
                    string n = trimmed[6..].Trim();
                    if (!int.TryParse(n, out before) || before is < 1 or > 3) throw Error($"[before n] needs n = 1, 2 or 3, not '{n}'");
                    SkipSpaceAndComments();
                    if (Peek('['))
                    {
                        string content2 = ReadBracket();
                        if (!TryParsePosition(content2, out var pos)) throw Error($"unknown reset position [{content2}]");
                        position = pos;
                    }
                    else text = ReadString(allowEmpty: false);
                }
                else if (TryParsePosition(trimmed, out var pos)) position = pos;
                else throw Error($"unknown reset position [{trimmed}]");
            }
            else text = ReadString(allowEmpty: false);
            Rules.Add(new CldrReset(text, position, before) { Line = line });
            _sawReset = true;
        }

        private void ParseRelation(int line)
        {
            if (!_sawReset) throw Error("a relation before any reset");
            CldrRelationStrength strength;
            if (_t[_i] == '=') { strength = CldrRelationStrength.Identity; _i++; }
            else
            {
                int n = 0;
                while (_i < _t.Length && _t[_i] == '<') { n++; _i++; }
                strength = n switch { 1 => CldrRelationStrength.Primary, 2 => CldrRelationStrength.Secondary, 3 => CldrRelationStrength.Tertiary, 4 => CldrRelationStrength.Quaternary, _ => throw Error("too many '<'") };
            }
            bool starred = false;
            if (_i < _t.Length && _t[_i] == '*') { starred = true; _i++; }
            SkipSpaceAndComments();
            if (starred)
            {
                // <* item item…: every item its own relation at this strength; an unquoted '-' between two items is a range.
                var items = ReadStarredItems();
                if (items.Count == 0) throw Error("a starred relation needs at least one character");
                foreach (int cp in items)
                    Rules.Add(new CldrRelation(strength, char.ConvertFromUtf32(cp), null, null) { Line = line });
                return;
            }
            string first = ReadString(allowEmpty: false);
            string? prefix = null, extension = null;
            SkipSpaceAndComments();
            string textStr = first;
            if (Peek('|'))
            {
                _i++;
                SkipSpaceAndComments();
                prefix = first;
                textStr = ReadString(allowEmpty: false);
                SkipSpaceAndComments();
            }
            if (Peek('/'))
            {
                _i++;
                SkipSpaceAndComments();
                extension = ReadString(allowEmpty: false);
            }
            Rules.Add(new CldrRelation(strength, textStr, prefix, extension) { Line = line });
        }

        private void ParseSetting(int line)
        {
            string content = ReadBracket();
            string trimmed = content.Trim();
            int sp = trimmed.IndexOfAny([' ', '\t', '\n', '\r', '[']);
            string keyword = sp < 0 ? trimmed : trimmed[..sp];
            string value = sp < 0 ? "" : trimmed[sp..].Trim();
            switch (keyword.ToLowerInvariant())
            {
                case "import":
                {
                    string v = value.Trim();
                    string locale = v, type = "standard";
                    int k = v.IndexOf("-u-co-", StringComparison.OrdinalIgnoreCase);
                    if (k >= 0) { locale = v[..k]; type = CldrCollation.CanonicalType(v[(k + 6)..]); }
                    if (locale.Equals("und", StringComparison.OrdinalIgnoreCase)) locale = "root";
                    var import = new CldrImport(locale.Replace('_', '-'), type);
                    Imports.Add(import);
                    Rules.Add(new CldrImportRule(import) { Line = line });
                    break;
                }
                case "strength":
                {
                    string v = value.ToLowerInvariant();
                    var s = v switch
                    {
                        "1" or "primary" => CollationStrength.Primary,
                        "2" or "secondary" => CollationStrength.Secondary,
                        "3" or "tertiary" => CollationStrength.Tertiary,
                        "4" or "quaternary" => CollationStrength.Quaternary,
                        "5" or "i" or "identical" => CollationStrength.Identical,
                        _ => throw Error($"[strength {value}] is not a strength"),
                    };
                    Settings = Settings with { Strength = s };
                    break;
                }
                case "alternate":
                    Settings = Settings with
                    {
                        Alternate = value.ToLowerInvariant() switch
                        {
                            "shifted" => AlternateHandling.Shifted,
                            "non-ignorable" or "nonignorable" => AlternateHandling.NonIgnorable,
                            _ => throw Error($"[alternate {value}] is not shifted / non-ignorable"),
                        },
                    };
                    break;
                case "maxvariable":
                    Settings = Settings with
                    {
                        MaxVariable = value.ToLowerInvariant() switch
                        {
                            "space" => MaxVariable.Space,
                            "punct" => MaxVariable.Punct,
                            "symbol" => MaxVariable.Symbol,
                            "currency" => MaxVariable.Currency,
                            _ => throw Error($"[maxVariable {value}] is not space / punct / symbol / currency"),
                        },
                    };
                    break;
                case "casefirst":
                    Settings = Settings with
                    {
                        CaseFirst = value.ToLowerInvariant() switch
                        {
                            "upper" => CaseFirst.Upper,
                            "lower" => CaseFirst.Lower,
                            "off" or "false" => CaseFirst.Off,
                            _ => throw Error($"[caseFirst {value}] is not upper / lower / off"),
                        },
                    };
                    break;
                case "backwards":
                    if (value.Trim() != "2") throw Error($"[backwards {value}] — only level 2 can be backwards");
                    Settings = Settings with { BackwardsSecondary = true };
                    break;
                case "normalization": Settings = Settings with { Normalization = OnOff(value, keyword) }; break;
                case "caselevel": Settings = Settings with { CaseLevel = OnOff(value, keyword) }; break;
                case "numericordering": Settings = Settings with { NumericOrdering = OnOff(value, keyword) }; break;
                case "hiraganaq": Settings = Settings with { HiraganaQuaternary = OnOff(value, keyword) }; break;
                case "reorder":
                    Settings = Settings with { Reorder = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries) };
                    break;
                case "suppresscontractions":
                    Settings = Settings with { SuppressContractions = ParseUnicodeSet(value) };
                    break;
                case "optimize":
                    Settings = Settings with { Optimize = ParseUnicodeSet(value) };
                    break;
                case "before":
                    throw Error("[before n] must follow '&'");
                default:
                    if (TryParsePosition(trimmed, out _)) throw Error($"the position [{trimmed}] must follow '&'");
                    Unsupported.Add($"line {line}: unknown setting [{trimmed}]");
                    break;
            }
        }

        private bool OnOff(string value, string keyword) => value.ToLowerInvariant() switch
        {
            "on" or "true" or "yes" => true,
            "off" or "false" or "no" => false,
            _ => throw Error($"[{keyword} {value}] is not on / off"),
        };

        // ---- lexical ----

        private bool Peek(char c) => _i < _t.Length && _t[_i] == c;

        private void SkipSpaceAndComments()
        {
            while (_i < _t.Length)
            {
                char c = _t[_i];
                if (c == '\n') { _line++; _i++; continue; }
                if (IsRuleWhiteSpace(c)) { _i++; continue; }
                if (c == '#')
                {
                    while (_i < _t.Length && _t[_i] != '\n') _i++;
                    continue;
                }
                return;
            }
        }

        /// <summary>The content between a '[' at the cursor and its matching ']' (nested brackets balanced), cursor after ']'.</summary>
        private string ReadBracket()
        {
            if (!Peek('[')) throw Error("expected '['");
            int start = _i + 1, depth = 0;
            for (int j = _i; j < _t.Length; j++)
            {
                char c = _t[j];
                if (c == '\n') _line++;
                if (c == '[') depth++;
                else if (c == ']' && --depth == 0)
                {
                    _i = j + 1;
                    return _t[start..j];
                }
            }
            throw Error("unterminated '['");
        }

        /// <summary>A rule string: unquoted characters up to the next syntax character or whitespace, quoted parts
        /// ('…', '' = apostrophe), and escapes, concatenated. Empty is an error unless allowed.</summary>
        private string ReadString(bool allowEmpty)
        {
            var sb = new StringBuilder();
            while (_i < _t.Length)
            {
                char c = _t[_i];
                if (c == '\'')
                {
                    if (_i + 1 < _t.Length && _t[_i + 1] == '\'') { sb.Append('\''); _i += 2; continue; }
                    int close = _t.IndexOf('\'', _i + 1);
                    if (close < 0) throw Error("unterminated quote");
                    string inner = _t[(_i + 1)..close];
                    _i = close + 1;
                    // Escapes ARE interpreted inside quotes in ICU rules (' ').
                    int k = 0;
                    while (k < inner.Length)
                    {
                        if (inner[k] == '\\') sb.Append(char.ConvertFromUtf32(ReadEscape(inner, ref k)));
                        else sb.Append(inner[k++]);
                    }
                    continue;
                }
                if (c == '\\') { sb.Append(char.ConvertFromUtf32(ReadEscape(_t, ref _i))); continue; }
                if (IsSyntax(c)) break;
                sb.Append(c);
                _i++;
            }
            if (sb.Length == 0 && !allowEmpty) throw Error("expected a string");
            return sb.ToString();
        }

        /// <summary>The items of a starred relation: characters (quoted, escaped or bare) with unquoted '-' ranges,
        /// up to the next operator / setting / reset / comment.</summary>
        private List<int> ReadStarredItems()
        {
            var items = new List<int>();
            int? pending = null;
            bool dash = false;
            while (_i < _t.Length)
            {
                char c = _t[_i];
                if (c == '\n') { _line++; _i++; continue; }
                if (IsRuleWhiteSpace(c)) { _i++; continue; }
                if (c is '&' or '<' or '=' or '[' or '#') break;
                if (c == '-' && pending is not null && !dash) { dash = true; _i++; continue; }
                int cp;
                if (c == '\'')
                {
                    if (_i + 1 < _t.Length && _t[_i + 1] == '\'') { cp = '\''; _i += 2; }
                    else
                    {
                        int close = _t.IndexOf('\'', _i + 1);
                        if (close < 0) throw Error("unterminated quote");
                        string inner = _t[(_i + 1)..close];
                        _i = close + 1;
                        // A quoted run in a starred relation is a sequence of single characters, each its own item.
                        int k = 0;
                        var runItems = new List<int>();
                        while (k < inner.Length)
                        {
                            if (inner[k] == '\\') runItems.Add(ReadEscape(inner, ref k));
                            else { int x = char.ConvertToUtf32(inner, k); k += x > 0xFFFF ? 2 : 1; runItems.Add(x); }
                        }
                        if (runItems.Count == 0) continue;
                        // Feed all but the last directly; the last becomes 'pending' so a following '-' can range from it.
                        for (int r = 0; r < runItems.Count - 1; r++) { Emit(runItems[r]); }
                        cp = runItems[^1];
                    }
                }
                else if (c == '\\') cp = ReadEscape(_t, ref _i);
                else if (IsSyntax(c)) throw Error($"unexpected '{c}' in a starred relation");
                else { cp = char.ConvertToUtf32(_t, _i); _i += cp > 0xFFFF ? 2 : 1; }
                Emit(cp);
            }
            if (pending is { } last) items.Add(last);
            return items;

            void Emit(int cp)
            {
                if (dash && pending is { } lo)
                {
                    if (cp < lo) throw Error($"range out of order in a starred relation: {lo:X4}-{cp:X4}");
                    for (int x = lo; x <= cp; x++) items.Add(x);
                    pending = null;
                    dash = false;
                    return;
                }
                if (pending is { } prev) items.Add(prev);
                pending = cp;
            }
        }

        private FormatException Error(string message) => new($"CLDR rules line {_line}: {message}");
    }
}
