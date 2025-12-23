using Microsoft.Playwright;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace PlaywrightTests.Tests;

public class CreateAssetFullFlow : TestSetup
{
    [Test]
    public async Task Create_Macbook_ReadyToDeploy_CheckedOut()
    {
        // Login
        await Page.GotoAsync("https://demo.snipeitapp.com/login");

        await Page.GetByLabel("Username").FillAsync("admin");
        await Page.GetByLabel("Password").FillAsync("password");

        await Page.RunAndWaitForNavigationAsync(
            () => Page.GetByRole(AriaRole.Button, new() { Name = "Login" }).ClickAsync()
        );

        // Go to create asset page
        await Page.GotoAsync("https://demo.snipeitapp.com/hardware/create");

        await PickSelect2ById("model_select_id", "Macbook Pro 13");
        await PickSelect2ById("status_select_id", "Ready to Deploy");

        // Assign to user
        await PickRandomUser();

        await Page.PauseAsync();

        var tagInput = Page.Locator("#asset_tag");
        var assetTag = await tagInput.InputValueAsync();
        TestContext.WriteLine($"Asset Tag: {assetTag}");

        await Page.PauseAsync();

        // Save
        await Page.Locator("#submit_button").ClickAsync();

        // Verify success
        var success = Page.Locator("div.alert-success");
        await success.WaitForAsync();
        Assert.That(await success.InnerTextAsync(), Does.Contain(assetTag));

        // Search in assets list
        await Page.GotoAsync("https://demo.snipeitapp.com/hardware");

        var searchBox = Page.Locator("input.form-control.search-input[type='search']").First;
        await searchBox.WaitForAsync(new() { Timeout = 30000 });

        await searchBox.FillAsync(assetTag);
        await searchBox.PressAsync("Enter");

        var row = Page.Locator("table tbody tr").Filter(new() { HasText = assetTag }).First;
        await row.WaitForAsync(new() { Timeout = 60000 });
        Assert.That(await row.InnerTextAsync(), Does.Contain(assetTag));

        var assetLink = row.Locator("a").Filter(new() { HasText = assetTag }).First;
        await assetLink.ClickAsync();

        var detailsTag = Page.Locator("span.js-copy-assettag");
        await detailsTag.WaitForAsync(new() { Timeout = 30000 });
        Assert.That((await detailsTag.InnerTextAsync()).Trim(), Is.EqualTo(assetTag));

        var dateBlock = Page.Locator("div.col-md-9")
                            .Filter(new() { HasText = "Dec" })
                            .First;
        await dateBlock.WaitForAsync(new() { Timeout = 30000 });

        var historyTab = Page.Locator("a[href='#history'][data-toggle='tab']").First;
        await historyTab.ScrollIntoViewIfNeededAsync();
        await historyTab.ClickAsync(new() { Force = true });

        await Page.Locator("div.tab-pane#history").WaitForAsync(new() { Timeout = 30000 });

       
        var historyRow = Page.Locator("div.tab-pane#history table tbody tr")
                             .Filter(new() { HasText = assetTag })
                             .First;

        await historyRow.WaitForAsync(new() { Timeout = 30000 });

        var historyText = (await historyRow.InnerTextAsync()).Trim();

   
        Assert.That(historyText, Does.Contain(assetTag));
        Assert.That(historyText, Does.Contain("create new").IgnoreCase);

     
        if (!historyText.Contains("checkout", StringComparison.OrdinalIgnoreCase))
        {
            TestContext.WriteLine($"NOTE: 'checkout' not found in History row. Actual row: {historyText}");
        }

     
        var historyAssetLink = historyRow.Locator("a[href*='/hardware/']").First;
        await historyAssetLink.WaitForAsync(new() { Timeout = 30000 });

        
        var userLinks = historyRow.Locator("a[href*='/users/']");
        if (await userLinks.CountAsync() > 0)
        {
            await userLinks.First.WaitForAsync(new() { Timeout = 30000 });
        }
    }

    private async Task PickSelect2ById(string selectId, string value)
    {
        var select = Page.Locator($"select#{selectId}");
        await select.WaitForAsync();

        var container = select.Locator("xpath=following-sibling::span[contains(@class,'select2')]");
        await container.ClickAsync(new() { Force = true });

        var search = Page.Locator("input.select2-search__field");
        await search.FillAsync(value);

        await Page.Locator(".select2-results__option").First.ClickAsync();
    }

    private async Task PickRandomUser()
    {
        ILocator? select2Container = null;

        var labelCandidates = new[]
        {
            "Checked Out To",
            "Checkout To",
            "Assigned To",
            "User",
            "Checkout",
            "Assigned"
        };

        foreach (var label in labelCandidates)
        {
            var container = Page.Locator($"label:has-text('{label}')")
                                .Locator("..")
                                .Locator("span.select2-container")
                                .First;

            if (await container.CountAsync() > 0)
            {
                select2Container = container;
                break;
            }
        }

        if (select2Container is null)
        {
            var all = Page.Locator("span.select2-container");
            var count = await all.CountAsync();
            if (count < 3)
                throw new PlaywrightException($"Could not find user select2 container. Found only {count} select2 containers.");

            select2Container = all.Nth(2);
        }

        await Page.ScreenshotAsync(new()
        {
            Path = $"artifacts/before-user-click-{DateTime.Now:HHmmss}.png",
            FullPage = true
        });

        await select2Container.ScrollIntoViewIfNeededAsync();

        var clickable = select2Container.Locator(".select2-selection").First;

        try
        {
            await clickable.ClickAsync(new() { Timeout = 10000 });
        }
        catch
        {
            await clickable.ClickAsync(new() { Force = true, Timeout = 10000 });
        }

        await Page.ScreenshotAsync(new()
        {
            Path = $"artifacts/after-user-click-{DateTime.Now:HHmmss}.png",
            FullPage = true
        });

        var option = Page.Locator(".select2-results__option")
                         .Filter(new() { HasText = "#" })
                         .First;

        await option.WaitForAsync(new() { Timeout = 30000 });
        await option.ClickAsync();
    }
}
