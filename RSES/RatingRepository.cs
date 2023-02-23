using System;
using System.Collections.Generic;
using System.Text;
using Nest;
using RSModels;

namespace RSES
{
    public class RatingRepository : IESRepository
    {
        private static string _default_index = "";
        public RatingRepository(string modify_index)
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
        private static RatingRepository _instance;
        public static RatingRepository Instance
        {
            get
            {
                if (_instance == null)
                {
                    _default_index = "rs_raw_data";
                    _instance = new RatingRepository(_default_index);
                }
                return _instance;
            }
        }
        public bool Index(RawData data)
        {
            return Index(_default_index, data, "", "");
        }
        public bool IndexMany(IEnumerable<RawData> data)
        {
            return IndexMany<RawData>(_default_index, data);
        }
        public IEnumerable<RawData> GetAll()
        {
            return GetAll<RawData>(_default_index);
        }
    }
}
