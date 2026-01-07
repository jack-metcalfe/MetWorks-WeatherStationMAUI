# ProjectName

This project is a minimal class library scaffold.  
It does not include shared DTOs or other assemblies by default — references are opt‑in.

## Symbols and defaults

- TargetFramework: defaults to `net8.0`
- ImplicitUsings: defaults to `false`
- Nullable: defaults to `enable`
- LangVersion: defaults to `latest`

## Usage examples

Create a plain class library:

```bash
dotnet new met-classlib -n MyLib -o src/MyLib

Override defaults at creation time:

dotnet new met-classlib -n MyLib -o src/MyLib \
  --TargetFramework net9.0 \
  --ImplicitUsings true \
  --Nullable disable \
  --LangVersion preview

Adding references manually
This template does not add project references automatically. To reference another project in your solution:

dotnet add src/MyLib/MyLib.csproj reference src/DdiCodeGen.SourceDto/DdiCodeGen.SourceDto.csproj

You can also use the helper scripts provided in the repo:

Linux/macOS: ./add-references.sh "src/DdiCodeGen.SourceDto/DdiCodeGen.SourceDto.csproj" src/MyLib/MyLib.csproj

Windows: add-references.cmd "src/DdiCodeGen.SourceDto/DdiCodeGen.SourceDto.csproj" src\MyLib\MyLib.csproj

For multiple references, separate paths with semicolons.
