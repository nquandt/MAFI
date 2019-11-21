using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace McMasterAddin
{
  /// <summary>
  /// Interaction logic for SettingsControl.xaml
  /// </summary>
  public partial class SettingsView : UserControl
  {
    //private MainWindow _myWindow;
    private Brush ForegroundColorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC"));
    //private Brush BackgroundColorBrush;

    public SettingsView()
    {
      InitializeComponent();
      projectFolderTextBlock.Text = Properties.Settings.Default.projectFolder;
    }

    private void saveButton_Click(object sender, RoutedEventArgs e)
    {
      string s = projectFolderTextBlock.Text;
        if (s.Substring(s.Length-1,1) != @"\")
      {
        s += @"\";
      }
      Properties.Settings.Default.projectFolder = s;
      Properties.Settings.Default.Save();
      if (cancelButton.Command.CanExecute(cancelButton.CommandParameter)){
        cancelButton.Command.Execute(cancelButton.CommandParameter);
      }
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      if (IsInitialized)
      {                
      }
    }
  }

  public class UserControl2ViewModel : BaseViewModel, IPageViewModel
  {
    private ICommand _goTo1;

    public ICommand GoTo1
    {
      get
      {
        return _goTo1 ?? (_goTo1 = new RelayCommand(x =>
        {
          Mediator.Notify("GoTo1Screen", "");
        }));
      }
    }
  }
}
