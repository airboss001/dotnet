﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Aurelia.DotNet.Extensions.Models
{
    public class Route
    {
        public Route()
        {
            this.Paths = new HashSet<string>();
            this.ChildRoutes = new ObservableCollection<Route>();
        }
        public string Title { get; set; }
        public bool CanNavigate { get; set; }
        public string Name { get; set; }
        public IEnumerable<string> Paths { get; set; }
        public string ModuleName { get; set; }
        public IEnumerable<Route> ChildRoutes { get; set; }
    }
}

