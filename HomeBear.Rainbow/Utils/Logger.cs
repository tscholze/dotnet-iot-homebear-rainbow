namespace HomeBear.Rainbow.Utils
{
    static class Logger
    {
        /// <summary>
        /// Logs given message.
        /// </summary>
        /// <param name="sender">Underlying sender class.</param>
        /// <param name="message">Message that will be shown.</param>
        public static void Log(object sender, string message)
        {
            // Format values.
            var breadcrumb = sender.ToString().Split('.');
            var name = breadcrumb[breadcrumb.Length - 1];

            // Print.
            System.Diagnostics.Debug.WriteLine($"{name} :: {message}");
        }
    }
}
