using Audit.Application.Models.Results.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace Audit.Application.Models.Commands.Ingestion;

public sealed record IngestAuditEventCommand(
    string MessageId,
    string EventType,
    string AggregateType,
    string AggregateId,
    string? AggregatePublicId,
    int? AggregateVersion,
    string PayloadJson,
    string? HeadersJson,
    string? CorrelationId,
    long? InitiatorUserId,
    int Priority,
    DateTime OccurredAtUtc,
    DateTime? PublishedAtUtc,
    string ConsumerName)
    : IRequest<Result<IngestAuditEventResult>>;