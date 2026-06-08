CobolSharp COBOL JSON & XML Processing Architecture (CIL‑Only)
=============================================================

> **STATUS BANNER (2026-06-07).** **Design reference / target design — NOT yet implemented.**
> Implementation status: **DESIGN-ONLY (Phase C).** A permissive *grammar overlay* exists
> (`src/CobolSharp.Compiler/Grammar/CobolParserJsonXml.g4` + `Core/CobolExtensionsJsonXml.g4`),
> but it is only a token‑accepting stub — e.g. `jsonStatement : JSON (dataReference | literal)+ ;`
> and `xmlStatement : XML (dataReference | literal)+ ;` — with **no binding, no lowering, and no
> runtime engine.** There is **no `JsonEngine`/`XmlEngine`** in `src/CobolSharp.Runtime` and no
> JSON/XML PARSE/GENERATE CIL lowering anywhere in `src/`. Treat everything below as the intended
> design to be built in Phase C (per the conformance plan), to be reconciled against the actual
> data model when implemented.
>
> Stack: **.NET 10 / C# 14.** Backend: **CIL‑only via Mono.Cecil — NO custom VM / NO bytecode
> interpreter** (a Roslyn C# backend is a *future additive* option, Stage‑5, with Cecil as the
> oracle). When implemented, JSON/XML must lower to CIL like every other statement.
>
> Data‑model note: the runtime data model is migrating to **typed‑native** representations
> (character → `string` (UTF‑16); numeric → `long`/`decimal`; groups → nested `record struct`;
> `OCCURS` → `T[]`; pointers → the single **`ManagedPointer`** carrier), gated behind
> `EnableTypedFields` (default OFF), CORE complete through Stage‑4, with the byte/`StorageBlock`
> engine being *islanded* as a classifier‑scoped fallback. The "DETAIL group / OCCURS table /
> record‑pointer" wording in this design predates that migration; when JSON/XML is built, bind to
> the **typed‑native** model (and the byte image only via the classifier fallback), not the legacy
> byte layout.
>
> Plan SSOT: **`docs/MASTER_PLAN.md`**. Conformance roadmap: `docs/ISO2023_CONFORMANCE_PLAN.md`.
> Doctrine: `PROMPT.md`.

Purpose
-------
Define the authoritative target architecture for CobolSharp's COBOL‑2014+ structured‑data features:
- JSON PARSE / JSON GENERATE
- XML PARSE / XML GENERATE
- SAX‑style streaming event models for both JSON and XML
- Data binding to COBOL storage (object binding + event‑level binding)
- Mapping COBOL records ↔ JSON/XML structures
- COUNT IN, PROCESSING PROCEDURE, WITH DETAIL, NAME OF, SUPPRESS, OMITTED, WITH ATTRIBUTES
- XML namespace handling (prefix, URI, default namespace)
- OCCURS, OCCURS DEPENDING ON, nested group, and REDEFINES handling
- National vs alphanumeric encoding; UTF‑8/UTF‑16 conversion
- Exception routing (ON EXCEPTION, NOT ON EXCEPTION, declaratives)
- CIL‑friendly lowering
- AOT/WASM‑safe processing
- Integration with the runtime JSON/XML engines

This document governs how CobolSharp implements COBOL's structured‑data features on .NET.

------------------------------------------------------------
SECTION 1 — OVERVIEW OF JSON/XML SUPPORT
------------------------------------------------------------

CobolSharp targets:
- Full ISO/IEC 1989:2023 JSON PARSE and JSON GENERATE (COBOL‑2014+)
- Full ISO/IEC 1989:2023 XML PARSE and XML GENERATE (COBOL‑2014+)
- Deterministic mapping between COBOL data structures and JSON/XML
- Streaming SAX‑style parsers (no DOM construction)
- Streaming generators
- Strict COBOL type validation/binding
- Declarative exception routing
- CIL‑only lowering with no external dependencies

The JSON/XML engines are:
- Pure managed code
- Deterministic (no locale‑dependent formatting; same output across platforms)
- Unicode‑safe
- AOT/WASM compatible (no reflection, no dynamic codegen, no unsafe code)

