// Copyright (c) 2012 The Chromium Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

namespace NativeClientVSAddIn
{
  using System;

  using EnvDTE;
  using EnvDTE80;
  using Extensibility;
  using Microsoft.VisualStudio;
  using Microsoft.VisualStudio.VCProjectEngine;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Reflection;

  /// <summary>The object for implementing an Add-in.</summary>
  /// <seealso class='IDTExtensibility2' />
  public class Connect : IDTExtensibility2
  {
    /// <summary>
    /// The main Visual Studio interface.
    /// </summary>
    private DTE2 dte_;

    /// <summary>
    /// Receives events related to starting/stopping debugging.
    /// </summary>
    private DebuggerEvents debuggerEvents_;

    /// <summary>
    /// Receives all generic events from Visual Studio.
    /// </summary>
    private CommandEvents commandEvents_;

    /// <summary>
    /// Holds methods related to debugging.
    /// </summary>
    private PluginDebuggerBase debugger_;

    /// <summary>
    /// The web server launched during debugging.
    /// </summary>
    private WebServer webServer_;

    /// <summary>
    /// Visual Studio output window pane that captures output from the web server, and displays
    /// other web-server related information.
    /// </summary>
    private OutputWindowPane webServerOutputPane_;

    /// <summary>
    /// Implements the OnConnection method of the IDTExtensibility2 interface.
    /// Receives notification that the Add-in is being loaded.
    /// </summary>
    /// <param name="application">Root object of the host application.</param>
    /// <param name="connectMode">
    /// Describes how the Add-in is being loaded (e.g. command line or UI). This is unused since
    /// the add-in functions the same regardless of how it was loaded.
    /// </param>
    /// <param name="addInInst">Object representing this Add-in.</param>
    /// <param name="custom">Unused, but could contain host specific data for the add-in.</param>
    /// <seealso class='IDTExtensibility2' />
    public void OnConnection(
        object application,
        ext_ConnectMode connectMode,
        object addInInst,
        ref Array custom)
    {
      dte_ = (DTE2)application;

      debuggerEvents_ = dte_.Events.DebuggerEvents;
      debuggerEvents_.OnEnterDesignMode += DebuggerOnEnterDesignMode;
      debuggerEvents_.OnEnterRunMode += DebuggerOnEnterRunMode;

      commandEvents_ = dte_.Events.CommandEvents;
      commandEvents_.AfterExecute += CommandEventsAfterExecute;

      try
      {
        webServerOutputPane_ = dte_.ToolWindows.OutputWindow.OutputWindowPanes.Item(
            Strings.WebServerOutputWindowTitle);
      }
      catch (ArgumentException)
      {
        // This exception is expected if the window pane hasn't been created yet.
        webServerOutputPane_ = dte_.ToolWindows.OutputWindow.OutputWindowPanes.Add(
            Strings.WebServerOutputWindowTitle);
      }
    }

    /// <summary>
    /// Implements the OnDisconnection method of the IDTExtensibility2
    /// interface. Receives notification that the Add-in is being unloaded.
    /// </summary>
    /// <param name='disconnectMode'>Describes how the Add-in is being unloaded.</param>
    /// <param name='custom'>Array of parameters that are host application specific.</param>
    /// <seealso class='IDTExtensibility2' />
    public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
    {
    }

    /// <summary>
    /// Implements the OnAddInsUpdate method of the IDTExtensibility2 interface.
    /// Receives notification when the collection of Add-ins has changed.
    /// </summary>
    /// <param name='custom'>Array of parameters that are host application specific.</param>
    /// <seealso class='IDTExtensibility2' />
    public void OnAddInsUpdate(ref Array custom)
    {
    }

    /// <summary>
    /// Implements the OnStartupComplete method of the IDTExtensibility2 interface.
    /// Receives notification that the host application has completed loading.
    /// </summary>
    /// <param name='custom'>Array of parameters that are host application specific.</param>
    /// <seealso class='IDTExtensibility2' />
    public void OnStartupComplete(ref Array custom)
    {
    }

    /// <summary>
    /// Implements the OnBeginShutdown method of the IDTExtensibility2 interface.
    /// Receives notification that the host application is being unloaded.
    /// </summary>
    /// <param name='custom'>Array of parameters that are host application specific.</param>
    /// <seealso class='IDTExtensibility2' />
    public void OnBeginShutdown(ref Array custom)
    {
    }

    /// <summary>
    /// Receives notification after any generic VS command has executed.
    /// Here we capture the SolutionPlatform event which indicates the solution platform has
    /// changed.  We use this event to trigger a modification of property settings since this
    /// event happens immediately after the platforms are added. See PerformPropertyModifications()
    /// for what sort of modifications we are doing.
    /// </summary>
    /// <param name="guid">Guid of the command grouping.</param>
    /// <param name="id">ID of the command within its grouping.</param>
    /// <param name="customIn">Command specific input.</param>
    /// <param name="customOut">Command specific parameter.</param>
    private void CommandEventsAfterExecute(string guid, int id, object customIn, object customOut)
    {
      const string VSStd2KCmdIDEnumGuid = "{1496A755-94DE-11D0-8C3F-00C04FC2AAE2}";
      if (guid.Equals(VSStd2KCmdIDEnumGuid, StringComparison.OrdinalIgnoreCase))
      {
        // If loading a NaCl or Pepper platform, perform property modifications.
        if (id == (int)VSConstants.VSStd2KCmdID.SolutionPlatform)
        {
          string platform = customOut as string;
          if (PropertyManager.IsNaClPlatform(platform) || PropertyManager.IsPepperPlatform(platform))
          {
            PerformPropertyModifications();
          }
        }
      }
    }

