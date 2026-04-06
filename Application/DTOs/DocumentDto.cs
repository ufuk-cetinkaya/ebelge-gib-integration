namespace Application.DTOs;

public record DocumentDto
{
    public string BelgeNumarasi { get; set; } = null!;
    public string Ettn { get; set; } = null!;
    public DateTime BelgeTarihi { get; set; }
    public string Senaryo { get; set; } = null!;
    public int DurumKodu { get; set; }
    public string DurumAciklamasi { get; set; } = null!;
    public string? BelgeTipi { get; set; }
    public decimal? OdenecekTutar { get; set; }
    public string? ParaBirimi { get; set; }
    public string GondericiVergiNo { get; set; } = null!;
    public string GondericiUnvan { get; set; } = null!;
    public string MusteriVergiNo { get; set; } = null!;
    public string MusteriUnvan { get; set; } = null!;
    public string Yon { get; set; } = null!;
    public string IptalMi { get; set; } = null!;
    public DateTime? IptalTarihi { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public string? ReferansNo { get; set; }
    public string? YanitKodu { get; set; }
    public string? YanitAciklamasi { get; set; }
}