Runtime entry points (target):
- `JsonEngine` — exposed via `ExecutionContext.JsonEngine` / `CobolSharp.Runtime.JsonEngine`
- `XmlEngine` — exposed via `ExecutionContext.XmlEngine` / `CobolSharp.Runtime.XmlEngine`

Each engine provides (target surface):
- `Parse(input, handler|targetRecord)`
- `Generate(source) → output` (and a streaming `Generate(output, handler)` form)
- Namespace resolution (XML only)
- UTF‑8/UTF‑16 conversion
- Error detection and `ExceptionState` population

------------------------------------------------------------
SECTION 2 — JSON PARSE STATEMENT
------------------------------------------------------------

### 2.1 Basic forms
```
JSON PARSE json-text INTO data-item
```
```
JSON PARSE json-text
    INTO data-item
    WITH DETAIL [name-value-pairs]
    NAME OF ...
    SUPPRESS ...
    COUNT IN count-var
    ON EXCEPTION
        ...
    NOT ON EXCEPTION
        ...
END-JSON
```

### 2.2 Modes / phrases
- `INTO data-item` — object binding (default).
- `WITH DETAIL` — event‑level binding. Preserves nested structure; maps arrays → OCCURS, nested
  objects → nested groups. (Without `WITH DETAIL` the structure is flattened — not recommended.)
- `NAME OF` — custom field‑name matching.
- `SUPPRESS` — omit fields.
- `OMITTED` — skip missing fields (parse does not require them).
- `COUNT IN var` — stores the number of JSON elements processed (array → element count; object →
  field count).
- `PROCESSING PROCEDURE proc` — called for each JSON element, receiving element name, value, type,
  and current OCCURS index.

### 2.3 Input encoding
Input may be:
- DISPLAY (ASCII / UTF‑8)
- NATIONAL (UTF‑16)
- FD record buffer (raw bytes)

### 2.4 NOT ON EXCEPTION
Executed only if the parse succeeds.

------------------------------------------------------------
SECTION 3 — JSON SAX‑STYLE EVENT MODEL
------------------------------------------------------------

### 3.1 Events
`JsonEngine` emits:
- `StartObject` / `EndObject`
- `StartArray` / `EndArray`
- `Key(name)`
- `Value(string | number | boolean | null)`

### 3.2 Handler
The compiler generates a handler class with:
`OnStartObject`, `OnEndObject`, `OnStartArray`, `OnEndArray`, `OnKey`, `OnValue`.

### 3.3 WITH DETAIL detail‑item
`WITH DETAIL` causes each event to populate a DETAIL group containing (target shape — bind to the
typed‑native model when implemented):
- `EVENT-TYPE`
- `NAME`
- `VALUE`
- `DEPTH`
- `INDEX`

Detail entry value/type table: Name, Type (string, number, boolean, null, object, array), Value
(string or numeric), Depth (optional). The engine emits a table of detail entries stored in the
target detail group.

------------------------------------------------------------
SECTION 4 — JSON → COBOL MAPPING RULES (PARSE)
------------------------------------------------------------

| JSON | COBOL |
|------|-------|
| object | group item |
| array | OCCURS table (OCCURS DEPENDING ON supported; bounds checked) |
| string | PIC X (DISPLAY) or PIC N (NATIONAL) |
| number | numeric item (Decimal; COMP‑3 / COMP‑5 supported) |
| boolean | `"TRUE"`/`"FALSE"` (PIC X) or 1/0 (PIC 9); condition name |
| null | spaces (alphanumeric) / zero (numeric) / false (condition name) |

Detailed rules:
- **Object → group:** keys matched to child field names; **case‑insensitive**; hyphens and
  underscores normalized; missing fields → default; extra fields → ignored (unless `WITH DETAIL`).
- **Array → OCCURS:** logical length = array length; must not exceed max OCCURS; OCCURS DEPENDING ON
  set from array length.
- **String → PIC X / PIC N:** UTF‑8 → UTF‑16 conversion; truncated if longer than target; padded
  with spaces if shorter.
- **Number → numeric PIC:** decimal conversion; overflow → ON EXCEPTION.
- **Nested objects:** mapped recursively to nested groups.
- **NAME OF override:** `05 CustName PIC X(20) NAME OF "name".`
- **OMITTED:** field not required during parse.

