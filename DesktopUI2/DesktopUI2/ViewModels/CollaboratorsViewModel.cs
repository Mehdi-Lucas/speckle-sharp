﻿using Avalonia.Controls;
using Avalonia.Controls.Selection;
using DesktopUI2.Models;
using DesktopUI2.Views.Pages;
using DesktopUI2.Views.Windows.Dialogs;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Splat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;

namespace DesktopUI2.ViewModels
{
  public class CollaboratorsViewModel : ReactiveObject, IRoutableViewModel
  {
    public IScreen HostScreen { get; }
    public string UrlPathSegment { get; } = "collaborators";

    private ConnectorBindings Bindings;

    #region bindings

    public ReactiveCommand<Unit, Unit> GoBack => MainViewModel.RouterInstance.NavigateBack;

    private string _searchQuery = "";

    private Action userSearchDebouncer = null;

    public string SearchQuery
    {
      get => _searchQuery;
      set
      {
        this.RaiseAndSetIfChanged(ref _searchQuery, value);
        Search();
      }
    }
    private List<AccountViewModel> _users;
    public List<AccountViewModel> Users
    {
      get => _users;
      private set
      {
        this.RaiseAndSetIfChanged(ref _users, value);
      }
    }

    private ObservableCollection<AccountViewModel> _selectedUsers = new ObservableCollection<AccountViewModel>();
    public ObservableCollection<AccountViewModel> AddedUsers
    {
      get => _selectedUsers;
      private set
      {
        this.RaiseAndSetIfChanged(ref _selectedUsers, value);
      }
    }

    public AccountViewModel SelectedUser
    {
      set
      {
        if (value != null)
        {
          AddedUsers.Add(value);
          SearchQuery = "";
          this.RaisePropertyChanged("AddedUsers");
          this.RaisePropertyChanged("HasAddedUsers");
        }

      }
    }


    private bool _dropDownOpen;
    public bool DropDownOpen
    {
      get => _dropDownOpen;
      private set
      {
        this.RaiseAndSetIfChanged(ref _dropDownOpen, value);
      }
    }

    private bool _showProgress;
    public bool ShowProgress
    {
      get => _showProgress;
      private set
      {
        this.RaiseAndSetIfChanged(ref _showProgress, value);
      }
    }

    private string _role;
    public string Role
    {
      get => _role;
      private set
      {
        this.RaiseAndSetIfChanged(ref _role, value);
      }
    }

    public bool HasAddedUsers
    {
      get => AddedUsers.Any();
    }

    public bool HasSelectedUsers
    {
      get => SelectionModel.SelectedItems.Any();
    }

    public SelectionModel<AccountViewModel> SelectionModel { get; private set; }
    #endregion

    private StreamViewModel _stream;

    public CollaboratorsViewModel(IScreen screen, StreamViewModel stream)
    {
      HostScreen = screen;
      _stream = stream;
      Role = stream.Stream.role;
      Bindings = Locator.Current.GetService<ConnectorBindings>();

      userSearchDebouncer = Utils.Debounce(SearchUsers);

      SelectionModel = new SelectionModel<AccountViewModel>();
      SelectionModel.SingleSelect = false;
      SelectionModel.SelectionChanged += SelectionModel_SelectionChanged;

      foreach (var collab in stream.Stream.collaborators)
      {
        //skip myself
        if (_stream.StreamState.Client.Account.userInfo.id == collab.id)
          continue;
        AddedUsers.Add(new AccountViewModel(collab));
      }
    }

    private void Search()
    {

      Focus();
      if (SearchQuery.Length < 3)
        return;

      if (SearchQuery.Contains("@"))
      {
        if (Utils.IsValidEmail(SearchQuery))
        {
          var emailAcc = new AccountViewModel()
          {
            Name = SearchQuery
          };
          Users = new List<AccountViewModel> { emailAcc };

          ShowProgress = false;
          DropDownOpen = true;
        }
      }
      else
      {
        userSearchDebouncer();
      }

    }

