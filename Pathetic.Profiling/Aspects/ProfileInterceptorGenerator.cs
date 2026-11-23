using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Pathetic.Profiling.Aspects;

[Generator(LanguageNames.CSharp)]
public sealed class ProfileInterceptorGenerator : IIncrementalGenerator
{
    private const string Void = "void";
    private const string Self = "self";
    private const string Span = "span";
    private const string Result = "result";

    private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private static readonly SymbolDisplayFormat MethodFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .AddMemberOptions(SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeContainingType);

    private static readonly DiagnosticDescriptor UnreadableTag = new(
        id: "PATHPROF001",
        title: "Profile tag cannot be read",
        messageFormat: "[ProfileTag] on '{0}' would be silently ignored because {1}",
        category: "Pathetic.Profiling",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<bool> enabled = context.ParseOptionsProvider
            .Select(static (options, _) => options.PreprocessorSymbolNames.Contains(ProfilingName.Symbol));

        IncrementalValueProvider<ImmutableArray<CallSite>> sites = context.SyntaxProvider
            .CreateSyntaxProvider(static (node, _) => node is InvocationExpressionSyntax, Analyze)
            .SelectMany(static (found, _) => found)
            .Collect();

        context.RegisterSourceOutput(sites.Combine(enabled), static (output, input) =>
        {
            (ImmutableArray<CallSite> woven, bool isEnabled) = input;
            if (isEnabled && !woven.IsDefaultOrEmpty)
                output.AddSource("ProfileInterceptors.g.cs", SourceText.From(Writer.Write(woven), System.Text.Encoding.UTF8));
        });

        IncrementalValueProvider<ImmutableArray<TagProblem>> problems = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                $"{typeof(ProfileTagAttribute).Namespace}.{nameof(ProfileTagAttribute)}",
                static (node, _) => node is PropertyDeclarationSyntax,
                FindTagProblem)
            .Where(static problem => problem is not null)
            .Select(static (problem, _) => problem!)
            .Collect();

