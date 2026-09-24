namespace Claims.Api.Features.Guias;

internal sealed record CriarGuiaRequest(int BeneficiarioId, int PrestadorId, DateOnly DataAtendimento);
