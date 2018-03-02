using System.Windows;

namespace thearcadearcade
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            UI.MenuHandler = new UI.CEF.DebugToolsMenuHandler();

            this.Width = SystemParameters.WorkArea.Width;
            this.Height = SystemParameters.WorkArea.Height;
            UI.Width = SystemParameters.WorkArea.Width;
            UI.Height = SystemParameters.WorkArea.Height;
        }
    }
}
