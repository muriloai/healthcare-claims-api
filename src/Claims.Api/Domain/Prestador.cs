namespace Claims.Api.Domain;

internal sealed class Prestador
{
    public int Id { get; set; }
    public required string NomeFicticio { get; set; }
    public bool CredenciadoAtivo { get; set; }
}
