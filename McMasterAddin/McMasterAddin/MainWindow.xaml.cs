using System.Threading;
using System.Windows;
using System.Windows.Input;
using System;
using CefSharp;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;

namespace McMasterAddin
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  /// 
  public partial class MainWindow : Window
  {
    private StandardAddInServer _stAddIn;

    public StandardAddInServer StandardAddInServer
    {
      get { return _stAddIn; }
      set { _stAddIn = value; }
    }

    public MainWindow(StandardAddInServer a)
    {
      InitializeComponent();
      _stAddIn = a;
    }

    private void Window_Closing(object sender,
      System.ComponentModel.CancelEventArgs e)
    {
      _stAddIn.DeleteTempFiles();
      ((MainWindowViewModel)DataContext).OnGo1Screen(null);
    }
  }

  public class RelayCommand<T> : ICommand
  {
    private readonly Predicate<T> _canExecute;
    private readonly Action<T> _execute;

    public RelayCommand(Action<T> execute)
       : this(execute, null)
    {
      _execute = execute;
    }

    public RelayCommand(Action<T> execute, Predicate<T> canExecute)
    {
      if (execute == null)
      {
        throw new ArgumentNullException("execute");
      }
      _execute = execute;
      _canExecute = canExecute;
    }

    public bool CanExecute(object parameter)
    {
      return _canExecute == null || _canExecute((T)parameter);
    }

    public void Execute(object parameter)
    {
      _execute((T)parameter);
    }

    public event EventHandler CanExecuteChanged
    {
      add { CommandManager.RequerySuggested += value; }
      remove { CommandManager.RequerySuggested -= value; }
    }
  }

  public class RelayCommand : ICommand
  {
    private readonly Predicate<object> _canExecute;
    private readonly Action<object> _execute;

    public RelayCommand(Action<object> execute)
       : this(execute, null)
    {
      _execute = execute;
    }

    public RelayCommand(Action<object> execute, Predicate<object> canExecute)
    {
      if (execute == null)
      {
        throw new ArgumentNullException("execute");
      }
      _execute = execute;
      _canExecute = canExecute;
    }

    public bool CanExecute(object parameter)
    {
      return _canExecute == null || _canExecute(parameter);
    }

    public void Execute(object parameter)
    {
      _execute(parameter);
    }

    // Ensures WPF commanding infrastructure asks all RelayCommand objects whether their
    // associated views should be enabled whenever a command is invoked 
    public event EventHandler CanExecuteChanged
    {
      add
      {
        CommandManager.RequerySuggested += value;
        CanExecuteChangedInternal += value;
      }
      remove
      {
        CommandManager.RequerySuggested -= value;
        CanExecuteChangedInternal -= value;
      }
    }

    private event EventHandler CanExecuteChangedInternal;

    public void RaiseCanExecuteChanged()
    {
      CanExecuteChangedInternal.Raise(this);
    }
  }

  public static class EventRaiser
  {
    public static void Raise(this EventHandler handler, object sender)
    {
      handler?.Invoke(sender, EventArgs.Empty);
    }

    public static void Raise<T>(this EventHandler<EventArgs<T>> handler, object sender, T value)
    {
      handler?.Invoke(sender, new EventArgs<T>(value));
    }

    public static void Raise<T>(this EventHandler<T> handler, object sender, T value) where T : EventArgs
    {
      handler?.Invoke(sender, value);
    }

    public static void Raise<T>(this EventHandler<EventArgs<T>> handler, object sender, EventArgs<T> value)
    {
      handler?.Invoke(sender, value);
    }
  }
  public class EventArgs<T> : EventArgs
  {
    public EventArgs(T value)
    {
      Value = value;
    }

    public T Value { get; private set; }
  }

  public abstract class BaseViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
      VerifyPropertyName(propertyName);
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    [Conditional("DEBUG")]
    private void VerifyPropertyName(string propertyName)
    {
      if (TypeDescriptor.GetProperties(this)[propertyName] == null)
        throw new ArgumentNullException(GetType().Name + " does not contain property: " + propertyName);
    }
  }
  public static class Mediator
  {
    private static IDictionary<string, List<Action<object>>> pl_dict =
       new Dictionary<string, List<Action<object>>>();

    public static void Subscribe(string token, Action<object> callback)
    {
      if (!pl_dict.ContainsKey(token))
      {
        var list = new List<Action<object>>();
        list.Add(callback);
        pl_dict.Add(token, list);
      }
      else
      {
        bool found = false;
        foreach (var item in pl_dict[token])
          if (item.Method.ToString() == callback.Method.ToString())
            found = true;
        if (!found)
          pl_dict[token].Add(callback);
      }
    }

    public static void Unsubscribe(string token, Action<object> callback)
    {
      if (pl_dict.ContainsKey(token))
        pl_dict[token].Remove(callback);
    }

    public static void Notify(string token, object args = null)
    {
      if (pl_dict.ContainsKey(token))
        foreach (var callback in pl_dict[token])
          callback(args);
    }
  }
}
