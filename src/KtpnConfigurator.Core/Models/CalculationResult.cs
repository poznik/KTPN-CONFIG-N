namespace KtpnConfigurator.Core.Models;

public enum Severity { Info, Warning, Error }

public sealed class ValidationMessage
{
    public Severity Severity { get; set; }
    public string Text { get; set; } = "";

    public ValidationMessage() { }
    public ValidationMessage(Severity severity, string text)
    {
        Severity = severity;
        Text = text;
    }

    public override string ToString() => $"[{Severity}] {Text}";
}

/// <summary>Результат расчёта габаритов, масс и валидации.</summary>
public sealed class CalculationResult
{
    public double LengthCalc { get; set; }
    public double LengthFinal { get; set; }
    public double WidthCalc { get; set; }
    public double WidthFinal { get; set; }
    public double HeightCalc { get; set; }
    public double HeightFinal { get; set; }

    public double BaseMassCalc { get; set; }
    public double BaseMass { get; set; }
    public double BodyMassCalc { get; set; }
    public double BodyMass { get; set; }
    public double GrossMassCalc { get; set; }
    public double GrossMass { get; set; }

    public double InputNominal { get; set; }
    public double RatedCurrentA { get; set; }
    public bool ValidationOk { get; set; }

    public string BusbarHv { get; set; } = "-";
    public string BusbarLv { get; set; } = "-";
    public string BusbarPe { get; set; } = "-";

    public List<ValidationMessage> Messages { get; set; } = new();

    public bool HasErrors => Messages.Exists(m => m.Severity == Severity.Error);
}
