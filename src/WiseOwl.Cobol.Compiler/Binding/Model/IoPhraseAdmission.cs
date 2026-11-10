// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>The statements that can be written with an INVALID KEY phrase.</summary>
internal enum InvalidKeyVerb { Read, Write, Rewrite, Delete, Start }

/// <summary>
/// ⛔ <b>THE ONE "CAN THIS STATEMENT BE WRITTEN WITH AN INVALID KEY PHRASE?" QUESTION</b> (kb/Work PB5076). Two rules ask it
/// of the same statement and file: FLAG-14 d (<c>FlagConformancePass</c>, §2023:7.3.15.4 GR4 d: a statement that "can be
/// specified with an INVALID KEY phrase" but omits it while an open-mode USE declarative is in effect) and COBOL-85's
/// mandatory-phrase syntax rules (<c>ExceptionPhraseRequirement</c>: §1985:VIII.4.9.3 SR3 and its siblings make the
/// phrase mandatory where it can be written and no applicable USE procedure is specified). They are one question, so
/// the answer is written once.
/// </summary>
internal static class IoPhraseAdmission
{
    /// <summary>Whether <paramref name="verb"/> on <paramref name="file"/> "can be specified with an INVALID KEY phrase" — the
    /// syntax rules of each statement decide, not the file's organization alone. A file with SEQUENTIAL / LINE SEQUENTIAL
    /// organization never raises an invalid-key condition (§2023:12.4.5.10 — only a RELATIVE or INDEXED, i.e. keyed, file
    /// does); then WRITE takes the phrase for every keyed file (§2023:14.9.51.3 SR3: format 2), START and a random READ
    /// likewise, but REWRITE shall not specify it "for a file with sequential organization or a file with relative
    /// organization and sequential access mode" (§2023:14.9.35.3 SR2) and DELETE RECORD shall not when it "references a
    /// file that is in sequential access mode" (§2023:14.9.10.3 SR2). Flagging a statement the phrase cannot be written on
    /// is a false warning no source change can silence.</summary>
    public static bool AdmitsInvalidKey(InvalidKeyVerb verb, FileModel? file)
        => file is { Organization: FileOrganization.Relative or FileOrganization.Indexed } && verb switch
        {
            InvalidKeyVerb.Rewrite => !(file.Organization == FileOrganization.Relative && file.AccessMode == FileAccessMode.Sequential),
            InvalidKeyVerb.Delete => file.AccessMode != FileAccessMode.Sequential,
            _ => true,
        };
}
