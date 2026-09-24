using Claims.Api.Data;
using Claims.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Claims.Api.Features.Guias;
/// <summary>
/// Viadao
/// </summary>
// Agrupa as rotas de criação e consulta de guias de consulta.
internal static class GuiaEndpoints
{
    // Declara as rotas e seus possíveis status para o documento OpenAPI.

    internal static IEndpointRouteBuilder MapGuiaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/guias");

        group.MapPost("", CriarAsync)
            .Produces<GuiaResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapGet("/{id:guid}", ObterAsync)
            .WithName("ConsultarGuia")
            .Produces<GuiaResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> CriarAsync(CriarGuiaRequest request, ClaimsDbContext db, CancellationToken cancellationToken)
    {
        // Valida o formato básico antes de consultar o banco.
        if (request.BeneficiarioId <= 0 || request.PrestadorId <= 0 || request.DataAtendimento == default)
            return Problem(StatusCodes.Status400BadRequest, "invalid_request", "Informe identificadores positivos e uma data de atendimento válida.");

        // Confirma que os cadastros referenciados existem.
        var beneficiario = await db.Beneficiarios.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.BeneficiarioId, cancellationToken);
        if (beneficiario is null)
            return Problem(StatusCodes.Status404NotFound, "beneficiary_not_found", "Beneficiário não encontrado.");

        var prestador = await db.Prestadores.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.PrestadorId, cancellationToken);
        if (prestador is null)
            return Problem(StatusCodes.Status404NotFound, "provider_not_found", "Prestador não encontrado.");

        // Aplica as regras de elegibilidade para a data do atendimento.
        if (!beneficiario.Ativo)
            return Problem(StatusCodes.Status409Conflict, "beneficiary_inactive", "O beneficiário está inativo.");

        if (!prestador.CredenciadoAtivo)
            return Problem(StatusCodes.Status409Conflict, "provider_inactive", "O prestador não possui credenciamento ativo.");

        if (request.DataAtendimento < beneficiario.InicioVigencia)
            return Problem(StatusCodes.Status409Conflict, "coverage_not_started", "A data do atendimento é anterior ao início da vigência do beneficiário.");

        if (beneficiario.FimCarencia is { } fimCarencia && request.DataAtendimento < fimCarencia)
            return Problem(StatusCodes.Status409Conflict, "waiting_period", "A data do atendimento é anterior ao fim do período de carência.");

        // Persiste a guia e responde 201 com a URL que permite consultá-la.
        var guia = new GuiaConsulta {
            BeneficiarioId = beneficiario.Id,
            PrestadorId = prestador.Id,
            DataAtendimento = request.DataAtendimento
        };

        db.GuiasConsulta.Add(guia);
        await db.SaveChangesAsync(cancellationToken);

        var response = ToResponse(guia);
        return Results.CreatedAtRoute("ConsultarGuia", new { id = guia.Id }, response);
    }

    private static async Task<IResult> ObterAsync(Guid id, ClaimsDbContext db, CancellationToken cancellationToken)
    {
        // Consulta sem rastreamento porque este endpoint não altera a entidade.
        var guia = await db.GuiasConsulta.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        return guia is null
            ? Problem(StatusCodes.Status404NotFound, "guide_not_found", "Guia não encontrada.")
            : Results.Ok(ToResponse(guia));
    }

    // Expõe somente os campos do contrato HTTP, sem serializar a entidade do EF Core.
    private static GuiaResponse ToResponse(GuiaConsulta guia) =>
        new(guia.Id, guia.BeneficiarioId, guia.PrestadorId, guia.DataAtendimento, guia.LoteId);

    // Mantém respostas de erro consistentes e inclui um código estável para clientes da API.
    private static IResult Problem(int status, string code, string detail) =>
        Results.Problem(
            statusCode: status,
            title: status switch {
                StatusCodes.Status400BadRequest => "Requisição inválida",
                StatusCodes.Status404NotFound => "Recurso não encontrado",
                _ => "Conflito de regra de negócio"
            },
            detail: detail,
            extensions: new Dictionary<string, object?> { ["code"] = code });
}
