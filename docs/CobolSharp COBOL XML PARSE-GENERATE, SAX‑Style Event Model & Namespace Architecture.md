CobolSharp COBOL XML PARSE/GENERATE, SAX‑Style Event Model & Namespace Architecture (CIL‑Only)
==============================================================================================

Purpose
-------
Define the authoritative architecture for:
- XML PARSE statement
- XML GENERATE statement
- SAX‑style event model
- Namespace handling (prefix, URI, default namespace)
- Attribute binding
- Element/attribute name matching
- WITH DETAIL semantics
- Exception routing (ON EXCEPTION, declaratives)
- UTF‑8/UTF‑16 conversion
- Integration with ExecutionContext.XmlEngine
- AOT/WASM‑safe XML processing
- CIL‑friendly lowering

This document governs how CobolSharp implements COBOL’s XML facilities on .NET.

------------------------------------------------------------
SECTION 1 — XML ENGINE OVERVIEW
------------------------------------------------------------

CobolSharp implements XML using:
- A streaming SAX‑style parser
- A streaming generator
- Zero‑allocation UTF‑8 processing
- Deterministic event ordering
- Strict COBOL type binding
- Namespace‑aware element/attribute matching

ExecutionContext.XmlEngine provides:
- Parse(input, handler)
- Generate(output, handler)
- Namespace resolution
- UTF‑8/UTF‑16 conversion
- Error detection and ExceptionState population

------------------------------------------------------------
SECTION 2 — XML PARSE STATEMENT
------------------------------------------------------------

2.1 Basic form
--------------
XML PARSE xmlText
    PROCESSING PROCEDURE procName
    ON EXCEPTION
        ...
    NOT ON EXCEPTION
        ...

2.2 Modes
---------
CobolSharp supports:
- PROCESSING PROCEDURE (event‑driven)
- WITH DETAIL (event metadata)
- NAME OF (custom element/attribute names)
- SUPPRESS (ignore fields)
- OMITTED (skip missing elements)

2.3 Input encoding
------------------
Input may be:
- DISPLAY (ASCII/UTF‑8)
- NATIONAL (UTF‑16)
- FD record buffer (raw bytes)

------------------------------------------------------------
SECTION 3 — SAX‑STYLE EVENT MODEL
------------------------------------------------------------

3.1 Events
----------
XmlEngine emits:
- StartElement(name, prefix, namespaceUri)
- EndElement(name, prefix, namespaceUri)
- Attribute(name, prefix, namespaceUri, value)
- Characters(text)
- StartDocument
- EndDocument

3.2 Handler
-----------
Compiler generates a handler class with:
- OnStartElement
- OnEndElement
- OnAttribute
- OnCharacters
- OnStartDocument
- OnEndDocument

3.3 WITH DETAIL
---------------
WITH DETAIL causes:
- Each event to populate DETAIL‑item
- DETAIL‑item includes:
  - EVENT‑TYPE
  - NAME
  - PREFIX
  - NAMESPACE‑URI
  - VALUE
  - DEPTH

------------------------------------------------------------
SECTION 4 — NAMESPACE ARCHITECTURE
------------------------------------------------------------

4.1 Namespace resolution
------------------------
XmlEngine maintains:
- Prefix → URI mapping stack
- Default namespace
- In‑scope namespaces per element

4.2 Matching rules
------------------
COBOL field name matches XML element if:
- NAME OF overrides match
- Else:
  - Local name matches field name
  - Namespace URI matches (if specified)
  - Prefix irrelevant for matching

4.3 NAME OF
-----------
Overrides element/attribute name:
05 CustName PIC X(20) NAME OF "CustomerName".

4.4 SUPPRESS
------------
Field ignored during XML GENERATE.

4.5 OMITTED
-----------
Field not required during XML PARSE.

------------------------------------------------------------
SECTION 5 — DATA BINDING MODEL
------------------------------------------------------------

5.1 INTO dataItem (object binding)
----------------------------------
Maps XML elements to COBOL group:
- Element → group field
- Attribute → subordinate field
- Text → PIC X or PIC N
- Missing elements → default
- Extra elements → ignored unless WITH DETAIL

5.2 Arrays
----------
Repeated elements map to:
- OCCURS tables
- OCCURS DEPENDING ON supported

5.3 Scalars
-----------
Text → DISPLAY or NATIONAL  
Numbers → Decimal  
Booleans → "TRUE"/"FALSE"

5.4 Nested elements
-------------------
Mapped recursively to nested groups.

