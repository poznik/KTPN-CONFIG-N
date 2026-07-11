using System.Text.RegularExpressions;
using KtpnConfigurator.Core.Catalogs;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.Core.Engine;

/// <summary>Проверки проектных решений (ошибки блокируют выпуск Приказа).</summary>
public static partial class ValidationEngine
{
    public static void Apply(ProjectConfig c, CalculationResult res, CatalogStore store, bool transformerFound)
    {
        var msgs = res.Messages;

        if (!transformerFound)
        {
            msgs.Add(new(Severity.Error, "Не выбран трансформатор — расчёт габаритов невозможен."));
            return;
        }

        // --- Критическая проверка номинала ввода (A45) ---
        if (res.ValidationOk)
        {
            msgs.Add(new(Severity.Info,
                $"Номинал вводного устройства ({res.InputNominal:0} А) достаточен для тока ТМГ ({res.RatedCurrentA:0} А)."));
        }
        else
        {
            string who = res.InputNominal <= 0
                ? "вводное устройство РУНН не задано"
                : $"номинал ввода {res.InputNominal:0} А";
            msgs.Add(new(Severity.Error,
                $"ОШИБКА ПРОЕКТИРОВАНИЯ: {who} меньше номинального тока трансформатора ({res.RatedCurrentA:0} А)."));
        }

        // --- Проверка номиналов аппаратов по диапазонам каталога ---
        CheckBrandRange(c, store, msgs, "Предохранитель-разъединитель (ПВР / NH)", c.PvrOn, c.PvrManufacturer, c.PvrNominal, "ПВР");
        CheckBrandRange(c, store, msgs, "Рубильник открытый (РЕ)", c.ReOn, c.ReManufacturer, c.ReNominal, "Рубильник РЕ");
        CheckBrandRange(c, store, msgs, "Автоматический выключатель (АВ)", c.AvInOn, c.AvInManufacturer, c.AvInNominal, "Вводной АВ");
        CheckMetering(c, msgs);
        CheckCurrentTransformerRatio(store, "ТТ учета на вводе", c.CtRatio, (int)res.InputNominal, msgs, enabled: c.HasCt);
        CheckCurrentTransformerRatio(store, "ТТ КИП на вводе", c.CtKipRatio, (int)res.InputNominal, msgs, enabled: c.HasCtKip);
        CheckRuvn(c, store, msgs);
        CheckOutgoingFeeders(c, res.InputNominal, store, msgs);
        CheckAuxiliaryNeeds(c, store, msgs);
        CheckEnclosure(c, msgs);

        // --- Предупреждения по ручным габаритам ---
        if (res.LengthFinal == 0 || res.WidthFinal == 0)
            msgs.Add(new(Severity.Warning, "Габарит задан вручную как 0 — проверьте корректность."));

        // --- Полнота заполнения ---
        if (string.IsNullOrWhiteSpace(c.Channel) || store.ChannelWeight(c.Channel) <= 0)
            msgs.Add(new(Severity.Warning, "Не выбран швеллер основания или его масса неизвестна — масса основания будет некорректной."));
        if (string.IsNullOrWhiteSpace(c.SteelType) || store.SteelWeight(c.Thickness) <= 0)
            msgs.Add(new(Severity.Warning, "Не задан тип/толщина стали корпуса — масса корпуса будет некорректной."));
        if (string.IsNullOrWhiteSpace(c.BusbarHvMaterial))
            msgs.Add(new(Severity.Warning, "Не выбран материал шин РУВН."));
        if (string.IsNullOrWhiteSpace(c.BusbarLvMaterial))
            msgs.Add(new(Severity.Warning, "Не выбран материал шин РУНН."));
        if (string.IsNullOrWhiteSpace(c.BusbarNMaterial))
            msgs.Add(new(Severity.Warning, "Не выбран материал N-шины."));
        if (RuvnEngineering.HasRuvn(c) && string.IsNullOrWhiteSpace(c.Voltage))
            msgs.Add(new(Severity.Warning, "Не выбрано напряжение РУВН."));
    }

