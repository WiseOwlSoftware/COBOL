// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Semantics;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.CodeGen;

/// <summary>
/// Locates a COBOL data item within its ProgramState byte buffer.
/// The CIL emitter uses this to generate Span slicing and runtime
/// MOVE/COMPARE calls against the correct byte range.
/// </summary>
/// <param name="Area">Which storage area (WORKING-STORAGE, FILE SECTION, etc.) owns the bytes.</param>
/// <param name="Offset">Byte offset from the start of the storage area.</param>
/// <param name="Length">Total byte length, including OCCURS expansion.</param>
/// <param name="Pic">PIC descriptor carrying category, editing, sign, and format metadata for runtime dispatch.</param>
public readonly record struct StorageLocation(
    StorageAreaKind Area,
    int Offset,
    int Length,
    PicDescriptor Pic);

/// <summary>
/// Compiler-side bridge that creates <see cref="PicDescriptor"/> instances from
/// <see cref="DataSymbol"/>s. Delegates PIC string parsing to the canonical
/// <see cref="Runtime.PicDescriptorFactory"/>, then overlays compiler-only
/// knowledge: storage length (from layout), explicit SIGN clause, JUSTIFIED RIGHT,
/// and the IsGroup flag for group items.
/// </summary>
public static class CompilerPicDescriptorFactory
{
    /// <summary>
    /// Builds a <see cref="PicDescriptor"/> for <paramref name="symbol"/>,
    /// combining runtime PIC parsing with compiler-specific overrides.
    /// For group items (no PIC string), returns an alphanumeric descriptor with IsGroup set.
    /// </summary>
    public static PicDescriptor FromDataSymbol(DataSymbol symbol, int storageLength,
        Runtime.PicEnvironment? environment = null)
    {
        var env = environment ?? Runtime.PicEnvironment.Default;
        var pic = symbol.ResolvedType?.Pic;
        bool isSigned = pic?.IsSigned ?? false;
        var signStorage = DetermineSignStorage(isSigned, symbol);

        // Single pipeline: all PIC semantics come from the canonical runtime factory.
        // The compiler only overlays storage length (from layout) and sign storage
        // (from explicit SIGN clause in data description).
        if (symbol.PicString != null)
        {
            bool blankWhenZero = pic?.BlankWhenZero ?? false;
            var desc = Runtime.PicDescriptorFactory.FromPicBody(
                symbol.PicString,
                usage: symbol.Usage,
                isSigned: isSigned,
                signStorage: signStorage,
                blankWhenZero: blankWhenZero,
                environment: env);

            return new PicDescriptor(
                totalDigits: desc.TotalDigits,
                fractionDigits: desc.FractionDigits,
                isSigned: desc.IsSigned,
                isNumeric: desc.IsNumeric,
                isAlphanumeric: desc.IsAlphanumeric,
                hasEditing: desc.HasEditing,
                storageLength: storageLength,
                usage: symbol.Usage,
                category: desc.Category,
                signStorage: signStorage,
                editing: desc.Editing,
                blankWhenZero: desc.BlankWhenZero,
                leadingScaleDigits: desc.LeadingScaleDigits,
                trailingScaleDigits: desc.TrailingScaleDigits,
                editPattern: desc.EditPattern,
                isJustifiedRight: symbol.IsJustifiedRight,
                environment: env);
        }

        // Group items (no PIC): alphanumeric DISPLAY
        var category = symbol.ResolvedType?.Category ?? CobolCategory.Alphanumeric;
        return new PicDescriptor(
            totalDigits: 0,
            fractionDigits: 0,
            isSigned: false,
            isNumeric: false,
            isAlphanumeric: true,
            hasEditing: false,
            storageLength: storageLength,
            usage: symbol.Usage,
            category: category,
            signStorage: SignStorageKind.None,
            editing: EditingKind.None,
            blankWhenZero: false,
            leadingScaleDigits: 0,
            trailingScaleDigits: 0,
            editPattern: null,
            environment: env) { IsGroup = true };
    }

    /// <summary>
    /// Resolves sign storage: explicit SIGN clause wins, otherwise defaults to
    /// trailing overpunch (the COBOL standard default for DISPLAY numerics).
    /// </summary>
    private static SignStorageKind DetermineSignStorage(bool isSigned, DataSymbol symbol)
    {
        if (!isSigned) return SignStorageKind.None;

        if (symbol.ExplicitSignStorage.HasValue)
            return symbol.ExplicitSignStorage.Value;

        return SignStorageKind.TrailingOverpunch;
    }
}
