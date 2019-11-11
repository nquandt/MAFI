using System.Threading;
using System.Windows;
using CefSharp;
using System.Text.RegularExpressions;

namespace McMasterAddin
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {    
    private StandardAddInServer _stAddIn;
    // chromium does not manage timeouts, so we'll implement one
    private ManualResetEvent manualResetEvent = new ManualResetEvent(false);
    
    private string currentURL = "";
    private string currentSource = "";
    private string s = "";

    public MainWindow(StandardAddInServer a)
    {
      InitializeComponent();      
      Browser.RegisterAsyncJsObject("mainWindowOBJ", 
        new JavaScriptInteractionObj(a,this));
      _stAddIn = a;
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

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      Browser.LoadingStateChanged += PageLoadingStateChanged;
    }

    private void Window_Closing(object sender, 
      System.ComponentModel.CancelEventArgs e)
    {
      _stAddIn.DeleteTempFiles();
    }
  }

  public class JavaScriptInteractionObj
  {
    private StandardAddInServer _stAddIn;
    private MainWindow _mW;

    public JavaScriptInteractionObj(StandardAddInServer s, MainWindow m)
    {
      _stAddIn = s;
      _mW = m;
    }

    public void AddToAssembly(string pNumber)
    {
      MessageBox.Show(pNumber);
      _stAddIn.GetSource(StandardAddInServer.urlBase + 
        pNumber.Replace("partNumber",""),true);      
    }

    public void OpenAsPart(string pNumber)
    {
      MessageBox.Show(pNumber);
      _stAddIn.GetSource(StandardAddInServer.urlBase +
        pNumber.Replace("partNumber", ""),false);
      _mW.Dispatcher.BeginInvoke(new System.Action(() =>
      {
        _mW.Close();
      }));      
    }
  }

}
