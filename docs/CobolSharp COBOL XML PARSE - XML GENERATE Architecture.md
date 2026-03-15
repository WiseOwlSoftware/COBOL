CobolSharp COBOL XML PARSE / XML GENERATE Architecture (CIL‑Only)
=================================================================

Purpose
-------
Define the authoritative architecture for:
- XML PARSE semantics (SAX-style)
- XML GENERATE semantics
- Mapping COBOL data items ↔ XML elements/attributes
- NATIONAL and DISPLAY string handling
- Numeric, boolean, and null mapping
- OCCURS and nested group mapping
- Exception routing (ON EXCEPTION)
- Integration with ExecutionContext.XmlEngine
- CIL‑friendly lowering
- AOT/WASM‑safe XML processing

This document governs how CobolSharp implements COBOL’s XML facilities on .NET.

------------------------------------------------------------
SECTION 1 — XML ENGINE OVERVIEW
------------------------------------------------------------

CobolSharp implements:
- Full ISO/IEC 1989:2023 XML PARSE and XML GENERATE
- Streaming SAX-style parsing
- Deterministic mapping rules
- Unicode-safe processing
- Strict type validation
- Declarative exception routing
- AOT/WASM-safe XML processing

XML operations are handled by:
ExecutionContext.XmlEngine

Which provides:
- Parse(xmlString, handler)
- Generate(sourceRecord) → xmlString
- Exception metadata

------------------------------------------------------------
SECTION 2 — XML PARSE SEMANTICS
------------------------------------------------------------

2.1 Basic form
--------------
XML PARSE xml-text
    PROCESSING PROCEDURE proc-name

2.2 Event-driven model
----------------------
CobolSharp uses SAX-style events:
- START-OF-DOCUMENT
- START-OF-ELEMENT
- ATTRIBUTE
- CONTENT-CHARACTERS
- END-OF-ELEMENT
- END-OF-DOCUMENT

Each event triggers:
PERFORM proc-name

2.3 ON EXCEPTION
----------------
Triggered by:
- Invalid XML
- Mismatched tags
- Invalid UTF-8/UTF-16
- Unexpected end of document
- Numeric overflow during mapping

2.4 NOT ON EXCEPTION
--------------------
Executed only if parse succeeds.

------------------------------------------------------------
SECTION 3 — XML PARSE EVENT DATA
------------------------------------------------------------

3.1 XML-CODE
------------
XML-CODE contains:
- Event type
- Element name
- Attribute name
- Attribute value
- Character data
- Depth
- Error code (if exception)

3.2 XML-TEXT
------------
Contains:
- Raw text of element or attribute
- UTF-16 encoded

3.3 XML-NAMESPACE (optional)
----------------------------
CobolSharp supports:
- Namespace URI
- Local name
- Prefix

------------------------------------------------------------
SECTION 4 — XML → COBOL MAPPING RULES
------------------------------------------------------------

4.1 Element → group item
------------------------
<customer> maps to:
01 CUSTOMER.

4.2 Attribute → elementary item
-------------------------------
<customer id="123">
Maps to:
05 ID PIC X(10).

4.3 Text content → PIC X / PIC N
---------------------------------
<name>John</name>
Maps to:
05 NAME PIC X(20).

4.4 Numeric content → numeric PIC
---------------------------------
<age>42</age>
Maps to:
05 AGE PIC 9(3).

4.5 Boolean content
-------------------
<active>true</active>
Maps to:
- "TRUE"/"FALSE" for PIC X
- 1/0 for PIC 9

4.6 Empty elements
------------------
<empty/>
Maps to:
- Spaces for PIC X/N
- Zero for numeric

4.7 Repeated elements → OCCURS
------------------------------
<item>...</item>
<item>...</item>
Maps to OCCURS table.

------------------------------------------------------------
SECTION 5 — COBOL → XML MAPPING RULES
------------------------------------------------------------

5.1 Group item → element
------------------------
01 CUSTOMER.
→ <customer> ... </customer>

5.2 Elementary item → element or attribute
------------------------------------------
CobolSharp defaults to:
- Element for PIC X/N
- Element for numeric
- Attribute only if ATTRIBUTE clause used (future)

5.3 OCCURS → repeated elements
------------------------------
Each element becomes:
<item>...</item>

5.4 NATIONAL → UTF-8
--------------------
UTF-16 → UTF-8 conversion.

5.5 Numeric → text content
--------------------------
Decimal → string.

5.6 Condition names
-------------------
TRUE → "true"
FALSE → "false"

------------------------------------------------------------
SECTION 6 — XML GENERATE SEMANTICS
------------------------------------------------------------

6.1 Basic form
--------------
XML GENERATE xml-text FROM data-item

6.2 WITH ATTRIBUTES
-------------------
Not yet implemented.

6.3 WITH ENCODING
-----------------
CobolSharp defaults to UTF-8.

6.4 ON EXCEPTION
----------------
Triggered by:
- Invalid characters
- Numeric conversion failure
- OCCURS overflow
- Unsupported type

------------------------------------------------------------
SECTION 7 — CIL LOWERING RULES
------------------------------------------------------------

7.1 XML PARSE lowering
----------------------
Generated IL:
- Load xml-text
- Load handler procedure pointer
- Call ctx.XmlEngine.Parse
- Check ExceptionState
- Branch to ON EXCEPTION or NOT ON EXCEPTION

7.2 XML GENERATE lowering
-------------------------
Generated IL:
- Load source record pointer
- Call ctx.XmlEngine.Generate
- Store result into target

7.3 Event handler lowering
--------------------------
Each event triggers:
PERFORM proc-name

------------------------------------------------------------
SECTION 8 — XML ENGINE IMPLEMENTATION
------------------------------------------------------------

8.1 Parser
----------
- Streaming SAX parser
- UTF-8 decoder
- Namespace-aware
- No DOM construction
- AOT/WASM-safe

8.2 Generator
-------------
- UTF-16 → UTF-8 encoder
- Minimal escaping (&, <, >, ", ')
- Deterministic element ordering (declaration order)

8.3 Performance
---------------
- Zero-copy for character data
- Pooled buffers
- Streaming output

------------------------------------------------------------
SECTION 9 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Current XML event
- Element name
- Attribute name/value
- Character data
- Depth
- ExceptionState
- Mapped COBOL fields

------------------------------------------------------------
SECTION 10 — EDGE-CASE BEHAVIOR
------------------------------------------------------------

10.1 Mixed content
------------------
Text + child elements:
- Text delivered as CONTENT-CHARACTERS events
- No automatic concatenation

10.2 CDATA sections
-------------------
Delivered as CONTENT-CHARACTERS.

10.3 Comments
-------------
Ignored.

10.4 Processing instructions
----------------------------
Ignored.

10.5 Namespaces
---------------
Preserved in event data.

10.6 Invalid UTF-8
------------------
ON EXCEPTION.

10.7 Empty OCCURS
-----------------
Generates zero elements.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp XML Architecture:
- Implements full COBOL XML PARSE and XML GENERATE semantics
- Uses SAX-style streaming for performance and AOT/WASM safety
- Provides deterministic mapping between COBOL records and XML
- Supports NATIONAL, numeric, boolean, OCCURS, and nested groups
- Integrates tightly with ExecutionContext.XmlEngine
- Generates clean, verifiable, debugger-friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
