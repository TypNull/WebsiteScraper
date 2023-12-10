using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using WebsiteCreator.MVVM.ViewModel;

namespace WebsiteCreator.MVVM.View
{
    /// <summary>
    /// Interaktionslogik für RadioExpandableList.xaml
    /// </summary>
    public partial class RadioExpandableList : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public RadioExpandableList()
        {
            InitializeComponent();
        }

        public string TagFocusString = string.Empty;

        public CustomDropHandler CustomDrop { get; } = new();

        [Bindable(true)]
        public RadioTagInfo NewRadioTag
        {
            get { return (RadioTagInfo)GetValue(NewRadioTagProperty); }
            set { SetValue(NewRadioTagProperty, value); }
        }
        public static readonly DependencyProperty NewRadioTagProperty =
          DependencyProperty.Register("NewRadioTag", typeof(RadioTagInfo), typeof(RadioExpandableList));


        [Bindable(true)]
        public ObservableCollection<RadioTagInfo> ItemsSource
        {
            get { return (ObservableCollection<RadioTagInfo>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }
        public static readonly DependencyProperty ItemsSourceProperty =
          DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<RadioTagInfo>), typeof(RadioExpandableList));

        public static DependencyProperty NewRadioTagLostFocusCommandProperty = DependencyProperty.Register("NewRadioTagLostFocusCommand", typeof(ICommand), typeof(RadioExpandableList));

        public ICommand NewRadioTagLostFocusCommand
        {
            get => (ICommand)GetValue(NewRadioTagLostFocusCommandProperty);
            set => SetValue(NewRadioTagLostFocusCommandProperty, value);
        }

        public static DependencyProperty DeleteCommandProperty = DependencyProperty.Register("DeleteCommand", typeof(ICommand), typeof(RadioExpandableList));

        public ICommand DeleteCommand
        {
            get => (ICommand)GetValue(DeleteCommandProperty);
            set => SetValue(DeleteCommandProperty, value);
        }
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(RadioExpandableList));

        public string NewFirstPlaceholder => "New " + FirstPlaceholder;
        public string FirstPlaceholder
        {
            get { return (string)GetValue(FirstPlaceholderProperty); }
            set { SetValue(FirstPlaceholderProperty, value); }
        }

        public static readonly DependencyProperty FirstPlaceholderProperty =
            DependencyProperty.Register("FirstPlaceholder", typeof(string), typeof(RadioExpandableList), new PropertyMetadata(OnChangedCallback));

        public string NewSecondPlaceholder => "New " + SecondPlaceholder;
        public string SecondPlaceholder
        {
            get { return (string)GetValue(SecondPlaceholderProperty); }
            set { SetValue(SecondPlaceholderProperty, value); }
        }

        public static readonly DependencyProperty SecondPlaceholderProperty =
            DependencyProperty.Register("SecondPlaceholder", typeof(string), typeof(RadioExpandableList), new PropertyMetadata(OnChangedCallback));

        public string NewGroupNamePlaceholder => "New " + GroupNamePlaceholder;
        public string GroupNamePlaceholder
        {
            get { return (string)GetValue(GroupNamePlaceholderProperty); }
            set { SetValue(GroupNamePlaceholderProperty, value); }
        }

        public static readonly DependencyProperty GroupNamePlaceholderProperty =
            DependencyProperty.Register("GroupNamePlaceholder", typeof(string), typeof(RadioExpandableList), new PropertyMetadata(OnChangedCallback));

        public string NewTitlePlaceholder => "New " + TitlePlaceholder;
        public string TitlePlaceholder
        {
            get { return (string)GetValue(TitlePlaceholderProperty); }
            set { SetValue(TitlePlaceholderProperty, value); }
        }

        public static readonly DependencyProperty TitlePlaceholderProperty =
            DependencyProperty.Register("TitlePlaceholder", typeof(string), typeof(RadioExpandableList), new PropertyMetadata(OnChangedCallback));

        private static void OnChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            RadioExpandableList? c = sender as RadioExpandableList;
            if (c != null)
                c.OnChanged(e);
        }

        protected virtual void OnChanged(DependencyPropertyChangedEventArgs e) => OnPropertyChanged("New" + e.Property.Name);
        private void Tag_Initialized(object sender, System.EventArgs e)
        {
            if (TagFocusString.Equals("New " + (sender as TextBox)?.Tag?.ToString()))
                ((TextBox)sender).Focus();
        }

        private void NewTag_Initialized(object sender, System.EventArgs e)
        {
            if (TagFocusString.Equals((sender as TextBox)?.Tag?.ToString()))
                ((TextBox)sender).Focus();
        }

        private void NewTag_LostFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            TextBox? textBox = sender as TextBox;
            if (BindingOperations.GetBindingExpression(textBox, TextBox.TextProperty) is BindingExpression binding && binding != null && binding.ResolvedSource != null)
                binding.UpdateSource();
            if (!string.IsNullOrWhiteSpace(textBox?.Text) && textBox?.DataContext is RadioTagInfo tagInfo)
            {
                TagFocusString = (e.NewFocus is TextBox ? ((FrameworkElement)e.NewFocus).Tag.ToString() : "") ?? "";
                tagInfo.Add();
            }
            else TagFocusString = "";
        }

        private void NewRadioTag_LostFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            TextBox? textBox = sender as TextBox;
            if (BindingOperations.GetBindingExpression(textBox, TextBox.TextProperty) is BindingExpression binding && binding != null && binding.ResolvedSource != null)
                binding.UpdateSource();
            if (!string.IsNullOrWhiteSpace(textBox?.Text))
            {
                TagFocusString = NewTitlePlaceholder;
                NewRadioTagLostFocusCommand?.Execute(NewRadioTag);
            }
            else TagFocusString = "";
        }
    }
}
