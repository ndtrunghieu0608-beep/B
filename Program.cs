namespace SchoolFacilities;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
            MessageBox.Show(e.Exception.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);

        while (true)
        {
            using (var login = new LoginForm())
            {
                if (login.ShowDialog() != DialogResult.OK) break;
            }

            var main = new MainForm();
            Application.Run(main);
            if (!main.LoggedOut) break;
        }
    }
}
