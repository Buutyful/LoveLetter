﻿using LoveLetter.GameCore;

namespace LoveLetter.Api.Data.Models;

public abstract record PendingRequests(Guid Id);
public record CardSelectionRequest(
    Guid Id,
    int CardsToSelect,
    TaskCompletionSource<Card[]> PendingTask,
    CancellationTokenSource Cts) : PendingRequests(Id);
public record CardPlayRequest(
    Guid Id,
    TaskCompletionSource<ActionParameters> PendingTask,
    CancellationTokenSource Cts) : PendingRequests(Id);