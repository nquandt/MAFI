using System.Threading;
using System.Windows;
using CefSharp;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using McMasterAddin;
using System.Windows.Input;

namespace McMasterAddin
{
  /// <summary>
  /// Interaction logic for MainView.xaml
  /// </summary>
  public partial class MainView : UserControl
  {
    private MainWindow _myWindow;
    // chromium does not manage timeouts, so we'll implement one
    //private ManualResetEvent manualResetEvent = new ManualResetEvent(false);

    private string currentURL = "";
    //private string currentSource = "";
    private string s = "";
    JavaScriptInteractionObj jsObj;
    public MainView()
    {
      InitializeComponent();
      jsObj = new JavaScriptInteractionObj();
      Browser.RegisterAsyncJsObject("mainWindowOBJ",jsObj);
      currentURL = (string)Browser.Address.Clone();
      using (System.IO.Stream myStream = System.Reflection
        .Assembly.GetExecutingAssembly()
         .GetManifestResourceStream(
          "McMasterAddin.Resources.addButtonScripts.js"))
      {
        using (System.IO.StreamReader sRdr =
          new System.IO.StreamReader(myStream))
        {
          s = sRdr.ReadToEnd();
        }
      }
    }

    /// <summary>
    /// Manage the IsLoading parameter
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PageLoadingStateChanged(object sender,
      LoadingStateChangedEventArgs e)
    {
      if (!e.IsLoading)
      {
        Dispatcher.Invoke(new System.Action(() =>
        {
          if (currentURL != Browser.Address)
          {
            Browser.ExecuteScriptAsync(s);
            currentURL = (string)Browser.Address.Clone();
          }
        }));
      }
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      if (IsInitialized)
      {
        Browser.LoadingStateChanged += PageLoadingStateChanged;
        _myWindow = (MainWindow)((Grid)((ContentControl)
          ((ContentPresenter)this.TemplatedParent)
          .TemplatedParent).Parent).Parent;
        jsObj._mW = _myWindow;
      }
    }
  }

  public class UserControl1ViewModel : BaseViewModel, IPageViewModel
  {
    private ICommand _goTo2;

    public ICommand GoTo2
    {
      get
      {
        return _goTo2 ?? (_goTo2 = new RelayCommand(x =>
        {
          Mediator.Notify("GoTo2Screen", "");
        }));
      }
    }
  }
  public class JavaScriptInteractionObj
  {
    public MainWindow _mW;

    public JavaScriptInteractionObj()
    {

    }
    public void PreLoadStep(string pNumber)
    {
      _mW.StandardAddInServer.PreLoadStepFile(
        pNumber.Replace("partNumber", ""),0);
    }
    public void AddToAssembly(string pNumber)
    {
      //MessageBox.Show(pNumber);
      _mW.StandardAddInServer.GetSource(
        pNumber.Replace("partNumber", ""), true);
    }

    public void OpenAsPart(string pNumber)
    {
      //MessageBox.Show(pNumber);
      _mW.StandardAddInServer.GetSource(
        pNumber.Replace("partNumber", ""), false);
      _mW.Dispatcher.BeginInvoke(new System.Action(() =>
      {
        _mW.Close();
      }));
    }
  }

}
