using MetWorks.DI.Declarative.Generator;
using MetWorks.DI.Declarative.Syntax;
using MetWorks.DI.Declarative.Templates;

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 1;
}

static void PrintDiagnostic(MetWorks.DI.Declarative.Diagnostics.Diagnostic diag)
{
    var line = diag.Location is null ? "?" : (diag.Location.LineZeroBased + 1).ToString();
    var col = diag.Location is null ? "?" : (diag.Location.ColumnZeroBased + 1).ToString();
    var path = diag.Location?.LogicalPath ?? "unknown";

    var messageLines = (diag.Message ?? string.Empty)
        .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

    var firstLine = messageLines.Length > 0 ? messageLines[0] : string.Empty;
    Console.Error.WriteLine($"{diag.DiagnosticCode}: {firstLine} (line {line}, col {col}, path {path})");

    for (var i = 1; i < messageLines.Length; i++)
    {
        if (messageLines[i].Length == 0)
            continue;

        Console.Error.WriteLine($"    {messageLines[i]}");
    }
}

static (string YamlPath, string OutputDir, List<string> ReferenceAssemblyPaths, bool Verbose, bool Report, string? ReportFile, bool FormatYamlInPlace, bool CheckYamlFormat, bool SortInstancesInPlace) ParseArgs(string[] args)
{
    string? yamlPath = null;
    string? outputDir = null;
    var referenceAssemblyPaths = new List<string>();
    string? referenceAssemblyListFile = null;
    var verbose = false;
    var report = false;
    string? reportFile = null;
    var formatYamlInPlace = false;
    var checkYamlFormat = false;
    var sortInstancesInPlace = false;

    for (var i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        switch (arg)
        {
            case "--yaml" when i + 1 < args.Length:
                yamlPath = args[++i];
                break;
            case "--out" when i + 1 < args.Length:
                outputDir = args[++i];
                break;
            case "--ref" when i + 1 < args.Length:
                referenceAssemblyPaths.Add(args[++i]);
                break;
            case "--refs" when i + 1 < args.Length:
                referenceAssemblyPaths.AddRange(args[++i].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                break;
            case "--refsFile" when i + 1 < args.Length:
                referenceAssemblyListFile = Path.GetFullPath(args[++i]);
                break;
            case "--verbose":
                verbose = true;
                break;
            case "--report":
                report = true;
                break;
            case "--reportFile" when i + 1 < args.Length:
                reportFile = Path.GetFullPath(args[++i]);
                break;
            case "--formatYaml":
                formatYamlInPlace = true;
                break;
            case "--checkYamlFormat":
                checkYamlFormat = true;
                break;
            case "--sortInstances":
                sortInstancesInPlace = true;
                break;
        }
    }

    if (string.IsNullOrWhiteSpace(yamlPath))
        throw new ArgumentException("Missing required argument: --yaml <path>");

    if (string.IsNullOrWhiteSpace(outputDir) && !(formatYamlInPlace || checkYamlFormat || sortInstancesInPlace))
        throw new ArgumentException("Missing required argument: --out <dir>");

    if (!string.IsNullOrWhiteSpace(referenceAssemblyListFile) && File.Exists(referenceAssemblyListFile))
    {
        referenceAssemblyPaths.AddRange(
            File.ReadAllLines(referenceAssemblyListFile)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Trim()));
    }

    return (
        Path.GetFullPath(yamlPath),
        string.IsNullOrWhiteSpace(outputDir) ? string.Empty : Path.GetFullPath(outputDir),
        referenceAssemblyPaths,
        verbose,
        report,
        reportFile,
        formatYamlInPlace,
        checkYamlFormat,
        sortInstancesInPlace);
}

static void CleanupGeneratedFiles(string outputDir, string? reportFileName)
{
    // Safety: only delete DDI-generated .g.cs, never hand-written files.
    foreach (var file in Directory.EnumerateFiles(outputDir, "*.g.cs", SearchOption.TopDirectoryOnly))
    {
        File.Delete(file);
    }

    // Also remove template docs emitted by the generator if present.
    var generatorSummary = Path.Combine(outputDir, "GeneratorChangeSummary.md");
    if (File.Exists(generatorSummary))
        File.Delete(generatorSummary);

    if (!string.IsNullOrWhiteSpace(reportFileName))
    {
        var reportPath = Path.Combine(outputDir, reportFileName);
        if (File.Exists(reportPath))
            File.Delete(reportPath);
    }
}

