namespace Claims.Api.Features.Guias;

internal sealed record GuiaResponse(
    Guid Id,
    int BeneficiarioId,
    int PrestadorId,
    DateOnly DataAtendimento,
    Guid? LoteId);
