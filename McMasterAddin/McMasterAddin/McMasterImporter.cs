using Inventor;

namespace McMasterAddin
{
  public class McMasterImporter
  {
    private StandardAddInServer _stAddIn;
    private string translatorID = "";

    public McMasterImporter(StandardAddInServer s)
    {
      _stAddIn = s;
      GetTranslatorAddInID("Translator: STEP");
    }

    public string Translate(string substitutePathVal)
    {
      string strFilePath = substitutePathVal.Substring(4);
      string strFileName = strFilePath.Substring(strFilePath.Length - int.Parse(substitutePathVal.Substring(0, 4), System.Globalization.NumberStyles.HexNumber));
      strFileName = strFileName.Substring(0, strFileName.Length - 5);
      System.Diagnostics.Debug.WriteLine(strFileName + "///" + strFilePath);
      ApplicationAddIns oAddIns = _stAddIn.m_invApp.ApplicationAddIns;
      TranslatorAddIn oTransAddIn = (TranslatorAddIn)oAddIns.ItemById[translatorID];
      oTransAddIn.Activate();

      TransientObjects transObj = _stAddIn.m_invApp.TransientObjects;

      DataMedium file = transObj.CreateDataMedium();
      file.FileName = strFilePath;

      TranslationContext context = transObj.CreateTranslationContext();
      context.Type = IOMechanismEnum.kFileBrowseIOMechanism;

      NameValueMap options = transObj.CreateNameValueMap();

      bool oHasOpt = oTransAddIn.HasOpenOptions[file, context, options];

      oTransAddIn.Open(file, context, options, out object oDoc);

      Document doc = (Document)oDoc;
      _stAddIn.m_invApp.SilentOperation = true;
      string savingDirectory = Properties.Settings.Default.projectFolder;
      if (savingDirectory == "")
      {
        savingDirectory = _stAddIn.m_invApp.DesignProjectManager.ActiveDesignProject.WorkspacePath + @"\MCMASTER_REPOSITORY\";
      }
      doc.SaveAs(savingDirectory + strFileName + ".ipt", false);      
      if (System.IO.File.Exists(strFilePath))
      {
        System.IO.File.Delete(strFilePath);
      }
      _stAddIn.m_invApp.SilentOperation = false;
      doc.Close();
      return savingDirectory + strFileName + ".ipt";
    }

    public void Open(string filePath, bool isAssembly)
    {
      //Add the converted .ipt file into my active assembly
      if (isAssembly)
      {        
        //Create an operation matrix that contains information
        //about starting position of my part.
        Matrix oMatrix = _stAddIn.m_invApp
          .TransientGeometry.CreateMatrix();
        oMatrix.SetTranslation(_stAddIn.m_invApp
          .TransientGeometry.CreateVector(), true);
        ((AssemblyDocument)_stAddIn.m_invApp.ActiveDocument)
          .ComponentDefinition.Occurrences.Add(filePath, oMatrix);
      }
      else
      {
        Document doc = _stAddIn.m_invApp.Documents.Open(filePath);
      }
    }

    private void GetTranslatorAddInID(string translatorName)
    {
      ApplicationAddIns oAddIns =
        _stAddIn.m_invApp.ApplicationAddIns;
      TranslatorAddIn oTransAddIn;

      foreach (ApplicationAddIn a in oAddIns)
      {
        if (a.AddInType == 
          ApplicationAddInTypeEnum.kTranslationApplicationAddIn)
        {
          oTransAddIn = (TranslatorAddIn)a;
          if (oTransAddIn.DisplayName == translatorName)
          {
            translatorID = oTransAddIn.ClassIdString;
          }
        }
      }
    }
  }
}
