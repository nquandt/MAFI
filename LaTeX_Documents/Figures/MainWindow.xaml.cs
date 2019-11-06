using System.Windows;

namespace McMasterAddin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }


        private readonly string urlBase = "https://www.mcmaster.com/";
        private string currentPartNumber = "#";

        /// <summary>
        /// This will send the part number information,
        /// to the main code in order to 
        /// download a .STEP file from mcmaster.com and
        /// translate into a .IPT to open into inventor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Browser_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                try
                {
                    currentPartNumber = HoverLinkBehaviour.HoverLink.Substring(urlBase.Length);
                    System.Diagnostics.Debug.WriteLine(currentPartNumber);
                }
                catch { }
            }
        }
    }
}
