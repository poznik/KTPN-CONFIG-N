using KtpnConfigurator.Core.Catalogs;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.Core.Engine;

/// <summary>
/// Расчётное ядро. Перенос формул листа «Расчет КТПН» 1:1.
/// Все числовые правила задокументированы в ПОСТАНОВКА_КТПН.md §7.
/// </summary>
public static class CalculationEngine
{
    /// <summary>Округление как Excel ROUND — половина «от нуля».</summary>
    private static double RoundExcel(double value) =>
        Math.Round(value, MidpointRounding.AwayFromZero);

    public static CalculationResult Calculate(ProjectConfig c, CatalogStore store)
    {
        var m = store.Options.Methodology;
        var t = store.GetTransformer(c.Mark);
        var res = new CalculationResult();

        if (t is null)
        {
            // Нет трансформатора — расчёт габаритов невозможен; вернём нули,
            // валидация добавит сообщение об ошибке.
            res.RatedCurrentA = 0;
            ValidationEngine.Apply(c, res, store, transformerFound: false);
            return res;
        }

        res.RatedCurrentA = t.RatedCurrentA;
        res.BusbarHv = string.IsNullOrEmpty(t.BusbarHv) ? "-" : t.BusbarHv;
        res.BusbarLv = string.IsNullOrEmpty(t.BusbarLv) ? "-" : t.BusbarLv;
        res.BusbarPe = string.IsNullOrEmpty(t.BusbarPe) ? "-" : t.BusbarPe;

        // --- Длина (B53) ---
        double ruvnLen = c.RuvnType == "Нет" ? 0 : c.LenRuvn;
        double baseLen = ruvnLen + t.LengthMm + c.LenRunn + c.LengthBuffer;
        res.LengthCalc = SnapLength(baseLen, m.StandardLengths);
        res.LengthFinal = WithMinOverride(res.LengthCalc, c.ManualLength, m.MinDimensionMm);

        // --- Ширина (B54) ---
        res.WidthCalc = CalcWidth(c, t.WidthMm, m);
        res.WidthFinal = WithMinOverride(res.WidthCalc, c.ManualWidth, m.MinDimensionMm);

        // --- Высота (B55) — ручной ввод без контроля минимума ---
        res.HeightCalc = t.HeightMm >= m.HeightThresholdMm ? m.TallHeightMm : m.ShortHeightMm;
        res.HeightFinal = c.ManualHeight ?? res.HeightCalc;

        // --- Массы (ручной ввод C56/C57/C58 имеет приоритет, как в исходной книге) ---
        res.BaseMassCalc = BaseMass(res.LengthFinal, res.WidthFinal, store.ChannelWeight(c.Channel), m);
        res.BaseMass = c.ManualBaseMass ?? res.BaseMassCalc;
        res.BodyMassCalc = BodyMass(res.LengthFinal, res.WidthFinal, res.HeightFinal, store.SteelWeight(c.Thickness), m);
        res.BodyMass = c.ManualBodyMass ?? res.BodyMassCalc;
        res.GrossMassCalc = t.MassKg + res.BaseMass + res.BodyMass;
        res.GrossMass = c.ManualGrossMass ?? res.GrossMassCalc;

        // --- Проверка номинала ввода ---
        res.InputNominal = Math.Max(c.PvrOn ? c.PvrNominal : 0,
                            Math.Max(c.ReOn ? c.ReNominal : 0,
                                     c.AvInOn ? c.AvInNominal : 0));
        res.ValidationOk = res.InputNominal >= t.RatedCurrentA;

        ValidationEngine.Apply(c, res, store, transformerFound: true);
        return res;
    }

    private static double SnapLength(double baseLen, IReadOnlyList<double> ladder)
    {
        foreach (var step in ladder)
            if (baseLen <= step)
                return step;
        return baseLen;
    }

    private static double CalcWidth(ProjectConfig c, double trWidth, MethodologyParams m)
    {
        if (c.RuvnType == "Проходная")
            return m.PassageWidthMm;
        bool multi = (c.AvOn ? c.AvQty : 0) > m.MultiBayAvQty
                  || (c.RpsOn ? c.RpsQty : 0) > m.MultiBayRpsQty;
        double baseMin = multi ? m.MultiBayWidthMm : m.StandardWidthMm;
        return Math.Max(baseMin, trWidth + c.TransformerTolerance);
    }

    /// <summary>D53/D54: ручной ввод имеет приоритет; 0 → 0; иначе не менее минимума.</summary>
    private static double WithMinOverride(double calc, double? manual, double minDim)
    {
        if (!manual.HasValue)
            return calc;
        if (manual.Value == 0)
            return 0;
        return Math.Max(manual.Value, minDim);
    }

    private static double BaseMass(double len, double wid, double channelWeightPerM, MethodologyParams m)
    {
        double frame = ((len * 2 + wid * 4) / 1000.0) * channelWeightPerM * m.FrameCoef;
        double floor = (len * wid / 1_000_000.0) * m.FloorSheetKgPerM2;
        return RoundExcel(frame + floor);
    }

    private static double BodyMass(double len, double wid, double height, double steelWeightPerM2, MethodologyParams m)
    {
        double walls = (len + wid) * 2 * height / 1_000_000.0;
        double roofFloor = len * wid / 1_000_000.0;
        return RoundExcel((walls + roofFloor) * steelWeightPerM2 * m.BodyWasteCoef);
    }
}
