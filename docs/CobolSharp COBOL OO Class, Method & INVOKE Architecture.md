CobolSharp COBOL OO Class, Method & INVOKE Architecture (CIL‑Only)
=================================================================

Purpose
-------
Define the authoritative architecture for:
- COBOL OO class and object model
- FACTORY and OBJECT sections
- Method definitions and signatures
- INVOKE semantics
- RETURNING semantics
- Object instantiation and lifetime
- Inheritance and interfaces
- Property and indexer semantics
- CIL‑friendly lowering
- Integration with ExecutionContext and runtime services

This document governs how CobolSharp implements COBOL’s object‑oriented features on .NET.

------------------------------------------------------------
SECTION 1 — OO MODEL OVERVIEW
------------------------------------------------------------

CobolSharp implements the full COBOL OO model:
- CLASS‑ID
- FACTORY and OBJECT sections
- METHOD‑ID
- INVOKE
- NEW
- RETURNING
- INHERITS
- IMPLEMENTS
- PROPERTY
- STATIC methods (FACTORY)
- INSTANCE methods (OBJECT)

The OO model maps directly to:
- .NET classes
- .NET instance/static methods
- .NET inheritance
- .NET interfaces

------------------------------------------------------------
SECTION 2 — CLASS STRUCTURE
------------------------------------------------------------

2.1 CLASS‑ID
------------
CLASS-ID. MyClass.

Generates:
public class MyClass { ... }

2.2 FACTORY section
-------------------
FACTORY.
    METHOD-ID. New.
    ...
END METHOD.

FACTORY maps to:
- Static members
- Static methods
- Constructors

2.3 OBJECT section
------------------
OBJECT.
    METHOD-ID. DoWork.
    ...
END METHOD.

OBJECT maps to:
- Instance members
- Instance methods

2.4 END CLASS
-------------
Marks end of class.

------------------------------------------------------------
SECTION 3 — METHOD DEFINITIONS
------------------------------------------------------------

3.1 METHOD-ID
-------------
METHOD-ID. MethodName.

Generates:
public void MethodName(ExecutionContext ctx, ...)

3.2 USING parameters
--------------------
METHOD-ID. Foo USING a b c.

Lowered to:
public void Foo(ExecutionContext ctx, ref DataItem a, ref DataItem b, ref DataItem c)

3.3 RETURNING
-------------
METHOD-ID. Compute RETURNING result.

Lowered to:
public ReturnType Compute(ExecutionContext ctx, ...)

3.4 STATIC vs INSTANCE
----------------------
FACTORY → static  
OBJECT → instance  

------------------------------------------------------------
SECTION 4 — INVOKE SEMANTICS
------------------------------------------------------------

4.1 Basic form
--------------
INVOKE object "Method" USING args RETURNING result

Lowering:
- Load object reference
- Load args
- Callvirt or call (static)
- Store result

4.2 INVOKE on FACTORY
----------------------
INVOKE MyClass "New" RETURNING obj

Lowered to:
obj = MyClass.New(ctx)

4.3 INVOKE on OBJECT
---------------------
INVOKE obj "DoWork"

Lowered to:
obj.DoWork(ctx)

4.4 INVOKE with RETURNING
-------------------------
INVOKE obj "Compute" RETURNING x

Lowered to:
x = obj.Compute(ctx)

------------------------------------------------------------
SECTION 5 — OBJECT INSTANTIATION
------------------------------------------------------------

5.1 NEW
-------
INVOKE MyClass "New" RETURNING obj

Maps to:
- Static FACTORY method
- Equivalent to constructor

5.2 Lifetime
------------
Objects:
- Are managed by .NET GC
- Do not require explicit disposal unless implementing IDisposable

5.3 THIS object
---------------
Inside OBJECT section:
- THIS maps to "this" reference

------------------------------------------------------------
SECTION 6 — INHERITANCE
------------------------------------------------------------

6.1 INHERITS
------------
CLASS-ID. Child INHERITS Base.

Lowered to:
public class Child : Base

6.2 Method overriding
---------------------
METHOD-ID. Foo OVERRIDE.

Lowered to:
public override void Foo(...)

6.3 Base method call
---------------------
INVOKE SUPER "Foo"

Lowered to:
base.Foo(ctx)

------------------------------------------------------------
SECTION 7 — INTERFACES
------------------------------------------------------------

7.1 IMPLEMENTS
--------------
CLASS-ID. MyClass IMPLEMENTS IThing.

Lowered to:
public class MyClass : IThing

7.2 Interface methods
---------------------
METHOD-ID. DoThing.

Must match interface signature.

------------------------------------------------------------
SECTION 8 — PROPERTIES
------------------------------------------------------------

8.1 PROPERTY definition
-----------------------
PROPERTY-ID. Value GET SET.

Lowered to:
public DataType Value { get; set; }

8.2 GET/SET methods
-------------------
PROPERTY-ID. Value.
    GET.
        ...
    SET.
        ...
END PROPERTY.

Lowered to:
public DataType Value {
    get { ... }
    set { ... }
}

------------------------------------------------------------
SECTION 9 — INDEXERS
------------------------------------------------------------

9.1 PROPERTY with index
-----------------------
PROPERTY-ID. Item USING index GET SET.

Lowered to:
public DataType this[int index] { get; set; }

------------------------------------------------------------
SECTION 10 — CIL LOWERING RULES
------------------------------------------------------------

10.1 Method lowering
--------------------
Each METHOD-ID becomes:
- A .NET method
- With ExecutionContext as first parameter

10.2 INVOKE lowering
--------------------
INVOKE → callvirt or call

10.3 THIS lowering
------------------
THIS → ldarg.0

10.4 SUPER lowering
-------------------
SUPER → base call

10.5 RETURNING lowering
-----------------------
RETURNING → ret value

------------------------------------------------------------
SECTION 11 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Class name
- Method name
- THIS object
- Parameters
- RETURNING value
- OO call stack
- INVOKE targets

Sequence points emitted for:
- METHOD-ID
- INVOKE
- RETURNING
- PROPERTY GET/SET

------------------------------------------------------------
SECTION 12 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

12.1 INVOKE on null object
--------------------------
Runtime exception → ON EXCEPTION

12.2 RETURNING on void method
-----------------------------
Illegal; compile‑time error

12.3 METHOD-ID inside DECLARATIVES
----------------------------------
Illegal

12.4 PROPERTY with OCCURS
-------------------------
Illegal unless explicitly indexed

12.5 INHERITS with multiple bases
---------------------------------
Illegal; COBOL supports single inheritance

12.6 IMPLEMENTS with multiple interfaces
----------------------------------------
Allowed

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp OO Class, Method & INVOKE Architecture:
- Implements full COBOL OO semantics
- Maps FACTORY/OBJECT to static/instance .NET methods
- Supports INVOKE, NEW, RETURNING, INHERITS, IMPLEMENTS, PROPERTY
- Generates clean, verifiable, debugger‑friendly CIL
- Integrates tightly with ExecutionContext and runtime services
- Ensures correctness across CoreCLR, AOT, and WASM
