using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;

namespace McMasterAddin
{
  public interface IPageViewModel
  {
  }

  public class MainWindowViewModel : BaseViewModel
  {
    private IPageViewModel _currentPageViewModel;
    private List<IPageViewModel> _pageViewModels;

    public List<IPageViewModel> PageViewModels
    {
      get
      {
        if (_pageViewModels == null)
          _pageViewModels = new List<IPageViewModel>();

        return _pageViewModels;
      }
    }

    public IPageViewModel CurrentPageViewModel
    {
      get
      {
        return _currentPageViewModel;
      }
      set
      {
        _currentPageViewModel = value;
        OnPropertyChanged("CurrentPageViewModel");
      }
    }

    private void ChangeViewModel(IPageViewModel viewModel)
    {
      if (!PageViewModels.Contains(viewModel))
        PageViewModels.Add(viewModel);

      CurrentPageViewModel = PageViewModels
          .FirstOrDefault(vm => vm == viewModel);
    }

    public void OnGo1Screen(object obj)
    {
      ChangeViewModel(PageViewModels[0]);
    }

    public void OnGo2Screen(object obj)
    {
      ChangeViewModel(PageViewModels[1]);
    }

    public MainWindowViewModel()
    {
      // Add available pages and set page
      PageViewModels.Add(new UserControl1ViewModel());
      PageViewModels.Add(new UserControl2ViewModel());

      CurrentPageViewModel = PageViewModels[0];

      Mediator.Subscribe("GoTo1Screen", OnGo1Screen);
      Mediator.Subscribe("GoTo2Screen", OnGo2Screen);
    }
  }
}