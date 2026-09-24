namespace Claims.Api.Domain;

internal sealed class Beneficiario
{
    public int Id { get; set; }
    public required string NomeFicticio { get; set; }
    public required string Carteirinha { get; set; }
    public bool Ativo { get; set; }
    public DateOnly InicioVigencia { get; set; }
    public DateOnly? FimCarencia { get; set; }
}