        context.RegisterSourceOutput(problems.Combine(enabled), static (output, input) =>
        {
            (ImmutableArray<TagProblem> found, bool isEnabled) = input;
            if (!isEnabled)
                return;

            foreach (TagProblem problem in found)
                output.ReportDiagnostic(Diagnostic.Create(UnreadableTag, problem.ToLocation(), problem.Property, problem.Reason));
        });
    }

    // A tag that cannot be read would compile fine and then be skipped, so it is rejected instead.
    private static TagProblem? FindTagProblem(GeneratorAttributeSyntaxContext context, CancellationToken cancellation)
    {
        var property = (IPropertySymbol)context.TargetSymbol;
        Compilation compilation = context.SemanticModel.Compilation;

        string? reason = property switch
        {
            { IsStatic: true } => "it is static",
            { GetMethod: null } => "it has no getter",
            { GetMethod: { } getter } when !IsCallableFromGeneratedCode(getter, compilation) => "its getter is not accessible from the same assembly (make it internal or public)",
            _ => null,
        };

        Location location = property.Locations[0];
        return reason is null
            ? null
            : new TagProblem(property.Name, reason, location.SourceTree?.FilePath ?? string.Empty, location.SourceSpan, location.GetLineSpan().Span);
    }

    private static ImmutableArray<CallSite> Analyze(GeneratorSyntaxContext context, CancellationToken cancellation)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (context.SemanticModel.GetSymbolInfo(invocation, cancellation).Symbol is not IMethodSymbol method)
            return [];

        Compilation compilation = context.SemanticModel.Compilation;
        WeaveKind kind = FindWeaveKind(method, compilation);
        InterceptableLocation? location = context.SemanticModel.GetInterceptableLocation(invocation, cancellation);

        return kind == WeaveKind.None || !CanBeWoven(method, compilation) || location is null
            ? []
            : [ToCallSite(method, kind, location, compilation)];
    }

    private static WeaveKind FindWeaveKind(IMethodSymbol method, Compilation compilation)
    {
        if (HasAttribute(method, nameof(ProfileAttribute)))
            return WeaveKind.Annotated;

        return IsListedByAssembly(method, compilation) ? WeaveKind.Listed : WeaveKind.None;
    }

    private static bool HasAttribute(ISymbol symbol, string attributeName) =>
        symbol.GetAttributes().Any(attribute => attribute.AttributeClass?.Name == attributeName);

    private static bool IsListedByAssembly(IMethodSymbol method, Compilation compilation) =>
        compilation.Assembly.GetAttributes()
            .Where(attribute => attribute.AttributeClass?.Name == nameof(ProfileCallsAttribute))
            .Any(attribute => ListsMethod(attribute, method));

    private static bool ListsMethod(AttributeData assemblyAttribute, IMethodSymbol method)
    {
        (TypedConstant type, TypedConstant methods) = (assemblyAttribute.ConstructorArguments[0], assemblyAttribute.ConstructorArguments[1]);
        return SymbolEqualityComparer.Default.Equals(type.Value as ITypeSymbol, method.ContainingType)
               && methods.Values.Any(name => $"{name.Value}" == method.Name);
    }

    private static bool CanBeWoven(IMethodSymbol method, Compilation compilation) =>
        method.MethodKind == MethodKind.Ordinary
        && !method.IsGenericMethod
        && !method.ContainingType.IsGenericType
        && !method.ContainingType.IsValueType
        && IsCallableFromGeneratedCode(method, compilation);

    private static bool IsCallableFromGeneratedCode(IMethodSymbol method, Compilation compilation) =>
        compilation.IsSymbolAccessibleWithin(method, compilation.Assembly)
        && compilation.IsSymbolAccessibleWithin(method.ContainingType, compilation.Assembly);

    private static CallSite ToCallSite(IMethodSymbol method, WeaveKind kind, InterceptableLocation location, Compilation compilation)
    {
        bool annotated = kind == WeaveKind.Annotated;
        return new CallSite(
            Key: method.ToDisplayString(MethodFormat),
            OperationName: $"{method.ContainingType.Name}.{method.Name}",
            MethodName: method.Name,
            IsStatic: method.IsStatic,
            ReceiverType: method.ContainingType.ToDisplayString(TypeFormat),
            ReturnType: method.ReturnsVoid ? Void : method.ReturnType.ToDisplayString(TypeFormat),
            Parameters: [.. method.Parameters.Select(ToParameter)],
            IsFrame: annotated && MarksFrame(method),
            ResultTags: annotated ? FindResultTags(method, compilation) : [],
            LocationVersion: location.Version,
            LocationData: location.Data);
    }

    private static bool MarksFrame(IMethodSymbol method) =>
        method.GetAttributes()
            .Where(attribute => attribute.AttributeClass?.Name == nameof(ProfileAttribute))
            .SelectMany(attribute => attribute.NamedArguments)
            .Any(argument => argument.Key == nameof(ProfileAttribute.Frame) && argument.Value.Value is true);

    private static Parameter ToParameter(IParameterSymbol parameter)
    {
        (string declaration, string argument) = parameter.RefKind switch
        {
            RefKind.Ref => ("ref ", "ref "),
            RefKind.Out => ("out ", "out "),
            RefKind.In => ("in ", string.Empty),
            RefKind.RefReadOnlyParameter => ("ref readonly ", "in "),
            _ => (string.Empty, string.Empty),
        };

        return new Parameter(parameter.Type.ToDisplayString(TypeFormat), parameter.Name, declaration, argument);
    }

    private static ImmutableArray<ResultTag> FindResultTags(IMethodSymbol method, Compilation compilation) =>
        method.IsStatic
            ? []
            : [.. method.ContainingType.GetMembers().OfType<IPropertySymbol>()
                .Where(property => IsReadableFromGeneratedCode(property, compilation))
                .SelectMany(property => property.GetAttributes()
                    .Where(attribute => attribute.AttributeClass?.Name == nameof(ProfileTagAttribute))
                    .Select(attribute => new ResultTag($"{attribute.ConstructorArguments[0].Value}", property.Name)))];

    private static bool IsReadableFromGeneratedCode(IPropertySymbol property, Compilation compilation) =>
        property is { IsStatic: false, GetMethod: { } getter }
        && compilation.IsSymbolAccessibleWithin(getter, compilation.Assembly);

    private static string Literal(string value) => SymbolDisplay.FormatLiteral(value, quote: true);

    private static class Writer
    {
        public static string Write(ImmutableArray<CallSite> sites)
        {
            IEnumerable<string> interceptors = sites
                .GroupBy(site => site.Key)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select((group, index) => Interceptor(group.OrderBy(site => site.LocationData, StringComparer.Ordinal).ToList(), index));

            return $$"""
                // <auto-generated/>
                #nullable enable

                namespace System.Runtime.CompilerServices
                {
                    [global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]
                    internal sealed class InterceptsLocationAttribute : global::System.Attribute
                    {
                        public InterceptsLocationAttribute(int version, string data) { }
                    }
                }

                namespace {{ProfilingName.InterceptorNamespace}}
                {
                    internal static class ProfileInterceptors
                    {
                        private static readonly global::System.Diagnostics.ActivitySource Source = new({{Literal(ProfilingName.SourceName)}});

                {{string.Join("\n", interceptors)}}
                    }
                }
                """;
        }

        private static string Interceptor(List<CallSite> sites, int index)
        {
            CallSite site = sites[0];
            bool returnsValue = site.ReturnType != Void;
            string locations = string.Join("\n", sites.Select(s =>
                $"        [global::System.Runtime.CompilerServices.InterceptsLocation({s.LocationVersion}, {Literal(s.LocationData)})]"));
            string parameters = string.Join(", ", site.Parameters.Select(p => $"{p.Declaration}{p.Type} {p.Name}")
                .Prepend(site.IsStatic ? null : $"this {site.ReceiverType} {Self}")
                .OfType<string>());
            string arguments = string.Join(", ", site.Parameters.Select(p => $"{p.Argument}{p.Name}"));
            string call = $"{(site.IsStatic ? site.ReceiverType : Self)}.{site.MethodName}({arguments})";

            return $$"""
                {{locations}}
                        internal static {{site.ReturnType}} {{site.MethodName}}_{{index}}({{parameters}})
                        {
                            using global::System.Diagnostics.Activity? {{Span}} = Source.StartActivity({{Literal(site.OperationName)}});
                {{Lines(site.IsFrame ? [Tag(ProfilingName.FrameTag, "true")] : [])}}
                            {{(returnsValue ? $"var {Result} = {call}" : call)}};
                {{Lines(site.ResultTags.Select(tag => Tag(tag.TagName, $"{Self}.{tag.PropertyName}")))}}
                            {{(returnsValue ? $"return {Result};" : string.Empty)}}
                        }

                """;
        }

        private static string Tag(string name, string value) => $"{Span}?.SetTag({Literal(name)}, {value});";

        private static string Lines(IEnumerable<string> statements) =>
            string.Join("\n", statements.Select(statement => $"            {statement}"));
    }

    private enum WeaveKind
    {
        None,
        Annotated,
        Listed,
    }

    private sealed record Parameter(string Type, string Name, string Declaration, string Argument);

    private sealed record ResultTag(string TagName, string PropertyName);

    private sealed record TagProblem(string Property, string Reason, string FilePath, TextSpan Span, LinePositionSpan LineSpan)
    {
        public Location ToLocation() => Location.Create(FilePath, Span, LineSpan);
    }

    private sealed record CallSite(
        string Key,
        string OperationName,
        string MethodName,
        bool IsStatic,
        string ReceiverType,
        string ReturnType,
        ImmutableArray<Parameter> Parameters,
        bool IsFrame,
        ImmutableArray<ResultTag> ResultTags,
        int LocationVersion,
        string LocationData);
}
