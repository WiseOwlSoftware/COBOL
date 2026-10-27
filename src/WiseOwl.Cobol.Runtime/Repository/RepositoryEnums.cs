// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.ComponentModel;

namespace WiseOwl.Cobol.Runtime.Repository;

// ⛔ THE PERSISTED VOCABULARY OF THE REPOSITORY SCHEMA (DESIGN-external-repository §6.2 "Encoding" rule 1; kb/Work PB2100).
// An attribute property may be typed by an enum the READER can decode without the defining assembly, and the compiler's own
// model enums (PicCategory, Usage, GroupUsage, DynamicLengthPrefix, LiteralClass, NumericDisplayForm) cannot type a
// property of an attribute in the runtime, which references none of them. These enums are therefore NOT aliases of the
// compiler's: they are the PERSISTED FORMAT, whose values may never move, while the compiler's enums may be reordered by any
// refactoring. Every member carries an explicit value; every enum is int-backed (a reader decodes an enum-typed argument
// without learning an underlying type from anywhere else); PublicAPI.Shipped.txt pins each published value, so moving one is
// a runtime-major change under scripts/abi_pin.py (§15.4). The one translation between the compiler's enums and these is
// the compiler's RepositoryVocabulary, which a drift test holds total over every member of every compiler enum (§17.2).
// The two the runtime already has are used as they are: SignEncoding, and ObjectReferenceKind (whose None replaces a null kind).

/// <summary>The kind of a published unit (DESIGN-external-repository §5.1): what an entry of the assembly's
/// <c>Units</c> index and a <see cref="CobolUnitAttribute"/> says the unit is. A prototype and its definition publish the
/// same record shape under their own kind.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public enum RepositoryUnitKind
{
    /// <summary>A program definition (PROGRAM-ID).</summary>
    Program = 0,
    /// <summary>A user-defined function definition (FUNCTION-ID).</summary>
    Function = 1,
    /// <summary>A class definition (CLASS-ID).</summary>
    Class = 2,
    /// <summary>An interface definition (INTERFACE-ID).</summary>
    Interface = 3,
    /// <summary>A program prototype (PROGRAM-ID … IS PROTOTYPE, ISO §10.6.2).</summary>
    ProgramPrototype = 4,
    /// <summary>A function prototype (FUNCTION-ID … IS PROTOTYPE).</summary>
    FunctionPrototype = 5,
    /// <summary>A parameterized class (CLASS-ID … USING, ISO §10.6; carries a skeleton).</summary>
    ParameterizedClass = 6,
    /// <summary>A parameterized interface (INTERFACE-ID … USING; carries a skeleton).</summary>
    ParameterizedInterface = 7,
    /// <summary>An expansion of a parameterized definition (DESIGN-external-repository §10.5).</summary>
    Expansion = 8,
    /// <summary>Reserved (§4.6): a CLR type a COBOL unit may name.</summary>
    ClrType = 9,
    /// <summary>Reserved (§4.6): a CLR method a COBOL unit may invoke.</summary>
    ClrMethod = 10,
}

/// <summary>How a formal parameter receives its argument (ISO §14.2.3 8) and 9): BY REFERENCE or BY VALUE).</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public enum PassingMode
{
    /// <summary>BY REFERENCE — the formal denotes the caller's variable.</summary>
    Reference = 0,
    /// <summary>BY VALUE — the formal is a copy of the argument.</summary>
    Value = 1,
}

/// <summary>The data category of a described item (ISO §8.5.2), as persisted: the compiler's <c>PicCategory</c>, in its
/// own numbering.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public enum RepositoryCategory
{
    /// <summary>A group item.</summary>
    Group = 0,
    /// <summary>Alphanumeric or alphabetic.</summary>
    Alphanumeric = 1,
    /// <summary>Numeric.</summary>
    Numeric = 2,
    /// <summary>Numeric-edited.</summary>
    NumericEdited = 3,
    /// <summary>National.</summary>
    National = 4,
    /// <summary>Boolean.</summary>
    Boolean = 5,
    /// <summary>Object reference.</summary>
    ObjectReference = 6,
    /// <summary>Data pointer.</summary>
    Pointer = 7,
    /// <summary>Program pointer.</summary>
    ProgramPointer = 8,
    /// <summary>Function pointer.</summary>
    FunctionPointer = 9,
}

