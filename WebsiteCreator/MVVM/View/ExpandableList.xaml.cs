using GongSolutions.Wpf.DragDrop;
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
    /// Interaktionslogik für ExpandableList.xaml
    /// </summary>
    /// 
    public partial class ExpandableList : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public ExpandableList()
        {
            InitializeComponent();
        }

        public string TagFocusString = string.Empty;
        public CustomDropHandler CustomDrop { get; } = new();


        [Bindable(true)]
        public TagInfo NewTag
        {
            get { return (TagInfo)GetValue(NewTagProperty); }
            set { SetValue(NewTagProperty, value); }
        }
        public static readonly DependencyProperty NewTagProperty =
          DependencyProperty.Register("NewTag", typeof(TagInfo), typeof(ExpandableList));


        [Bindable(true)]
        public ObservableCollection<TagInfo> ItemsSource
        {
            get { return (ObservableCollection<TagInfo>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }
        public static readonly DependencyProperty ItemsSourceProperty =
          DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<TagInfo>), typeof(ExpandableList));

        public static DependencyProperty NewTagLostFocusCommandProperty = DependencyProperty.Register("NewTagLostFocusCommand", typeof(ICommand), typeof(ExpandableList));

        public ICommand NewTagLostFocusCommand
        {
            get => (ICommand)GetValue(NewTagLostFocusCommandProperty);
            set => SetValue(NewTagLostFocusCommandProperty, value);
        }

        public static DependencyProperty DeleteCommandProperty = DependencyProperty.Register("DeleteCommand", typeof(ICommand), typeof(ExpandableList));

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
            DependencyProperty.Register("Title", typeof(string), typeof(ExpandableList));

        public string NewFirstPlaceholder => "New " + FirstPlaceholder;
        public string FirstPlaceholder
        {
            get { return (string)GetValue(FirstPlaceholderProperty); }
            set { SetValue(FirstPlaceholderProperty, value); }
        }

        public static readonly DependencyProperty FirstPlaceholderProperty =
            DependencyProperty.Register("FirstPlaceholder", typeof(string), typeof(ExpandableList), new PropertyMetadata(OnChangedCallback));

        public string NewSecondPlaceholder => "New " + SecondPlaceholder;
        public string SecondPlaceholder
        {
            get { return (string)GetValue(SecondPlaceholderProperty); }
            set { SetValue(SecondPlaceholderProperty, value); }
        }

        public static readonly DependencyProperty SecondPlaceholderProperty =
            DependencyProperty.Register("SecondPlaceholder", typeof(string), typeof(ExpandableList), new PropertyMetadata(OnChangedCallback));

        public string NewThirdPlaceholder => "New " + ThirdPlaceholder;
        public string ThirdPlaceholder
        {
            get { return (string)GetValue(ThirdPlaceholderProperty); }
            set { SetValue(ThirdPlaceholderProperty, value); }
        }

        public static readonly DependencyProperty ThirdPlaceholderProperty =
            DependencyProperty.Register("ThirdPlaceholder", typeof(string), typeof(ExpandableList), new PropertyMetadata(OnChangedCallback));

        public string NewTitlePlaceholder => "New " + TitlePlaceholder;
        public string TitlePlaceholder
        {
            get { return (string)GetValue(TitlePlaceholderProperty); }
            set { SetValue(TitlePlaceholderProperty, value); }
        }

        public static readonly DependencyProperty TitlePlaceholderProperty =
            DependencyProperty.Register("TitlePlaceholder", typeof(string), typeof(ExpandableList), new PropertyMetadata(OnChangedCallback));

        private static void OnChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ExpandableList? c = sender as ExpandableList;
            if (c != null)
                c.OnChanged(e);
        }

        protected virtual void OnChanged(DependencyPropertyChangedEventArgs e) => OnPropertyChanged("New" + e.Property.Name);
        private void Tag_Initialized(object sender, System.EventArgs e)
        {
            if (TagFocusString.Equals("New " + (sender as TextBox)?.Tag?.ToString()))
                ((TextBox)sender).Focus();
        }

        private void NewTag_LostFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            TextBox? textBox = sender as TextBox;
            if (BindingOperations.GetBindingExpression(textBox, TextBox.TextProperty) is BindingExpression binding && binding != null && binding.ResolvedSource != null)
                binding.UpdateSource();
            if (!string.IsNullOrWhiteSpace(textBox?.Text))
            {
                TagFocusString = (e.NewFocus is TextBox ? ((FrameworkElement)e.NewFocus).Tag.ToString() : "") ?? string.Empty;
                NewTagLostFocusCommand?.Execute(NewTag);
            }
            else TagFocusString = "";
        }
    }

    public class CustomDropHandler : DefaultDropHandler
    {
        public override void DragOver(IDropInfo dropInfo)
        {
            if ((dropInfo.TargetCollection as ObservableCollection<TagInfo>)?.Contains((TagInfo)dropInfo.Data) == true)
                base.DragOver(dropInfo);
        }
    }
}
