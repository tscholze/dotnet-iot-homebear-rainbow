namespace HomeBear.Rainbow.Utils
{
    static class Logger
    {
        public static void Log(object sender, string message)
        {
            System.Diagnostics.Debug.WriteLine($"[{sender.ToString()}] -> {message}");
        }
    }
}