------------------------------------------------------------
SECTION 5 — JSON GENERATE STATEMENT
------------------------------------------------------------

### 5.1 Basic form
```
JSON GENERATE json-target
    FROM cobol-record
    [WITH DETAIL]
    COUNT IN count-var
    NAME OF ...
    SUPPRESS ...
    ON EXCEPTION
        ...
    NOT ON EXCEPTION
        ...
END-JSON
```

### 5.2 COBOL → JSON mapping rules
| COBOL | JSON |
|-------|------|
| group item | object (`"field-name": value`; empty group → `{}`) |
| OCCURS | array (`[ element1, element2, ... ]`) |
| PIC X | string (trim trailing spaces; UTF‑16 → UTF‑8) |
| PIC N (NATIONAL) | string (UTF‑16 internally → UTF‑8 output) |
| numeric (incl. COMP/COMP‑3/COMP‑5) | number (COMP‑3 unpacked; COMP‑5 → integer) |
| 88‑level condition name | boolean (`true`/`false`) |
| REDEFINES | only the active view is emitted |

### 5.3 Output encoding
- DISPLAY → UTF‑8
- NATIONAL → UTF‑16

### 5.4 Other phrases
- **SUPPRESS:** field omitted from output.
- **OMITTED:** field not included in output.
- **NAME OF:** custom key name used.
- **WITH DETAIL:** emits DETAIL events for each generated element.

------------------------------------------------------------
SECTION 6 — XML PARSE STATEMENT
------------------------------------------------------------

### 6.1 Basic forms
```
XML PARSE xml-text PROCESSING PROCEDURE proc-name
```
```
XML PARSE xml-text
    PROCESSING PROCEDURE proc-name
    WITH DETAIL
    NAME OF ...
    SUPPRESS ...
    COUNT IN count-var
    ON EXCEPTION
        ...
    NOT ON EXCEPTION
        ...
END-XML
```

### 6.2 Event‑driven model
XML PARSE is event‑driven; each event triggers `PERFORM proc-name`. Modes:
- `PROCESSING PROCEDURE` (event‑driven, primary)
- `WITH DETAIL` (event metadata)
- `NAME OF` (custom element/attribute names)
- `SUPPRESS` (ignore fields)
- `OMITTED` (skip missing elements)
- `COUNT IN var` (number of XML nodes processed)

### 6.3 Input encoding
DISPLAY (ASCII/UTF‑8), NATIONAL (UTF‑16), or FD record buffer (raw bytes).

### 6.4 NOT ON EXCEPTION
Executed only if the parse succeeds.

------------------------------------------------------------
SECTION 7 — XML SAX‑STYLE EVENT MODEL & EVENT DATA
------------------------------------------------------------

### 7.1 Events
`XmlEngine` emits:
- `StartDocument` / `EndDocument` (a.k.a. START‑OF‑DOCUMENT / END‑OF‑DOCUMENT)
- `StartElement(name, prefix, namespaceUri)` (START‑OF‑ELEMENT)
- `EndElement(name, prefix, namespaceUri)` (END‑OF‑ELEMENT)
- `Attribute(name, prefix, namespaceUri, value)`
- `Characters(text)` (CONTENT‑CHARACTERS)

### 7.2 Handler
Generated handler class: `OnStartDocument`, `OnEndDocument`, `OnStartElement`, `OnEndElement`,
`OnAttribute`, `OnCharacters`.

### 7.3 Special registers
- **XML‑CODE** — event type, element name, attribute name, attribute value, character data, depth,
  and error code (if exception).
- **XML‑TEXT** — raw text of element or attribute (UTF‑16 encoded).
- **XML‑NAMESPACE** (optional) — namespace URI, local name, prefix.

### 7.4 WITH DETAIL detail‑item (XML)
`WITH DETAIL` causes each event to populate a DETAIL item including:
`EVENT-TYPE`, `NAME`, `PREFIX`, `NAMESPACE-URI`, `VALUE`, `DEPTH`.

------------------------------------------------------------
SECTION 8 — XML NAMESPACE ARCHITECTURE
------------------------------------------------------------

### 8.1 Namespace resolution
`XmlEngine` maintains:
- A prefix → URI mapping stack
- The default namespace
- In‑scope namespaces per element

