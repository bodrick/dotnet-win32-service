namespace DasMulli.Win32.ServiceUtils;

/// <summary>
/// Allows managing registered Windows services on a machine
/// </summary>
public sealed class Win32ServiceManager
{
    private readonly string? _databaseName;
    private readonly string? _machineName;
    private readonly INativeInterop _nativeInterop;

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32ServiceManager"/> class that
    /// can manage Windows services on a specified machine and configuration database.
    /// </summary>
    /// <param name="machineName">The name of the machine to manage.</param>
    /// <param name="databaseName">The name of the database to manage.</param>
    public Win32ServiceManager(string? machineName = null, string? databaseName = null) : this(machineName, databaseName, Win32Interop.Wrapper)
    {
    }

    internal Win32ServiceManager(string? machineName, string? databaseName, INativeInterop nativeInterop)
    {
        _machineName = machineName;
        _databaseName = databaseName;
        _nativeInterop = nativeInterop;
    }

    /// <summary>
    /// Creates or updates a Windows service.
    /// This does not cause the service to restart.
    /// </summary>
    /// <param name="serviceDefinition">The service definition.</param>
    /// <param name="startImmediately">If set to <see langword="true"/>, the service will be started immediately after updating. Has no effect if the service is already running.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="ServiceDefinition.BinaryPath"/> is null or empty or <see cref="ServiceDefinition.ServiceName"/> is null or empty.
    /// </exception>
    /// <exception cref="PlatformNotSupportedException">Thrown when run on a non-Windows platform.</exception>
    public void CreateOrUpdateService(ServiceDefinition serviceDefinition, bool startImmediately = false)
    {
        if (string.IsNullOrEmpty(serviceDefinition.BinaryPath))
        {
            throw new ArgumentException($"Invalid service definition. {nameof(ServiceDefinition.BinaryPath)} must not be null or empty.", nameof(serviceDefinition));
        }
        if (string.IsNullOrEmpty(serviceDefinition.ServiceName))
        {
            throw new ArgumentException($"Invalid service definition. {nameof(ServiceDefinition.ServiceName)} must not be null or empty.", nameof(serviceDefinition));
        }

        try
        {
            using var mgr = ServiceControlManager.Connect(_nativeInterop, _machineName, _databaseName, ServiceControlManagerAccessRights.All);
            if (mgr.TryOpenService(serviceDefinition.ServiceName, ServiceControlAccessRights.All, out var existingService, out var errorException))
            {
                using (existingService)
                {
                    DoUpdateService(existingService, serviceDefinition, startImmediately);
                }
            }
            else
            {
                if (errorException.NativeErrorCode == KnownWin32ErrorCodes.ERROR_SERVICE_DOES_NOT_EXIST)
                {
                    DoCreateService(mgr, serviceDefinition, startImmediately);
                }
                else
                {
                    throw errorException;
                }
            }
        }
        catch (DllNotFoundException dllException)
        {
            throw new PlatformNotSupportedException(nameof(Win32ServiceHost) + " is only supported on Windows with service management API set.", dllException);
        }
    }

    /// <summary>
    /// Creates a new Windows service.
    /// </summary>
    /// <param name="serviceDefinition">The service definition.</param>
    /// <param name="startImmediately">If set to <see langword="true"/>, the service will be started immediately after registering.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="ServiceDefinition.BinaryPath"/> is null or empty or <see cref="ServiceDefinition.ServiceName"/> is null or empty.
    /// </exception>
    /// <exception cref="PlatformNotSupportedException">Thrown when run on a non-Windows platform.</exception>
    public void CreateService(ServiceDefinition serviceDefinition, bool startImmediately = false)
    {
        if (string.IsNullOrEmpty(serviceDefinition.BinaryPath))
        {
            throw new ArgumentException($"Invalid service definition. {nameof(ServiceDefinition.BinaryPath)} must not be null or empty.", nameof(serviceDefinition));
        }
        if (string.IsNullOrEmpty(serviceDefinition.ServiceName))
        {
            throw new ArgumentException($"Invalid service definition. {nameof(ServiceDefinition.ServiceName)} must not be null or empty.", nameof(serviceDefinition));
        }

        try
        {
            using var mgr = ServiceControlManager.Connect(_nativeInterop, _machineName, _databaseName, ServiceControlManagerAccessRights.All);
            DoCreateService(mgr, serviceDefinition, startImmediately);
        }
        catch (DllNotFoundException dllException)
        {
            throw new PlatformNotSupportedException(nameof(Win32ServiceHost) + " is only supported on Windows with service management API set.", dllException);
        }
    }

    /// <summary>
    /// Deletes a Windows service.
    /// </summary>
    /// <param name="serviceName">The name of the service to delete.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="serviceName"/> is null or empty.</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown when run on a non-Windows platform.</exception>
    public void DeleteService(string serviceName)
    {
        if (string.IsNullOrEmpty(serviceName))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(serviceName));
        }

        try
        {
            using var mgr = ServiceControlManager.Connect(_nativeInterop, _machineName, _databaseName, ServiceControlManagerAccessRights.All);
            using var svc = mgr.OpenService(serviceName, ServiceControlAccessRights.All);
            svc.Delete();
        }
        catch (DllNotFoundException dllException)
        {
            throw new PlatformNotSupportedException(nameof(Win32ServiceHost) + " is only supported on Windows with service management API set.", dllException);
        }
    }

    private static void DoCreateService(ServiceControlManager serviceControlManager, ServiceDefinition serviceDefinition, bool startImmediately)
    {
        using var svc = serviceControlManager.CreateService(serviceDefinition.ServiceName, serviceDefinition.DisplayName, serviceDefinition.BinaryPath, ServiceType.Win32OwnProcess,
            serviceDefinition.AutoStart ? ServiceStartType.AutoStart : ServiceStartType.StartOnDemand, serviceDefinition.ErrorSeverity, serviceDefinition.Credentials);

        if (!string.IsNullOrEmpty(serviceDefinition.Description))
        {
            svc.SetDescription(serviceDefinition.Description);
        }

        if (serviceDefinition.FailureActions != null)
        {
            svc.SetFailureActions(serviceDefinition.FailureActions);
            svc.SetFailureActionFlag(serviceDefinition.FailureActionsOnNonCrashFailures);
        }

        if (serviceDefinition.AutoStart && serviceDefinition.DelayedAutoStart)
        {
            svc.SetDelayedAutoStartFlag(true);
        }

        if (startImmediately)
        {
            svc.Start();
        }
    }

    private static void DoUpdateService(ServiceHandle existingService, ServiceDefinition serviceDefinition, bool startIfNotRunning)
    {
        existingService.ChangeConfig(serviceDefinition.DisplayName, serviceDefinition.BinaryPath, ServiceType.Win32OwnProcess,
            serviceDefinition.AutoStart ? ServiceStartType.AutoStart : ServiceStartType.StartOnDemand, serviceDefinition.ErrorSeverity,
            serviceDefinition.Credentials);
        existingService.SetDescription(serviceDefinition.Description);
        existingService.SetFailureActions(serviceDefinition.FailureActions);
        existingService.SetFailureActionFlag(serviceDefinition.FailureActionsOnNonCrashFailures);
        existingService.SetDelayedAutoStartFlag(serviceDefinition.AutoStart && serviceDefinition.DelayedAutoStart);
        if (startIfNotRunning)
        {
            existingService.Start(false);
        }
    }
}
