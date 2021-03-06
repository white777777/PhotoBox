﻿using System;
using ImageMaker.AdminViewModels.Helpers;
using ImageMaker.CommonViewModels.DragDrop;
using ImageMaker.CommonViewModels.ViewModels;

namespace ImageMaker.AdminViewModels.ViewModels.Images
{
    public class TemplateImageViewModel : BaseViewModel, ICopyable<TemplateImageViewModel>, ISelectable, IResizable, IDragable
    {
        private double _parentWidth;
        private double _parentHeight;

        private double _x;
        private double _y;
        private double _width;
        private double _height;
        private bool _isSelected;
        private int _index;

        public TemplateImageViewModel(double x, double y, double width, double height, int id, double parentWidth, double parentHeight, bool isInstaPrinterImage)
            : this(parentWidth, parentHeight, isInstaPrinterImage)
        {
            Id = id;
            _x = x;
            _y = y;
            _width = width;
            //_height = height;
            _height = GetCorrectHeight(_width);
            //if (_isInstaPrinterImage)
            //    _height = _width;
        }

        public TemplateImageViewModel(double parentWidth, double parentHeight, bool isInstaPrinterImage)
        {
            _isInstaPrinterImage = isInstaPrinterImage;

            _parentHeight = parentHeight;
            _parentWidth = parentWidth;
            if (IsInstaPrinterImage)
                _parentHeight = parentWidth;
            _x = 0;
            _y = 0;
            _width = 0.1;
            //_height = 0.1;
            _height = GetCorrectHeight(_width);

            //if (_isInstaPrinterImage)
            //{
            //    _width = 0.1;
            //    _height = 0.1;
            //}
        }

        public double X
        {
            get { return _x; }
            set
            {
                if (Math.Abs(_x - value) < double.Epsilon)
                    return;


                PushState();

                _x = value;
                RaisePropertyChanged(() => X);
                RaiseSelectionChanged();
            }
        }

        public double Y
        {
            get { return _y; }
            set
            {
                if (Math.Abs(_y - value) < double.Epsilon)
                    return;


                PushState();

                _y = value;
                RaisePropertyChanged(() => Y);
                RaiseSelectionChanged();
            }
        }

        public double Width
        {
            get { return _width; }
            set
            {
                if (Math.Abs(_width - value) < double.Epsilon)
                    return;

                PushState();

                _width = value;
                Height = GetCorrectHeight(_width);
                RaisePropertyChanged(() => Width);
                RaiseSelectionChanged();
            }
        }

        public double Height
        {
            get { return _height; }
            set
            {
                if (Math.Abs(_height - value) < double.Epsilon)
                    return;


                PushState();
                _height = value;
                Width = GetCorrectWidth(_height);
                RaisePropertyChanged(() => Height);
                RaiseSelectionChanged();
            }
        }

        public int Index
        {
            get { return _index; }
            set
            {
                if (_index == value)
                    return;

                _index = value;
                RaisePropertyChanged();
            }
        }

        public int Id { get; private set; }
        private bool _isInstaPrinterImage;
        public bool IsInstaPrinterImage { get { return _isInstaPrinterImage; } }

        private void PushState()
        {
            TemplateEditorViewModel.Stack.Value.Do(this);
        }

        protected void RaiseSelectionChanged()
        {
            SelectionChanged?.Invoke(this);
        }

        private void UpdateProperties()
        {
            RaisePropertyChanged(() => X);
            RaisePropertyChanged(() => Y);
            RaisePropertyChanged(() => Width);
            RaisePropertyChanged(() => Height);
            RaiseSelectionChanged();
            RaisePropertyChanged(() => IsSelected);
        }

        public TemplateImageViewModel Copy()
        {
            var viewModel = new TemplateImageViewModel(X, Y, Width, Height, Id, _parentWidth, _parentHeight, _isInstaPrinterImage)
            {
                _isSelected = IsSelected
            };

            return viewModel;
        }

