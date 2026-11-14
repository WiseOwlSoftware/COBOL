// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding;

/// <summary>
/// The types of user-defined words — ISO §2023:8.3.2.2, one member per entry of its list, in the list's order. A
/// declaration of any of them is announced through <see cref="DataBinder.DeclareUserWord"/>, the one funnel the
/// rules about a user-defined word AS DECLARED are asked from (today: §2023:8.3.2.1 rule 5 and §2023:12.3.8.3 SR12/SR13 —
/// an intrinsic-function-name the REPOSITORY paragraph identifies shall not be a user-defined word in its scope —
/// and §2023:8.3.2.2's one-type-per-word rule, kb/Work PB990, whose exceptions are <see cref="UserWordKinds.MayBeOneWord"/>).
/// <para>⛔ THE LIST IS THE SPEC'S, NOT OURS (kb/Work PB1083). The screen used to be called from six hand-picked
/// declaration funnels, so an alphabet-name, a class-name, a symbolic-character or a contained program's name
/// spelling a REPOSITORY intrinsic compiled clean. <c>UserWordDeclarationDriftTests</c> parses §2023:8.3.2.2's list from
/// <c>specs/ISO_COBOL_2023.md</c> and fails when a type has no member here, and when a member is neither declared at some
/// call site nor carried on its <c>NotDeclaredInASourceElement</c> exemption list with the reason.</para>
/// </summary>
public enum UserWordKind
{
    AlphabetName,
    /// <summary>A cd-name (ISO/IEC 1989:2002 8.3.1.1.1.2): no 2023 type, since the communication facility left the standard at 2014.</summary>
    CdName,
    ClassName,
    CompilationVariableName,
    ConditionName,
    ConstantName,
    DataName,
    DirectiveName,
    DynamicLengthStructureName,
    FileName,
    FunctionPrototypeName,
    IndexName,
    InterfaceName,
    LevelNumber,
    /// <summary>A library-name (§1985:IV.4.2.2.1.1, the COPY statement's library-name-1): a user-defined word of a disjoint set of
    /// its own at COBOL-85 only; ISO 2002 made it a system-name (§2002:F.2, item 135; kb/Work PB5482).</summary>
    LibraryName,
    LocaleName,
    MethodName,
    MnemonicName,
    ObjectClassName,
    OrderingName,
    ParagraphName,
    ParameterName,
    ProgramName,
    ProgramPrototypeName,
    PropertyName,
    RecordKeyName,
    RecordName,
    ReportName,
    /// <summary>A routine-name (§1985:IV.4.2.2.1.1, the ENTER statement's routine-name-1, §1985:VI.6.12.3 SR2): no 2002 or 2023
    /// type, since ISO 2002 deleted the ENTER statement (kb/Work PB5178).</summary>
    RoutineName,
    ScreenName,
    SectionName,
    SymbolicCharacter,
    /// <summary>A text-name (§1985:IV.4.2.2.1.1, the COPY statement's text-name-1): a user-defined word of a disjoint set of its
    /// own at COBOL-85 only; ISO 2002 made it a system-name (§2002:F.2, item 135; kb/Work PB5482).</summary>
    TextName,
    TypeName,
    UserFunctionName,
}

/// <summary>The §2023:8.3.2.2 spelling of a <see cref="UserWordKind"/> — for diagnostics, and the key the drift test
/// matches against the standard's list — and the ONE statement of which types may SHARE a word.</summary>
public static class UserWordKinds
{
    /// <summary>⛔ §2023:8.3.2.2 EXCEPTION 3 AS DATA: "<i>the same name may be used as any of the following types of
    /// user-defined words — constant-name, data-name, property-name, record-key-name, record-name</i>". The drift test
    /// parses the standard's list and fails when this set and the list disagree (kb/Work PB990).</summary>
    public static IReadOnlySet<UserWordKind> MayShareOneName { get; } = new HashSet<UserWordKind>
    {
        UserWordKind.ConstantName, UserWordKind.DataName, UserWordKind.PropertyName, UserWordKind.RecordKeyName,
        UserWordKind.RecordName,
    };