    //focus is lost when the dropdown gets closed
    private void Focus()
    {
      DropDownOpen = false;
      var searchBox = CollaboratorsView.Instance.FindControl<TextBox>("SearchBox");
      searchBox.Focus();
    }

    private void SelectionModel_SelectionChanged(object sender, SelectionModelSelectionChangedEventArgs<AccountViewModel> e)
    {
      this.RaisePropertyChanged("HasSelectedUsers");
    }

    private async void SearchUsers()
    {
      ShowProgress = true;

      //exclude existing ones
      var users = (await _stream.StreamState.Client.UserSearch(SearchQuery)).Where(x => !AddedUsers.Any(u => u.Id == x.id));
      //exclude myself
      users = users.Where(x => _stream.StreamState.Client.Account.userInfo.id != x.id);


      Users = users.Select(x => new AccountViewModel(x)).ToList();

      ShowProgress = false;
      DropDownOpen = true;

    }

    private async void SaveCommand()
    {

      foreach (var user in AddedUsers)
      {
        //invite users by email
        if (Utils.IsValidEmail(user.Name))
        {
          try
          {
            await _stream.StreamState.Client.StreamInviteCreate(new StreamInviteCreateInput { email = user.Name, streamId = _stream.StreamState.StreamId, message = "I would like to share a model with you via Speckle!", role = user.Role });
            Analytics.TrackEvent(_stream.StreamState.Client.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Share" }, { "method", "Invite Email" } });
          }
          catch (Exception e)
          {

          }
        }
        //add new collaborators
        else if (!_stream.Stream.collaborators.Any(x => x.id == user.Id))
        {
          try
          {
            await _stream.StreamState.Client.StreamInviteCreate(new StreamInviteCreateInput { userId = user.Id, streamId = _stream.StreamState.StreamId, message = "I would like to share a model with you via Speckle!", role = "stream:" + user.Role });
            Analytics.TrackEvent(_stream.StreamState.Client.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Share" }, { "method", "Invite User" } });
          }
          catch (Exception e)
          {

          }
        }
        //update permissions
        else
        {
          try
          {
            await _stream.StreamState.Client.StreamUpdatePermission(new StreamPermissionInput { userId = user.Id, streamId = _stream.StreamState.StreamId, role = "stream:" + user.Role });
            Analytics.TrackEvent(_stream.StreamState.Client.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Share" }, { "method", "Update Permissions" } });
          }
          catch (Exception e)
          {

          }
        }
      }

      //remove collaborators
      foreach (var user in _stream.Stream.collaborators)
      {
        if (!AddedUsers.Any(x => x.Id == user.id))
        {
          try
          {
            await _stream.StreamState.Client.StreamRevokePermission(new StreamRevokePermissionInput { userId = user.id, streamId = _stream.StreamState.StreamId });
            Analytics.TrackEvent(_stream.StreamState.Client.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Share" }, { "method", "Remove User" } });
          }
          catch (Exception e)
          {

          }
        }
      }

      try
      {
        _stream.Stream = await _stream.StreamState.Client.StreamGet(_stream.StreamState.StreamId);
        _stream.StreamState.CachedStream = _stream.Stream;

      }
      catch (Exception e)
      {
      }

      MainViewModel.RouterInstance.NavigateBack.Execute();
    }

    private async void CloseCommand()
    {
      MainViewModel.RouterInstance.NavigateBack.Execute();
    }


    private void ClearSearchCommand()
    {
      SearchQuery = "";
    }

    private void RemoveSeletedUsersCommand()
    {
      foreach (var item in SelectionModel.SelectedItems.ToList())
      {
        AddedUsers.Remove(item);
      }

      this.RaisePropertyChanged("HasSelectedUsers");
      this.RaisePropertyChanged("HasAddedUsers");
    }

    private async void ChangeRoleSeletedUsersCommand()
    {
      var dialog = new ChangeRoleDialog();
      var result = await dialog.ShowDialog<string>();

      if (result != null)
      {
        foreach (var item in SelectionModel.SelectedItems.ToList())
        {
          item.Role = "stream:" + result;
        }
      }

    }
  }
}