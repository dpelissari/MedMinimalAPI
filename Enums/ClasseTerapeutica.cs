using System.ComponentModel;

public enum ClasseTerapeutica
{
    [Description("Analgésico")]
    Analgesico = 10,

    [Description("Antitérmico / Antipirético")]
    Antipiretico = 20,

    [Description("Anti-inflamatório")]
    AntiInflamatorio = 30,

    [Description("Antibiótico")]
    Antibiotico = 40,

    [Description("Antifúngico")]
    Antifungico = 50,

    [Description("Antiviral")]
    Antiviral = 60,

    [Description("Antialérgico")]
    Antialergico = 70,

    [Description("Anti-hipertensivo")]
    AntiHipertensivo = 80,

    [Description("Antidiabético")]
    Antidiabetico = 90,

    [Description("Antidepressivo")]
    Antidepressivo = 100,

    [Description("Ansiolítico")]
    Ansiolitico = 110,

    [Description("Protetor Gástrico")]
    Gastroprotetor = 120
}
