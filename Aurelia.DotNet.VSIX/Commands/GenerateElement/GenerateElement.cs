﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;
using Aurelia.Dotnet.Wizard.CommandWizards;
using Aurelia.DotNet.VSIX.Helpers;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Aurelia.DotNet.VSIX.Commands.GenerateElement
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class GenerateElement
    {
        private DTE2 _dte;

        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateElement"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private GenerateElement(AsyncPackage package, OleMenuCommandService commandService, DTE2 dte)
        {
            _dte = dte;
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(PackageGuids.guidAureliaCommandsSet, PackageIds.cmdGenerateElement);
            var menuItem = new OleMenuCommand(this.ExecuteAsync, menuCommandID);
            menuItem.BeforeQueryStatus += MenuItem_BeforeQueryStatus;
            commandService.AddCommand(menuItem);
        }
        private async void MenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            Helpers.DteHelpers.GetSelectionData(_dte, out var targetFolderPath, out var projectFolderPath, out var projectFullName);
            button.Visible = await Helpers.Aurelia.IsInAureliaRoot(targetFolderPath, projectFolderPath);

        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static GenerateElement Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in Command1's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            var dte = await package.GetServiceAsync(typeof(DTE)) as DTE2;

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new GenerateElement(package, commandService, dte);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private async void ExecuteAsync(object sender, EventArgs e)
        {
            var item = _dte.SelectedItems.Item(1);
            Helpers.DteHelpers.GetSelectionData(_dte, out var targetFile, out var projectFolderPath, out var projectFullName);
            var targetFolder = Path.GetDirectoryName(targetFile);

            if (string.IsNullOrEmpty(targetFolder) || !Directory.Exists(targetFolder))
                return;

            var selectedItem = item as ProjectItem;
            var selectedProject = item as Project;
            Project project = selectedItem?.ContainingProject ?? selectedProject ?? DteHelpers.GetActiveProject(_dte);


            var dialog = new ElementGenerationDialog();
            var hwnd = new IntPtr(_dte.MainWindow.HWnd);
            var window = (System.Windows.Window)HwndSource.FromHwnd(hwnd).RootVisual;
            dialog.Owner = window;


            if (!(dialog.ShowDialog() ?? false)) { return; }

            var type = dialog.Type;
            var elementName = dialog.ElementName;
            var bindablePropertyNames = dialog.PropertyNames;
            var elementTemplates = Template.GetTemplateFilesByType("element").Where(y => y.Contains(type)).ToList();

            await Task.WhenAll(elementTemplates.Select(async y =>
            {
                using (var reader = new StreamReader(y))
                {
                    var fileName = Path.GetFileName(y);
                    var parts = fileName.Split('.');
                    var extension = parts[parts.Length - 2];
                    var fullFileName = Path.Combine(targetFolder, $"{elementName}.{extension}");


                    string templateText = await reader.ReadToEndAsync();
                    templateText = templateText.Replace("%elementName%", elementName);
                    File.WriteAllText(fullFileName, templateText);
                    VsShellUtilities.OpenDocument(package, fullFileName);

                }
            }));

        }
    }
}