namespace DasMulli.Win32.ServiceUtils;

/// <inheritdoc />
/// <summary>
/// Represents a set of configurations that specify which actions to take if a service fails.
/// A managed class that holds data referring to a <see cref="ServiceFailureActionsInfo" /> class which has unmanaged resources.
/// </summary>
public class ServiceFailureActions : IEquatable<ServiceFailureActions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceFailureActions" /> class.
    /// </summary>
    /// <param name="resetPeriod">The reset period in seconds after which previous failures are cleared.</param>
    /// <param name="rebootMessage">The reboot message used in case a reboot failure action is contained in <paramref name="actions"/>.</param>
    /// <param name="restartCommand">The command run in case a "run command" failure action is contained in <paramref name="actions"/>.</param>
    /// <param name="actions">The failure actions.</param>
    public ServiceFailureActions(TimeSpan resetPeriod, string? rebootMessage, string? restartCommand, IReadOnlyCollection<ScAction>? actions)
    {
        ResetPeriod = resetPeriod;
        RebootMessage = rebootMessage;
        RestartCommand = restartCommand;
        Actions = actions;
    }

    /// <summary>
    /// Gets the collections of configured failure actions for each successive time the service fails.
    /// </summary>
    /// <value>
    /// The collections of configured failure actions for each successive time the service fails.
    /// </value>
    public IReadOnlyCollection<ScAction>? Actions { get; }

    /// <summary>
    /// Gets the reboot message used in case a reboot failure action is configured.
    /// </summary>
    /// <value>
    /// The reboot message used in case a reboot failure action is configured.
    /// </value>
    public string? RebootMessage { get; }

    /// <summary>
    /// Gets the reset period in seconds after which previous failures are cleared.
    /// For example: When a service fails two times and then doesn't fail for this amount of time, then an
    /// additional failure is considered a first failure and not a third.
    /// </summary>
    /// <value>
    /// The reset period in seconds after which previous failures are cleared.
    /// For example: When a service fails two times and then doesn't fail for this amount of time, then an
    /// additional failure is considered a first failure and not a third.
    /// </value>
    public TimeSpan ResetPeriod { get; }

    /// <summary>
    /// Gets the command run in case a "run command" failure action is configured.
    /// </summary>
    /// <value>
    /// The command run in case a "run command" failure action is configured.
    /// </value>
    public string? RestartCommand { get; }

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to this instance.
    /// </summary>
    /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        return obj is ServiceFailureActions actions && Equals(actions);
    }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    /// true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.
    /// </returns>
    public bool Equals(ServiceFailureActions? other)
    {
        if (other == null)
        {
            return false;
        }
        return GetHashCode() == other.GetHashCode();
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
    /// </returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ResetPeriod);
        hash.Add(RebootMessage);
        hash.Add(RestartCommand);
        if (Actions != null)
        {
            foreach (var action in Actions)
            {
                hash.Add(action);
            }
        }

        return hash.ToHashCode();
    }
}
