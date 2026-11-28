// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;

using WiseOwl.Cobol.Editions;

using Core = WiseOwl.Cobol.Frontend.Parsing.CobolParserCore;

namespace WiseOwl.Cobol.Compiler.Binding;

/// <summary>⛔ THE ONE CLASSIFICATION OF A STATEMENT AS AN INPUT-OUTPUT STATEMENT (kb/Work PB6032): the parse tree's statement sorted
/// into its <see cref="IoStatementForms"/> member here, and asked of the edition's own set (<see cref="InputOutputStatementSet"/>), so
/// "is this an input-output statement" has one answer per edition: COBOL-85's fifteen (§1985:III.2 p139), the later editions' eight
/// (§2023:9.1.13.1). A rule that names the term asks <see cref="Of"/>; none spells its own list of statement contexts.</summary>
internal static class InputOutputStatementForms
{
    /// <summary>The form statement <paramref name="s"/> is written in, or <see cref="IoStatementForms.None"/> for a statement that is no
    /// input-output statement at any edition. ACCEPT MESSAGE COUNT (a cd-name's) and the 2023 message-control RECEIVE and SEND are
    /// their own grammar rules, and every SET format but the switch one (§1985:VI.6.23 format 3) is another rule of the SET
    /// statement, so each is told apart by its rule alone.</summary>
    public static IoStatementForms FormOf(Core.StatementContext s) => s.GetChild(0) switch
    {
        Core.AcceptStatementContext => IoStatementForms.AcceptIdentifier,
        Core.CloseStatementContext => IoStatementForms.Close,
        Core.DeleteStatementContext or Core.DeleteFileStatementContext => IoStatementForms.Delete,
        Core.DisableStatementContext => IoStatementForms.Disable,
        Core.DisplayStatementContext => IoStatementForms.Display,
        Core.EnableStatementContext => IoStatementForms.Enable,
        Core.OpenStatementContext => IoStatementForms.Open,
        Core.PurgeStatementContext => IoStatementForms.Purge,
        Core.ReadStatementContext => IoStatementForms.Read,
        Core.CommunicationReceiveStatementContext => IoStatementForms.Receive,
        Core.RewriteStatementContext => IoStatementForms.Rewrite,
        Core.CommunicationSendStatementContext => IoStatementForms.Send,
        Core.SetStatementContext { ChildCount: > 0 } set when set.GetChild(0) is Core.SetSwitchStatementContext => IoStatementForms.SetSwitch,
        Core.StartStatementContext => IoStatementForms.Start,
        Core.UnlockStatementContext => IoStatementForms.Unlock,
        Core.WriteStatementContext => IoStatementForms.Write,
        _ => IoStatementForms.None,
    };

    /// <summary>Statement <paramref name="s"/>'s own node when it is an input-output statement of <paramref name="edition"/>, else null.</summary>
    public static ParserRuleContext? Of(Core.StatementContext s, EditionInfo edition) =>
        FormOf(s) is var form && form != IoStatementForms.None && InputOutputStatementSet.Of(edition.Year).HasFlag(form)
            ? (ParserRuleContext)s.GetChild(0)
            : null;
}
