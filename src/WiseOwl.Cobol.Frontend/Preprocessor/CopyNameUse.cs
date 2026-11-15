// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Common;

namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>The text-name or library-name a COPY statement wrote as a COBOL word (kb/Work PB5482): the word, whether it is
/// the statement's library-name-1 rather than its text-name-1, and the file and physical line of the COPY statement. A name
/// written as a literal is no user-defined word and is not recorded. Below COBOL-2002 each is a user-defined word of a disjoint
/// set of its own, so the binder's one word-type funnel (<c>DataBinder.DeclareUserWord</c>) is told each of them
/// (§1985:IV.4.2.2.1.1); <see cref="CopyProcessor.CopyNames"/> is where the preprocessor, which alone resolves the statement,
/// hands them over.</summary>
public readonly record struct CopyNameUse(string Name, bool IsLibraryName, SourceOrigin At);