    private static void CheckEnclosure(ProjectConfig c, List<ValidationMessage> msgs)
    {
        if (string.IsNullOrWhiteSpace(c.ClimateExecution))
            msgs.Add(new(Severity.Warning, "Не выбрано климатическое исполнение корпуса."));
        if (string.IsNullOrWhiteSpace(c.ProtectionDegree))
            msgs.Add(new(Severity.Warning, "Не выбрана степень защиты корпуса."));
        if (string.IsNullOrWhiteSpace(c.RuvnDoorConfiguration))
            msgs.Add(new(Severity.Warning, "Не выбрана конфигурация дверей РУВН."));
        if (string.IsNullOrWhiteSpace(c.RunnDoorConfiguration))
            msgs.Add(new(Severity.Warning, "Не выбрана конфигурация дверей РУНН."));
        if (string.IsNullOrWhiteSpace(c.TransformerDoorConfiguration))
            msgs.Add(new(Severity.Warning, "Не выбрана конфигурация дверей трансформаторного отсека."));
        if (!c.HasRigelLock
            && !c.HasPadlockProvision
            && (string.IsNullOrWhiteSpace(c.NetworkLockType)
                || c.NetworkLockType.Equals("Нет", StringComparison.OrdinalIgnoreCase)))
            msgs.Add(new(Severity.Warning, "Не выбран замок дверей корпуса."));
        if (string.IsNullOrWhiteSpace(c.GroundingType))
            msgs.Add(new(Severity.Warning, "Не выбран тип заземления."));
        if (string.IsNullOrWhiteSpace(c.VentilationType))
            msgs.Add(new(Severity.Warning, "Не выбран тип вентиляции корпуса."));
        if (string.IsNullOrWhiteSpace(c.RoofColor))
            msgs.Add(new(Severity.Warning, "Не выбран цвет крыши."));
        if (string.IsNullOrWhiteSpace(c.BaseColor))
            msgs.Add(new(Severity.Warning, "Не выбран цвет основания/цоколя."));
        if (string.IsNullOrWhiteSpace(c.InternalPanelColor))
            msgs.Add(new(Severity.Warning, "Не выбран цвет внутренних панелей."));
        if (c.HasLogo && string.IsNullOrWhiteSpace(c.LogoPlacement))
            msgs.Add(new(Severity.Warning, "Включен логотип, но не задано размещение."));
    }

    private static void CheckRuvn(ProjectConfig c, CatalogStore store, List<ValidationMessage> msgs)
    {
        if (!RuvnEngineering.HasRuvn(c))
        {
            if (RuvnEngineering.HasDevice(c.RuvnSwitch)
                || RuvnEngineering.HasDevice(c.FuseType)
                || c.RuvnSurgeArrester)
            {
                msgs.Add(new(Severity.Warning, "РУВН отключена, но в проекте остались аппараты/ПКТ/ОПН РУВН."));
            }
            return;
        }

        var branches = RuvnEngineering.Branches(c);
        foreach (var branch in branches)
        {
            if (!RuvnEngineering.HasDevice(branch.SwitchType))
                msgs.Add(new(Severity.Warning, $"РУВН: для цепи \"{branch.Title}\" не выбран аппарат ВНА/ВНР/РВЗ или вакуумный выключатель."));
            if (RuvnEngineering.IsVacuumBreaker(branch.SwitchType))
            {
                CheckRuvnVacuumBranch(branch, msgs);
                continue;
            }

            if (branch.SwitchNominal <= 0)
                msgs.Add(new(Severity.Warning, $"РУВН: для цепи \"{branch.Title}\" не задан номинал аппарата."));
            if (branch.FuseOn)
            {
                if (!RuvnEngineering.HasDevice(branch.FuseType))
                    msgs.Add(new(Severity.Warning, $"РУВН: для цепи \"{branch.Title}\" включен ПКТ, но не выбран тип."));
                if (!RuvnEngineering.HasDevice(branch.FuseNominal))
                    msgs.Add(new(Severity.Warning, $"РУВН: для цепи \"{branch.Title}\" включен ПКТ, но не выбран номинал."));
            }
        }

        if (!RuvnEngineering.IsPassThrough(c))
        {
            if (c.RuvnIncomingFuseOn || c.RuvnOutgoingFuseOn)
                msgs.Add(new(Severity.Info, "Тупиковая РУВН использует только ответвление на ТМГ; ПКТ входящей/отходящей линии не учитываются."));
        }

        var transformerBranch = branches.FirstOrDefault(b => b.Key == "transformer");
        if (transformerBranch is not null)
        {
            if (RuvnEngineering.IsVacuumBreaker(transformerBranch.SwitchType))
            {
                // Защита ТМГ в этой ветке выполняется выключателем и РЗА, поэтому ПКТ не обязателен.
            }
            else if (!transformerBranch.FuseOn)
            {
                msgs.Add(new(Severity.Warning, "РУВН: для защиты ТМГ не выбран ПКТ."));
            }
            else
            {
                var recommended = RuvnEngineering.RecommendedFuseNominal(c, store);
                if (!string.IsNullOrWhiteSpace(recommended)
                    && RuvnEngineering.HasDevice(transformerBranch.FuseNominal)
                    && !NormalizeCurrent(transformerBranch.FuseNominal).Equals(NormalizeCurrent(recommended), StringComparison.OrdinalIgnoreCase))
                {
                    msgs.Add(new(Severity.Warning,
                        $"РУВН: для выбранного ТМГ рекомендуется ПКТ {recommended}, выбрано {transformerBranch.FuseNominal}."));
                }
            }
        }

        CheckRuvnSurgeArrester(c, msgs);
    }

