using System.IO;
using KtpnConfigurator.Core.Catalogs;
using KtpnConfigurator.Core.Documents;

namespace KtpnConfigurator.App.Services;

/// <summary>Загрузка справочников и шаблонов из папки Data рядом с приложением.</summary>
public sealed class AppEnvironment
{
    public CatalogStore Catalog { get; }
    public DocTemplates Templates { get; set; }
    public string DataDir { get; }

    private AppEnvironment(string dataDir, CatalogStore catalog, DocTemplates templates)
    {
        DataDir = dataDir;
        Catalog = catalog;
        Templates = templates;
    }

    public static AppEnvironment Load()
    {
        var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
        var catalog = CatalogStore.Load(dataDir);
        var templates = DocTemplates.Load(dataDir);
        return new AppEnvironment(dataDir, catalog, templates);
    }
}
