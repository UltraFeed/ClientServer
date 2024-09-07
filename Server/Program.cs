namespace Server;
internal static class Program
{
    [STAThread]
    private static void Main ()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        using ServerForm form = new();
        Application.Run(form);
    }
}
