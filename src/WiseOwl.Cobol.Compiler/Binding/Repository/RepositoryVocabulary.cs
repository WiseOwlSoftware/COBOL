// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Common;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Repository;

namespace WiseOwl.Cobol.Binding.Repository;

/// <summary>
/// ⛔ THE ONE TRANSLATION BETWEEN THE COMPILER'S MODEL ENUMS AND THE REPOSITORY'S PERSISTED VOCABULARY
/// (docs/rearchitecture/DESIGN-external-repository.md §6.2 "Encoding" rule 1; kb/Work PB2100). The compiler's enums
/// (<see cref="PicCategory"/>, <see cref="Usage"/>, <see cref="GroupUsage"/>, <see cref="DynamicLengthPrefix"/>,
/// <see cref="LiteralClass"/>, <see cref="NumericDisplayForm"/>, <see cref="SynchronizedPhrase"/>,
/// <see cref="RaisingTargetKind"/>, <see cref="ObjectRefKind"/>) may be reordered by any refactoring; the runtime's
/// <c>Repository*</c> enums are the persisted format, whose values never move. One pair of switch expressions per enum,
/// each listing every member, each ending in a discard arm that throws <see cref="UnreachableException"/> (CS8524 makes a
/// discard-free enum switch unavailable under TreatWarningsAsErrors); <c>RepositoryVocabularyDriftTests</c> round-trips every
/// member of every compiler enum through its pair, so a member added without a persisted value is red before it can reach the
/// discard. For <see cref="ObjectReferenceKind"/> the forward half is <see cref="ObjectRefDescriptor.RuntimeKind"/>, which
/// this class calls rather than writing a second mapping.
/// </summary>
internal static class RepositoryVocabulary
{
    public static RepositoryCategory Persisted(PicCategory c) => c switch
    {
        PicCategory.Group => RepositoryCategory.Group,
        PicCategory.Alphanumeric => RepositoryCategory.Alphanumeric,
        PicCategory.Numeric => RepositoryCategory.Numeric,
        PicCategory.NumericEdited => RepositoryCategory.NumericEdited,
        PicCategory.National => RepositoryCategory.National,
        PicCategory.Boolean => RepositoryCategory.Boolean,
        PicCategory.ObjectReference => RepositoryCategory.ObjectReference,
        PicCategory.Pointer => RepositoryCategory.Pointer,
        PicCategory.ProgramPointer => RepositoryCategory.ProgramPointer,
        PicCategory.FunctionPointer => RepositoryCategory.FunctionPointer,
        _ => throw new UnreachableException($"PicCategory.{c} has no persisted value"),
    };

    public static PicCategory FromPersisted(RepositoryCategory c) => c switch
    {
        RepositoryCategory.Group => PicCategory.Group,
        RepositoryCategory.Alphanumeric => PicCategory.Alphanumeric,
        RepositoryCategory.Numeric => PicCategory.Numeric,
        RepositoryCategory.NumericEdited => PicCategory.NumericEdited,
        RepositoryCategory.National => PicCategory.National,
        RepositoryCategory.Boolean => PicCategory.Boolean,
        RepositoryCategory.ObjectReference => PicCategory.ObjectReference,
        RepositoryCategory.Pointer => PicCategory.Pointer,
        RepositoryCategory.ProgramPointer => PicCategory.ProgramPointer,
        RepositoryCategory.FunctionPointer => PicCategory.FunctionPointer,
        _ => throw new UnreachableException($"RepositoryCategory {(int)c} is no persisted category"),
    };

    public static RepositoryUsage Persisted(Usage u) => u switch
    {
        Usage.Display => RepositoryUsage.Display,
        Usage.Binary => RepositoryUsage.Binary,
        Usage.Packed => RepositoryUsage.Packed,
        Usage.Comp5 => RepositoryUsage.Comp5,
        Usage.Float => RepositoryUsage.Float,
        Usage.Double => RepositoryUsage.Double,
        Usage.Index => RepositoryUsage.Index,
        Usage.ObjectReference => RepositoryUsage.ObjectReference,
        Usage.National => RepositoryUsage.National,
        Usage.Bit => RepositoryUsage.Bit,
        Usage.Pointer => RepositoryUsage.Pointer,
        Usage.ProgramPointer => RepositoryUsage.ProgramPointer,
        Usage.FunctionPointer => RepositoryUsage.FunctionPointer,
        Usage.MessageTag => RepositoryUsage.MessageTag,
        Usage.FloatShort => RepositoryUsage.FloatShort,
        Usage.FloatLong => RepositoryUsage.FloatLong,
        Usage.FloatExtended => RepositoryUsage.FloatExtended,
        Usage.FloatBinary32 => RepositoryUsage.FloatBinary32,
        Usage.FloatBinary64 => RepositoryUsage.FloatBinary64,
        Usage.FloatBinary128 => RepositoryUsage.FloatBinary128,
        Usage.FloatDecimal16 => RepositoryUsage.FloatDecimal16,
        Usage.FloatDecimal34 => RepositoryUsage.FloatDecimal34,
        Usage.BinaryChar => RepositoryUsage.BinaryChar,
        Usage.BinaryShort => RepositoryUsage.BinaryShort,
        Usage.BinaryLong => RepositoryUsage.BinaryLong,
        Usage.BinaryDouble => RepositoryUsage.BinaryDouble,
        _ => throw new UnreachableException($"Usage.{u} has no persisted value"),
    };

