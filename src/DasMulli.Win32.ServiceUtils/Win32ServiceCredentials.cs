using System;
using JetBrains.Annotations;

namespace DasMulli.Win32.ServiceUtils
{
    /// <summary>
    /// Represents credentials for accounts to run Windows services with.
    /// </summary>
    /// <seealso cref="IEquatable{T}" />
    [PublicAPI]
    public struct Win32ServiceCredentials : IEquatable<Win32ServiceCredentials>
    {
        /// <summary>
        /// The Local Service account. The service will have minimum access to the system and anonymous network credentials.
        /// Recommended for use in logic-only applications.
        /// Consider using a custom account instead for granular control over file system permissions.
        /// </summary>
        public static Win32ServiceCredentials LocalService = new(@"NT AUTHORITY\LocalService", null);

        /// <summary>
        /// The Local System account. The service will have full access to the system and machine network credentials.
        /// Not recommended to use in production environments.
        /// </summary>
        public static Win32ServiceCredentials LocalSystem = new(null, null);

        /// <summary>
        /// The Network Service account. The service will have minimum access to the system and machine network credentials.
        /// Recommended for use in logic-only applications that need to authenticate to networks using machine credentials.
        /// Consider using a custom account instead for granular control over file system permissions and network authorization control.
        /// </summary>
        public static Win32ServiceCredentials NetworkService = new(@"NT AUTHORITY\NetworkService", null);

        /// <summary>
        /// Creates a new <see cref="Win32ServiceCredentials"/> instance to represent an account under which to run Windows services.
        /// </summary>
        /// <param name="userName">The name of the user.</param>
        /// <param name="password">The password.</param>
        public Win32ServiceCredentials(string? userName, string? password)
        {
            UserName = userName;
            Password = password;
        }

        /// <summary>
        /// Gets the password.
        /// </summary>
        /// <value>
        /// The password.
        /// </value>
        public string? Password { get; }

        /// <summary>
        /// Gets the name of the user.
        /// </summary>
        /// <value>
        /// The name of the user.
        /// </value>
        public string? UserName { get; }

        /// <summary>
        /// Implements the operator <c>!=</c>.
        /// </summary>
        public static bool operator !=(Win32ServiceCredentials left, Win32ServiceCredentials right) => !left.Equals(right);

        /// <summary>
        /// Implements the operator <c>==</c>.
        /// </summary>
        public static bool operator ==(Win32ServiceCredentials left, Win32ServiceCredentials right) => left.Equals(right);

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(Win32ServiceCredentials other) =>
            string.Equals(UserName, other.UserName, StringComparison.Ordinal) &&
            string.Equals(Password, other.Password, StringComparison.Ordinal);

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
            return obj is Win32ServiceCredentials credentials && Equals(credentials);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(UserName, Password);
    }
}
