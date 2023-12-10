using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using WebsiteCreator.MVVM.ViewModel;

namespace WebsiteCreator.Core.Converter
{
    public class GetIndexMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] != null && values[1] is ObservableCollection<TagInfo> collection)
            {
                int itemIndex = collection.IndexOf((TagInfo)values[0]);
                return itemIndex.ToString();
            }
            else if (values[0] != null && values[1] is ObservableCollection<RadioTagInfo> collection1)
            {
                int itemIndex = collection1.IndexOf((RadioTagInfo)values[0]);
                return itemIndex.ToString();
            }
            return "0";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("GetIndexMultiConverter_ConvertBack");
        }
    }
}
