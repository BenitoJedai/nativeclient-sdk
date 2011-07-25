/*
 * Copyright 2009 The Native Client Authors. All rights reserved.
 * Use of this source code is governed by a BSD-style license that can
 * be found in the LICENSE file.
 */
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VsSDK.IntegrationTestLibrary;
using Microsoft.VSSDK.Tools.VsIdeTesting;

namespace IntegrationTests {
  [TestClass]
  public class CSharpProjectTests {
    #region fields

    private delegate void ThreadInvoker();

    #endregion

    #region properties

    /// <summary>
    ///Gets or sets the test context which provides
    ///information about and functionality for the current test run.
    ///</summary>
    public TestContext TestContext { get; set; }

    #endregion

    #region ctors

    #endregion

    #region Additional test attributes

    //
    // You can use the following additional attributes as you write your tests:
    //
    // Use ClassInitialize to run code before running the first test in the class
    // [ClassInitialize()]
    // public static void MyClassInitialize(TestContext testContext) { }
    //
    // Use ClassCleanup to run code after all tests in a class have run
    // [ClassCleanup()]
    // public static void MyClassCleanup() { }
    //
    // Use TestInitialize to run code before running each test 
    // [TestInitialize()]
    // public void MyTestInitialize() { }
    //
    // Use TestCleanup to run code after each test has run
    // [TestCleanup()]
    // public void MyTestCleanup() { }
    //

    #endregion

    [TestMethod]
    [HostType("VS IDE")]
    public void WinformsApplication() {
      UIThreadInvoker.Invoke((ThreadInvoker) delegate {
                                               var testUtils = new TestUtils();

                                               testUtils.CreateEmptySolution(
                                                   TestContext.TestDir,
                                                   "CreateWinformsApplication");
                                               Assert.AreEqual(0,
                                                               testUtils.
                                                                   ProjectCount());

                                               //Create Winforms application project
                                               //TestUtils.CreateProjectFromTemplate("MyWindowsApp", "Windows Application", "CSharp", false);
                                               //Assert.AreEqual<int>(1, TestUtils.ProjectCount());

                                               //TODO Verify that we can debug launch the application

                                               //TODO Set Break point and verify that will hit

                                               //TODO Verify Adding new project item to project
                                             });
    }
  }
}