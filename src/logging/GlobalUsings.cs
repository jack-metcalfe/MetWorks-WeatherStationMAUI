global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Text.Json;
global using System.Text.RegularExpressions;
global using System.Threading.Tasks;

global using Interfaces;

global using Microsoft.Maui.Storage;

global using Npgsql;

global using Serilog;
global using Serilog.Events;

global using Utility;

global using static Constants.Settings.Paths.GroupSettingDefinitions;

global using ILogger = Interfaces.ILogger;

global using SysDiagDebug = System.Diagnostics.Debug;
