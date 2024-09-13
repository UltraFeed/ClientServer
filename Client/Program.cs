namespace Client;
internal static class Program
{
    [STAThread]
    private static void Main ()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        using ClientForm form = new("Client1");
        Application.Run(form);
    }
}