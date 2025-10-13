using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces.Services;

namespace TechBlog.Web.TagHelpers
{
    [HtmlTargetElement("recaptcha")]
    public class RecaptchaTagHelper : TagHelper
    {
        private readonly IRecaptchaService _recaptchaService;
        private readonly IOptions<RecaptchaSettings> _recaptchaSettings;

        public string Action { get; set; } = "homepage";
        public string Size { get; set; } = "normal";
        public string Theme { get; set; } = "light";

        public RecaptchaTagHelper(IRecaptchaService recaptchaService, IOptions<RecaptchaSettings> recaptchaSettings)
        {
            _recaptchaService = recaptchaService;
            _recaptchaSettings = recaptchaSettings;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var settings = await _recaptchaService.GetSettingsAsync();
            if (!settings.IsEnabled || string.IsNullOrEmpty(settings.SiteKey))
            {
                output.SuppressOutput();
                return;
            }

            output.TagName = "div";
            output.Attributes.SetAttribute("class", "g-recaptcha");
            output.Attributes.SetAttribute("data-sitekey", settings.SiteKey);
            output.Attributes.SetAttribute("data-size", Size);
            output.Attributes.SetAttribute("data-theme", Theme);
            output.Attributes.SetAttribute("data-action", Action);

            // Add the reCAPTCHA script if not already added
            var script = $@"
                <script src=""https://www.google.com/recaptcha/api.js?render={settings.SiteKey}"" async defer></script>
                <script>
                    function onSubmit(token) {{
                        document.getElementById(""recaptcha-form"").submit();
                    }}
                </script>";

            output.PostElement.AppendHtml(script);
        }
    }
}
