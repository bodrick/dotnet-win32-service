using System.Runtime.InteropServices;
using DasMulli.Win32.ServiceUtils;

[StructLayout(LayoutKind.Sequential)]
internal struct ServiceFailureActionsInfo : IServiceInfo
{
    private uint dwResetPeriod;
    private string? lpRebootMsg;
    private string? lpCommand;
    private int cActions;
    private IntPtr lpsaActions;

    public TimeSpan ResetPeriod => TimeSpan.FromSeconds(dwResetPeriod);

    public string? RebootMsg => lpRebootMsg;

    public string? Command => lpCommand;

    public int CountActions => cActions;

    public ScAction[] Actions => lpsaActions.MarshalUnmanagedArrayToStruct<ScAction>(cActions);

    /// <summary>
    /// This is the default, as reported by Windows.
    /// </summary>
    internal static ServiceFailureActionsInfo Default =
        new()
        {
            dwResetPeriod = 0,
            lpRebootMsg = null,
            lpCommand = null,
            cActions = 0,
            lpsaActions = IntPtr.Zero
        };

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceFailureActionsInfo"/> class.
    /// </summary>
    /// <param name="resetPeriod"></param>
    /// <param name="rebootMessage"></param>
    /// <param name="restartCommand"></param>
    /// <param name="actions"></param>
    internal ServiceFailureActionsInfo(TimeSpan resetPeriod, string? rebootMessage, string? restartCommand, IReadOnlyCollection<ScAction>? actions)
    {
        dwResetPeriod = resetPeriod == TimeSpan.MaxValue ? uint.MaxValue : (uint)Math.Round(resetPeriod.TotalSeconds);
        lpRebootMsg = rebootMessage;
        lpCommand = restartCommand;

        if (actions != null)
        {
            cActions = actions.Count;
            if (actions.Count > 0)
            {
                lpsaActions = Marshal.AllocHGlobal(Marshal.SizeOf<ScAction>() * cActions);

                if (lpsaActions == IntPtr.Zero)
                {
                    throw new InsufficientMemoryException(
                        $"Unable to allocate memory for service action, error was: 0x{Marshal.GetLastWin32Error():X}");
                }

                var nextAction = lpsaActions;

                foreach (var action in actions)
                {
                    Marshal.StructureToPtr(action, nextAction, false);
                    nextAction = (IntPtr)(nextAction.ToInt64() + Marshal.SizeOf<ScAction>());
                }
            }
            else
            {
                lpsaActions = IntPtr.Zero;
            }
        }
        else
        {
            cActions = 0;
            lpsaActions = IntPtr.Zero;
        }
    }
}
