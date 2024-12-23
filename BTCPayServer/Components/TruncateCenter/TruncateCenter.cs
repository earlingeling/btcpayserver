using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BTCPayServer.Components.TruncateCenter;

/// <summary>
/// Truncates long strings in the center with ellipsis: Turns e.g. a BOLT11 into "lnbcrt7…q2ns60y"
/// </summary>
/// <param name="text">The full text, e.g. a Bitcoin address or BOLT11</param>
/// <param name="link">Optional link, e.g. a block explorer URL</param>
/// <param name="classes">Optional additional CSS classes</param>
/// <param name="padding">The number of characters to show on each side</param>
/// <param name="copy">Display a copy button</param>
/// <returns>HTML with truncated string</returns>
public class TruncateCenter : ViewComponent
{
    public IViewComponentResult Invoke(string text, string link = null, string classes = null, int padding = 7, bool copy = true, bool elastic = false, bool isVue = false, string id = null)
    {
        if (string.IsNullOrEmpty(text))
            return new HtmlContentViewComponentResult(new StringHtmlContent(string.Empty));

        var vm = new TruncateCenterViewModel
        {
            Classes = classes,
            Padding = padding,
            Elastic = elastic,
            IsVue = isVue,
            Copy = copy,
            Text = text,
            Link = link,
            Id = id,
            IsTruncated = text.Length > 2 * padding && padding > 0
        };
        
        // Update the Start and End properties based on the IsTruncated condition
        if (!vm.IsVue)
        {
            if (vm.IsTruncated && !vm.Elastic)
            {
                // Only set Start and End if truncation is needed
                vm.Start = text[..padding];
                vm.End = text[^padding..];
            }
            else
            {
                // No truncation needed, use the full text
                vm.Start = text;
                vm.End = string.Empty;
            }
        }
        
        return View(vm);
    }
}