### 8.2 Matching rules
A COBOL field name matches an XML element when:
- A `NAME OF` override matches, OR
- The local name matches the field name AND the namespace URI matches (if specified).
- The prefix is irrelevant for matching.

### 8.3 NAME OF (XML)
Overrides element/attribute name: `05 CustName PIC X(20) NAME OF "CustomerName".`

------------------------------------------------------------
SECTION 9 — XML ↔ COBOL MAPPING RULES
------------------------------------------------------------

### 9.1 XML → COBOL (PARSE / binding)
| XML | COBOL |
|-----|-------|
| element | group item (`<customer>` → `01 CUSTOMER`) |
| attribute | elementary item (`<customer id="123">` → `05 ID PIC X(10)`) |
| text content | PIC X / PIC N (`<name>John</name>` → `05 NAME PIC X(20)`) |
| numeric content | numeric PIC (`<age>42</age>` → `05 AGE PIC 9(3)`) |
| boolean content | `"TRUE"`/`"FALSE"` (PIC X) or 1/0 (PIC 9) |
| empty element | spaces (PIC X/N) / zero (numeric) / zero‑length string |
| repeated elements | OCCURS table (OCCURS DEPENDING ON supported) |

- Attribute → subordinate field; text → PIC X/N; missing elements → default; extra elements →
  ignored unless `WITH DETAIL`; nested elements → recursive nested groups.
- Numbers → Decimal; booleans → `"TRUE"`/`"FALSE"`.

### 9.2 COBOL → XML (GENERATE)
| COBOL | XML |
|-------|-----|
| group item | element (`01 CUSTOMER` → `<customer>...</customer>`) |
| elementary item | child element (or attribute via `WITH ATTRIBUTES` / `ATTRIBUTE` clause — future) |
| OCCURS | repeated elements (`<item>...</item>`) |
| PIC X / PIC N | text node (PIC N: UTF‑16 internal → UTF‑8 output) |
| numeric | text content (Decimal → string) |
| 88‑level condition name | `true`/`false`; with `WITH ATTRIBUTES`, mapped to attributes |

- Default for elementary items: element for PIC X/N and numeric; attribute only when the
  `ATTRIBUTE` clause is used (future).
- `WITH ATTRIBUTES`: maps 88‑levels (and VALUE clauses) to attributes; REDEFINES ignored unless the
  active view.

------------------------------------------------------------
SECTION 10 — XML GENERATE STATEMENT
------------------------------------------------------------

### 10.1 Basic form
```
XML GENERATE xml-text FROM data-item
    [WITH ATTRIBUTES]
    [WITH ENCODING ...]
    [WITH DETAIL]
    COUNT IN count-var
    NAME OF ...
    SUPPRESS ...
    ON EXCEPTION
        ...
    NOT ON EXCEPTION
        ...
END-XML
```

### 10.2 Phrases
- **WITH ATTRIBUTES** — map 88‑levels/VALUE clauses to attributes (initially not implemented; see
  §9.2).
- **WITH ENCODING** — defaults to UTF‑8.
- **SUPPRESS** — field omitted from output.
- **NAME OF** — custom element/attribute name used.
- **WITH DETAIL** — emits DETAIL events for each generated element/attribute/text node.

### 10.3 Output encoding
DISPLAY → UTF‑8; NATIONAL → UTF‑16.

------------------------------------------------------------
SECTION 11 — UTF‑8 / UTF‑16 CONVERSION
------------------------------------------------------------

- **UTF‑8 input:** parsed directly; no intermediate string allocation.
- **UTF‑16 input:** converted to a UTF‑8 stream; surrogate pairs validated.
- **Output:** DISPLAY → UTF‑8; NATIONAL → UTF‑16.
- All JSON strings are UTF‑8; XML text is UTF‑8 or UTF‑16 depending on the target; PIC N always uses
  UTF‑16 internally (consistent with the typed‑native NATIONAL → `string` representation).

------------------------------------------------------------
SECTION 12 — ERROR HANDLING & EXCEPTIONSTATE
------------------------------------------------------------

### 12.1 JSON parse errors
Invalid JSON syntax / unexpected token; type mismatch; missing required field; numeric overflow;
array bounds overflow (array too large for OCCURS); invalid UTF‑8/UTF‑16; unexpected JSON type.