    public static Usage FromPersisted(RepositoryUsage u) => u switch
    {
        RepositoryUsage.Display => Usage.Display,
        RepositoryUsage.Binary => Usage.Binary,
        RepositoryUsage.Packed => Usage.Packed,
        RepositoryUsage.Comp5 => Usage.Comp5,
        RepositoryUsage.Float => Usage.Float,
        RepositoryUsage.Double => Usage.Double,
        RepositoryUsage.Index => Usage.Index,
        RepositoryUsage.ObjectReference => Usage.ObjectReference,
        RepositoryUsage.National => Usage.National,
        RepositoryUsage.Bit => Usage.Bit,
        RepositoryUsage.Pointer => Usage.Pointer,
        RepositoryUsage.ProgramPointer => Usage.ProgramPointer,
        RepositoryUsage.FunctionPointer => Usage.FunctionPointer,
        RepositoryUsage.MessageTag => Usage.MessageTag,
        RepositoryUsage.FloatShort => Usage.FloatShort,
        RepositoryUsage.FloatLong => Usage.FloatLong,
        RepositoryUsage.FloatExtended => Usage.FloatExtended,
        RepositoryUsage.FloatBinary32 => Usage.FloatBinary32,
        RepositoryUsage.FloatBinary64 => Usage.FloatBinary64,
        RepositoryUsage.FloatBinary128 => Usage.FloatBinary128,
        RepositoryUsage.FloatDecimal16 => Usage.FloatDecimal16,
        RepositoryUsage.FloatDecimal34 => Usage.FloatDecimal34,
        RepositoryUsage.BinaryChar => Usage.BinaryChar,
        RepositoryUsage.BinaryShort => Usage.BinaryShort,
        RepositoryUsage.BinaryLong => Usage.BinaryLong,
        RepositoryUsage.BinaryDouble => Usage.BinaryDouble,
        _ => throw new UnreachableException($"RepositoryUsage {(int)u} is no persisted usage"),
    };

    public static RepositoryGroupUsage Persisted(GroupUsage g) => g switch
    {
        GroupUsage.None => RepositoryGroupUsage.None,
        GroupUsage.Bit => RepositoryGroupUsage.Bit,
        GroupUsage.National => RepositoryGroupUsage.National,
        _ => throw new UnreachableException($"GroupUsage.{g} has no persisted value"),
    };

    public static GroupUsage FromPersisted(RepositoryGroupUsage g) => g switch
    {
        RepositoryGroupUsage.None => GroupUsage.None,
        RepositoryGroupUsage.Bit => GroupUsage.Bit,
        RepositoryGroupUsage.National => GroupUsage.National,
        _ => throw new UnreachableException($"RepositoryGroupUsage {(int)g} is no persisted group usage"),
    };

    public static RepositoryPrefix Persisted(DynamicLengthPrefix p) => p switch
    {
        DynamicLengthPrefix.None => RepositoryPrefix.None,
        DynamicLengthPrefix.Unsigned32 => RepositoryPrefix.Unsigned32,
        DynamicLengthPrefix.Signed32 => RepositoryPrefix.Signed32,
        DynamicLengthPrefix.Unsigned16 => RepositoryPrefix.Unsigned16,
        DynamicLengthPrefix.Signed16 => RepositoryPrefix.Signed16,
        _ => throw new UnreachableException($"DynamicLengthPrefix.{p} has no persisted value"),
    };

    public static DynamicLengthPrefix FromPersisted(RepositoryPrefix p) => p switch
    {
        RepositoryPrefix.None => DynamicLengthPrefix.None,
        RepositoryPrefix.Unsigned32 => DynamicLengthPrefix.Unsigned32,
        RepositoryPrefix.Signed32 => DynamicLengthPrefix.Signed32,
        RepositoryPrefix.Unsigned16 => DynamicLengthPrefix.Unsigned16,
        RepositoryPrefix.Signed16 => DynamicLengthPrefix.Signed16,
        _ => throw new UnreachableException($"RepositoryPrefix {(int)p} is no persisted prefix"),
    };

