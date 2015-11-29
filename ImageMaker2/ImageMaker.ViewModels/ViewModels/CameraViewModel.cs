﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.CommandWpf;
using ImageMaker.CommonViewModels.Providers;
using ImageMaker.CommonViewModels.Services;
using ImageMaker.CommonViewModels.ViewModels;
using ImageMaker.CommonViewModels.ViewModels.Navigation;
using ImageMaker.CommonViewModels.ViewModels.Settings;
using ImageMaker.PatternProcessing.Dto;
using ImageMaker.PatternProcessing.ImageProcessors;
using ImageMaker.SDKData.Enums;
using ImageMaker.SDKData.Events;

namespace ImageMaker.ViewModels.ViewModels
{
    public class CameraViewModel : BaseViewModel
    {
        private const int CDefWidth = 2048;
        private const int CDefHeight = 1536;

        private readonly SettingsProvider _settingsProvider;
        private readonly IDialogService _dialogService;
        private readonly IViewModelNavigator _navigator;
        private readonly CompositionModelProcessor _imageProcessor;
        private int _width;
        private int _height;
        private int _imageNumber;

        private RelayCommand _goBackCommand;
        private RelayCommand _openSessionCommand;
        private RelayCommand _closeSessionCommand;
        private RelayCommand _refreshCameraCommand;
        private RelayCommand _startLiveViewCommand;
        private RelayCommand _takePictureCommand;
        private RelayCommand<uint> _setFocusCommand;

        private byte[] _liveViewImageStream;

        private bool _sessionOpened;
        private bool _takingPicture;
        private bool _isLiveViewOn;
        private int _counter;

        public CameraViewModel(
            SettingsProvider settingsProvider,
            IDialogService dialogService,
            IViewModelNavigator navigator,
            CompositionModelProcessor imageProcessor
            )
        {
            _settingsProvider = settingsProvider;
            _dialogService = dialogService;
            _navigator = navigator;
            _imageProcessor = imageProcessor;

            _width = CDefWidth;
            _height = CDefHeight;
        }

        public override void Initialize()
        {
            if (!Debugger.IsAttached)
                Debugger.Launch();
            _imageProcessor.TimerElapsed += ImageProcessorOnTimerElapsed;
            _imageProcessor.CameraErrorEvent += ImageProcessorOnCameraErrorEvent;
            _imageProcessor.ImageChanged += ImageProcessorOnStreamChanged;
            _imageProcessor.ImageNumberChanged += ImageProcessorOnImageNumberChanged;


            _imageProcessor.InitializeProcessor();
            OpenSession();
            if (!_sessionOpened)
                return;

            CameraSettingsDto settings = _settingsProvider.GetCameraSettings();

            if (settings != null)
            {
                _imageProcessor.SetSetting((uint) PropertyId.AEMode, (uint) settings.SelectedAeMode);
                _imageProcessor.SetSetting((uint) PropertyId.WhiteBalance, (uint) settings.SelectedWhiteBalance);
                _imageProcessor.SetSetting((uint) PropertyId.Av, (uint) settings.SelectedAvValue);
                _imageProcessor.SetSetting((uint) PropertyId.ExposureCompensation, (uint) settings.SelectedCompensation);
                _imageProcessor.SetSetting((uint) PropertyId.ISOSpeed, (uint) settings.SelectedIsoSensitivity);
                _imageProcessor.SetSetting((uint) PropertyId.Tv, (uint) settings.SelectedShutterSpeed);
            }

            StartLiveView();
        }

        private void ImageProcessorOnImageNumberChanged(object sender, int newValue)
        {
            ImageNumber = newValue;
        }

        public override void Dispose()
        {
            _imageProcessor.TimerElapsed -= ImageProcessorOnTimerElapsed;
            _imageProcessor.CameraErrorEvent -= ImageProcessorOnCameraErrorEvent;
            _imageProcessor.ImageChanged -= ImageProcessorOnStreamChanged;
            _imageProcessor.ImageNumberChanged -= ImageProcessorOnImageNumberChanged;

            _sessionOpened = false;
            TakingPicture = false;
            _isLiveViewOn = false;
            _imageProcessor.Dispose();
        }

        private void ImageProcessorOnTimerElapsed(object sender, int tick)
        {
            Counter = tick;
        }

        private void ImageProcessorOnCameraErrorEvent(object sender, CameraEventBase cameraErrorInfo)
        {
            switch (cameraErrorInfo.EventType)
            {
                case CameraEventType.Shutdown:
                    TakingPicture = false;
                    _dialogService.ShowInfo(cameraErrorInfo.Message);
                    Dispose();
                    UpdateCommands();
                    break;
                case CameraEventType.Error:
                    TakingPicture = false;
                    SetWindowStatus(true);
                    UpdateCommands();

                    ErrorEvent ev = cameraErrorInfo as ErrorEvent;
                    if (ev != null && ev.ErrorCode == ReturnValue.TakePictureAutoFocusNG)
                    {
                        _dialogService.ShowInfo("Не удалось сфокусироваться. Пожалуйста, повторите попытку.");
                        Dispose();
                        Initialize();
                    }
                    
                    break;
            }
        }


