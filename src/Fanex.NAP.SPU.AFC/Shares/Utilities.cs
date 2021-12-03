using Fanex.Logging.Extensions;
using Fanex.Logging.Extensions.Database;
using Newtonsoft.Json;
using Sentry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Fanex.NAP.SPU.AFC.Shares
{
    public static class Utilities
    {
        public static IEnumerable<IEnumerable<TValue>> Chunk2<TValue>(
            this IEnumerable<TValue> values,
            Int32 chunkSize)
        {
            return values
                .Select((v, i) => new { v, groupIndex = i / chunkSize })
                .GroupBy(x => x.groupIndex)
                .Select(g => g.Select(x => x.v));
        }

        public static IEnumerable<IEnumerable<T>> ChunkBy<T>(this IEnumerable<T> source, int chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }

        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunksize)
        {
            var q = new Queue<T>();

            foreach (var item in source)
            {
                // check if q is full
                if (q.Count == chunksize)
                {
                    var ret = q;
                    q = new Queue<T>();

                    yield return ret;
                }

                // enqueue item
                q.Enqueue(item);
            }

            yield return q;
        }

        public static async Task<TResult> Post<TParam, TResult>(string url, TParam param)
        {
            using (var web = new HttpClient { Timeout = new TimeSpan(0, 5, 0) })
            {
                var response = await web.PostAsJsonAsync(url, param).ConfigureAwait(false);

                return await response.Content.ReadFromJsonAsync<TResult>().ConfigureAwait(false);
            }
        }

        public static async Task<TResult> Post<TResult>(string url)
        {
            using (var web = new HttpClient { Timeout = new TimeSpan(0, 5, 0) })
            {
                var response = await web.PostAsJsonAsync(url, string.Empty).ConfigureAwait(false);

                return await response.Content.ReadFromJsonAsync<TResult>().ConfigureAwait(false);
            }
        }

        public static void LogSentryInfo<T>(string serviceName, string message, T data, bool isWriteLog, ISentryClient sentryClient)
        {
            if (isWriteLog)
            {
                var logMessage = $"{serviceName} - {message} {(data != null ? " - Data:" + JsonConvert.SerializeObject(data) : "")} - {DateTime.Now}";

                sentryClient.CaptureMessage(logMessage);
            }
        }

        public static void LogSentryError<T>(string serviceName, string message, T data, Exception ex, ISentryClient sentryClient)
        {
            var logMessage = new LogMessage(ex.Message)
                .AddMessage($"{serviceName}")
                .AddSection("Data Info", DatabaseInfoGetters.GetDbExceptionInfo(ex))
                .AddSection("Exception Info", CommonInfoGetters.GetExceptionInfo(ex))
                .AddMessage($"{message} - Data {JsonConvert.SerializeObject(data)} - { DateTime.Now}").ToString();

            sentryClient.CaptureMessage(logMessage, Sentry.Protocol.SentryLevel.Error);
        }

        public static string ReadTextFile(string filePath)
        {
            using (var sr = new StreamReader(filePath))
            {
                return sr.ReadToEnd();
            }
        }
    }
}