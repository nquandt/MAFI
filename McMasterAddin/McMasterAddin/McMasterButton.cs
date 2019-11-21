using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inventor;
using System.Drawing;
using System;

namespace McMasterAddin
{
  class McMasterButton
  {
    private StandardAddInServer _stAddIn;

    public ButtonDefinition m_buttonDefinition;

    private MainWindowViewModel mv;

    private ButtonDefinitionSink_OnExecuteEventHandler
      m_button_Definition_OnExecute_Delegate;

    public McMasterButton(StandardAddInServer s)
    {
      _stAddIn = s;
       mv = new MainWindowViewModel();
      Stream myStream = System.Reflection.Assembly.
        GetExecutingAssembly().GetManifestResourceStream(
        "McMasterAddin.Resources.mcmaster.ico");

      stdole.IPictureDisp largeImage =
        PictureDispConverter.ToIPictureDisp(new Icon(myStream));

      //Button definition
      m_buttonDefinition = _stAddIn.m_invApp.CommandManager.
        ControlDefinitions.AddButtonDefinition("Browse",
        "BrowseButton",
        CommandTypesEnum.kQueryOnlyCmdType, StandardAddInServer.m_ClientIDstr,
        "Browse McMaster-Carr Inventory", "Use this to find " +
        "hardware and other products available on McMaster.com",
        largeImage, largeImage, ButtonDisplayEnum.kAlwaysDisplayText);

      m_button_Definition_OnExecute_Delegate = new 
        ButtonDefinitionSink_OnExecuteEventHandler(
        m_button_OnExecute);
      m_buttonDefinition.OnExecute += 
        m_button_Definition_OnExecute_Delegate;
      m_buttonDefinition.Enabled = true;
    }

    public void AddToUI()
    {

      UserInterfaceManager UIManager = 
        _stAddIn.m_invApp.UserInterfaceManager;

      Ribbon assemblyRibbon = UIManager.Ribbons["Assembly"];
      RibbonTab assembleTab = assemblyRibbon.RibbonTabs["id_TabAssemble"];
      bool exists = false;
      foreach (RibbonPanel r in assembleTab.RibbonPanels)
      {
        if (r.InternalName == "McMasterPanelAssembly")
        {
          exists = true;
        }
      }
      if (!exists)
      {
        RibbonPanel mcMasterPanel =
          assembleTab.RibbonPanels.Add("McMaster-Carr",
            "McMasterPanelAssembly", StandardAddInServer.m_ClientIDstr);

        mcMasterPanel.CommandControls
          .AddButton(m_buttonDefinition, true);
      }
      Ribbon partRibbon = UIManager.Ribbons["Part"];

      RibbonTab partTab = partRibbon.RibbonTabs["id_TabManage"];
      exists = false;
      foreach (RibbonPanel r in partTab.RibbonPanels)
      {
        if (r.InternalName == "McMasterPanelPart")
        {
          exists = true;
        }
      }
      if (!exists)
      {
        RibbonPanel mcMasterPanel =
          partTab.RibbonPanels.Add("McMaster-Carr",
            "McMasterPanelPart", StandardAddInServer.m_ClientIDstr);

        mcMasterPanel.CommandControls
          .AddButton(m_buttonDefinition, true);
      }

    }

    /// <summary>
    /// This is the mcMaster-addin button execution
    /// </summary>
    /// <param name="Context"></param>
    private void m_button_OnExecute(NameValueMap Context)
    {
      //Refer to MainWindow.xaml for code of the browser extension
      var wpfWindow = new McMasterAddin.MainWindow(_stAddIn);
      //This allows for a WPF control to be displayed without
      //the need of a fullfledge WPF Application.
      
      var helper = new System.Windows
        .Interop.WindowInteropHelper(wpfWindow)
      {
        Owner = new IntPtr(_stAddIn.m_invApp.MainFrameHWND)
      };
      wpfWindow.DataContext = mv;
      wpfWindow.ShowDialog();
    }
  }
}