        public void CopyTo(TemplateImageViewModel to)
        {
            to._parentHeight = _parentHeight;
            to._parentWidth = _parentWidth;
            to._x = X;
            to._y = Y;
            to._width = Width;
            to._height = Height;
            to._isSelected = IsSelected;
            to._isInstaPrinterImage = IsInstaPrinterImage;
            to.Id = Id;

            to.UpdateProperties();
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected == value)
                    return;

                PushState();
                _isSelected = value;
                RaiseSelectionChanged();
                RaisePropertyChanged(() => IsSelected);
            }
        }

        public event Action<ISelectable> SelectionChanged;

        public void SetSelected(bool status)
        {
            _isSelected = status;
            //RaiseSelectionChanged();
            RaisePropertyChanged(() => IsSelected);
        }


        public void Resize(double deltaX, double deltaY, double offsetX, double offsetY)
        {
            double tmpX = X,
                tmpY = Y,
                tmpW,
                tmpH;

            var tmpRightX = X + Width;
            var tmpRightY = Y + Height;

            //не двигается левый верхний угол
            if (Math.Abs(offsetX) < double.Epsilon && Math.Abs(offsetY) < double.Epsilon)
            {
                tmpW = Width + deltaX / _parentWidth;
                //tmpH = Height + deltaY/_parentHeight;
                tmpH = GetCorrectHeight(tmpW);
            }
            //не двигается левый нижний угол
            else if (Math.Abs(offsetX) < double.Epsilon)
            {
                tmpW = Width + deltaX / _parentWidth;
                tmpH = GetCorrectHeight(tmpW);
                tmpY = tmpRightY - tmpH;
            }
            //не двигается правый верхний угол
            else if (Math.Abs(offsetY) < double.Epsilon)
            {
                tmpH = Height + deltaY / _parentHeight;
                tmpW = GetCorrectWidth(tmpH);
                tmpX = tmpRightX - tmpW;
            }
            //не двигается правый нижний угол
            else
            {
                if (Math.Abs(offsetX) > double.Epsilon)
                {
                    tmpX = X + offsetX / _parentWidth;
                    tmpW = tmpRightX - tmpX;
                    tmpH = GetCorrectHeight(tmpW);
                    tmpY = tmpRightY - tmpH;
                }
                else
                {
                    tmpY = Y + offsetY / _parentHeight;
                    tmpH = tmpRightY - tmpY;
                    tmpW = GetCorrectWidth(tmpH);
                    tmpX = tmpRightX - tmpW;
                }
            }

            if (tmpX < 0 || tmpW <= 0 || (tmpX + tmpW) > 1 || tmpY < 0 || tmpH <= 0 || (tmpY + tmpH) > 1)
                return;

            X = tmpX;
            Y = tmpY;

            Width = tmpW;
            Height = tmpH;
        }

        public void Move(double deltaX, double deltaY)
        {
            if (Math.Abs(deltaX) < double.Epsilon && Math.Abs(deltaY) < double.Epsilon)
                return;

            var tmpX = X + deltaX / _parentWidth;
            var tmpY = Y + deltaY / _parentHeight;

            if (tmpX < 0 || (tmpX + Width) > 1 || tmpY < 0 || (tmpY + Height) > 1)
                return;

            X = tmpX;
            Y = tmpY;
        }

        public Type DataType { get { return typeof(TemplateImageViewModel); } }

        public void Update(double x, double y)
        {
            var tmpX = (x / _parentWidth)/*.ThreeDigits()*/ - (Width / 2);//.ThreeDigits();
            var tmpY = (y / _parentHeight)/*.ThreeDigits()*/ - (Height / 2);//.ThreeDigits();
            if (tmpX < 0 || (tmpX + Width) > 1 || tmpY < 0 || (tmpY + Height) > 1)
                return;

            X = tmpX;
            Y = tmpY;
        }

        private double GetCorrectHeight(double width)
        {

            //default camera resolution is 1056w x 704h
            return (width * _parentWidth / 1056.0 * 704) / _parentHeight;
        }
        private double GetCorrectWidth(double height)
        {

            //default camera resolution is 1056w x 704h
            return (height * _parentHeight / 704.0 * 1056) / _parentWidth;
        }
    }
}
