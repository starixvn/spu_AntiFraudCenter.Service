namespace Fanex.NAP.SPU.AFC.JobService
{
    using Microsoft.Extensions.Configuration;
    using System;
    using System.ComponentModel;

    public interface IAppSettings
    {
        int Environment { get; }

        string EvironmentName { get; }

        string SPName { get; }

        string SentryDsn { get; }

        string AFCInsertAccountsAPI { get; }

        string AFCUpdateAccountsAPI { get; }

        string AFCDeleteAccountsAPI { get; }
    }

    public class AppSettings : IAppSettings
    {
        private readonly IConfiguration configuration;

        private readonly string environment;
        private readonly string evironmentName;
        private readonly string spName;
        private readonly string sentryDsn;
        private readonly string afcInsertAccountsAPI;
        private readonly string afcUpdateAccountsAPI;
        private readonly string afcDeleteAccountsAPI;

        public AppSettings(IConfiguration configuration)
        {
            this.configuration = configuration;

            environment = GetValue<string>(nameof(Environment));
            evironmentName = GetValue<string>(nameof(Environment));
            spName = GetValue<string>(nameof(SPName));
            sentryDsn = GetValue<string>("Sentry:Dsn");
            afcInsertAccountsAPI = GetValue<string>(nameof(AFCInsertAccountsAPI));
            afcUpdateAccountsAPI = GetValue<string>(nameof(AFCUpdateAccountsAPI));
            afcDeleteAccountsAPI = GetValue<string>(nameof(AFCDeleteAccountsAPI));
        }

        #region Variables

        public int Environment
        {
            get
            {
                switch (environment)
                {
                    case "LOCAL":
                        return 1;

                    case "UAT":
                        return 2;

                    default:
                        return 3;
                }
            }
        }

        public string EvironmentName => evironmentName;

        public string SPName => spName;

        public string SentryDsn => sentryDsn;

        public string AFCInsertAccountsAPI => afcInsertAccountsAPI;

        public string AFCUpdateAccountsAPI => afcUpdateAccountsAPI;

        public string AFCDeleteAccountsAPI => afcDeleteAccountsAPI;

        #endregion Variables

        #region Methods

        public T GetValue<T>(string key)
        {
            try
            {
                var value = configuration[key];

                var typeConverter = TypeDescriptor.GetConverter(typeof(T));

                return (T)typeConverter.ConvertFromString(value);
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"Key: {key}", ex);
            }
        }

        public T GetSection<T>(string key)
        {
            var section = configuration.GetSection(key);

            return section.Get<T>();
        }

        public void SetValue<T>(string key, T value)
        {
            try
            {
                var typeConverter = TypeDescriptor.GetConverter(typeof(T));

                configuration[key] = typeConverter.ConvertToString(value);
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"Key: {key}", ex);
            }
        }

        public bool UpdateIsWriteLogSetting(string serviceName, bool isWriteLog)
        {
            var key = serviceName + ":IsWriteLog";

            if (configuration[key] == null)
            {
                return false;
            }

            try
            {
                SetValue<bool>(serviceName + ":IsWriteLog", isWriteLog);
            }
            catch
            {
                return false;
            }

            return true;
        }

        #endregion Methods
    }
}