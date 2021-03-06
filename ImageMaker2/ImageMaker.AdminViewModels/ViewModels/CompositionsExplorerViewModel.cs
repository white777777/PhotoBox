﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using GalaSoft.MvvmLight.CommandWpf;
using ImageMaker.AdminViewModels.Providers;
using ImageMaker.AdminViewModels.ViewModels.Enums;
using ImageMaker.AdminViewModels.ViewModels.Images;
using ImageMaker.Common.Extensions;
using ImageMaker.CommonViewModels.Services;
using ImageMaker.CommonViewModels.ViewModels;
using ImageMaker.CommonViewModels.ViewModels.Navigation;
using TemplateViewModel = ImageMaker.AdminViewModels.ViewModels.Images.TemplateViewModel;

namespace ImageMaker.AdminViewModels.ViewModels
{
    public class CompositionsExplorerViewModel : BaseViewModel
    {
        private readonly IDialogService _dialogService;
        private readonly IViewModelNavigator _navigator;
        private readonly TemplateProviderFactory _providerFactory;
        private RelayCommand _addComposition;
        private RelayCommand _removeCompositionsCommand;
        private RelayCommand _saveCommand;
        private RelayCommand _updateCompositionCommand;
        private TemplateViewModel _selectedTemplate;
        private CheckableCompositionViewModel _selectedComposition;

        private bool _dataLoaded;

        public CompositionsExplorerViewModel(
            IDialogService dialogService,
            IViewModelNavigator navigator, 
            TemplateProviderFactory providerFactory)
        {
            _dialogService = dialogService;
            _navigator = navigator;
            _providerFactory = providerFactory;
        }

        public ICollectionView TemplatesView
            => _templatesView ?? (_templatesView = CollectionViewSource.GetDefaultView(Templates));

        public ICollectionView CompositionsView
        {
            get
            {
                if (_compositionsView != null)
                    return _compositionsView;

                _compositionsView = CollectionViewSource.GetDefaultView(Compositions);
                _compositionsView.Filter += Filter;
                return _compositionsView;
            }
        }

        public ObservableCollection<TemplateViewModel> Templates
            => _templates ?? (_templates = new ObservableCollection<TemplateViewModel>());

        public ObservableCollection<CheckableCompositionViewModel> Compositions
            => _compositions ?? (_compositions = new ObservableCollection<CheckableCompositionViewModel>());

        public TemplateViewModel SelectedTemplate
        {
            get { return _selectedTemplate; }
            set
            {
                if (_selectedTemplate == value)
                    return;

                _selectedTemplate = value;
                RaisePropertyChanged();
                UpdateCommands();
            }
        }

        public CheckableCompositionViewModel SelectedComposition
        {
            get { return _selectedComposition; }
            set
            {
                if (_selectedComposition == value)
                    return;

                _selectedComposition = value;
                RaisePropertyChanged();
                UpdateCommands();
            }
        }

        public RelayCommand AddCompositionCommand
        {
            get { return _addComposition ?? (_addComposition = new RelayCommand(AddComposition, () => SelectedTemplate != null)); }
        }

        public RelayCommand RemoveCompositionsCommand
        {
            get { return _removeCompositionsCommand ?? (_removeCompositionsCommand = new RelayCommand(RemoveCompositions, () => _canRemove)); }
        }

        public RelayCommand SaveCommand
        {
            get { return _saveCommand ?? (_saveCommand = new RelayCommand(Save, () => _canSave)); }
        }

        public RelayCommand UpdateCompositionCommand
        {
            get { return _updateCompositionCommand ?? (_updateCompositionCommand = new RelayCommand(UpdateCompositions, () => SelectedComposition != null)); }
        }

        public RelayCommand CheckCommand => _checkCommand ?? (_checkCommand = new RelayCommand(CheckItem));

        public RelayCommand GoBackCommand => _goBackCommand ?? (_goBackCommand = new RelayCommand(GoBack));

        //public RelayCommand<FilterEventArgs> FilterCommand
        //{
        //    get { return _filterCommand ?? (_filterCommand = new RelayCommand<FilterEventArgs>(Filter)); }
        //}

        private bool Filter(object item)
        {
            var composition = (CheckableCompositionViewModel) item;
            return composition.State != ItemState.Removed;
        }

