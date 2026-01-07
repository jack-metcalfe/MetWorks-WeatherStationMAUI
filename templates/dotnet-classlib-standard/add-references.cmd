@echo off
REM Usage: add-references.cmd "path1;path2" "ProjectFile.csproj"

set refs=%1
set proj=%2

for %%R in (%refs:;= %) do (
  echo Adding reference to %%R
  dotnet add "%proj%" reference "%%~R"
)
