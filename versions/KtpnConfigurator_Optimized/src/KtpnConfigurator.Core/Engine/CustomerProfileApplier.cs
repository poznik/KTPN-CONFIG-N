using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.Core.Engine;

public static class CustomerProfileApplier
{
    public static bool Apply(ProjectConfig config, CustomerProfileSpec? profile)
    {
        if (profile is null)
            return false;

        config.AuxiliaryNeeds ??= new AuxiliaryNeedsConfig();
        var settings = profile.Settings ?? new CustomerProfileSettings();
        var changed = false;

        SetString(() => config.SteelType, v => config.SteelType = v, settings.SteelType);
        SetDouble(() => config.Thickness, v => config.Thickness = v, settings.Thickness);
        SetString(() => config.BodyColor, v => config.BodyColor = v, settings.BodyColor);
        SetString(() => config.DoorColor, v => config.DoorColor = v, settings.DoorColor);
        SetString(() => config.RoofColor, v => config.RoofColor = v, settings.RoofColor);
        SetString(() => config.BaseColor, v => config.BaseColor = v, settings.BaseColor);
        SetString(() => config.InternalPanelColor, v => config.InternalPanelColor = v, settings.InternalPanelColor);
        SetString(() => config.LogoColor, v => config.LogoColor = v, settings.LogoColor);
        SetString(() => config.ClimateExecution, v => config.ClimateExecution = v, settings.ClimateExecution);
        SetString(() => config.ProtectionDegree, v => config.ProtectionDegree = v, settings.ProtectionDegree);
        SetString(() => config.RuvnExecution, v => config.RuvnExecution = v, settings.RuvnExecution);
        SetString(() => config.RuvnSurgeArresterThroughput, v => config.RuvnSurgeArresterThroughput = v, settings.RuvnSurgeArresterThroughput);
        SetString(() => config.RuvnDoorConfiguration, v => config.RuvnDoorConfiguration = v, settings.RuvnDoorConfiguration);
        SetString(() => config.RunnDoorConfiguration, v => config.RunnDoorConfiguration = v, settings.RunnDoorConfiguration);
        SetString(() => config.TransformerDoorConfiguration, v => config.TransformerDoorConfiguration = v, settings.TransformerDoorConfiguration);
        SetBool(() => config.HasRigelLock, v => config.HasRigelLock = v, settings.HasRigelLock);
        SetString(() => config.NetworkLockType, v => config.NetworkLockType = v, settings.NetworkLockType);
        SetBool(() => config.HasPadlockProvision, v => config.HasPadlockProvision = v, settings.HasPadlockProvision);
        SetString(() => config.GroundingType, v => config.GroundingType = v, settings.GroundingType);
        SetString(() => config.VentilationType, v => config.VentilationType = v, settings.VentilationType);
        SetBool(() => config.HasRoofDeflector, v => config.HasRoofDeflector = v, settings.HasRoofDeflector);
        SetBool(() => config.HasNameplate, v => config.HasNameplate = v, settings.HasNameplate);
        SetBool(() => config.HasDoorCanopies, v => config.HasDoorCanopies = v, settings.HasDoorCanopies);
        SetBool(() => config.HasDoorSeals, v => config.HasDoorSeals = v, settings.HasDoorSeals);
        SetBool(() => config.HasTransformerMeshDoors, v => config.HasTransformerMeshDoors = v, settings.HasTransformerMeshDoors);
        SetBool(() => config.HasLouverAnimalProtection, v => config.HasLouverAnimalProtection = v, settings.HasLouverAnimalProtection);
        SetBool(() => config.HasAntiVandalHinges, v => config.HasAntiVandalHinges = v, settings.HasAntiVandalHinges);
        SetBool(() => config.HasDoorSealing, v => config.HasDoorSealing = v, settings.HasDoorSealing);
        SetBool(() => config.HasLogo, v => config.HasLogo = v, settings.HasLogo);
        SetString(() => config.LogoPlacement, v => config.LogoPlacement = v, settings.LogoPlacement);
        SetBool(() => config.HasWarningLabels, v => config.HasWarningLabels = v, settings.HasWarningLabels);
        SetBool(() => config.HasDispatcherNameplate, v => config.HasDispatcherNameplate = v, settings.HasDispatcherNameplate);
        SetBool(() => config.HasFeederLabels, v => config.HasFeederLabels = v, settings.HasFeederLabels);
        SetString(() => config.MarkingNotes, v => config.MarkingNotes = v, settings.MarkingNotes);
        SetString(() => config.BusbarHvMaterial, v => config.BusbarHvMaterial = v, settings.BusbarHvMaterial);
        SetString(() => config.BusbarLvMaterial, v => config.BusbarLvMaterial = v, settings.BusbarLvMaterial);
        SetString(() => config.BusbarNMaterial, v => config.BusbarNMaterial = v, settings.BusbarNMaterial);

        if (!string.IsNullOrWhiteSpace(settings.RuvnSurgeArresterLocation))
        {
            SetString(() => config.RuvnSurgeArresterLocation, v => config.RuvnSurgeArresterLocation = v, settings.RuvnSurgeArresterLocation);
            var shouldInstall = !settings.RuvnSurgeArresterLocation.Equals(
                RuvnEngineering.NoSurgeArrester,
                StringComparison.OrdinalIgnoreCase);
            SetBool(() => config.RuvnSurgeArrester, v => config.RuvnSurgeArrester = v, shouldInstall);
        }

        var aux = config.AuxiliaryNeeds;
        SetBool(() => aux.Enabled, v => aux.Enabled = v, settings.AuxiliaryEnabled);
        SetBool(() => aux.LightingEnabled, v => aux.LightingEnabled = v, settings.LightingEnabled);
        SetInt(() => aux.LightingFixtureQuantity, v => aux.LightingFixtureQuantity = v, settings.LightingFixtureQuantity);
        SetString(() => aux.LightingAreas, v => aux.LightingAreas = v, settings.LightingAreas);
        SetInt(() => aux.RepairLightingVoltage, v => aux.RepairLightingVoltage = v, settings.RepairLightingVoltage);
        SetBool(() => aux.OutdoorLightingEnabled, v => aux.OutdoorLightingEnabled = v, settings.OutdoorLightingEnabled);
        SetBool(() => aux.SocketEnabled, v => aux.SocketEnabled = v, settings.SocketEnabled);
        SetInt(() => aux.SocketQuantity, v => aux.SocketQuantity = v, settings.SocketQuantity);
        SetBool(() => aux.HeatingEnabled, v => aux.HeatingEnabled = v, settings.HeatingEnabled);
        SetBool(() => aux.MeterHeatingEnabled, v => aux.MeterHeatingEnabled = v, settings.MeterHeatingEnabled);
        SetBool(() => aux.VentilationEnabled, v => aux.VentilationEnabled = v, settings.VentilationEnabled);
        SetBool(() => aux.OpsEnabled, v => aux.OpsEnabled = v, settings.OpsEnabled);
        SetString(() => aux.OpsType, v => aux.OpsType = v, settings.OpsType);
        SetString(() => aux.OpsManufacturer, v => aux.OpsManufacturer = v, settings.OpsManufacturer);
        SetString(() => aux.OpsModel, v => aux.OpsModel = v, settings.OpsModel);
        SetInt(() => aux.OpsLoops, v => aux.OpsLoops = v, settings.OpsLoops);

        return changed;

        void SetString(Func<string> get, Action<string> set, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            var normalized = value.Trim();
            if (get() == normalized)
                return;

            set(normalized);
            changed = true;
        }

        void SetDouble(Func<double> get, Action<double> set, double? value)
        {
            if (!value.HasValue || Math.Abs(get() - value.Value) < 1e-9)
                return;

            set(value.Value);
            changed = true;
        }

        void SetInt(Func<int> get, Action<int> set, int? value)
        {
            if (!value.HasValue || get() == value.Value)
                return;

            set(value.Value);
            changed = true;
        }

        void SetBool(Func<bool> get, Action<bool> set, bool? value)
        {
            if (!value.HasValue || get() == value.Value)
                return;

            set(value.Value);
            changed = true;
        }
    }

}
