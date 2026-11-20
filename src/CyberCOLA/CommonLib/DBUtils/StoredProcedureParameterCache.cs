using Microsoft.Data.SqlClient;
using System.Collections.Concurrent;

namespace Com.Cybersoft4u.CommonLib.DBUtils
{
    /// <summary>
    /// Stored procedure parameter definitions, derived once per connection string and procedure and then copied onto
    /// each command. Deriving them on every call (SqlCommandBuilder.DeriveParameters) cost one extra database round
    /// trip per stored procedure call: about half of an Auth request's time under load (AuthTiming, 2026-11-16).
    /// A procedure whose signature changes while the service runs fails once with the old definitions; callers
    /// invalidate the entry on any execution error, so the next call derives again.
    /// </summary>
    public static class StoredProcedureParameterCache
    {
        private static readonly ConcurrentDictionary<string, SqlParameter[]> Templates = new(StringComparer.Ordinal);

        /// <summary>Cache key; the connection string keeps procedures of different databases apart.</summary>
        public static string Key(string connectionString, string storeProcedureName) => connectionString + "\u0001" + storeProcedureName;

        /// <summary>
        /// Fills <paramref name="command"/>.Parameters from the cached definitions, or calls <paramref name="derive"/>
        /// (which must populate the command's parameters from the database) and caches a copy of the result.
        /// </summary>
        /// <returns>true when the definitions came from the cache.</returns>
        public static bool Apply(string key, SqlCommand command, Action<SqlCommand> derive)
        {
            if (Templates.TryGetValue(key, out var template))
            {
                command.Parameters.Clear();
                foreach (var parameter in template)
                {
                    command.Parameters.Add(Copy(parameter));
                }
                return true;
            }

            derive(command);
            // Copies, so values later assigned to this command never leak into the template shared across threads.
            Templates[key] = command.Parameters.Cast<SqlParameter>().Select(Copy).ToArray();
            return false;
        }

        public static void Invalidate(string key) => Templates.TryRemove(key, out _);

        /// <summary>For tests.</summary>
        public static void Clear() => Templates.Clear();

        private static SqlParameter Copy(SqlParameter parameter)
        {
            var copy = (SqlParameter)((ICloneable)parameter).Clone();
            copy.Value = null;
            return copy;
        }
    }
}