        private void GoBack()
        {
            _navigator.NavigateBack(this);
        }

        private bool _canRemove;
        private bool _canSave;
        private RelayCommand _checkCommand;
        private CheckableCompositionViewModel _updatedComposition;
        private RelayCommand _goBackCommand;
        private RelayCommand<FilterEventArgs> _filterCommand;
        private ObservableCollection<TemplateViewModel> _templates;
        private ObservableCollection<CheckableCompositionViewModel> _compositions;
        private bool _isBusyLoading;
        private ICollectionView _templatesView;
        private ICollectionView _compositionsView;

        public override void Initialize()
        {
            if (!_dataLoaded)
            {
                IsBusyLoading = true;

                var compositionsTask = Task.Factory.StartNew(() => _providerFactory.CompositionsProvider.GetCompositionsAsync().Result)
                .ContinueWith(t => t.Result.Select(x => x.ToCheckable()).CopyTo(Compositions),
                TaskScheduler.FromCurrentSynchronizationContext());

                var templatesTask = Task.Factory.StartNew(() => _providerFactory.TemplateProvider.GetTemplatesAsync().Result)
                .ContinueWith(t =>
                {
                    t.Result.CopyTo(Templates);
                },
                TaskScheduler.FromCurrentSynchronizationContext());

                Task.WhenAll(compositionsTask, templatesTask)
                .ContinueWith(result =>
                    {
                        IsBusyLoading = false;
                        _dataLoaded = true;
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.None,
                    TaskScheduler.FromCurrentSynchronizationContext());
            }

            if (_updatedComposition == null || _updatedComposition.State == ItemState.Unchanged)
            {
                return;
            }

            if (!Compositions.Contains(_updatedComposition))
                Compositions.Add(_updatedComposition);

            CheckItem();
            _updatedComposition = null;
        }

        private void CheckItem()
        {
            _canRemove = Compositions.Any(x => x.IsChecked);
            _canSave = Compositions.Any(x => x.State != ItemState.Unchanged);

            UpdateCommands();
        }

        private void UpdateCommands()
        {
            AddCompositionCommand.RaiseCanExecuteChanged();
            RemoveCompositionsCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
            UpdateCompositionCommand.RaiseCanExecuteChanged();
        }

        private void UpdateCompositions()
        {
            _updatedComposition = SelectedComposition;
            _navigator.NavigateForward<CompositionsEditorViewModel>(this, _updatedComposition);
        }

        private void Save()
        {
            var removed = Compositions.Where(x => x.State == ItemState.Removed).ToList();
            removed.ForEach(x => Compositions.Remove(x));

            var added = Compositions.Where(x => x.State == ItemState.Added).ToList();
            added.ForEach(x => x.State = ItemState.Unchanged);

            var updated = Compositions.Where(x => x.State == ItemState.Updated).ToList();
            updated.ForEach(x => x.State = ItemState.Unchanged);

            _providerFactory.CompositionsProvider.RemoveCompositions(removed);
            _providerFactory.CompositionsProvider.AddCompositions(added);
            _providerFactory.CompositionsProvider.UpdateCompositions(updated);

            _providerFactory.CompositionsProvider.SaveChanges();

            CheckItem();
        }

        private void RemoveCompositions()
        {
            foreach (var child in Compositions.Where(x => x.IsChecked).ToList())
            {
                child.IsChecked = false;
                if (child.State == ItemState.Added)
                    Compositions.Remove(child);
                else
                {
                    child.State = ItemState.Removed;
                }
            }

            CompositionsView.Refresh();
            CheckItem();
        }

        private void AddComposition()
        {
            var viewModel = new ObjectNameViewModel("Введите имя композиции");
            
            bool result = _dialogService.ShowResultDialog(viewModel);
            if (!result)
                return;

            string name = viewModel.Name;

            _updatedComposition = CheckableCompositionViewModel.CreateEmpty(name, SelectedTemplate);

            _navigator.NavigateForward<CompositionsEditorViewModel>(this, _updatedComposition);
        }

        public bool IsBusyLoading
        {
            get { return _isBusyLoading; }
            set
            {
                if (_isBusyLoading == value)
                    return;

                _isBusyLoading = value;
                RaisePropertyChanged();
            }
        }
    }
}
