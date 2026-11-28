// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>What a line is to an obsolete IDENTIFICATION comment-entry paragraph (AUTHOR / INSTALLATION /
/// DATE-WRITTEN / DATE-COMPILED / SECURITY / REMARKS): not part of one, the header line that opens it, or the Area-B
/// body of its comment-entry — and, when the division could not be told, the same two as a CANDIDATE, which the
/// conversion CARRIES (<see cref="CommentEntryCarrier"/>) for <see cref="CommentEntryResolver"/> to decide.</summary>
internal enum CommentEntryText { None, Header, Body, CandidateHeader, CandidateBody }
