namespace KtpnConfigurator.Core.Models;

/// <summary>
/// Полная конфигурация одного проекта КТПН — все входные данные конфигуратора.
/// Сохраняется/загружается как файл проекта (*.ktpn, JSON).
/// </summary>
public sealed class ProjectConfig
{
    // --- Мета ---
    public string ProjectName { get; set; } = "Новый проект КТПН";
    public string Author { get; set; } = "";
    public string GridCompany { get; set; } = "";

    // --- Трансформатор ---
    public string Manufacturer { get; set; } = "";
    public string Mark { get; set; } = "";

    // --- Конструктив корпуса ---
    public string SteelType { get; set; } = "";
    public double Thickness { get; set; } = 2.0;
    public string Channel { get; set; } = "";
    public string BodyColor { get; set; } = "";
    public string DoorColor { get; set; } = "";

    // --- Габаритные настройки отсеков ---
    public double LenRuvn { get; set; } = 1300;
    public double LenRunn { get; set; } = 600;
    public double TransformerTolerance { get; set; } = 300;
    public double LengthBuffer { get; set; } = 10;

    // --- Ввод РУВН ---
    public string Voltage { get; set; } = "";
    public string RuvnType { get; set; } = "";
    public string RuvnSwitch { get; set; } = "";
    public int RuvnSwitchNominal { get; set; }
    public string FuseType { get; set; } = "";
    public string FuseNominal { get; set; } = "";
    public string RuvnExecution { get; set; } = "";
    public bool RuvnSurgeArrester { get; set; }

    // --- Ввод РУНН ---
    public bool PvrOn { get; set; }
    public int PvrNominal { get; set; }
    public string PvrManufacturer { get; set; } = "";
    public bool ReOn { get; set; }
    public int ReNominal { get; set; }
    public string ReManufacturer { get; set; } = "";
    public bool AvInOn { get; set; }
    public int AvInNominal { get; set; }
    public string AvInManufacturer { get; set; } = "";
    public bool RunnSurgeArrester { get; set; }
    public bool HasCt { get; set; }
    public string CtRatio { get; set; } = "";
    public bool HasCtKip { get; set; }
    public string CtKipRatio { get; set; } = "";
    public bool HasMeter { get; set; }

    // --- Отходящие линии РУНН ---
    public bool AvOn { get; set; }
    public int AvQty { get; set; }
    public string AvBrand { get; set; } = "";
    public bool RpsOn { get; set; }
    public int RpsQty { get; set; }
    public string RpsBrand { get; set; } = "";
    public string OutgoingExecution { get; set; } = "";

    // --- Ручное переопределение габаритов (null = не задано / ISNUMBER=ЛОЖЬ) ---
    public double? ManualLength { get; set; }
    public double? ManualWidth { get; set; }
    public double? ManualHeight { get; set; }
    public double? ManualBaseMass { get; set; }
    public double? ManualBodyMass { get; set; }
    public double? ManualGrossMass { get; set; }

    public string Notes { get; set; } = "";

    public ProjectConfig Clone()
    {
        return (ProjectConfig)MemberwiseClone();
    }
}