    private static void CheckRuvnVacuumBranch(RuvnBranchSelection branch, List<ValidationMessage> msgs)
    {
        var equipment = branch.Equipment ?? new RuvnBranchEquipmentConfig();
        var title = $"РУВН: вакуумный выключатель, {branch.Title}";

        if (string.IsNullOrWhiteSpace(equipment.VacuumBreakerModel))
            msgs.Add(new(Severity.Warning, $"{title}: не выбрана модель выключателя."));
        if (equipment.VacuumBreakerNominal <= 0)
            msgs.Add(new(Severity.Warning, $"{title}: не задан номинальный ток выключателя."));
        if (equipment.VacuumBreakerBreakingCurrentKa <= 0)
            msgs.Add(new(Severity.Warning, $"{title}: не задан ток отключения."));
        if (!RuvnEngineering.HasVisibleBreak(equipment.VisibleBreakBefore))
            msgs.Add(new(Severity.Warning, $"{title}: проверьте видимый разрыв перед выключателем, обычно ставят РВЗ."));
        if (!RuvnEngineering.HasVisibleBreak(equipment.VisibleBreakAfter))
            msgs.Add(new(Severity.Warning, $"{title}: проверьте видимый разрыв после выключателя, обычно ставят РВЗ."));
        if (string.IsNullOrWhiteSpace(equipment.RzaTerminal))
            msgs.Add(new(Severity.Warning, $"{title}: не выбран терминал РЗА."));
        if (string.IsNullOrWhiteSpace(equipment.OperationalPower))
            msgs.Add(new(Severity.Warning, $"{title}: не указано оперативное питание управления и РЗА."));
        if (string.IsNullOrWhiteSpace(equipment.ProtectionCtRatio))
            msgs.Add(new(Severity.Warning, $"{title}: не задан коэффициент ТТ защиты."));
        if (equipment.ProtectionCtQuantity <= 0)
            msgs.Add(new(Severity.Warning, $"{title}: не задано количество ТТ защиты."));
        if (string.IsNullOrWhiteSpace(RuvnEngineering.RzaFunctions(equipment)))
            msgs.Add(new(Severity.Warning, $"{title}: не выбраны функции РЗА."));
        if (equipment.RzaGroundFault && !equipment.HasTtnp)
            msgs.Add(new(Severity.Info, $"{title}: для защиты ОЗЗ проверьте необходимость ТТНП по схеме ввода."));
        if (equipment.HasTtnp && string.IsNullOrWhiteSpace(equipment.TtnpModel))
            msgs.Add(new(Severity.Warning, $"{title}: включен ТТНП, но не указана модель/тип."));
        if (equipment.RzaAvr && !equipment.HasVoltageTransformer)
            msgs.Add(new(Severity.Warning, $"{title}: включен АВР, но не выбран ТН для контроля напряжения."));
        if (equipment.HasVoltageTransformer && string.IsNullOrWhiteSpace(equipment.VoltageTransformerModel))
            msgs.Add(new(Severity.Warning, $"{title}: включен ТН, но не указана модель/тип."));
    }

