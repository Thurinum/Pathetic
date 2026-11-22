using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Pathetic.Slang.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class SlangGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor UnparsableJson = new(
        id: "PATSLANG001",
        title: "Unreadable Slang reflection file",
        messageFormat: "Could not read '{0}': {1}",
        category: "Pathetic.Slang",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateModule = new(
        id: "PATSLANG002",
        title: "Duplicate Slang module",
        messageFormat: "Slang module '{0}' is declared by more than one reflection file",
        category: "Pathetic.Slang",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedBinding = new(
        id: "PATSLANG003",
        title: "Unsupported Slang binding kind",
        messageFormat: "Binding '{0}' of module '{1}' has unsupported kind '{2}'",
        category: "Pathetic.Slang",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidIdentifier = new(
        id: "PATSLANG004",
        title: "Invalid generated identifier",
        messageFormat: "'{0}' is not a valid C# identifier",
        category: "Pathetic.Slang",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NamespaceMismatch = new(
        id: "PATSLANG005",
        title: "Inconsistent generated namespace",
        messageFormat: "Module '{0}' uses namespace '{1}' while module '{2}' uses '{3}'",
        category: "Pathetic.Slang",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ParsedModule> parsed = context.AdditionalTextsProvider
            .Where(static text => text.Path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .Select(static (text, token) => ModuleParser.Parse(text.Path, text.GetText(token)?.ToString()));

        context.RegisterSourceOutput(parsed.Collect(), static (production, modules) => Emit(production, modules));
    }

    private static void Emit(SourceProductionContext context, ImmutableArray<ParsedModule> parsed)
    {
        List<SlangModuleModel> modules = [];

        foreach (ParsedModule module in parsed)
        {
            if (module.Model is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(UnparsableJson, Location.None, module.Path, module.Error));
                continue;
            }

            modules.Add(module.Model);
        }

        modules.Sort(static (a, b) => string.CompareOrdinal(a.Module, b.Module));

        for (int i = 1; i < modules.Count; i++)
        {
            if (string.Equals(modules[i].Module, modules[i - 1].Module, StringComparison.Ordinal))
                context.ReportDiagnostic(Diagnostic.Create(DuplicateModule, Location.None, modules[i].Module));
        }

        if (modules.Count == 0)
            return;

        string targetNamespace = modules[0].Namespace;

        foreach (SlangModuleModel module in modules.Skip(1))
        {
            if (!string.Equals(module.Namespace, targetNamespace, StringComparison.Ordinal))
                context.ReportDiagnostic(Diagnostic.Create(NamespaceMismatch, Location.None, module.Module, module.Namespace, modules[0].Module, targetNamespace));
        }

        context.AddSource("ResourceRefs.g.cs", SourceText.From(ResourceRefsEmitter.Emit(targetNamespace), Encoding.UTF8));

        foreach (SlangModuleModel module in modules)
        {
            if (!Identifier.IsValid(module.Module))
            {
                context.ReportDiagnostic(Diagnostic.Create(InvalidIdentifier, Location.None, module.Module));
                continue;
            }

            bool valid = true;

            foreach (BindingModel binding in module.Bindings)
            {
                if (!ResourceRefsEmitter.TryResolveToken(binding.Kind, out _))
                {
                    context.ReportDiagnostic(Diagnostic.Create(UnsupportedBinding, Location.None, binding.Name, module.Module, binding.Kind));
                    valid = false;
                }
            }

            foreach (TypeModel type in module.Types)
            {
                if (!Identifier.IsValid(type.Name))
                {
                    context.ReportDiagnostic(Diagnostic.Create(InvalidIdentifier, Location.None, type.Name));
                    valid = false;
                }

                foreach (FieldModel field in type.Fields)
                {
                    if (!Identifier.IsValid(field.Name))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(InvalidIdentifier, Location.None, field.Name));
                        valid = false;
                    }
                }
            }

            if (!valid)
                continue;

            context.AddSource(
                $"{targetNamespace}.{module.Module}.Resources.g.cs",
                SourceText.From(ResourcesEmitter.Emit(targetNamespace, module), Encoding.UTF8));

            foreach (TypeModel type in module.Types)
            {
                context.AddSource(
                    $"{targetNamespace}.{module.Module}.{type.Name}.g.cs",
                    SourceText.From(StructEmitter.Emit(targetNamespace, module, type), Encoding.UTF8));
            }
        }
    }
}
