namespace Chamados.Api.Options;

public class UploadOptions
{
    public const string SectionName = "Upload";

    public string StoragePath { get; set; } = "uploads";

    public long MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024;

    // Sem valor default aqui de propósito: o ConfigurationBinder concatena
    // (em vez de substituir) um array já não-vazio com o que vem do
    // appsettings.json, duplicando os itens. A lista real vive em
    // appsettings.json (chave "Upload:AllowedExtensions").
    public string[] AllowedExtensions { get; set; } = [];
}