    private static void CheckRuvnSurgeArrester(ProjectConfig c, List<ValidationMessage> msgs)
    {
        var location = string.IsNullOrWhiteSpace(c.RuvnSurgeArresterLocation)
            ? RuvnEngineering.NoSurgeArrester
            : c.RuvnSurgeArresterLocation;
        var hasSurge = !location.Equals(RuvnEngineering.NoSurgeArrester, StringComparison.OrdinalIgnoreCase);

        if (c.RuvnSurgeArrester && !hasSurge)
            msgs.Add(new(Severity.Warning, "РУВН: ОПН включен, но место установки не выбрано."));

        if (IsAirInput(c) && !hasSurge)
            msgs.Add(new(Severity.Warning, "РУВН: выбран воздушный ввод, укажите место установки ОПН."));

        if (location.Equals(RuvnEngineering.SurgeArresterAtAirPortal, StringComparison.OrdinalIgnoreCase)
            && !IsAirInput(c))
        {
            msgs.Add(new(Severity.Warning, "РУВН: ОПН на воздушном портале выбран без воздушного ввода/портала."));
        }

        if (location.Equals(RuvnEngineering.SurgeArresterAtBusBridge, StringComparison.OrdinalIgnoreCase)
            && !IsBusBridgeInput(c))
        {
            msgs.Add(new(Severity.Warning, "РУВН: ОПН на шинном мосту выбран, но исполнение ввода не \"Шинный мост\"."));
        }

        if (hasSurge && string.IsNullOrWhiteSpace(c.RuvnSurgeArresterThroughput))
            msgs.Add(new(Severity.Warning, "РУВН: для ОПН не выбрана пропускная способность."));
    }

    private static void CheckMetering(ProjectConfig c, List<ValidationMessage> msgs)
    {
        if (c.HasMeter && !c.HasCt)
            msgs.Add(new(Severity.Error, "Выбран счетчик на вводе РУНН, но не включены ТТ учета."));

        foreach (var feeder in c.OutgoingFeeders ?? new List<OutgoingFeederConfig>())
        {
            if (HasFeederMetering(feeder) && string.IsNullOrWhiteSpace(feeder.TtRatio))
                msgs.Add(new(Severity.Error, $"На отходящей линии {feeder.DeviceType}-{feeder.Number} выбран счетчик, но не задан ТТ."));
        }
    }

    private static void CheckOutgoingFeeders(ProjectConfig c, double inputNominal, CatalogStore store, List<ValidationMessage> msgs)
    {
        foreach (var feeder in c.OutgoingFeeders ?? new List<OutgoingFeederConfig>())
        {
            var title = $"{feeder.DeviceType}-{feeder.Number}";
            if (string.IsNullOrWhiteSpace(feeder.CableMark) ^ string.IsNullOrWhiteSpace(feeder.CableSection))
                msgs.Add(new(Severity.Warning, $"{title}: кабель заполнен не полностью - укажите марку и сечение."));

            var model = FindDeviceModel(store, feeder.DeviceType, feeder.Manufacturer, feeder.Model);
            if (model is not null && model.Nominals.Count > 0 && !model.Nominals.Contains(feeder.Nominal))
                msgs.Add(new(Severity.Warning, $"{title}: номинал {feeder.Nominal} А отсутствует в базе модели {feeder.Model}."));
            if (inputNominal > 0 && IsCircuitBreakerType(feeder.DeviceType) && feeder.Nominal > inputNominal)
                msgs.Add(new(Severity.Error,
                    $"ОШИБКА ПРОЕКТИРОВАНИЯ: отходящий автомат {title} {feeder.Nominal} А больше номинала ввода {inputNominal:0} А."));
            CheckCurrentTransformerRatio(store, $"ТТ отходящей линии {title}", feeder.TtRatio, feeder.Nominal, msgs, enabled: HasFeederMetering(feeder));
            CheckSymbolConfidence(store, model?.SymbolKey, $"аппарат {title}", msgs);
        }
    }

