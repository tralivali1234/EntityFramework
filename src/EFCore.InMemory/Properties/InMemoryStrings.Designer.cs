// <auto-generated />

using System.Reflection;
using System.Resources;
using JetBrains.Annotations;

namespace Microsoft.EntityFrameworkCore.Internal
{
    /// <summary>
    ///		This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public static class InMemoryStrings
    {
        private static readonly ResourceManager _resourceManager
            = new ResourceManager("Microsoft.EntityFrameworkCore.Properties.InMemoryStrings", typeof(InMemoryStrings).GetTypeInfo().Assembly);

        /// <summary>
        ///     Saved {count} entities to in-memory store.
        /// </summary>
        public static string LogSavedChanges([CanBeNull] object count)
            => string.Format(
                GetString("LogSavedChanges", nameof(count)),
                count);

        /// <summary>
        ///     Transactions are not supported by the in-memory store. See http://go.microsoft.com/fwlink/?LinkId=800142
        /// </summary>
        public static string TransactionsNotSupported
            => GetString("TransactionsNotSupported");

        /// <summary>
        ///     Attempted to update or delete an entity that does not exist in the store.
        /// </summary>
        public static string UpdateConcurrencyException
            => GetString("UpdateConcurrencyException");

        private static string GetString(string name, params string[] formatterNames)
        {
            var value = _resourceManager.GetString(name);
            for (var i = 0; i < formatterNames.Length; i++)
            {
                value = value.Replace("{" + formatterNames[i] + "}", "{" + i + "}");
            }

            return value;
        }
    }
}
