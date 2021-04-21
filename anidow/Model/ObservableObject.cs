using System.ComponentModel;
using System.Runtime.CompilerServices;
using Anidow.Annotations;

namespace Anidow.Model
{
    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
    }
}