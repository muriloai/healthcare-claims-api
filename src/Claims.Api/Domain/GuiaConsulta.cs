namespace Claims.Api.Domain;
public sealed class GuiaConsulta { public Guid Id { get; set; } public int BeneficiarioId { get; set; } public int PrestadorId { get; set; } public DateOnly DataAtendimento { get; set; } public Guid? LoteId { get; set; } }
