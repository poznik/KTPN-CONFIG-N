namespace KtpnConfigurator.Core.Models;

public enum Severity { Info, Warning, Error }

public sealed class ValidationMessage
{
    public Severity Severity { get; set; }
    public string Text { get; set; } = "";
    public string Section { get; set; } = "Результат";
    public int TabIndex { get; set; } = 5;

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
    public double EquipmentMassEstimate { get; set; }
    public double BusbarMassEstimate { get; set; }
    public double DoorMassEstimate { get; set; }
    public double AuxiliaryMassEstimate { get; set; }
    public double EnclosureOptionMassEstimate { get; set; }
    public double AdditionalMassEstimate { get; set; }
    public double GrossMassEstimated { get; set; }
    public double TransformerMass { get; set; }
    public bool UsesManualMass { get; set; }

    public double InputNominal { get; set; }
    public double RatedCurrentA { get; set; }
    public bool ValidationOk { get; set; }

    public string BusbarHv { get; set; } = "-";
    public string BusbarLv { get; set; } = "-";
    public string BusbarN { get; set; } = "-";
    public string BusbarPe { get; set; } = "-";

    public double Section1RatedCurrentA { get; set; }
    public double Section2RatedCurrentA { get; set; }
    public double Section1InputNominalA { get; set; }
    public double Section2InputNominalA { get; set; }
    public double Section1TransformerMass { get; set; }
    public double Section2TransformerMass { get; set; }
    public double Section1EstimatedMass { get; set; }
    public double Section2EstimatedMass { get; set; }
    public double Section1ShortCircuitCurrentKa { get; set; }
    public double Section2ShortCircuitCurrentKa { get; set; }
    public string Section1BusbarLv { get; set; } = "-";
    public string Section2BusbarLv { get; set; } = "-";
    public string Section1BusbarN { get; set; } = "-";
    public string Section2BusbarN { get; set; } = "-";
    public string Section1BusbarPe { get; set; } = "-";
    public string Section2BusbarPe { get; set; } = "-";

    public List<ValidationMessage> Messages { get; set; } = new();

    public bool HasErrors => Messages.Exists(m => m.Severity == Severity.Error);
}
