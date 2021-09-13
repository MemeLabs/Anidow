// ReSharper disable InconsistentNaming

#pragma warning disable IDE1006 // Naming Styles
namespace Anidow.Torrent_Clients
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class QBitTorrentEntry
    {
        public int added_on { get; set; }
        public long amount_left { get; set; }
        public bool auto_tmm { get; set; }
        public float availability { get; set; }
        public string category { get; set; }
        public long completed { get; set; }
        public long completion_on { get; set; }
        public int dl_limit { get; set; }
        public int dlspeed { get; set; }
        public long downloaded { get; set; }
        public long downloaded_session { get; set; }
        public long eta { get; set; }
        public bool f_l_piece_prio { get; set; }
        public bool force_start { get; set; }
        public string hash { get; set; }
        public int last_activity { get; set; }
        public string magnet_uri { get; set; }
        public int max_ratio { get; set; }
        public int max_seeding_time { get; set; }
        public string name { get; set; }
        public int num_complete { get; set; }
        public int num_incomplete { get; set; }
        public int num_leechs { get; set; }
        public int num_seeds { get; set; }
        public int priority { get; set; }
        public float progress { get; set; }
        public float ratio { get; set; }
        public int ratio_limit { get; set; }
        public string save_path { get; set; }
        public int seeding_time_limit { get; set; }
        public int seen_complete { get; set; }
        public bool seq_dl { get; set; }
        public long size { get; set; }
        public string state { get; set; }
        public bool super_seeding { get; set; }
        public string tags { get; set; }
        public int time_active { get; set; }
        public long total_size { get; set; }
        public string tracker { get; set; }
        public int up_limit { get; set; }
        public long uploaded { get; set; }
        public long uploaded_session { get; set; }
        public int upspeed { get; set; }
    }
}
#pragma warning restore IDE1006 // Naming Styles