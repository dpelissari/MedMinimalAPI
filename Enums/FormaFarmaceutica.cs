using System.ComponentModel;

public enum FormaFarmaceutica
{
    [Description("Comprimido")]
    Comprimido = 10, 

    [Description("Cápsula")]
    Capsula = 20,

    [Description("Drágeas")]
    Drageas = 30,

    [Description("Outros")]
    Outros = 40
}