    private static void CheckAuxiliaryNeeds(ProjectConfig c, CatalogStore store, List<ValidationMessage> msgs)
    {
        var aux = c.AuxiliaryNeeds;
        if (aux is null || !aux.HasAuxiliaryCabinet)
            return;

        Require(aux.MainBreakerManufacturer, "Для ЩСН не выбран производитель вводного автомата.", msgs);
        Require(aux.MainBreakerModel, "Для ЩСН не выбрана модель вводного автомата.", msgs);
        if (aux.MainBreakerNominal <= 0)
            msgs.Add(new(Severity.Error, "Для ЩСН не задан номинал вводного автомата."));
        CheckModelNominal(store, "АВ", aux.MainBreakerManufacturer, aux.MainBreakerModel, aux.MainBreakerNominal, "вводной автомат ЩСН", msgs);
        CheckSymbolConfidence(store, "auxiliaryCabinet", "ЩСН", msgs);

        if (aux.HasLighting)
        {
            Require(aux.LightingBreakerModel, "Включено освещение, но не выбран автомат освещения.", msgs);
            if (aux.LightingBreakerNominal <= 0)
                msgs.Add(new(Severity.Error, "Включено освещение, но не задан номинал автомата освещения."));
            if (aux.LightingCircuits <= 0)
                msgs.Add(new(Severity.Error, "Включено освещение, но количество цепей освещения равно 0."));
            if (aux.LightingFixtureQuantity <= 0)
                msgs.Add(new(Severity.Warning, "Включено освещение, но не задано количество светильников."));
            if (string.IsNullOrWhiteSpace(aux.LightingAreas))
                msgs.Add(new(Severity.Warning, "Включено освещение, но не указаны освещаемые отсеки."));
            if (aux.RepairLightingVoltage <= 0)
                msgs.Add(new(Severity.Warning, "Включено освещение, но не задано напряжение ремонтного освещения."));
            CheckModelNominal(store, "АВ", aux.LightingBreakerManufacturer, aux.LightingBreakerModel, aux.LightingBreakerNominal, "автомат освещения", msgs);
            if (aux.LightingControlMode.Contains("Фотореле", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(aux.PhotoRelayModel))
                msgs.Add(new(Severity.Error, "Для режима освещения «Фотореле» не выбрана модель фотореле."));
            if (aux.LightingControlMode.Contains("Астротаймер", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(aux.AstroTimerModel))
                msgs.Add(new(Severity.Error, "Для режима освещения «Астротаймер» не выбрана модель астротаймера."));
            if (aux.LightingControlMode.Contains("Реле времени", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(aux.TimeRelayModel))
                msgs.Add(new(Severity.Error, "Для режима освещения «Реле времени» не выбрана модель реле времени."));
            CheckSymbolConfidence(store, "lighting", "освещение", msgs);
        }

        if (aux.SocketEnabled)
        {
            Require(aux.SocketBreakerModel, "Включена сервисная розетка, но не выбран автомат розетки.", msgs);
            Require(aux.SocketModel, "Включена сервисная розетка, но не выбрана модель розетки.", msgs);
            CheckModelNominal(store, "АВ", aux.SocketBreakerManufacturer, aux.SocketBreakerModel, aux.SocketBreakerNominal, "автомат розетки", msgs);
            CheckSymbolConfidence(store, "socket", "сервисная розетка", msgs);
        }

        if (aux.HeatingEnabled)
        {
            Require(aux.HeatingBreakerModel, "Включен обогрев, но не выбран автомат обогрева.", msgs);
            Require(aux.HeaterModel, "Включен обогрев, но не выбран обогреватель.", msgs);
            Require(aux.ThermostatModel, "Включен обогрев, но не выбран термостат.", msgs);
            CheckModelNominal(store, "АВ", aux.HeatingBreakerManufacturer, aux.HeatingBreakerModel, aux.HeatingBreakerNominal, "автомат обогрева", msgs);
            CheckSymbolConfidence(store, "heating", "обогрев", msgs);
        }
        else if (aux.MeterHeatingEnabled)
        {
            msgs.Add(new(Severity.Warning, "Выбран обогрев счетчиков, но общий обогрев ЩСН выключен."));
        }

        if (aux.VentilationEnabled)
        {
            Require(aux.VentilationBreakerModel, "Включена вентиляция, но не выбран автомат вентиляции.", msgs);
            Require(aux.FanModel, "Включена вентиляция, но не выбран вентилятор.", msgs);
            CheckModelNominal(store, "АВ", aux.VentilationBreakerManufacturer, aux.VentilationBreakerModel, aux.VentilationBreakerNominal, "автомат вентиляции", msgs);
            CheckSymbolConfidence(store, "ventilation", "вентиляция", msgs);
        }

        if (aux.OpsEnabled)
        {
            Require(aux.OpsType, "Включена ОПС, но не выбран тип сигнализации.", msgs);
            Require(aux.OpsManufacturer, "Включена ОПС, но не выбран производитель.", msgs);
            Require(aux.OpsModel, "Включена ОПС, но не выбрана модель.", msgs);
            if (aux.OpsLoops <= 0)
                msgs.Add(new(Severity.Warning, "Включена ОПС, но не задано количество шлейфов."));
        }

        if (aux.HasRise)
        {
            Require(aux.RieseType, "Включен РИСЭ, но не выбран тип РИСЭ.", msgs);
            Require(aux.RieseProtectedCircuits, "Включен РИСЭ, но не указаны резервируемые цепи.", msgs);
            Require(aux.RieseProtectionModel, "Включен РИСЭ, но не выбран защитный аппарат.", msgs);
            if (aux.RiesePowerVa <= 0)
                msgs.Add(new(Severity.Error, "Включен РИСЭ, но не задана мощность."));
            if (aux.RieseAutonomyMinutes <= 0)
                msgs.Add(new(Severity.Warning, "Включен РИСЭ, но не задано время автономной работы."));
            CheckSymbolConfidence(store, "backupPowerSource", "РИСЭ", msgs);
        }
    }

    private static void Require(string value, string message, List<ValidationMessage> msgs)
    {
        if (string.IsNullOrWhiteSpace(value))
            msgs.Add(new(Severity.Error, message));
    }

    private static void CheckModelNominal(CatalogStore store, string type, string manufacturer, string model, int nominal, string label, List<ValidationMessage> msgs)
    {
        var dm = FindDeviceModel(store, type, manufacturer, model);
        if (dm is not null && dm.Nominals.Count > 0 && nominal > 0 && !dm.Nominals.Contains(nominal))
            msgs.Add(new(Severity.Warning, $"{label}: номинал {nominal} А отсутствует в базе модели {model}."));
        CheckSymbolConfidence(store, dm?.SymbolKey, label, msgs);
    }

    private static DeviceModel? FindDeviceModel(CatalogStore store, string type, string manufacturer, string model)
    {
        return store.DeviceModels.FirstOrDefault(d =>
            d.Type.Equals(type, StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrWhiteSpace(manufacturer) || d.Manufacturer.Equals(manufacturer, StringComparison.OrdinalIgnoreCase))
            && (string.IsNullOrWhiteSpace(model)
                || d.Model.Equals(model, StringComparison.OrdinalIgnoreCase)
                || d.Series.Equals(model, StringComparison.OrdinalIgnoreCase)));
    }

    private static void CheckSymbolConfidence(CatalogStore store, string? symbolKey, string label, List<ValidationMessage> msgs)
    {
        if (string.IsNullOrWhiteSpace(symbolKey))
            return;

        var symbol = store.DiagramSymbols.FirstOrDefault(s => s.SymbolKey.Equals(symbolKey, StringComparison.OrdinalIgnoreCase));
        if (symbol is null)
        {
            msgs.Add(new(Severity.Warning, $"{label}: нет УГО/буквенного обозначения в diagram_symbols.json."));
            return;
        }

        if (!symbol.SourceConfidence.Equals("verified", StringComparison.OrdinalIgnoreCase))
            msgs.Add(new(Severity.Warning, $"{label}: обозначение {symbol.LetterCode} требует проверки проектировщиком ({symbol.SourceConfidence})."));
    }

    private static void CheckCurrentTransformerRatio(
        CatalogStore store,
        string label,
        string ratio,
        int nominal,
        List<ValidationMessage> msgs,
        bool enabled = true)
    {
        if (!enabled || nominal <= 0 || string.IsNullOrWhiteSpace(ratio))
            return;

        var recommended = RecommendedCtRatio(store, nominal);
        if (!string.IsNullOrWhiteSpace(recommended)
            && !ratio.Equals(recommended, StringComparison.OrdinalIgnoreCase))
        {
            msgs.Add(new(Severity.Warning,
                $"{label}: для номинала {nominal} А рекомендуется ТТ {recommended}, выбрано {ratio}."));
        }
    }

    private static string RecommendedCtRatio(CatalogStore store, int nominal)
    {
        var candidates = store.Options.TtRatios
            .Select(ratio => new { Ratio = ratio, Primary = ParseCtPrimary(ratio) })
            .Where(x => x.Primary > 0)
            .OrderBy(x => Math.Abs(x.Primary - nominal))
            .ThenBy(x => x.Primary < nominal ? 1 : 0)
            .ToList();

        return candidates.FirstOrDefault()?.Ratio ?? "";
    }

    private static int ParseCtPrimary(string ratio)
    {
        if (string.IsNullOrWhiteSpace(ratio))
            return 0;

        var match = NumberRegex().Match(ratio);
        return match.Success && int.TryParse(match.Value, out var value)
            ? value
            : 0;
    }

    private static string NormalizeCurrent(string value) =>
        (value ?? "")
            .Replace(" ", "")
            .Replace(',', '.')
            .Replace("А", "A", StringComparison.OrdinalIgnoreCase)
            .Trim();

    private static bool IsAirInput(ProjectConfig c) =>
        c.RuvnExecution.Contains("возд", StringComparison.OrdinalIgnoreCase);

    private static bool IsBusBridgeInput(ProjectConfig c) =>
        c.RuvnExecution.Contains("шинн", StringComparison.OrdinalIgnoreCase);

    private static void CheckBrandRange(ProjectConfig c, CatalogStore store, List<ValidationMessage> msgs,
        string apparatusType, bool on, string manufacturer, int nominal, string label)
    {
        if (!on || string.IsNullOrWhiteSpace(manufacturer) || nominal <= 0)
            return;
        var spec = store.Apparatus.FirstOrDefault(a => a.Type == apparatusType && a.Manufacturer == manufacturer);
        if (spec is null)
            return;
        var (min, max) = ParseRange(spec.CurrentRange);
        if (max > 0 && (nominal < min || nominal > max))
            msgs.Add(new(Severity.Warning,
                $"{label} {nominal} А ({manufacturer}) вне диапазона каталога {spec.CurrentRange}."));
    }

    /// <summary>Парсит диапазон вида "16 - 4000 А".</summary>
    private static (double min, double max) ParseRange(string? range)
    {
        if (string.IsNullOrWhiteSpace(range)) return (0, 0);
        var nums = NumberRegex().Matches(range);
        if (nums.Count >= 2
            && double.TryParse(nums[0].Value, out var a)
            && double.TryParse(nums[1].Value, out var b))
            return (a, b);
        return (0, 0);
    }

    private static bool IsCircuitBreakerType(string deviceType) =>
        deviceType.Equals("АВ", StringComparison.OrdinalIgnoreCase)
        || deviceType.Contains("автомат", StringComparison.OrdinalIgnoreCase)
        || deviceType.Equals("QF", StringComparison.OrdinalIgnoreCase);

    private static bool HasFeederMetering(OutgoingFeederConfig feeder)
    {
        var type = string.IsNullOrWhiteSpace(feeder.MeteringType)
            ? (feeder.HasMeter ? "Коммерческий" : "Нет")
            : feeder.MeteringType.Trim();
        return feeder.HasMeter || !type.Equals("Нет", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex NumberRegex();
}
