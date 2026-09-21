namespace Claims.Api.Domain;
public enum StatusLote { Aberto, Fechado }
public sealed class Lote { public Guid Id { get; set; } public StatusLote Status { get; set; } public DateTime CriadoEmUtc { get; set; } public DateTime? FechadoEmUtc { get; set; } }
