using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Pathetic.Slang.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class SlangGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<(string HintName, string Source)> files = context.AdditionalTextsProvider
            .Where(static text => text.Path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
            .Select(static (text, token) => (HintName: Path.GetFileName(text.Path), Source: text.GetText(token)?.ToString() ?? ""));

        context.RegisterSourceOutput(files, static (production, file) =>
            production.AddSource(file.HintName, SourceText.From(file.Source, Encoding.UTF8)));
    }
}