------------------------------------------------------------
SECTION 6 — XML GENERATE STATEMENT
------------------------------------------------------------

6.1 Basic form
--------------
XML GENERATE xmlText
    FROM dataItem
    WITH DETAIL
    ON EXCEPTION
        ...

6.2 Output encoding
-------------------
Output is:
- UTF‑8 for DISPLAY
- UTF‑16 for NATIONAL

6.3 Field rules
---------------
DISPLAY → text node  
NATIONAL → text node  
COMP/COMP‑3/COMP‑5 → numeric text  
Group → element  
OCCURS → repeated elements

6.4 SUPPRESS
------------
Field omitted from output.

6.5 NAME OF
-----------
Custom element/attribute name used.

6.6 WITH DETAIL
---------------
Emits DETAIL events for each generated element/attribute/text node.

------------------------------------------------------------
SECTION 7 — ERROR HANDLING & EXCEPTIONSTATE
------------------------------------------------------------

7.1 Parse errors
----------------
- Invalid XML syntax
- Mismatched tags
- Invalid namespace prefix
- UTF‑8/UTF‑16 errors
- Numeric overflow
- Array bounds overflow

7.2 Generate errors
-------------------
- Invalid field type
- Non‑ASCII DISPLAY in UTF‑8 mode
- OCCURS DEPENDING ON out of range

7.3 ExceptionState
------------------
Populated with:
- Error category
- Error message
- Element/attribute name
- Namespace URI
- Raw token (optional)

7.4 Routing
-----------
1. ON EXCEPTION  
2. USE AFTER EXCEPTION ON XML  
3. USE AFTER STANDARD EXCEPTION  

------------------------------------------------------------
SECTION 8 — UTF‑8 / UTF‑16 CONVERSION
------------------------------------------------------------

8.1 UTF‑8 input
---------------
- Parsed directly
- No intermediate string allocation

8.2 UTF‑16 input
----------------
- Converted to UTF‑8 stream
- Surrogate pairs validated

8.3 Output
----------
DISPLAY → UTF‑8  
NATIONAL → UTF‑16

------------------------------------------------------------
SECTION 9 — CIL LOWERING RULES
------------------------------------------------------------

9.1 XML PARSE lowering
----------------------
Generated IL:
- Load input buffer
- newobj XmlParseHandler
- call XmlEngine.Parse

9.2 XML GENERATE lowering
-------------------------
Generated IL:
- newobj XmlGenerateHandler
- call XmlEngine.Generate
- Store output into target

9.3 WITH DETAIL lowering
------------------------
Compiler generates:
- DETAIL group
- Handler writes event info into DETAIL group

9.4 NAME OF lowering
--------------------
Compiler embeds:
- Custom element/attribute name in metadata

9.5 SUPPRESS lowering
---------------------
Compiler marks field as suppressed in metadata.

------------------------------------------------------------
SECTION 10 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Current XML event
- Element/attribute name
- Prefix
- Namespace URI
- Text value
- Depth
- DETAIL group contents
- ExceptionState
- Bound COBOL fields

------------------------------------------------------------
SECTION 11 — AOT/WASM‑SAFE XML PROCESSING
------------------------------------------------------------

11.1 No reflection
------------------
Handlers generated statically.

11.2 No dynamic codegen
-----------------------
Parser and generator use:
- Pure managed code
- No dynamic IL

11.3 No unsafe code
-------------------
- No pointers
- No stackalloc

11.4 Deterministic behavior
---------------------------
- Same output across platforms
- No locale‑dependent formatting

------------------------------------------------------------
SECTION 12 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

12.1 Empty elements
-------------------
Mapped to:
- Zero‑length string
- Or default value

12.2 Mixed content
------------------
Characters between elements:
- Concatenated into text node
- Bound to nearest PIC X/N field

12.3 Namespace redeclaration
----------------------------
Allowed; new scope pushed.

12.4 Missing end tag
--------------------
ON EXCEPTION.

12.5 Invalid prefix
-------------------
ON EXCEPTION.

12.6 Repeated elements without OCCURS
-------------------------------------
Last value wins.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp XML Architecture:
- Implements full COBOL XML PARSE/GENERATE semantics
- Provides SAX‑style event model and namespace‑aware binding
- Supports NAME OF, SUPPRESS, OMITTED, and WITH DETAIL
- Ensures deterministic UTF‑8/UTF‑16 processing
- Integrates tightly with ExecutionContext.XmlEngine
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
