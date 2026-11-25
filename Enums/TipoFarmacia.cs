using System.ComponentModel;

public enum TipoFarmacia
{
    [Description("Atenção básica")]
    AtencaoBasica = 10,

    [Description("Alto custo")]
    AltoCusto = 20,

    [Description("CEDIC")]
    Cedic = 30
}