### 12.2 JSON generate errors
Invalid field type; non‑ASCII DISPLAY in UTF‑8 mode; OCCURS DEPENDING ON out of range.

### 12.3 XML parse errors
Invalid XML syntax; mismatched tags; invalid/undeclared namespace prefix; UTF‑8/UTF‑16 errors;
unexpected end of document; numeric overflow during mapping.

### 12.4 XML generate errors
Invalid characters; numeric conversion failure; OCCURS overflow; unsupported type.

### 12.5 ExceptionState
Populated with (union over JSON and XML):
- Error category (e.g. `JSON EXCEPTION` / `XML EXCEPTION`)
- Error message
- JSON path / element‑or‑attribute name (property name where applicable)
- Namespace URI (XML)
- Event type
- Expected type / actual type (JSON)
- Raw token (optional)

### 12.6 Routing
1. `ON EXCEPTION`
2. `USE AFTER EXCEPTION ON JSON` / `USE AFTER EXCEPTION ON XML`
3. `USE AFTER STANDARD EXCEPTION` (a.k.a. `USE AFTER ERROR` / `USE AFTER EXCEPTION`)

------------------------------------------------------------
SECTION 13 — CIL LOWERING RULES
------------------------------------------------------------

When implemented, lowering is **CIL‑only** (Mono.Cecil). High‑level call shape:
```
JsonEngine.Parse   (ctx, jsonSource, targetRecord, options)
JsonEngine.Generate(ctx, targetString, sourceRecord, options)
XmlEngine.Parse    (ctx, xmlSource, processingProc, options)
XmlEngine.Generate (ctx, xmlTarget,  sourceRecord, options)
```

### 13.1 JSON PARSE lowering
Load input buffer → `newobj JsonParseHandler` → `call JsonEngine.Parse` → check `ExceptionState` →
branch to ON EXCEPTION / NOT ON EXCEPTION.

### 13.2 JSON GENERATE lowering
`newobj JsonGenerateHandler` → `call JsonEngine.Generate` → store output into target.

### 13.3 XML PARSE lowering
Load xml‑text → load handler/processing‑procedure pointer → `call XmlEngine.Parse` → check
`ExceptionState` → branch.

### 13.4 XML GENERATE lowering
`newobj XmlGenerateHandler` → `call XmlEngine.Generate` → store output into target.

### 13.5 PROCESSING PROCEDURE / event‑handler lowering
Each event triggers `PERFORM proc-name`; for handler‑class binding, the compiler generates a CIL
method invoked by the engine, receiving event metadata.

### 13.6 WITH DETAIL lowering
Compiler generates the DETAIL group; the handler writes event info into the DETAIL group, emitted
per event (JSON: populates the detail OCCURS table with name/value/type fields).

### 13.7 NAME OF / SUPPRESS lowering
`NAME OF`: the compiler embeds the custom key/element/attribute name in metadata. `SUPPRESS`: the
compiler marks the field as suppressed in metadata.

------------------------------------------------------------
SECTION 14 — RUNTIME ENGINE ARCHITECTURE
------------------------------------------------------------

### 14.1 JsonEngine responsibilities
Parse JSON text; validate structure; map JSON → COBOL record and COBOL record → JSON; handle OCCURS
and nested groups; handle COUNT IN and PROCESSING PROCEDURE; detect ON EXCEPTION conditions.

Parser/generator (target):
- Parser: streaming SAX‑style + recursive‑descent UTF‑8 decoder; no dynamic codegen; AOT/WASM‑safe.
- Generator: UTF‑16 → UTF‑8 encoder; minimal escaping; deterministic field ordering (declaration
  order).
- Performance: zero/low allocation for numeric conversion; pooled buffers; streaming for large JSON.

### 14.2 XmlEngine responsibilities
Parse XML text; emit XML text; handle SAX‑style events; map COBOL record ↔ XML; namespace
resolution; handle COUNT IN and PROCESSING PROCEDURE; detect ON EXCEPTION conditions.

Parser/generator (target):
- Parser: streaming SAX parser; UTF‑8 decoder; namespace‑aware; **no DOM construction**;
  AOT/WASM‑safe.
- Generator: UTF‑16 → UTF‑8 encoder; minimal escaping (`&`, `<`, `>`, `"`, `'`); deterministic
  element ordering (declaration order).
