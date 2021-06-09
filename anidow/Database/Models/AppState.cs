// // Created: 06-06-2021 21:45

using System;
using Anidow.Model;

namespace Anidow.Database.Models
{
    public class AppState : ObservableObject
    {
        public int Id { get; set; }
        public DateTime Created { get; set; }
        public bool FirstStart { get; set; } = true;
    }
}