        private void ImageProcessorOnStreamChanged(object sender, ImageDto image)
        {
            Width = image.Width;
            Height = image.Height;
            LiveViewImageStream = image.ImageData;
        }

        private void TakePicture()
        {
            if (!Debugger.IsAttached)
                Debugger.Launch();
            TakingPicture = true;
            UpdateCommands();
            //_imageProcessor.ImageChanged -= ImageProcessorOnStreamChanged;
            _imageProcessor.TakePicture(LiveViewImageStream)
                .ContinueWith(task =>
                {
                    //TakingPicture = false;
                    UpdateCommands();

                    SetWindowStatus(true);

                    _navigator.NavigateForward<CameraResultViewModel>(this, task.Result);
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void StartLiveView()
        {
            _imageProcessor.StartLiveView();
            _isLiveViewOn = true;
            UpdateCommands();
        }

        private void RefreshCamera()
        {
            _imageProcessor.RefreshCamera();
        }

        private void CloseSession()
        {
            _imageProcessor.CloseSession();
            _sessionOpened = false;
            UpdateCommands();
        }

        private void OpenSession()
        {
            if (!Debugger.IsAttached)
                Debugger.Launch();
            bool result = _imageProcessor.OpenSession();
            if (!result)
            {
                _dialogService.ShowInfo("Камера отсутствует или не готова");
                return;
            }

            _sessionOpened = true;
            UpdateCommands();
        }

        private void UpdateCommands()
        {
            OpenSessionCommand.RaiseCanExecuteChanged();
            CloseSessionCommand.RaiseCanExecuteChanged();
            TakePictureCommand.RaiseCanExecuteChanged();
            StartLiveViewCommand.RaiseCanExecuteChanged();
            RefreshCameraCommand.RaiseCanExecuteChanged();

            SetWindowStatus(!_sessionOpened);
        }

        private void SetWindowStatus(bool status)
        {
            _dialogService.SetWindowCloseState(status);
        }

        private void GoBack()
        {
            SetWindowStatus(true);

            _navigator.NavigateBack(this);
        }


        public int Counter
        {
            get { return _counter; }
            set { Set(() => Counter, ref _counter, value); }
        }


        public bool TakingPicture
        {
            get { return _takingPicture; }
            set
            {
                if (_takingPicture == value)
                    return;

                _takingPicture = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => NotTakingPicture);
            }
        }

        public bool NotTakingPicture
        {
            get { return !TakingPicture; }
        }

        public int Width
        {
            get { return _width; }
            set
            {
                Set(() => Width, ref _width, value);
            }
        }

        public int Height
        {
            get { return _height; }
            set
            {
                Set(() => Height, ref _height, value);
            }
        }

        public int ImageNumber
        {
            get { return _imageNumber; }
            set { Set(() => ImageNumber, ref _imageNumber, value); }
        }

        public byte[] LiveViewImageStream
        {
            get { return _liveViewImageStream; }
            set
            {
                Set(() => LiveViewImageStream, ref _liveViewImageStream, value);
            }
        }

        public RelayCommand TakePictureCommand
        {
            get { return _takePictureCommand ?? (_takePictureCommand = new RelayCommand(TakePicture, () => _sessionOpened && !TakingPicture)); }
        }

        #region unused


        private void SetFocus(uint focus)
        {
            _imageProcessor.SetFocus(focus);
        }

        private IList<uint> _focusPoints;
       

        public IList<uint> FocusPoints
        {
            get { return _focusPoints ?? (_focusPoints = Enum.GetValues(typeof(LiveViewDriveLens)).OfType<uint>().ToList()); }
        }

        public RelayCommand<uint> SetFocusCommand
        {
            get
            {
                return _setFocusCommand ?? (_setFocusCommand = new RelayCommand<uint>(SetFocus,
                    (x) => _sessionOpened && !TakingPicture && _isLiveViewOn));
            }
        }

        public RelayCommand GoBackCommand
        {
            get { return _goBackCommand ?? (_goBackCommand = new RelayCommand(GoBack, () => !TakingPicture)); }
        }

        public RelayCommand OpenSessionCommand
        {
            get { return _openSessionCommand ?? (_openSessionCommand = new RelayCommand(OpenSession, () => true)); } //todo
        }

        public RelayCommand CloseSessionCommand
        {
            get { return _closeSessionCommand ?? (_closeSessionCommand = new RelayCommand(CloseSession, () => _sessionOpened && !TakingPicture)); }
        }

        public RelayCommand RefreshCameraCommand
        {
            get { return _refreshCameraCommand ?? (_refreshCameraCommand = new RelayCommand(RefreshCamera, () => _sessionOpened && !TakingPicture)); }
        }

        public RelayCommand StartLiveViewCommand
        {
            get { return _startLiveViewCommand ?? (_startLiveViewCommand = new RelayCommand(StartLiveView, () => _sessionOpened && !TakingPicture)); }
        }

        #endregion
    }
}
