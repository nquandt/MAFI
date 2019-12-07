using System;
using System.IO;
using reflect = System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using Inv = Inventor;//two using statements to allow for disambiguation 
//between Inventor and System.IO namespace at times
using Inventor;
using CefSharp.Wpf;
using CefSharp;

#pragma warning disable IDE1006 //naming rules
namespace McMasterAddin
{
  /// <summary>
  /// This is the primary AddIn Server class that implements the
  /// ApplicationAddInServer interface that all Inventor AddIns 
  /// are required to implement. The communication between Inventor 
  /// and the AddIn is via the methods on this interface.
  /// </summary>
  [GuidAttribute("4989fc73-4710-47df-9034-98d770e68fbb")]
  public class StandardAddInServer : ApplicationAddInServer
  {
    #region ObjectInitialization        
    private static readonly GuidAttribute m_ClientID =
      (GuidAttribute)System.Attribute.GetCustomAttribute(
        typeof(StandardAddInServer), typeof(GuidAttribute));
    public static readonly string m_ClientIDstr =
      "{" + m_ClientID.Value + "}";

    private bool wasOff = false;

    // Inventor application object.
    public Inv.Application m_invApp;
    private HeadlessWebBrowser _headlessBrowser;

    public static readonly string urlBase = "https://www.mcmaster.com/";
    //public System.Collections.Generic.List<string> fileList = new System.Collections.Generic.List<string>();
    public System.Collections.Generic.Dictionary<string, string> fileList = new System.Collections.Generic.Dictionary<string, string>();

    //single button for McMaster Catalog
    private McMasterButton m_Button;
    private McMasterImporter m_Importer;
    //user interface event
    private UserInterfaceEvents m_UIEvents;

    private UserInterfaceEventsSink_OnResetRibbonInterfaceEventHandler
        UIESink_OnResetRibbonInterfaceEventDelegate;

    #endregion

    public StandardAddInServer()
    {
      //Keep empty
    }

    #region ApplicationAddInServer Members

    public void Activate(ApplicationAddInSite addInSiteObject, bool firstTime)
    {
      try
      {
        //the Activate method is called by Inventor when it loads the addin
        //the AddInSiteObject provides access to the Inventor Application 
        //object the FirstTime flag indicates if the addin is loaded for the
        //first time

        //initialize AddIn members
        m_invApp = addInSiteObject.Application;
        m_Importer = new McMasterImporter(this);
        
        if (Properties.Settings.Default.projectFolder == "")
        {
          Properties.Settings.Default.projectFolder = 
            m_invApp.DesignProjectManager.ActiveDesignProject
            .WorkspacePath + "\\MCMASTER_REPOSITORY\\";
          Properties.Settings.Default.Save();
        }
        //initialize event delegates
        m_UIEvents = m_invApp.UserInterfaceManager.UserInterfaceEvents;

        UIESink_OnResetRibbonInterfaceEventDelegate = new
    UserInterfaceEventsSink_OnResetRibbonInterfaceEventHandler(
            UIE_OnResetRibbonInterface);
        m_UIEvents.OnResetRibbonInterface +=
          UIESink_OnResetRibbonInterfaceEventDelegate;

        m_Button = new McMasterButton(this);

        if (firstTime == true)
        {
          //access user interface manager
          UserInterfaceManager UIManager = m_invApp.UserInterfaceManager;

          //create the UI for classic interface
          if (UIManager.InterfaceStyle == InterfaceStyleEnum.kClassicInterface)
          {
            //For first iterations assume RibbonInterface   
          }
          //create the UI for ribbon interface
          else if (UIManager.InterfaceStyle ==
            InterfaceStyleEnum.kRibbonInterface)
          {
            CreateOrUpdateRibbonUserInterface();
          }
        }

        InitializeCEF();
      }
      catch (Exception e)
      {
        MessageBox.Show(e.ToString());
      }
    }

    public void Deactivate()
    {
      //Need to call on main thread
      if (wasOff){
        Cef.Shutdown();
      }
    }

    public void ExecuteCommand(int commandID)
    {
      // Note:this method is now obsolete, you should use the 
      // ControlDefinition functionality for implementing commands.
    }

    public object Automation
    {
      // This property is provided to allow the AddIn to expose an API 
      // of its own to other programs. Typically, this  would be done by
      // implementing the AddIn's API interface in a class and returning 
      // that class object through this property.