    /// <summary>§2023:8.3.2.2 EXCEPTION 2 AS DATA: "<i>a level-number may be the same as a paragraph-name or a
    /// section-name</i>".</summary>
    public static IReadOnlySet<UserWordKind> MayShareWithALevelNumber { get; } = new HashSet<UserWordKind>
    {
        UserWordKind.ParagraphName, UserWordKind.SectionName,
    };

    /// <summary>⛔ THE TYPES THAT MAY BE LETTERLESS, AS DATA (§2023:8.3.2.2: "<i>With the exception of section-names,
    /// paragraph-names, and level-numbers, each user-defined word shall contain at least one basic letter or extended
    /// letter</i>"). A paragraph-name <c>1-2</c> is legal, so the lexer must admit the token, and every OTHER type is
    /// held to the rule where it is declared (<c>DataBinder.DeclareUserWord</c>; kb/Work PB1403). The drift test reads
    /// the exception list from the standard's text.</summary>
    public static IReadOnlySet<UserWordKind> MayBeLetterless { get; } = new HashSet<UserWordKind>
    {
        UserWordKind.SectionName, UserWordKind.ParagraphName, UserWordKind.LevelNumber,
    };

    /// <summary>⛔ THE TYPES WHOSE EVERY DECLARATION CAN BE REFERENCED ONLY UNQUALIFIED, AS DATA (kb/Work PB3216): a
    /// reference "shall contain a reference that uniquely identifies that resource" (ISO §2023:8.4.2.1), and these words
    /// have no qualifier (§2023:8.4.2.2 qualifies data, condition, file-report and index names), so a SECOND declaration of
    /// one in a source element leaves every reference to it ambiguous. <c>DataBinder.DeclareUserWord</c> refuses the
    /// second under <c>COBOL4250</c>; a switch's condition-names (the <c>ConditionName</c> type is also the
    /// level-88 entries', which qualification tells apart) ask the same check explicitly at their declaration. A new
    /// SPECIAL-NAMES declaration kind joins this set and nothing else.</summary>
    public static IReadOnlySet<UserWordKind> DeclaredUnqualifiable { get; } = new HashSet<UserWordKind>
    {
        UserWordKind.AlphabetName, UserWordKind.ClassName, UserWordKind.DynamicLengthStructureName,
        UserWordKind.LocaleName, UserWordKind.MnemonicName, UserWordKind.OrderingName,
    };

    /// <summary>⛔ THE ONE ANSWER to "may one word be used as BOTH of these types of user-defined word in a source
    /// element?" (ISO §2023:8.3.2.2: "<i>a given user-defined word may be used as only one type of user-defined word</i>"
    /// with three exceptions): the same type (a word declared twice as one type is a uniqueness question,
    /// §2023:8.4.2, asked elsewhere), exception 1 (a compilation-variable-name may be the same as ANY other type),
    /// exception 2 (<see cref="MayShareWithALevelNumber"/>) and exception 3 (<see cref="MayShareOneName"/>).</summary>
    public static bool MayBeOneWord(UserWordKind a, UserWordKind b) =>
        a == b
        || a == UserWordKind.CompilationVariableName || b == UserWordKind.CompilationVariableName
        || (a == UserWordKind.LevelNumber && MayShareWithALevelNumber.Contains(b))
        || (b == UserWordKind.LevelNumber && MayShareWithALevelNumber.Contains(a))
        || (MayShareOneName.Contains(a) && MayShareOneName.Contains(b));

    /// <summary>Whether <paramref name="kind"/> is a COPY statement's text-name or library-name — the two types that are user-defined
    /// words below COBOL-2002 only (<see cref="Editions.EditionInfo.CopyNamesAreUserDefinedWords"/>; kb/Work PB5482).</summary>
    public static bool IsCopyName(UserWordKind kind) => kind is UserWordKind.TextName or UserWordKind.LibraryName;

    /// <summary>The standard's own name for the type (<c>AlphabetName</c> → <c>alphabet-name</c>).</summary>
    public static string Spelling(this UserWordKind kind) =>
        string.Concat(kind.ToString().Select((c, i) =>
            char.IsUpper(c) && i > 0 ? "-" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
}
