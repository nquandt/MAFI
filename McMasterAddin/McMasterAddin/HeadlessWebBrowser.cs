using System.Threading;
using System;
using CefSharp;
using CefSharp.OffScreen;

namespace McMasterAddin
{
  public class HeadlessWebBrowser
  {

    /// <summary>
    /// The browser page
    /// </summary>
    public ChromiumWebBrowser Page { get; private set; }
    /// <summary>
    /// The request context
    /// </summary>
    public RequestContext RequestContext { get; private set; }

    // chromium does not manage timeouts, so we'll implement one
    private ManualResetEvent manualResetEvent = new ManualResetEvent(false);

    public HeadlessWebBrowser()
    {
      RequestContext = new RequestContext();
      Page = new ChromiumWebBrowser("", null, RequestContext);
      PageInitialize();
    }

    /// <summary>
    /// Open the given url
    /// </summary>
    /// <param name="url">the url</param>
    /// <returns></returns>
    public void OpenUrl(string url)
    {
      try
      {
        Page.LoadingStateChanged += PageLoadingStateChanged;
        if (Page.IsBrowserInitialized)
        {
          Page.Load(url);

          //create a 60 sec timeout 
          bool isSignalled = manualResetEvent.WaitOne(TimeSpan.FromSeconds(60));
          manualResetEvent.Reset();

          //As the request may actually get an answer, we'll force stop when the timeout is passed
          if (!isSignalled)
          {
            Page.Stop();
          }
        }
      }
      catch (ObjectDisposedException)
      {
        //happens on the manualResetEvent.Reset(); when a cancelation token has disposed the context
      }
      Page.LoadingStateChanged -= PageLoadingStateChanged;
    }

    /// <summary>
    /// Manage the IsLoading parameter
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PageLoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
    {
      // Check to see if loading is complete - this event is called twice, one when loading starts
      // second time when it's finished
      if (!e.IsLoading)
      {
        manualResetEvent.Set();
      }
    }

    /// <summary>
    /// Wait until page initialization
    /// </summary>
    private void PageInitialize()
    {
      SpinWait.SpinUntil(() => Page.IsBrowserInitialized);
    }
  }
}