/// <summary>The USAGE of a described item (ISO §13.18.60), as persisted: the compiler's <c>Usage</c>, in its own numbering.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public enum RepositoryUsage
{
    /// <summary>DISPLAY.</summary>
    Display = 0,
    /// <summary>BINARY / COMP / COMP-4.</summary>
    Binary = 1,
    /// <summary>PACKED-DECIMAL / COMP-3.</summary>
    Packed = 2,
    /// <summary>COMP-5.</summary>
    Comp5 = 3,
    /// <summary>COMP-1.</summary>
    Float = 4,
    /// <summary>COMP-2.</summary>
    Double = 5,
    /// <summary>INDEX.</summary>
    Index = 6,
    /// <summary>OBJECT REFERENCE.</summary>
    ObjectReference = 7,
    /// <summary>NATIONAL.</summary>
    National = 8,
    /// <summary>BIT.</summary>
    Bit = 9,
    /// <summary>POINTER.</summary>
    Pointer = 10,
    /// <summary>PROGRAM-POINTER.</summary>
    ProgramPointer = 11,
    /// <summary>FUNCTION-POINTER.</summary>
    FunctionPointer = 12,
    /// <summary>MESSAGE-TAG (declined non-support; the member exists so the recovery description round-trips).</summary>
    MessageTag = 13,
    /// <summary>FLOAT-SHORT.</summary>
    FloatShort = 14,
    /// <summary>FLOAT-LONG.</summary>
    FloatLong = 15,
    /// <summary>FLOAT-EXTENDED.</summary>
    FloatExtended = 16,
    /// <summary>FLOAT-BINARY-32.</summary>
    FloatBinary32 = 17,
    /// <summary>FLOAT-BINARY-64.</summary>
    FloatBinary64 = 18,
    /// <summary>FLOAT-BINARY-128.</summary>
    FloatBinary128 = 19,
    /// <summary>FLOAT-DECIMAL-16.</summary>
    FloatDecimal16 = 20,
    /// <summary>FLOAT-DECIMAL-34.</summary>
    FloatDecimal34 = 21,
    /// <summary>BINARY-CHAR.</summary>
    BinaryChar = 22,
    /// <summary>BINARY-SHORT.</summary>
    BinaryShort = 23,
    /// <summary>BINARY-LONG.</summary>
    BinaryLong = 24,
    /// <summary>BINARY-DOUBLE.</summary>
    BinaryDouble = 25,
}

/// <summary>The GROUP-USAGE of a group item (ISO §13.18.29), as persisted.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public enum RepositoryGroupUsage
{
    /// <summary>No GROUP-USAGE clause: an alphanumeric group.</summary>
    None = 0,
    /// <summary>GROUP-USAGE BIT.</summary>
    Bit = 1,
    /// <summary>GROUP-USAGE NATIONAL.</summary>
    National = 2,
}

/// <summary>The length field a PREFIXED dynamic-length structure puts before its data (ISO §12.3.7.4 GR18), as persisted.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public enum RepositoryPrefix
{
    /// <summary>No PREFIXED phrase.</summary>
    None = 0,
    /// <summary>PREFIXED: an unsigned 32-bit length.</summary>
    Unsigned32 = 1,
    /// <summary>SIGNED PREFIXED: a signed 32-bit length.</summary>
    Signed32 = 2,
    /// <summary>SHORT PREFIXED: an unsigned 16-bit length.</summary>
    Unsigned16 = 3,
    /// <summary>SIGNED SHORT PREFIXED: a signed 16-bit length.</summary>
    Signed16 = 4,
}

/// <summary>The class of a literal (ISO §8.3.5), as persisted: the class of a currency string or a locale name written as a literal.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public enum RepositoryLiteralClass
{
    /// <summary>An alphanumeric literal.</summary>
    Alphanumeric = 0,
    /// <summary>A national literal.</summary>
    National = 1,
    /// <summary>A boolean literal.</summary>
    Boolean = 2,
}

/// <summary>How DISPLAY shows and ACCEPT reads a numeric item (the <c>--numeric-display</c> option), as persisted: part of
/// the compile state a skeleton records (DESIGN-external-repository §6.1).</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public enum RepositoryNumericDisplay
{
    /// <summary>The value as a reader writes it.</summary>
    Decimal = 0,
    /// <summary>The digit image.</summary>
    Digits = 1,
}
