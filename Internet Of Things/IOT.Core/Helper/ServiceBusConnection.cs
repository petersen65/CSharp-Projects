using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOT.Core.Helper
{
    public static class ServiceBusConnection
    {
        private const string SCHEME_SERVICE_BUS = "sb";
        private const string PARTITION_CONNECTION_STRING = "Endpoint={0}://{1}.servicebus.windows.net;SharedSecretIssuer={2};SharedSecretValue={3}";

        private static readonly string[] _separators = new[] { "=", "://", ".", ";" };

        public static string FromIssuer(string ns, string issuer, string secret, string scheme = SCHEME_SERVICE_BUS)
        {
            return string.Format(PARTITION_CONNECTION_STRING, SCHEME_SERVICE_BUS, ns, issuer, secret);
        }

        public static void ToIssuer(string connectionString, out string scheme, out string ns)
        {
            var substrings = connectionString.Split(_separators, StringSplitOptions.RemoveEmptyEntries);

            scheme = substrings[1];
            ns = substrings[2];
        }

        public static void ToIssuer(string connectionString, out string scheme, out string ns, out string issuer, out string secret)
        {
            var substrings = connectionString.Split(_separators, StringSplitOptions.RemoveEmptyEntries);

            scheme = substrings[1];
            ns = substrings[2];
            issuer = substrings[7];
            secret = substrings[9];
        }
    }
}
