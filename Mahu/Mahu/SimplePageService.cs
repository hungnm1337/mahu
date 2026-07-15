using System;
using System.Windows;
using Wpf.Ui.Abstractions;

namespace Mahu
{
    public class SimplePageService : INavigationViewPageProvider
    {
        public object GetPage(Type pageType)
        {
            return Activator.CreateInstance(pageType);
        }
    }
}
