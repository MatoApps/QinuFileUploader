using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using Workshop.ViewModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QinuFileUploader
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BucketListPage : Page
    {
        public event EventHandler<EventArgs> OnSubmit;

        public BucketListPage()
        {
            this.InitializeComponent();
        }

        public BucketListPage(List<string> buckets) : this()
        {
            this.Buckets = buckets;
        }

        private List<string> _buckets;

        public List<string> Buckets
        {
            get { return _buckets; }
            set { _buckets = value; }
        }


        private string _currentBucket;

        public string CurrentBucket
        {
            get { return _currentBucket; }
            set
            {
                _currentBucket = value;
                OnSubmit?.Invoke(this, new EventArgs());
            }
        }


    }
}
