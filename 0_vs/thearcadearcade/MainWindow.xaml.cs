using System.Windows;

namespace thearcadearcade
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            UI.MenuHandler = new UI.CEF.DebugToolsMenuHandler();

            UI.JavascriptObjectRepository.ResolveObject += (sender, e) =>
            {
                var repo = e.ObjectRepository;
                if (e.ObjectName == "app")
                {
                    repo.Register("app", new AppCallbacks(), true);
                }
            };

            this.Width = SystemParameters.WorkArea.Width;
            this.Height = SystemParameters.WorkArea.Height;
            UI.Width = SystemParameters.WorkArea.Width;
            UI.Height = SystemParameters.WorkArea.Height;
        }
    }
}
