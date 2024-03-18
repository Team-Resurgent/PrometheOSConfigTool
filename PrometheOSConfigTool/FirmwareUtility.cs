namespace PrometheOSConfigTool
{
    public static class FirmwareUtility
    {
        public static int SearchData(byte[] data, byte[] searchPattern)
        {
            if (data.Length < searchPattern.Length)
            {
                return -1;
            }
            for (var i = 0; i < data.Length; ++i)
            {
                for (var j = 0; j < searchPattern.Length; ++j)
                {
                    if ((i + j) >= data.Length || searchPattern[j] != data[i + j])
                    {
                        break;
                    }
                    if (j == searchPattern.Length - 1)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public static bool LoadFirmwareComfig(string path, ref Config config, ref byte[] biosData)
        {
            biosData = File.ReadAllBytes(path);

            var searchPatternMain = new byte[] { (byte)'P', (byte)'R', (byte)'O', (byte)'M', (byte)'E', (byte)'T', (byte)'H', (byte)'E', (byte)'O', (byte)'S', (byte)'-', (byte)'-', (byte)'-', (byte)'-' };
            var configOffsetMain = SearchData(biosData, searchPatternMain);
            if (configOffsetMain >= 0)
            {                
                var version = new string(new char[] { (char)biosData[configOffsetMain + 14], (char)biosData[configOffsetMain + 15] });                
                if (version != "01")
                {
                    return false;
                }

                config.AVCheck = biosData[configOffsetMain + 16];
                config.DriveSetup = biosData[configOffsetMain + 17];
                config.UDMAMode = biosData[configOffsetMain + 18];
                config.SplashDelay = biosData[configOffsetMain + 19];
            }

            return true;
        }

        public static void SaveFirmwareConfig(Config config, string loadPath, string savePath, byte[] biosData, bool bfm)
        {
            var searchPatternMain = new byte[] { (byte)'P', (byte)'R', (byte)'O', (byte)'M', (byte)'E', (byte)'T', (byte)'H', (byte)'E', (byte)'O', (byte)'S', (byte)'-', (byte)'-', (byte)'-', (byte)'-' };
            var configOffsetMain = SearchData(biosData, searchPatternMain);
            if (configOffsetMain >= 0)
            {
                var version = new string(new char[] { (char)biosData[configOffsetMain + 14], (char)biosData[configOffsetMain + 15] });
                if (version != "01")
                {
                    return;
                }
                biosData[configOffsetMain + 16] = config.AVCheck;
                biosData[configOffsetMain + 17] = config.DriveSetup;
                biosData[configOffsetMain + 18] = config.UDMAMode;
                biosData[configOffsetMain + 19] = config.SplashDelay;
            }
            File.WriteAllBytes(savePath, biosData);
        }
    }
}
