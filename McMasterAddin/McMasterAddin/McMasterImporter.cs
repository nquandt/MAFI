using Inventor;

namespace McMasterAddin
{
  public class McMasterImporter
  {
    private StandardAddInServer _stAddIn;
    private string translatorID = "";
    private readonly static string tempDirectory = 
      System.IO.Path.GetTempPath();

    public McMasterImporter(StandardAddInServer s)
    {
      _stAddIn = s;
      GetTranslatorAddInID("Translator: STEP");
    }

    /// <summary>
    /// A method for converting a .STEP to .IPT silently and adding to assembly.
    /// </summary>
    /// <param name="strFilePath">Path location of .STEP file</param>
    /// <param name="strFileName">Name of final file without suffix or file path</param>
    public void Import(string strFilePath, string strFileName,bool isAssembly)
    {
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
      doc.SaveAs(tempDirectory + strFileName + ".ipt", false);
      _stAddIn.m_invApp.SilentOperation = false;

      //Add the converted .ipt file into my active assembly
      if (isAssembly)
      {
        doc.Close();
        //Create an operation matrix that contains information
        //about starting position of my part.
        Matrix oMatrix = _stAddIn.m_invApp
          .TransientGeometry.CreateMatrix();
        oMatrix.SetTranslation(_stAddIn.m_invApp
          .TransientGeometry.CreateVector(), true);
        ((AssemblyDocument)_stAddIn.m_invApp.ActiveDocument)
          .ComponentDefinition.Occurrences.Add(tempDirectory
          + strFileName + ".ipt", oMatrix);
      }
      else
      {
        doc.Views.Add();
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
