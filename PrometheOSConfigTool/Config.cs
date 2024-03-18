namespace PrometheOSConfigTool
{
    public struct Config
    {
        public byte AVCheck { get; set; }

        public byte DriveSetup { get; set; }

        public byte UDMAMode { get; set; }

        public byte SplashDelay { get; set; }

        public Config()
        {
            SetDefaults();
        }

        public void SetDefaults()
        {
            AVCheck = 1;
            DriveSetup = 1;
            UDMAMode = 2;
            SplashDelay = 6;
        }
    }
}