      get
      {
        // TODO: Add ApplicationAddInServer.Automation getter implementation
        return null;
      }
    }

    #endregion

    #region Event Handlers

    private void CreateOrUpdateRibbonUserInterface()
    {
      m_Button.AddToUI();
    }

    private void UIE_OnResetRibbonInterface(NameValueMap context)
    {
      CreateOrUpdateRibbonUserInterface();
    }

    #endregion

    public void PreLoadStepFile(string pNumber, int open)
    {
      if (!fileList.ContainsKey(pNumber))
      {
        fileList.Add(pNumber, "initialized");

        string savingDirectory = Properties.Settings.Default.projectFolder;
        foreach (string s in System.IO.Directory.GetFiles(savingDirectory))
        {
          if (s.Substring(savingDirectory.Length)
            .Contains(pNumber))
          {
            if (s.Contains(".ipt"))
            {
              fileList[pNumber] = "exists:" + s;
              return;
            }
          }
        }        
        
        //Load Offscreen browser to partNumber webpage, to extract file locations
        int tries = 0;
        string fileName = "";
      retry:
        if (tries < 3)
        {
          string url = urlBase + pNumber;
          _headlessBrowser.OpenUrl(url);
          var tS = _headlessBrowser.Page.EvaluateScriptAsync(@"var a = 'empty';for (let i of document.getElementsByClassName('li--cad')){if (i.dataset.mcmCadOption.includes('STEP')){a = i.dataset.mcmCadOption;}} a;");
          tS.Wait();
          JavascriptResponse response = tS.Result;
          var result = response.Success ? (response.Result ?? "null") : response.Message;
          fileName = (string)result;
          if (!fileName.Contains(pNumber))
          {            
            tries++;
            System.Threading.Thread.Sleep(500);
            goto retry;
          }
          else
          {
            fileList[pNumber] = urlBase + fileName.Substring(1);
            System.Diagnostics.Debug.WriteLine("url: " + fileName);
            fileName = ReverseString(fileName);
            fileName = ReverseString(fileName.Substring(0, fileName.IndexOf('/')));
            System.Diagnostics.Debug.WriteLine("good to go: " + fileName);
            if (Directory.Exists(savingDirectory))
            {
              using (var client = new System.Net.WebClient())
              {                
                string filePath = System.IO.Path.Combine(savingDirectory, fileName);//Saving Directory for .step temp file
                System.Diagnostics.Debug.WriteLine(filePath);
                System.Net.ServicePointManager.SecurityProtocol =
                  System.Net.SecurityProtocolType.Tls |
                  System.Net.SecurityProtocolType.Tls11 |
                  System.Net.SecurityProtocolType.Tls12;
                fileList[pNumber] = "beginDownload:" + fileList[pNumber];
                client.DownloadFile(new System.Uri(fileList[pNumber]
                  .Substring("beginDownload:".Length)), filePath);
                fileList[pNumber] = "isDownloaded:" + fileName.Length.ToString("X4") + filePath;
                fileList[pNumber] = "exists:" + m_Importer.Translate(fileList[pNumber]
                  .Substring("isDownloaded:".Length));
                if (open != 0)
                {
                  bool isAssembly = true;
                  if (open == 2)
                  {
                    isAssembly = false;
                  }
                  m_Importer.Open(fileList[pNumber]
                    .Substring("exists:".Length), isAssembly);
                }
              }
            }
          }
        }
        else
        {
          fileList.Remove(pNumber);
          MessageBox.Show("Couldn't retrieve " + pNumber);          
        }
      }
    }

    public void GetSource(string pNumber, bool isAssembly)
    {
      if (fileList.ContainsKey(pNumber))
      {
        int tries = 0;
      retry:
        if (tries < 20)
        {
          if (fileList[pNumber].Contains("exists"))
          {
            m_Importer.Open(fileList[pNumber]
              .Substring("exists:".Length), isAssembly);
          }

        }
        else {
          if (fileList[pNumber].Contains("isDownloaded"))
          {
            tries = 0;
          }                     
              tries++;
              System.Threading.Thread.Sleep(500);
              goto retry;           
        }
      }
      else
      {
        int open = 2;
        if (isAssembly)
        {
          open = 1;
        }
        PreLoadStepFile(pNumber, open);
      }
    }