- Performance: zero‑copy for character data; pooled buffers; streaming output.

### 14.3 Encoding rules (engines)
All JSON strings are UTF‑8; all XML text is UTF‑8 or UTF‑16 depending on the target; PIC N always
UTF‑16 internally.

------------------------------------------------------------
SECTION 15 — DEBUGGER INTEGRATION
------------------------------------------------------------

The debugger surfaces:
- Current JSON/XML event; key/element/attribute name; prefix; namespace URI (XML); value/text;
  depth; index
- DETAIL group contents; COUNT IN values; PROCESSING PROCEDURE events
- Parsed JSON tree / parsed structure; OCCURS expansions / logical length; REDEFINES active view
- `ExceptionState`; bound/mapped COBOL fields

Sequence points are emitted for: each JSON element; each XML event; each assignment into the COBOL
record.

------------------------------------------------------------
SECTION 16 — AOT/WASM‑SAFE PROCESSING
------------------------------------------------------------

- **No reflection** — handlers generated statically.
- **No dynamic codegen** — parser and generator are pure managed code, no dynamic IL.
- **No unsafe code** — no pointers, no `stackalloc`.
- **Deterministic behavior** — same output across platforms; no locale‑dependent formatting.

(Correctness must hold across CoreCLR, AOT, and WASM.)

------------------------------------------------------------
SECTION 17 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

### 17.1 JSON
- **null** → spaces (alphanumeric) / zero (numeric) / all children defaulted (group) / false
  (condition name).
- **Empty arrays** → OCCURS DEPENDING ON = 0.
- **Missing keys** → allowed if `OMITTED`; else default value (missing object fields may be left
  unchanged).
- **Extra keys** → ignored unless `WITH DETAIL` (a `WITH DETAIL STRICT` mode is planned).
- **Numeric overflow** → SIZE ERROR → ON EXCEPTION.
- **Array too large for OCCURS** → ON EXCEPTION.
- **String too long** → truncated; no exception unless STRICT mode enabled.
- **OCCURS DEPENDING ON mismatch** → clamp to legal range.
- **Invalid UTF‑8** → ON EXCEPTION.
- **boolean → PIC X** → `"TRUE"`/`"FALSE"`.
- **Mixed national/alphanumeric** → illegal unless explicitly converted.

### 17.2 XML
- **Mixed content** (text + child elements): text delivered as CONTENT‑CHARACTERS events; no
  automatic concatenation in the event model. (For object binding, characters between elements are
  concatenated into a text node bound to the nearest PIC X/N field.)
- **CDATA sections** → delivered as CONTENT‑CHARACTERS.
- **Comments / processing instructions** → ignored.
- **Namespaces** → preserved in event data; redeclaration allowed (new scope pushed).
- **Empty elements** (`<name/>`) → empty/zero‑length string or default value.
- **Missing end tag / invalid prefix** → ON EXCEPTION.
- **Repeated elements without OCCURS** → last value wins.
- **Empty OCCURS** → generates zero elements.
- **Invalid UTF‑8** → ON EXCEPTION.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp JSON & XML Processing Architecture (target design):
- Implements full COBOL‑2014+ / ISO‑2023 JSON & XML PARSE/GENERATE semantics
- Provides SAX‑style streaming event models with structured data binding and XML namespace‑aware
  matching
- Supports COUNT IN, PROCESSING PROCEDURE, WITH DETAIL, NAME OF, SUPPRESS, OMITTED, WITH ATTRIBUTES
- Handles OCCURS / OCCURS DEPENDING ON, nested groups, REDEFINES, NATIONAL/PIC N, numeric, boolean,
  and null
- Uses dedicated runtime engines (`JsonEngine`/`XmlEngine`) for correctness and performance
- Ensures deterministic UTF‑8/UTF‑16 processing
- Generates clean, verifiable, debugger‑friendly **CIL** (no custom VM)
- Ensures correctness across CoreCLR, AOT, and WASM

**Reminder:** this remains DESIGN‑ONLY (Phase C). Build it against the typed‑native data model and
CIL backend per `docs/MASTER_PLAN.md` and `docs/ISO2023_CONFORMANCE_PLAN.md`.
