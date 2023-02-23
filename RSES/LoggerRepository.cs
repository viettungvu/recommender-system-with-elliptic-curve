using System;
using System.Collections.Generic;
using System.Text;
using Nest;
using RSModels;

namespace RSES
{
    public class LoggerRepository:IESRepository
    {
        private static string _default_index = "";
        public LoggerRepository(string modify_index)
        {
            _default_index = !string.IsNullOrEmpty(modify_index) ? modify_index : _default_index;
            ConnectionSettings settings = new ConnectionSettings(connectionPool, sourceSerializer: Nest.JsonNetSerializer.JsonNetSerializer.Default).DefaultIndex(_default_index).DisableDirectStreaming(true);
            settings.MaximumRetries(10);
            client = new ElasticClient(settings);
            var ping = client.Ping(p => p.Pretty(true));
            if (ping.ServerError != null && ping.ServerError.Error != null)
            {
                throw new Exception("START ES FIRST");
            }
        }
        private static LoggerRepository _instance;
        public static LoggerRepository Instance
        {
            get
            {
                if (_instance == null)
                {
                    _default_index = "rs_log";
                    _instance = new LoggerRepository("rs_log");
                }
                return _instance;
            }
        }

        public bool Index(LogInfo data)
        {
            return Index<LogInfo>(_default_index, data, "", "");
        }
    }
}
