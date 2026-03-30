namespace VRise.Protocol.Connect.Messages.ResponseObj
{
    public class MobInfo
    {
        public int Id { get; set; }
        public string UniqueName { get; set; }  // 用於動態 offset 檢測
        public int Tier { get; set; }
        public string Type { get; set; }
        public string HarvestableType { get; set; }
        public int Rarity { get; set; }
        public string Queue { get; set; }
        public string MobName { get; set; }
    }
}
