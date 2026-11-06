// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime.IO.Store;

/// <summary>The trees one statement edited, each with the root it sets in the meta: <see cref="PrimeRoot"/> for the
/// prime (relative: record-number) tree, an alternate key's index for its tree.</summary>
internal readonly record struct TreeChange(BTree Tree, int Root)
{
    /// <summary>The <see cref="Root"/> of the prime (relative: record-number) tree.</summary>
    public const int PrimeRoot = -1;
}

/// <summary>What a statement's in-memory edit did: the page layer's status, whether the change applies (false: the
/// tree declined it — a key already present, a key absent — and nothing is written), and every tree it edited.</summary>
internal readonly record struct Edit(StoreStatus Status, bool Done, IReadOnlyList<TreeChange> Trees);

/// <summary>What a statement adds to its trees' pages in the SAME page statement: trees of its own, flushed after the
/// edited ones, whose roots it sets itself. The one client is a keyed store under commit control (its UNDO, TRANSACTIONS and
/// HOLDINGS trees, 2.2.1.10); a statement with no transaction has none.</summary>
internal interface IStatementAddition
{
    /// <summary>The trees the addition edited, counted into the statement's bound (<see cref="StatementBound.Of"/>).</summary>
    BTree[] Trees { get; }

    /// <summary>Flush the addition's trees into <paramref name="statement"/> and set their roots; on a refusal the addition
    /// has discarded its own pending edits.</summary>
    StoreStatus Flush(PageStatement statement);

    /// <summary>Discard every pending edit, for a statement that is refused before it flushes.</summary>
    void Discard();
}

/// <summary>⛔ ONE EDIT IS ONE STATEMENT OF THE PAGE LAYER, HOWEVER MANY TREES IT CHANGES, FOR EVERY CLIENT OF THE ENGINE
/// (docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.6 and 2.2.1.19; DESIGN-communication-facility.md §8). The keyed store
/// (<c>KeyedStore.Change</c>) and the message control system's store (<c>McsStore</c>, DESIGN-communication-facility.md §9 item 3, not yet built) each enter the statement and edit their
/// trees in memory, then land them through <see cref="Complete"/>: the transaction-free core, so there is one sequence of
/// sizing, beginning, flushing, rooting and completing, and one place the pending edits are discarded on a refusal.</summary>
internal static class StatementLanding
{
    /// <summary>The page statement that lands <paramref name="trees"/> (and <paramref name="addition"/>'s trees): sized to
    /// exactly the pages the edits write (<see cref="StatementBound.Of"/>: its claim, its reservation), begun
    /// (<see cref="PagedFile.Begin"/>, the statement's entry already made by the caller, <c>entered: true</c>), every tree
    /// flushed and its root set (<see cref="TreeChange.PrimeRoot"/> is the prime root, any other root an alternate's index),
    /// and the statement completed — page 2 last. <paramref name="ordinals"/> release ordinals and
    /// <paramref name="drawnTransaction"/> are the statement's other mints (a client with neither passes none).
    /// <para>Answers <see cref="StoreStatus.Ok"/> only when the statement LANDED. A torn page or a refused write answers '30'
    /// and a boundary '24' (<see cref="StoreStatus.Boundary"/>), page 2 then naming the previous statement's trees (R1), and
    /// on ANY refusal every pending edit of <paramref name="trees"/> and <paramref name="addition"/> is discarded, so the
    /// caller gives back what it drew and answers.</para></summary>
    public static StoreStatus Complete(PagedFile file, IReadOnlyList<TreeChange> trees, int ordinals = 0,
        ulong drawnTransaction = 0, IStatementAddition? addition = null)
    {
        var status = file.Begin(StatementBound.Of([.. trees.Select(t => t.Tree), .. addition?.Trees ?? []]),
            out var statement, ordinals: ordinals, drawnTransaction: drawnTransaction, entered: true);
        if (status != StoreStatus.Ok)
        {
            Discard(trees);
            addition?.Discard();
            return status;
        }
        var flushed = StoreStatus.Ok;
        foreach (var (tree, root) in trees)
        {
            if (flushed != StoreStatus.Ok) { tree.Discard(); continue; }
            flushed = tree.Flush(statement!, out var pointer);
            if (flushed != StoreStatus.Ok) continue;
            if (root == TreeChange.PrimeRoot) statement!.SetPrimeRoot(pointer);
            else statement!.SetAlternateRoot(root, pointer);
        }
        if (addition is not null)
        {
            if (flushed == StoreStatus.Ok) flushed = addition.Flush(statement!);
            else addition.Discard();
        }
        var completed = statement!.Complete();
        if (flushed == StoreStatus.Ok && completed == StoreStatus.Ok) return StoreStatus.Ok;
        return completed == StoreStatus.Ok ? StoreStatus.PermanentError : completed;
    }

    /// <summary>Discard every pending edit of <paramref name="trees"/>: the statement is refused before it flushes.</summary>
    public static void Discard(IReadOnlyList<TreeChange> trees)
    {
        foreach (var t in trees) t.Tree.Discard();
    }
}
