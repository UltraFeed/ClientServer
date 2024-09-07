namespace Client;
internal static class Program
{
    [STAThread]
    private static void Main ()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        using ClientForm form = new();
        Application.Run(form);
    }
}