using System;
using System.IO;
using reflect = System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using Inv = Inventor;
using Inventor;
using CefSharp.Wpf;
using CefSharp;

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
    // Inventor application object.
    private Inv.Application m_invApp;

    #region ObjectInitialization        

    //user interface event
    private UserInterfaceEvents m_UIEvents;
    private static GuidAttribute m_ClientID =
      (GuidAttribute)System.Attribute.GetCustomAttribute(
        typeof(StandardAddInServer), typeof(GuidAttribute));
    private static readonly string m_ClientIDstring =
      "{" + m_ClientID.Value + "}";

    //single button for McMaster Catalog
    private ButtonDefinition m_butDefinition;
    private ButtonDefinitionSink_OnExecuteEventHandler 
      m_butDefinition_OnExecute_Delegate;

    private UserInterfaceEventsSink_OnResetCommandBarsEventHandler
        UIESink_OnResetCommandBarsEventDelegate;
    private UserInterfaceEventsSink_OnResetEnvironmentsEventHandler
        UIESink_OnResetEnvironmentsEventDelegate;
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

        ControlDefinitions controlDefs = 
          m_invApp.CommandManager.ControlDefinitions;

        //initialize event delegates
        m_UIEvents = m_invApp.UserInterfaceManager.UserInterfaceEvents;

        UIESink_OnResetCommandBarsEventDelegate = new
          UserInterfaceEventsSink_OnResetCommandBarsEventHandler(
            UIE_OnResetCommandBars);
        m_UIEvents.OnResetCommandBars += 
          UIESink_OnResetCommandBarsEventDelegate;

        UIESink_OnResetEnvironmentsEventDelegate = new
          UserInterfaceEventsSink_OnResetEnvironmentsEventHandler(
            UIE_OnResetEnvironments);
        m_UIEvents.OnResetEnvironments +=
          UIESink_OnResetEnvironmentsEventDelegate;

        UIESink_OnResetRibbonInterfaceEventDelegate = new
          UserInterfaceEventsSink_OnResetRibbonInterfaceEventHandler(
            UIE_OnResetRibbonInterface);
        m_UIEvents.OnResetRibbonInterface +=
          UIESink_OnResetRibbonInterfaceEventDelegate;

        Stream myStream = reflect.Assembly.GetExecutingAssembly()
          .GetManifestResourceStream("McMasterAddin.Resources.mcmaster.ico");

        stdole.IPictureDisp largeImage = 
          PictureDispConverter.ToIPictureDisp(new Icon(myStream));

        //Button definition
        m_butDefinition = controlDefs.AddButtonDefinition("Browse",
          "BrowseButton",
          CommandTypesEnum.kQueryOnlyCmdType, m_ClientIDstring,
          "Browse McMaster-Carr Inventory", "Use this to find " +
          "hardware and other products available on McMaster.com",
          largeImage, largeImage, ButtonDisplayEnum.kAlwaysDisplayText);

        m_butDefinition_OnExecute_Delegate = 
          new ButtonDefinitionSink_OnExecuteEventHandler(m_but_OnExecute);
        m_butDefinition.OnExecute += m_butDefinition_OnExecute_Delegate;
        m_butDefinition.Enabled = true;


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

      }
      catch (Exception e)
      {
        MessageBox.Show(e.ToString());
      }

      //Keep CEF on until INVENTOR exits, not just the WPF form.
      CefSharpSettings.ShutdownOnExit = false;

      var settings = new CefSettings();

      //Example of setting a command line argument
      //Enables WebRTC
      settings.CefCommandLineArgs.Add("enable-media-stream", "1");
      //Must call once on main thread, and shutdown on main thread.
      Cef.Initialize(settings);
    }

    public void Deactivate()
    {
      //Need to call on main thread
      Cef.Shutdown();
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

    private void CreateOrUpdateRibbonUserInterface()
    {
      UserInterfaceManager UIManager = m_invApp.UserInterfaceManager;
      Ribbon assemblyRibbon = UIManager.Ribbons["Assembly"];
      RibbonTab assembleTab = assemblyRibbon.RibbonTabs["id_TabAssemble"];
      RibbonPanel mcMasterPanel = 
        assembleTab.RibbonPanels.Add("McMaster Carr", 
          "McMasterPanel", m_ClientIDstring);

      mcMasterPanel.CommandControls.AddButton(m_butDefinition, true);
    }

    private void UIE_OnResetCommandBars(
      ObjectsEnumerator commandBars, NameValueMap context)
    {
      UserInterfaceManager UIManager = m_invApp.UserInterfaceManager;
      Inv.Environment asmEnvironment = UIManager.Environments["AMxAssemblyEnvironment"];

      foreach (CommandBar commandBar in commandBars)
      {
        if (commandBar == asmEnvironment.PanelBar.DefaultCommandBar)
        {

        }
      }
    }

    private void UIE_OnResetEnvironments(
      ObjectsEnumerator environments, NameValueMap context)
    {
      try
      {
        Inv.Environment environment;
        for (int environmentCt = 1; 
          environmentCt <= environments.Count; environmentCt++)
        {
          environment = (Inv.Environment)environments[environmentCt];
          if (environment.InternalName == "PMxPartSketchEnvironment")
          {
            //make this command bar accessible in the 
            //panel menu for the 2d sketch environment.
            environment.PanelBar.CommandBarList.Add(
              m_invApp.UserInterfaceManager.CommandBars[
                "Autodesk:SimpleAddIn:SlotToolbar"]);

            return;
          }
        }
      }
      catch (Exception e)
      {
        MessageBox.Show(e.ToString());
      }
    }

    private void UIE_OnResetRibbonInterface(NameValueMap context)
    {
      CreateOrUpdateRibbonUserInterface();
    }

    /// <summary>
    /// This is the mcMaster-addin button execution
    /// </summary>
    /// <param name="Context"></param>
    void m_but_OnExecute(NameValueMap Context)
    {
      //Refer to MainWindow.xaml for code of the browser extension
      var wpfWindow = new McMasterAddin.MainWindow();
      //This allows for a WPF control to be displayed without
      //the need of a fullfledge WPF Application.
      var helper = new System.Windows.Interop.WindowInteropHelper(wpfWindow);
      helper.Owner = new IntPtr(m_invApp.MainFrameHWND);

      //Show modal Even though button executions 
      //seem to start their own threads.
      wpfWindow.ShowDialog();
    }

    #endregion

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