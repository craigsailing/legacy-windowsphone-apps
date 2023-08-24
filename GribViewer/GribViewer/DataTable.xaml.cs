using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace GribViewer
{
    public partial class DataTable : PhoneApplicationPage
    {
        private DataTableLayer _data = null;

        public DataTable()
        {
            InitializeComponent();
            _data = new DataTableLayer();
            DataViewList.ItemsSource = _data.DataGrouped;
            Analytics.LogEvent("DataTableViewed");
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {

        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            return;
        }

        private void chartWind_Loaded(object sender, RoutedEventArgs e)
        {
            chartWind.Series[0].ItemsSource = _data.Data;
            chartWind.Series[1].ItemsSource = _data.Data;
            chartWind.Series[2].ItemsSource = _data.Data;
        }

        private void LegendWindSpeed_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ToggleLegend(sender, 0);
        }

        private void LegendWindDirection_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ToggleLegend(sender, 1);
        }

        private void LegendPressure_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ToggleLegend(sender, 2);
        }

        private void ToggleLegend(object sender, int index)
        {
            if (chartWind.Series.Count > index)
            {
                Analytics.LogEvent("MeteoGramLegendToggle");

                if (chartWind.Series[index].Visibility == Visibility.Visible)
                {
                    StackPanel pannel = sender as StackPanel;
                    pannel.Opacity = 0.4;
                    chartWind.Series[index].Visibility = Visibility.Collapsed;
                    chartWind.Series[index].VerticalAxis.Visibility = Visibility.Collapsed;
                }
                else
                {
                    StackPanel pannel = sender as StackPanel;
                    pannel.Opacity = 1;
                    chartWind.Series[index].Visibility = Visibility.Visible;
                    chartWind.Series[index].VerticalAxis.Visibility = Visibility.Visible;
                }
            }
        }
    }
}