    public static RepositoryLiteralClass Persisted(LiteralClass c) => c switch
    {
        LiteralClass.Alphanumeric => RepositoryLiteralClass.Alphanumeric,
        LiteralClass.National => RepositoryLiteralClass.National,
        LiteralClass.Boolean => RepositoryLiteralClass.Boolean,
        _ => throw new UnreachableException($"LiteralClass.{c} has no persisted value"),
    };

    public static LiteralClass FromPersisted(RepositoryLiteralClass c) => c switch
    {
        RepositoryLiteralClass.Alphanumeric => LiteralClass.Alphanumeric,
        RepositoryLiteralClass.National => LiteralClass.National,
        RepositoryLiteralClass.Boolean => LiteralClass.Boolean,
        _ => throw new UnreachableException($"RepositoryLiteralClass {(int)c} is no persisted literal class"),
    };

    public static RepositoryNumericDisplay Persisted(NumericDisplayForm f) => f switch
    {
        NumericDisplayForm.Decimal => RepositoryNumericDisplay.Decimal,
        NumericDisplayForm.Digits => RepositoryNumericDisplay.Digits,
        _ => throw new UnreachableException($"NumericDisplayForm.{f} has no persisted value"),
    };

    public static NumericDisplayForm FromPersisted(RepositoryNumericDisplay f) => f switch
    {
        RepositoryNumericDisplay.Decimal => NumericDisplayForm.Decimal,
        RepositoryNumericDisplay.Digits => NumericDisplayForm.Digits,
        _ => throw new UnreachableException($"RepositoryNumericDisplay {(int)f} is no persisted numeric display form"),
    };

    /// <summary>The SYNCHRONIZED clause; null (no clause) is <see cref="RepositorySynchronized.None"/>.</summary>
    public static RepositorySynchronized Persisted(SynchronizedPhrase? s) => s switch
    {
        null => RepositorySynchronized.None,
        SynchronizedPhrase.Unqualified => RepositorySynchronized.Unqualified,
        SynchronizedPhrase.Left => RepositorySynchronized.Left,
        SynchronizedPhrase.Right => RepositorySynchronized.Right,
        _ => throw new UnreachableException($"SynchronizedPhrase.{s} has no persisted value"),
    };

    public static SynchronizedPhrase? FromPersisted(RepositorySynchronized s) => s switch
    {
        RepositorySynchronized.None => null,
        RepositorySynchronized.Unqualified => SynchronizedPhrase.Unqualified,
        RepositorySynchronized.Left => SynchronizedPhrase.Left,
        RepositorySynchronized.Right => SynchronizedPhrase.Right,
        _ => throw new UnreachableException($"RepositorySynchronized {(int)s} is no persisted SYNCHRONIZED form"),
    };

    public static RepositoryRaisingKind Persisted(RaisingTargetKind k) => k switch
    {
        RaisingTargetKind.ExceptionName => RepositoryRaisingKind.ExceptionName,
        RaisingTargetKind.ObjectClass => RepositoryRaisingKind.ObjectClass,
        RaisingTargetKind.Interface => RepositoryRaisingKind.Interface,
        _ => throw new UnreachableException($"RaisingTargetKind.{k} has no persisted value"),
    };

    public static RaisingTargetKind FromPersisted(RepositoryRaisingKind k) => k switch
    {
        RepositoryRaisingKind.ExceptionName => RaisingTargetKind.ExceptionName,
        RepositoryRaisingKind.ObjectClass => RaisingTargetKind.ObjectClass,
        RepositoryRaisingKind.Interface => RaisingTargetKind.Interface,
        _ => throw new UnreachableException($"RepositoryRaisingKind {(int)k} is no persisted RAISING kind"),
    };

    /// <summary>An object-reference description's kind; no description (not an object reference) is
    /// <see cref="ObjectReferenceKind.None"/>. The forward half is the descriptor's own <see cref="ObjectRefDescriptor.RuntimeKind"/>.</summary>
    public static ObjectReferenceKind Persisted(ObjectRefDescriptor? d) => d?.RuntimeKind ?? ObjectReferenceKind.None;

    public static ObjectRefKind? FromPersisted(ObjectReferenceKind k) => k switch
    {
        ObjectReferenceKind.None => null,
        ObjectReferenceKind.Universal => ObjectRefKind.Universal,
        ObjectReferenceKind.Interface => ObjectRefKind.Interface,
        ObjectReferenceKind.ObjectClass => ObjectRefKind.ObjectClass,
        ObjectReferenceKind.ActiveClass => ObjectRefKind.ActiveClass,
        _ => throw new UnreachableException($"ObjectReferenceKind {(int)k} is no persisted object-reference kind"),
    };
}
