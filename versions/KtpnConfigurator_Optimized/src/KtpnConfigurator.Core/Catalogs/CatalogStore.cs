using System.Text.Json;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.Core.Catalogs;

/// <summary>
/// Справочники приложения: трансформаторы, аппараты РУНН, списки/опции.
/// Загружаются из JSON-файлов каталога (папка Data рядом с приложением).
/// </summary>
public sealed class CatalogStore
{
    public IReadOnlyList<TransformerSpec> Transformers { get; }
    public IReadOnlyList<ApparatusSpec> Apparatus { get; }
    public CatalogOptions Options { get; }
    public IReadOnlyList<DeviceModel> DeviceModels { get; }
    public IReadOnlyList<DiagramSymbol> DiagramSymbols { get; }
    public DiagramRule DiagramRules { get; }
    public IReadOnlyList<CustomerProfileSpec> CustomerProfiles { get; }

    private readonly Dictionary<string, TransformerSpec> _byMark;
    private readonly Dictionary<string, CustomerProfileSpec> _customerProfiles;
    private readonly Dictionary<string, double> _channelWeight;
    private readonly List<SteelSpec> _steels;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public CatalogStore(IReadOnlyList<TransformerSpec> transformers,
                        IReadOnlyList<ApparatusSpec> apparatus,
                        CatalogOptions options,
                        IReadOnlyList<DeviceModel> deviceModels,
                        IReadOnlyList<DiagramSymbol> diagramSymbols,
                        DiagramRule diagramRules,
                        IReadOnlyList<CustomerProfileSpec> customerProfiles)
    {
        Transformers = transformers;
        Apparatus = apparatus;
        Options = options;
        DeviceModels = deviceModels;
        DiagramSymbols = diagramSymbols;
        DiagramRules = diagramRules;
        CustomerProfiles = customerProfiles;
        _byMark = transformers
            .GroupBy(t => t.Mark)
            .ToDictionary(g => g.Key, g => g.First());
        _customerProfiles = BuildCustomerProfileIndex(customerProfiles);
        // Дубликат типоразмера в options.json не должен ронять запуск приложения.
        _channelWeight = options.Channels
            .GroupBy(c => c.Size)
            .ToDictionary(g => g.Key, g => g.First().WeightPerM);
        _steels = options.Steels;
    }

    public static CatalogStore Load(string dataDir)
    {
        if (!Directory.Exists(dataDir))
            throw new DirectoryNotFoundException($"Папка справочников не найдена: {dataDir}");

        var transformers = ReadJson<List<TransformerSpec>>(Path.Combine(dataDir, "transformers.json")) ?? new();
        var apparatus = ReadJson<List<ApparatusSpec>>(Path.Combine(dataDir, "apparatus.json")) ?? new();
        var options = ReadJson<CatalogOptions>(Path.Combine(dataDir, "options.json")) ?? new();
        var deviceModels = ReadJson<List<DeviceModel>>(Path.Combine(dataDir, "device_models.json")) ?? new();
        var diagramSymbols = ReadJson<List<DiagramSymbol>>(Path.Combine(dataDir, "diagram_symbols.json")) ?? new();
        var diagramRules = ReadJson<DiagramRule>(Path.Combine(dataDir, "diagram_rules.json")) ?? new();
        var customerProfiles = ReadJson<List<CustomerProfileSpec>>(Path.Combine(dataDir, "customer_profiles.json")) ?? new();
        return new CatalogStore(transformers, apparatus, options, deviceModels, diagramSymbols, diagramRules, customerProfiles);
    }

    private static Dictionary<string, CustomerProfileSpec> BuildCustomerProfileIndex(IReadOnlyList<CustomerProfileSpec> profiles)
    {
        var result = new Dictionary<string, CustomerProfileSpec>(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in profiles)
            Add(profile.Name, profile, overwrite: true);

        foreach (var profile in profiles)
            foreach (var alias in profile.Aliases)
                Add(alias, profile, overwrite: false);

        return result;

        void Add(string key, CustomerProfileSpec profile, bool overwrite)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                if (overwrite)
                    result[key.Trim()] = profile;
                else
                    result.TryAdd(key.Trim(), profile);
            }
        }
    }

    private static T? ReadJson<T>(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Файл справочника не найден: {path}", path);

        using var fs = File.OpenRead(path);
        try
        {
            return JsonSerializer.Deserialize<T>(fs, JsonOpts);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Не удалось прочитать JSON справочника: {path}", ex);
        }
    }

    /// <summary>Производители в порядке появления в БД.</summary>
    public IReadOnlyList<string> Manufacturers()
    {
        var seen = new HashSet<string>();
        var result = new List<string>();
        foreach (var t in Transformers)
            if (!string.IsNullOrEmpty(t.Manufacturer) && seen.Add(t.Manufacturer))
                result.Add(t.Manufacturer);
        return result;
    }

    /// <summary>Марки трансформаторов выбранного производителя (каскад).</summary>
    public IReadOnlyList<string> MarksFor(string manufacturer) =>
        Transformers.Where(t => t.Manufacturer == manufacturer)
                    .Select(t => t.Mark)
                    .ToList();

    public TransformerSpec? GetTransformer(string mark) =>
        mark is not null && _byMark.TryGetValue(mark, out var t) ? t : null;

    public CustomerProfileSpec? GetCustomerProfile(string gridCompany) =>
        gridCompany is not null && _customerProfiles.TryGetValue(gridCompany.Trim(), out var p) ? p : null;

    /// <summary>Вес швеллера на метр; 0, если типоразмер не найден.</summary>
    public double ChannelWeight(string size) =>
        size is not null && _channelWeight.TryGetValue(size, out var w) ? w : 0;

    /// <summary>Вес стали на м² по толщине (точное совпадение, как VLOOKUP с FALSE).</summary>
    public double SteelWeight(double thickness)
    {
        foreach (var s in _steels)
            if (Math.Abs(s.ThicknessMm - thickness) < 1e-9)
                return s.WeightPerM2;
        return 0;
    }
}
