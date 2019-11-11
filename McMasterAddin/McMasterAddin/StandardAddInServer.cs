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
    public System.Collections.Generic.List<string> fileList = new System.Collections.Generic.List<string>();


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

    public void GetSource(string url, bool isAssembly)
    {
      //Load Offscreen browser to partNumber webpage, to extract file locations
      _headlessBrowser.OpenUrl(url);
      System.Threading.Thread.Sleep(1000);
      string source = "";
      var tS = _headlessBrowser.Page.EvaluateScriptAsync(
        @"document.getElementsByTagName('html')[0].innerHTML");
      tS.Wait();
      JavascriptResponse response = tS.Result;
      var result = response.Success ? (response.Result ?? "null") : response.Message;
      source = (string)result;
      //Reverse version of REGEX match pattern, to get shortest match due to non-greddy algorithm difficulty
      string exp = @">il/" + "<PETS D-3>\\\"PETS.+?\\\"=noitpo-dac-mcm-atad \\\"dac--il\\\"=ssalc il<";
      string[] matchEnds = new string[] { ">il/<PETS D-3>\"", "/\"=noitpo-dac-mcm-atad \"dac--il\"=ssalc il<" };
      source = ReverseString(source);
      System.Text.RegularExpressions.Regex rx = new System.Text.RegularExpressions.Regex(exp);
      System.Text.RegularExpressions.MatchCollection mS = rx.Matches(source);
      string fileName = "";
      foreach (System.Text.RegularExpressions.Match x in mS)
      {
        fileName = ReverseString(x.Groups[0].Value
          .Substring(matchEnds[0].Length))
          .Substring(matchEnds[1].Length);
      }
      if (fileName.Length > 0)
      {        
        if (Directory.Exists(System.IO.Path.GetTempPath()))
        {
          using (var client = new System.Net.WebClient())
          {
            string filePath = System.IO.Path.Combine(
              System.IO.Path.GetTempPath(), url.Substring(urlBase.Length) + ".STEP");
            System.Diagnostics.Debug.WriteLine(filePath);
            System.Net.ServicePointManager.SecurityProtocol =
              System.Net.SecurityProtocolType.Tls | 
              System.Net.SecurityProtocolType.Tls11 | 
              System.Net.SecurityProtocolType.Tls12;
            client.DownloadFile(new System.Uri(
              System.IO.Path.Combine(urlBase, fileName)), filePath);
            fileList.Add(filePath);
            m_Importer.Import(filePath, url.Substring(urlBase.Length),isAssembly);               
          }
        }
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
    public void DeleteTempFiles()
    {
      try
      {
        foreach (string s in fileList)
        {
          if (System.IO.File.Exists(s))
          {
            System.IO.File.Delete(s);
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