using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QinuFileUploader.Model
{

    public class ExplorerItem : ObservableObject
    {
        public enum ExplorerItemType { Folder, File };
        public string Name { get; set; }
        public string Path { get; set; }

        private bool _isCurrent;

        public bool IsCurrent
        {
            get { return _isCurrent; }
            set
            {
                _isCurrent = value;

                OnPropertyChanged(nameof(IsCurrent));

            }
        }

        public ExplorerItemType Type { get; set; }
        private ObservableCollection<ExplorerItem> m_children;
        public ObservableCollection<ExplorerItem> Children
        {
            get
            {
                if (m_children == null)
                {
                    m_children = new ObservableCollection<ExplorerItem>();
                }
                return m_children;
            }
            set
            {
                m_children = value;
            }
        }

        private bool m_isExpanded;
        public bool IsExpanded
        {
            get { return m_isExpanded; }
            set
            {
                if (m_isExpanded != value)
                {
                    m_isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }
    }

}
