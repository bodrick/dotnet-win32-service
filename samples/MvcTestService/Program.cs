using System.Reflection;
using System.Text.RegularExpressions;
using DasMulli.Hosting.WindowsServices;
using DasMulli.Win32.ServiceUtils;
using static System.Console;

namespace MvcTestService;

public static class Program
{
    private const string InteractiveFlag = "--interactive";
    private const string PreserveWorkingDirectoryFlag = "--preserve-working-directory";
    private const string RegisterServiceFlag = "--register-service";
    private const string RunAsServiceFlag = "--run-as-service";
    private const string ServiceDescription = "Demo ASP.NET Core MVC Service running on .NET Core";
    private const string ServiceDisplayName = "Demo .NET Core MVC Service";
    private const string ServiceName = "DemoMvcService";
    private const string ServiceWorkingDirectoryFlag = "--working-directory";
    private const string UnregisterServiceFlag = "--unregister-service";

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });

    public static void Main(string[] args)
    {
        try
        {
            if (args.Contains(RunAsServiceFlag))
            {
                args = args.Where(a => a != RunAsServiceFlag).ToArray();
                RunAsService(args);
            }
            else if (args.Contains(RegisterServiceFlag))
            {
                RegisterService();
            }
            else if (args.Contains(UnregisterServiceFlag))
            {
                UnregisterService();
            }
            else if (args.Contains(InteractiveFlag))
            {
                RunInteractive(args);
            }
            else
            {
                DisplayHelp();
            }
        }
        catch (Exception ex)
        {
            WriteLine($"An error occurred: {ex.Message}");
        }
    }

    private static void DisplayHelp()
    {
        WriteLine(ServiceDescription);
        WriteLine();
        WriteLine("This demo application is intended to be run as Windows service. Use one of the following options:");
        WriteLine();
        WriteLine($"  --register-service            Registers and starts this program as a Windows service named \"{ServiceDisplayName}\"");
        WriteLine("                                All additional arguments will be passed to ASP.NET Core's WebHostBuilder.");
        WriteLine();
        WriteLine("  --preserve-working-directory  Saves the current working directory to the service configuration.");
        WriteLine("                                Set this when running via 'dotnet run' or when the application content");
        WriteLine("                                is not located next to the application.");
        WriteLine();
        WriteLine("  --unregister-service          Removes the Windows service created by --register-service.");
        WriteLine();
        WriteLine("  --interactive                 Runs the underlying ASP.NET Core app. Useful to test arguments.");
    }

    private static string EscapeCommandLineArgument(string arg)
    {
        // http://stackoverflow.com/a/6040946/784387
        arg = Regex.Replace(arg, @"(\\*)" + "\"", @"$1$1\" + "\"");
        arg = "\"" + Regex.Replace(arg, @"(\\+)$", "$1$1") + "\"";
        return arg;
    }

    private static void RegisterService()
    {
        // Environment.GetCommandLineArgs() includes the current DLL from a "dotnet my.dll --register-service" call, which is not passed to Main()
        var commandLineArgs = Environment.GetCommandLineArgs();

        var serviceArgs = commandLineArgs
            .Where(arg => arg != RegisterServiceFlag && arg != PreserveWorkingDirectoryFlag)
            .Select(EscapeCommandLineArgument)
            .Append(RunAsServiceFlag);

        var host = Environment.ProcessPath;

        if (host?.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase) == false)
        {
            // For self-contained apps, skip the dll path
            serviceArgs = serviceArgs.Skip(1);
        }

        if (commandLineArgs.Contains(PreserveWorkingDirectoryFlag))
        {
            serviceArgs = serviceArgs
                .Append(ServiceWorkingDirectoryFlag)
                .Append(EscapeCommandLineArgument(Directory.GetCurrentDirectory()));
        }

        var fullServiceCommand = host + " " + string.Join(" ", serviceArgs);

        // Do not use LocalSystem in production.. but this is good for demos as LocalSystem will have access to some random git-clone path
        // When the service is already registered and running, it will be reconfigured but not restarted
        var serviceDefinition = new ServiceDefinitionBuilder(ServiceName)
            .WithDisplayName(ServiceDisplayName)
            .WithDescription(ServiceDescription)
            .WithBinaryPath(fullServiceCommand)
            .WithCredentials(Win32ServiceCredentials.LocalSystem)
            .WithAutoStart(true)
            .Build();

        new Win32ServiceManager().CreateOrUpdateService(serviceDefinition, true);

        WriteLine($@"Successfully registered and started service ""{ServiceDisplayName}"" (""{ServiceDescription}"")");
    }

    private static void RunAsService(string[] args)
    {
        // easy fix to allow using default web host builder without changes
        var wdFlagIndex = Array.IndexOf(args, ServiceWorkingDirectoryFlag);
        if (wdFlagIndex >= 0 && wdFlagIndex < args.Length - 1)
        {
            var workingDirectory = args[wdFlagIndex + 1];
            Directory.SetCurrentDirectory(workingDirectory);
            // remove the flat + argument from the args passed to the web host builder
            args = args.Take(wdFlagIndex).Concat(args.Skip(wdFlagIndex + 2)).ToArray();
        }
        else
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? throw new InvalidOperationException());
        }

        CreateHostBuilder(args).Build().RunAsService(ServiceName);
    }

    private static void RunInteractive(string[] args) => CreateHostBuilder(args.Where(a => a != InteractiveFlag).ToArray()).Build().Run();

    private static void UnregisterService()
    {
        new Win32ServiceManager()
            .DeleteService(ServiceName);

        WriteLine($@"Successfully unregistered service ""{ServiceDisplayName}"" (""{ServiceDescription}"")");
    }
}
