﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.TestInfra
{
    public class PlaywrightTestInfraFunctionTests
    {
        private Mock<ITestState> MockTestState;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<IPlaywright> MockPlaywrightObject;
        private Mock<IBrowserType> MockBrowserType;
        private Mock<IBrowser> MockBrowser;
        private Mock<IBrowserContext> MockBrowserContext;
        private Mock<IPage> MockPage;
        private Mock<IResponse> MockResponse;
        private Mock<ILogger> MockLogger;
        private Mock<IFileSystem> MockFileSystem;
        private Mock<IFrame> MockIFrame;
        private Mock<IElementHandle> MockElementHandle;

        public PlaywrightTestInfraFunctionTests()
        {
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockPlaywrightObject = new Mock<IPlaywright>(MockBehavior.Strict);
            MockBrowserType = new Mock<IBrowserType>(MockBehavior.Strict);
            MockBrowser = new Mock<IBrowser>(MockBehavior.Strict);
            MockBrowserContext = new Mock<IBrowserContext>(MockBehavior.Strict);
            MockPage = new Mock<IPage>(MockBehavior.Strict);
            MockResponse = new Mock<IResponse>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockIFrame = new Mock<IFrame>(MockBehavior.Strict);
            MockElementHandle = new Mock<IElementHandle>(MockBehavior.Strict);
        }

        [Theory]
        [InlineData("Chromium", null, null, null)]
        [InlineData("Chromium", "Pixel 2", null, null)]
        [InlineData("Safari", "iPhone 8", 400, null)]
        [InlineData("Safari", "iPhone 8", 400, 800)]
        public async Task SetupAsyncTest(string browser, string? device, int? screenWidth, int? screenHeight)
        {
            var browserConfig = new BrowserConfiguration()
            {
                Browser = browser,
                Device = device,
                ScreenHeight = screenHeight,
                ScreenWidth = screenWidth
            };

            var testSettings = new TestSettings()
            {
                RecordVideo = true,
                Timeout = 15
            };

            var testResultsDirectory = "C:\\TestResults";

            var devicesDictionary = new Dictionary<string, BrowserNewContextOptions>()
            {
                { "Pixel 2", new BrowserNewContextOptions() { UserAgent = "Pixel 2 User Agent "} },
                { "iPhone 8", new BrowserNewContextOptions() { UserAgent = "iPhone 8 User Agent "} }
            };

            MockSingleTestInstanceState.Setup(x => x.GetBrowserConfig()).Returns(browserConfig);
            MockPlaywrightObject.SetupGet(x => x[It.IsAny<string>()]).Returns(MockBrowserType.Object);
            MockPlaywrightObject.SetupGet(x => x.Devices).Returns(devicesDictionary);
            MockBrowserType.Setup(x => x.LaunchAsync(It.IsAny<BrowserTypeLaunchOptions>())).Returns(Task.FromResult(MockBrowser.Object));
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            MockSingleTestInstanceState.Setup(x => x.GetTestResultsDirectory()).Returns(testResultsDirectory);
            MockBrowser.Setup(x => x.NewContextAsync(It.IsAny<BrowserNewContextOptions>())).Returns(Task.FromResult(MockBrowserContext.Object));

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, MockPlaywrightObject.Object);
            await playwrightTestInfraFunctions.SetupAsync();

            MockSingleTestInstanceState.Verify(x => x.GetBrowserConfig(), Times.Once());
            MockPlaywrightObject.Verify(x => x[browserConfig.Browser], Times.Once());
            MockBrowserType.Verify(x => x.LaunchAsync(It.Is<BrowserTypeLaunchOptions>(y => y.Headless == false && y.Timeout == testSettings.Timeout)), Times.Once());
            MockTestState.Verify(x => x.GetTestSettings(), Times.Once());

            if (browserConfig.Device != null)
            {
                MockPlaywrightObject.Verify(x => x.Devices, Times.Once());
            }
            MockSingleTestInstanceState.Verify(x => x.GetTestResultsDirectory(), Times.Once());

            var verifyBrowserContextOptions = (BrowserNewContextOptions options) => {
                if (options.RecordVideoDir != testResultsDirectory)
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(browserConfig.Device))
                {
                    var device = devicesDictionary[browserConfig.Device];
                    if (device.UserAgent != options.UserAgent)
                    {
                        return false;
                    }
                }

                if (browserConfig.ScreenWidth != null && browserConfig.ScreenHeight != null)
                {
                    if (browserConfig.ScreenWidth != options.ViewportSize.Width)
                    {
                        return false;
                    }
                    if (browserConfig.ScreenHeight != options.ViewportSize.Height)
                    {
                        return false;
                    }
                } 
                else
                {
                    if (options.ViewportSize != null)
                    {
                        return false;
                    }
                }
                return true;
            };
            MockBrowser.Verify(x => x.NewContextAsync(It.Is<BrowserNewContextOptions>(y => verifyBrowserContextOptions(y))), Times.Once());
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task SetupAsyncThrowsOnInvalidBrowserTest(string? browser)
        {
            var browserConfig = new BrowserConfiguration()
            {
                Browser = browser
            };
            MockSingleTestInstanceState.Setup(x => x.GetBrowserConfig()).Returns(browserConfig);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, MockPlaywrightObject.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await playwrightTestInfraFunctions.SetupAsync());
        }

        [Fact]
        public async Task SetupAsyncThrowsOnNullTestSettingsTest()
        {
            TestSettings testSettings = null;
            var browserConfig = new BrowserConfiguration()
            {
                Browser = "Chromium"
            };
            MockSingleTestInstanceState.Setup(x => x.GetBrowserConfig()).Returns(browserConfig);

            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, MockPlaywrightObject.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await playwrightTestInfraFunctions.SetupAsync());
        }

        [Fact]
        public async Task SetupAsyncThrowsOnNullBrowserConfigTest()
        {
            BrowserConfiguration browserConfig = null;
            MockSingleTestInstanceState.Setup(x => x.GetBrowserConfig()).Returns(browserConfig);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, MockPlaywrightObject.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await playwrightTestInfraFunctions.SetupAsync());
        }

        [Fact]
        public async Task EndTestRunSuccessTest()
        {
            MockBrowserContext.Setup(x => x.CloseAsync()).Returns(Task.CompletedTask);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, browserContext: MockBrowserContext.Object);

            await playwrightTestInfraFunctions.EndTestRunAsync();

            MockBrowserContext.Verify(x => x.CloseAsync(), Times.Once);
        }

        [Fact]
        public async Task GoToUrlTest()
        {
            var urlToVisit = "https://make.powerapps.com";

            MockBrowserContext.Setup(x => x.NewPageAsync()).Returns(Task.FromResult(MockPage.Object));
            MockPage.Setup(x => x.GotoAsync(It.IsAny<string>(), It.IsAny<PageGotoOptions>())).Returns(Task.FromResult<IResponse?>(MockResponse.Object));
            MockResponse.SetupGet(x => x.Ok).Returns(true);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, browserContext: MockBrowserContext.Object);
            await playwrightTestInfraFunctions.GoToUrlAsync(urlToVisit);

            MockBrowserContext.Verify(x => x.NewPageAsync(), Times.Once);
            MockPage.Verify(x => x.GotoAsync(urlToVisit, It.Is<PageGotoOptions>((options) => options.WaitUntil == WaitUntilState.NetworkIdle)), Times.Once);

            var secondUrlToVisit = "https://powerapps.com";
            await playwrightTestInfraFunctions.GoToUrlAsync(secondUrlToVisit);
            MockBrowserContext.Verify(x => x.NewPageAsync(), Times.Once, "Should only create a new page once");
            MockPage.Verify(x => x.GotoAsync(secondUrlToVisit, It.Is<PageGotoOptions>((options) => options.WaitUntil == WaitUntilState.NetworkIdle)), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("www.microsoft.com")]
        [InlineData("file://c:/test.txt")]
        [InlineData("hi")]
        public async Task GoToUrlThrowsOnInvalidUrlTest(string? url)
        {
            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, browserContext: MockBrowserContext.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await playwrightTestInfraFunctions.GoToUrlAsync(url));
        }

        [Fact]
        public async Task GoToUrlThrowsOnUnsuccessfulResponseTest()
        {
            var urlToVisit = "https://make.powerapps.com";

            MockBrowserContext.Setup(x => x.NewPageAsync()).Returns(Task.FromResult(MockPage.Object));
            MockPage.Setup(x => x.GotoAsync(It.IsAny<string>(), It.IsAny<PageGotoOptions>())).Returns(Task.FromResult<IResponse?>(MockResponse.Object));
            MockResponse.SetupGet(x => x.Ok).Returns(false);
            MockResponse.SetupGet(x => x.Status).Returns(404);
            LoggingTestHelper.SetupMock(MockLogger);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, browserContext: MockBrowserContext.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await playwrightTestInfraFunctions.GoToUrlAsync(urlToVisit));
            LoggingTestHelper.VerifyLogging(MockLogger, (message) => message.Contains(urlToVisit) && message.Contains("404"), LogLevel.Error, Times.Once());
        }

        [Fact]
        public async Task PageFunctionsThrowOnNullPageTest()
        {
            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object, MockFileSystem.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await playwrightTestInfraFunctions.ScreenshotAsync("1.jpg"));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await playwrightTestInfraFunctions.FillAsync("[id=\"i0116\"]", "hello"));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await playwrightTestInfraFunctions.ClickAsync("[id=\"i0116\"]"));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await playwrightTestInfraFunctions.AddScriptTagAsync("script.js", "iframeName"));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await playwrightTestInfraFunctions.RunJavascriptAsync<bool>("console.log(\"hi\")", "iframeName"));
        }

        [Fact]
        public async Task ScreenshotSuccessfulTest()
        {
            var screenshotFilePath = "1.jpg";

            MockPage.Setup(x => x.ScreenshotAsync(It.IsAny<PageScreenshotOptions>())).Returns(Task.FromResult(new byte[] { }));
            MockFileSystem.Setup(x => x.IsValidFilePath(It.IsAny<string>())).Returns(true);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, page: MockPage.Object);
            await playwrightTestInfraFunctions.ScreenshotAsync(screenshotFilePath);

            MockPage.Verify(x => x.ScreenshotAsync(It.Is<PageScreenshotOptions>((options) => options.Path == screenshotFilePath)), Times.Once());
            MockFileSystem.Verify(x => x.IsValidFilePath(screenshotFilePath), Times.Once());
        }

        [Fact]
        public async Task ScreenshotThrowsOnInvalidScreenshotFilePath()
        {
            var screenshotFilePath = "";
            MockPage.Setup(x => x.ScreenshotAsync(It.IsAny<PageScreenshotOptions>())).Returns(Task.FromResult(new byte[] { }));
            MockFileSystem.Setup(x => x.IsValidFilePath(It.IsAny<string>())).Returns(false);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, page: MockPage.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await playwrightTestInfraFunctions.ScreenshotAsync(screenshotFilePath));
            MockFileSystem.Verify(x => x.IsValidFilePath(screenshotFilePath), Times.Once());
            MockPage.Verify(x => x.ScreenshotAsync(It.Is<PageScreenshotOptions>((options) => options.Path == screenshotFilePath)), Times.Never());
        }

        [Fact]
        public async Task FillAsyncSuccessfulTest()
        {
            var selector = "[id =\"i0116\"]";
            var value = "hello";

            MockPage.Setup(x => x.FillAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PageFillOptions?>())).Returns(Task.CompletedTask);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, page: MockPage.Object);
            await playwrightTestInfraFunctions.FillAsync(selector, value);

            MockPage.Verify(x => x.FillAsync(selector, value, null), Times.Once());
        }

        [Fact]
        public async Task ClickAsyncSuccessfulTest()
        {
            var selector = "[id =\"i0116\"]";

            MockPage.Setup(x => x.ClickAsync(It.IsAny<string>(), It.IsAny<PageClickOptions?>())).Returns(Task.CompletedTask);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, page: MockPage.Object);
            await playwrightTestInfraFunctions.ClickAsync(selector);

            MockPage.Verify(x => x.ClickAsync(selector, null), Times.Once());
        }

        [Fact]
        public async Task AddScriptTagSuccessfulTest()
        {
            var scriptTag = "test.js";
            string? frameName = null;

            MockPage.Setup(x => x.AddScriptTagAsync(It.IsAny<PageAddScriptTagOptions>())).Returns(Task.FromResult(MockElementHandle.Object));

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, page: MockPage.Object);
            await playwrightTestInfraFunctions.AddScriptTagAsync(scriptTag, frameName);

            MockPage.Verify(x => x.AddScriptTagAsync(It.Is<PageAddScriptTagOptions>((options) => options.Path == scriptTag)), Times.Once());
        }

        [Fact]
        public async Task AddScriptTagToFrameSuccessfulTest()
        {
            var scriptTag = "test.js";
            var frameName = "publishedAppFrame";

            MockIFrame.Setup(x => x.AddScriptTagAsync(It.IsAny<FrameAddScriptTagOptions>())).Returns(Task.FromResult(MockElementHandle.Object));
            MockPage.Setup(x => x.Frame(It.IsAny<string>())).Returns(MockIFrame.Object);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, page: MockPage.Object);
            await playwrightTestInfraFunctions.AddScriptTagAsync(scriptTag, frameName);

            MockIFrame.Verify(x => x.AddScriptTagAsync(It.Is<FrameAddScriptTagOptions>((options) => options.Path == scriptTag)), Times.Once());
            MockPage.Verify(x => x.Frame(frameName), Times.Once());
        }

        [Fact]
        public async Task RunJavascriptSuccessfulTest()
        {
            var jsExpression = "console.log('hello')";
            string? frameName = null;
            var expectedResponse = "hello";

            MockPage.Setup(x => x.EvaluateAsync<string>(It.IsAny<string>(), It.IsAny<object?>())).Returns(Task.FromResult(expectedResponse));

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, page: MockPage.Object);
            var result = await playwrightTestInfraFunctions.RunJavascriptAsync<string>(jsExpression, frameName);
            Assert.Equal(expectedResponse, result);

            MockPage.Verify(x => x.EvaluateAsync<string>(jsExpression, null), Times.Once());
        }

        [Fact]
        public async Task RunJavascriptInFrameSuccessfulTest()
        {
            var jsExpression = "console.log('hello')";
            var frameName = "publishedAppFrame";
            var expectedResponse = "hello";

            MockIFrame.Setup(x => x.EvaluateAsync<string>(It.IsAny<string>(), It.IsAny<object?>())).Returns(Task.FromResult(expectedResponse));
            MockPage.Setup(x => x.Frame(It.IsAny<string>())).Returns(MockIFrame.Object);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, page: MockPage.Object);
            var result = await playwrightTestInfraFunctions.RunJavascriptAsync<string>(jsExpression, frameName);
            Assert.Equal(expectedResponse, result);

            MockIFrame.Verify(x => x.EvaluateAsync<string>(jsExpression, null), Times.Once());
            MockPage.Verify(x => x.Frame(frameName), Times.Once());
        }
    }
}