try
{
    var (yamlPath, outputDir, referenceAssemblyPaths, verbose, report, reportFile, formatYamlInPlace, checkYamlFormat, sortInstancesInPlace) = ParseArgs(args);

    if (!File.Exists(yamlPath))
        return Fail($"YAML file not found: {yamlPath}");

    var yamlText = File.ReadAllText(yamlPath);

    if (formatYamlInPlace)
    {
        var formatted = YamlFormatter.Format(yamlText);
        if (!string.Equals(formatted, yamlText, StringComparison.Ordinal))
            File.WriteAllText(yamlPath, formatted);

        return 0;
    }

    if (checkYamlFormat)
    {
        if (!YamlFormatter.IsFormatted(yamlText))
            return Fail($"YAML is not formatted according to YamlDotNet defaults: {yamlPath}");

        return 0;
    }

    if (sortInstancesInPlace)
    {
        var sorted = YamlFormatter.SortInstancesByDependency(yamlText);
        if (!string.Equals(sorted, yamlText, StringComparison.Ordinal))
            File.WriteAllText(yamlPath, sorted);

        return 0;
    }

    Directory.CreateDirectory(outputDir);

    var loader = new Loader();
    var model = loader.Load(yamlText);

    if (model.Diagnostics.Count > 0)
    {
        foreach (var diag in model.Diagnostics)
            PrintDiagnostic(diag);

        return Fail($"DDI YAML validation failed ({model.Diagnostics.Count} diagnostics). No files were generated.");
    }

    var signatureDiagnostics = InitializerSignatureValidator.Validate(model, referenceAssemblyPaths);
    if (signatureDiagnostics.Count > 0)
    {
        foreach (var diag in signatureDiagnostics)
            PrintDiagnostic(diag);

        return Fail($"DDI initializer signature validation failed ({signatureDiagnostics.Count} diagnostics). No files were generated.");
    }

    var dottedPropertyDiagnostics = DottedPropertyReferenceValidator.Validate(model, referenceAssemblyPaths);
    if (dottedPropertyDiagnostics.Count > 0)
    {
        foreach (var diag in dottedPropertyDiagnostics)
            PrintDiagnostic(diag);

        var errorCount = dottedPropertyDiagnostics.Count(d => MetWorks.DI.Declarative.Diagnostics.DiagnosticCodeInfo.GetSeverity(d.DiagnosticCode) == MetWorks.DI.Declarative.Diagnostics.DiagnosticSeverity.Error);
        if (errorCount > 0)
            return Fail($"DDI dotted-property validation failed ({errorCount} errors, {dottedPropertyDiagnostics.Count} total diagnostics). No files were generated.");
    }

    var factoryBindingDiagnostics = FactoryMethodBindingValidator.Validate(model, referenceAssemblyPaths);
    if (factoryBindingDiagnostics.Count > 0)
    {
        foreach (var diag in factoryBindingDiagnostics)
            PrintDiagnostic(diag);

        return Fail($"DDI factory binding validation failed ({factoryBindingDiagnostics.Count} diagnostics). No files were generated.");
    }

    if (verbose)
    {
        foreach (var line in GenerationTraceV1.BuildLines(model, yamlPath, outputDir))
            Console.WriteLine(line);
    }

    if (report)
    {
        var resolvedReportFile = reportFile;
        if (string.IsNullOrWhiteSpace(resolvedReportFile))
            resolvedReportFile = Path.Combine(outputDir, "DdiGenerationReport.md");

        Directory.CreateDirectory(Path.GetDirectoryName(resolvedReportFile)!);

        var markdown = GenerationReportV1.BuildMarkdown(model, yamlPath, outputDir);
        File.WriteAllText(resolvedReportFile, markdown);
    }

    IReadOnlyDictionary<string, string> files;
    try
    {
        var generator = new CodeGenerator(new TemplateStore());
        files = generator.GenerateFiles(model);
    }
    catch (DdiGenerationException genEx)
    {
        foreach (var diag in genEx.Diagnostics)
            PrintDiagnostic(diag);

        return Fail("DDI generation failed.");
    }

    CleanupGeneratedFiles(outputDir, report && string.IsNullOrWhiteSpace(reportFile) ? "DdiGenerationReport.md" : null);

    foreach (var kvp in files)
    {
        var path = Path.Combine(outputDir, kvp.Key);
        File.WriteAllText(path, kvp.Value);
    }

    if (verbose)
        Console.WriteLine($"DDI_TRACE result=success filesGenerated={files.Count}");

    return 0;
}
catch (Exception ex)
{
    return Fail(ex.ToString());
}
