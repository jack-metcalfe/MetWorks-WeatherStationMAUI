global using System;
global using System.Collections.Generic;
global using System.ComponentModel;
global using System.Diagnostics;
global using System.Linq;
global using System.Runtime.CompilerServices;
global using System.Threading;
global using System.Threading.Tasks;
global using System.Timers;

global using MetWorks.Apps.MAUI.WeatherStationMaui.DeviceSelection;
global using MetWorks.Apps.MAUI.WeatherStationMaui.ViewModels;
global using MetWorks.Common;
global using MetWorks.Interfaces;
global using MetWorks.Models.Observables.Weather;
global using MetWorks.ServiceRegistry;

global using Microsoft.Extensions.Logging;
global using Microsoft.Maui;
global using Microsoft.Maui.ApplicationModel;
global using Microsoft.Maui.Controls;
global using Microsoft.Maui.Controls.Hosting;
global using Microsoft.Maui.Devices;
global using Microsoft.Maui.Hosting;

global using RedStar.Amounts;
global using RedStar.Amounts.StandardUnits;

global using MetWorks.Common.Utility;

global using ILogger = MetWorks.Interfaces.ILogger;
global using SystemTimer = System.Timers.Timer;
global using ThreadingTimer = System.Threading.Timer;