    /// <summary>
    /// A method to reverse a string type by 
    /// converting to charArray and using char[].Reverse method
    /// </summary>
    /// <param name="s">The string that you want to reverse the order of</param>
    /// <returns>A string with reversed character order</returns>
    public static string ReverseString(string s)
    {
      char[] arr = s.ToCharArray();
      Array.Reverse(arr);
      return new string(arr);
    }

    private void InitializeCEF()
    {
      //Keep CEF on until INVENTOR exits, not just the WPF form.
      if (!Cef.IsInitialized) {
        CefSharpSettings.ShutdownOnExit = false;

        var settings = new CefSettings { RemoteDebuggingPort = 8088 };
     //   Example of setting a command line argument
   //     Enables WebRTC
        settings.CefCommandLineArgs.Add("enable-media-stream", "1");
        //Must call once on main thread, and shutdown on main thread.
        Cef.Initialize(settings);
        wasOff = true;
      }
      CefSharpSettings.LegacyJavascriptBindingEnabled = true;
      _headlessBrowser = new HeadlessWebBrowser();
    }
    public void CleanupTempFiles()
    {
      try
      {
        foreach (System.Collections.Generic.KeyValuePair<string, string> s in fileList)
        {
          if (s.Value.Contains("isDownloaded"))
          {
            if (System.IO.File.Exists(
              s.Value.Substring("isDownloaded:XXXX".Length)))
            {
              System.IO.File.Delete(
                s.Value.Substring("isDownloaded:XXXX".Length));
            }
          }
        }
      }
      catch { }
    }
  }
  
  public sealed class PictureDispConverter

  {
    [DllImport("OleAut32.dll",
        EntryPoint = "OleCreatePictureIndirect",
        ExactSpelling = true,
        PreserveSig = false)]

    private static extern stdole.IPictureDisp
        OleCreatePictureIndirect(
            [MarshalAs(UnmanagedType.AsAny)] object picdesc,
            ref Guid iid,
            [MarshalAs(UnmanagedType.Bool)] bool fOwn);

    static Guid iPictureDispGuid = typeof(stdole.IPictureDisp).GUID;

    private static class PICTDESC
    {
      //Picture Types
      public const short PICTYPE_UNINITIALIZED = -1;
      public const short PICTYPE_NONE = 0;
      public const short PICTYPE_BITMAP = 1;
      public const short PICTYPE_METAFILE = 2;
      public const short PICTYPE_ICON = 3;
      public const short PICTYPE_ENHMETAFILE = 4;

      [StructLayout(LayoutKind.Sequential)]
      public class Icon
      {
        internal int cbSizeOfStruct =
            Marshal.SizeOf(typeof(PICTDESC.Icon));
        internal int picType = PICTDESC.PICTYPE_ICON;
        internal IntPtr hicon = IntPtr.Zero;
        internal int unused1;
        internal int unused2;

        internal Icon(System.Drawing.Icon icon)
        {
          this.hicon = icon.ToBitmap().GetHicon();
        }
      }

      [StructLayout(LayoutKind.Sequential)]
      public class Bitmap
      {
        internal int cbSizeOfStruct =
            Marshal.SizeOf(typeof(PICTDESC.Bitmap));

        internal int picType = PICTDESC.PICTYPE_BITMAP;
        internal IntPtr hbitmap = IntPtr.Zero;
        internal IntPtr hpal = IntPtr.Zero;
        internal int unused;

        internal Bitmap(System.Drawing.Bitmap bitmap)
        {
          this.hbitmap = bitmap.GetHbitmap();
        }
      }
    }

    public static stdole.IPictureDisp ToIPictureDisp(
        System.Drawing.Icon icon)
    {
      PICTDESC.Icon pictIcon = new PICTDESC.Icon(icon);

      return OleCreatePictureIndirect(
          pictIcon, ref iPictureDispGuid, true);
    }

    public static stdole.IPictureDisp ToIPictureDisp(
        System.Drawing.Bitmap bmp)
    {

      PICTDESC.Bitmap pictBmp = new PICTDESC.Bitmap(bmp);

      return OleCreatePictureIndirect(pictBmp, ref iPictureDispGuid, true);
    }
  }
}