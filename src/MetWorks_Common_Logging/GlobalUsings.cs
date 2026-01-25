global using System;
global using System.Collections.Concurrent;
global using System.Collections.Generic;
global using System.IO;
global using System.Text.Json;
global using System.Text.RegularExpressions;
global using System.Threading;
global using System.Threading.Tasks;

global using MetWorks.Interfaces;
global using MetWorks.Common.Utility;
global using MetWorks.Constants;

global using Npgsql;

global using Serilog;
global using Serilog.Core;
global using Serilog.Debugging;
global using Serilog.Events;

global using ILogger = MetWorks.Interfaces.ILogger;
global using SysDiagDebug = System.Diagnostics.Debug;
