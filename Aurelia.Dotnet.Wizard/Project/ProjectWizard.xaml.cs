﻿using Aurelia.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Aurelia.DotNet.Wizard
{
    /// <summary>
    /// Interaction logic for ProjectWizard.xaml
    /// </summary>
    public partial class ProjectWizard : Window
    {

        public event EventHandler<ProjectWizardViewModel> ChangesSaved;
        public ProjectWizardViewModel ViewModel { get => (ProjectWizardViewModel)this.DataContext; set => this.DataContext = value; }
        public ProjectWizard()
        {
            InitializeComponent();
            this.DataContext = new ProjectWizardViewModel();
            this.SetStyle();
        }

        private void SelectAllOnFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox)?.SelectAll();
        }

        private void SaveChanges(object sender, RoutedEventArgs e)
        {
            // Emit event to let people gather settings
            this.ChangesSaved?.Invoke(this, this.ViewModel);
            DialogResult = true;
            this.Close();
        }

        private void CancelChanges(object sender, RoutedEventArgs e)
        {
            ViewModel.Cancelled = true;
            DialogResult = false;
            this.Close();
        }

        private void TxtPort_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

        }

        private void AddRoute(object sender, RoutedEventArgs e)
        {
            var parentRoute = ((ContentPresenter)((Button)(sender)).TemplatedParent).Content as Route;
            parentRoute.ChildRoutes.Add(new Route
            {
                Title = $"{parentRoute.Title}Child{parentRoute.ChildRoutes.Count() + 1}",
                CanNavigate = parentRoute.CanNavigate
            });
            this.routeTreeView.SetStyle();
        }

        private void RouteTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            this.ViewModel.CurrentRoute = (Route)e.NewValue;
            FocusManager.SetFocusedElement(this, txtRoute);
            this.txtRoute.CaretIndex = 0;
            this.txtRoute.Select(0, this.txtRoute.Text.Length);
        }


        private void RemoveRoute(object sender, RoutedEventArgs e)
        {
            var route = ((ContentPresenter)((Button)(sender)).TemplatedParent).Content as Route;
            RemoveRoute(route, this.ViewModel.Routes);
        }

        private void RemoveRoute(Route route, ICollection<Route> routes)
        {
            var result = routes.Remove(route);
            if (!result)
            {
                routes.ToList().ForEach(y => RemoveRoute(route, y.ChildRoutes));
            }
        }

        private void AddRootRoute(object sender, RoutedEventArgs e)
        {
            this.ViewModel.Routes.Add(new Route
            {
                Title = $"App{this.ViewModel.Routes.Count() + 1}",

            });

        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {

            TAncestor FindAncestor<TAncestor>(UIElement uiElement) where TAncestor : class
            {
                while ((uiElement != null) && !(uiElement is TAncestor))
                    uiElement = VisualTreeHelper.GetParent(uiElement) as UIElement;

                return uiElement as TAncestor;
            }

            var treeViewItem = FindAncestor<TreeViewItem>(sender as UIElement);
            treeViewItem.IsSelected = true;

        }
    }
}
