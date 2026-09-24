namespace Claims.Api.Domain;

internal sealed class GuiaConsulta
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public int BeneficiarioId { get; set; }
    public int PrestadorId { get; set; }
    public DateOnly DataAtendimento { get; set; }
    public Guid? LoteId { get; set; }
}