    /// <summary>
    /// Goes through all projects in the solution and updates their properties with necessary
    /// modifications if they are NaCl or Pepper configurations. We add the version information
    /// here so that the version is stored directly in the project file. The call to
    /// PerformPropertyFixes() performs a work around on the property pages to force Visual Studio
    /// to save some specific properties into the project file to get around issue 140162.
    /// </summary>
    private void PerformPropertyModifications()
    {
      string naclAddInVersion = GetAddInMajorVersion().ToString();

      var configs = Utility.GetPlatformVCConfigurations(dte_, Strings.PepperPlatformName);
      configs.AddRange(Utility.GetPlatformVCConfigurations(dte_, Strings.NaCl64PlatformName));
      configs.AddRange(Utility.GetPlatformVCConfigurations(dte_, Strings.NaCl32PlatformName));
      configs.AddRange(Utility.GetPlatformVCConfigurations(dte_, Strings.PNaClPlatformName));


      var properties = new PropertyManager();
      foreach (VCConfiguration config in configs)
      {
        properties.SetTarget(config);

        IVCRulePropertyStorage debugger = config.Rules.Item("WindowsLocalDebugger");
        string executable = debugger.GetUnevaluatedPropertyValue("LocalDebuggerCommand");

        // Perform project modifications of the NaClAddInVersion in the project file
        // is out of date, or if the WindowsLocalDebugger contains CHROME_PATH.  The
        // later can happen if the developer deletes the .user file.
        if (executable.Contains("$(CHROME_PATH)") || properties.NaClAddInVersion != naclAddInVersion)
        {
          Debug.WriteLine("Modifying Config: " + config.Name);

          // Set the NaCl add-in version so that it is stored in the project file.
          properties.SetProperty("ConfigurationGeneral", "NaClAddInVersion", naclAddInVersion);

          // Expand the CHROME_PATH variable to its full path.
          string expandedChrome = properties.GetProperty(
              "WindowsLocalDebugger", "LocalDebuggerCommand");
          properties.SetProperty("WindowsLocalDebugger", "LocalDebuggerCommand", expandedChrome);

          // Change the library includes to have the appropriate extension.
          string libs = properties.GetProperty("Link", "AdditionalDependencies");
          if (properties.PlatformType == PropertyManager.ProjectPlatformType.NaCl)
          {
            libs = libs.Replace(".lib", string.Empty);
          }
          else if (properties.PlatformType == PropertyManager.ProjectPlatformType.Pepper)
          {
            string[] libsList = libs.Split(';');
            libs = string.Empty;
            foreach (string lib in libsList)
            {
              string baseLibName = lib.Replace(".lib", string.Empty);
              if (!string.IsNullOrWhiteSpace(lib))
              {
                libs = string.Concat(libs, baseLibName, ".lib;");
              }
            }
          }

          properties.SetProperty("Link", "AdditionalDependencies", libs);
        }
      }
    }

    /// <summary>
    /// Get the major version of the AddIn.
    /// </summary>
    /// <returns>The add-in major version number.</returns>
    private int GetAddInMajorVersion()
    {
      Assembly assem = Assembly.GetExecutingAssembly();
      AssemblyName assemName = assem.GetName();
      return assemName.Version.Major;
    }

    /// <summary>
    /// Called when Visual Studio ends a debugging session.
    /// Shuts down the web server and debugger.
    /// </summary>
    /// <param name="reason">The parameter is not used.</param>
    private void DebuggerOnEnterDesignMode(dbgEventReason reason)
    {
      if (debugger_ != null)
      {
        debugger_.Dispose();
        debugger_ = null;
      }

      if (webServer_ != null)
      {
        webServer_.Dispose();
        webServer_ = null;
      }
    }

    /// <summary>
    /// Called when Visual Studio starts a debugging session.
    /// Here we kick off the debugger and web server if appropriate.
    /// </summary>
    /// <param name="reason">Indicates how we are entering run mode (breakpoint or launch).</param>
    private void DebuggerOnEnterRunMode(dbgEventReason reason)
    {
      // If we are starting debugging (not re-entering from a breakpoint)
      // then load project settings and start the debugger-helper.
      if (reason == dbgEventReason.dbgEventReasonLaunchProgram)
      {
        PropertyManager properties = new PropertyManager();
        properties.SetTargetToActive(dte_);
        if (properties.PlatformType == PropertyManager.ProjectPlatformType.NaCl)
        {
          debugger_ = new PluginDebuggerGDB(dte_, properties);
          webServer_ = new WebServer(webServerOutputPane_, properties);
        }
        else if (properties.PlatformType == PropertyManager.ProjectPlatformType.Pepper)
        {
          debugger_ = new PluginDebuggerVS(dte_, properties);
          webServer_ = new WebServer(webServerOutputPane_, properties);
        }
      }
    }
  }
}
