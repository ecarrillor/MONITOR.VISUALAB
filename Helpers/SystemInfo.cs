namespace MONITOR.SERVICE.VISUALAB.Helpers
{
    public class SystemInfo
    {
        public class CoreInfo
        {
            public string? Name { get; set; }
            public double Load { get; set; }
            public double Temp { get; set; }
        }

        public List<CoreInfo> CoreInfos = new List<CoreInfo>();

        private CoreInfo GetCoreInfo(string name)
        {
            CoreInfo coreInfo = CoreInfos.SingleOrDefault(c => c.Name == name);
            if (coreInfo is null)
            {
                coreInfo = new CoreInfo { Name = name };
                CoreInfos.Add(coreInfo);
            }

            return coreInfo;
        }

        public void AddOrUpdateCoreTemp(string name, double temp)
        {
            CoreInfo coreInfo = GetCoreInfo(name);
            coreInfo.Temp = temp;
        }

        public void AddOrUpdateCoreLoad(string name, double load)
        {
            CoreInfo coreInfo = GetCoreInfo(name);
            coreInfo.Load = load;
        }
    }
}
